using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Components;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	[XmlInclude(typeof(ScheduledBlock))]
	[XmlInclude(typeof(ListProperty<ScheduledBlock>))]
	public class Propagator : Component<Main>, IUpdateableComponent
	{
		public enum Spark
		{
			Normal, Dangerous, Burn, Expander
		}

		public class ScheduledBlock
		{
			public Entity.Handle Map;
			public Map.Coordinate Coordinate;
			public float Time;
			[System.ComponentModel.DefaultValue(0)]
			public int Generation;
			[System.ComponentModel.DefaultValue(false)]
			public bool Removing;
		}

		private const float sparkLightFadeTime = 0.5f;
		private const float sparkLightBrightness = 2.0f;
		private const int maxSparkLights = 10;
		private const float propagateDelay = 0.07f;
		private const int maxGenerations = 4;

		public ListProperty<ScheduledBlock> BlockQueue = new ListProperty<ScheduledBlock>();
		private Random random = new Random();
		private List<PointLight> sparkLights = new List<PointLight>();
		private int activeSparkLights = 0;
		private int oldestSparkLight = 0;
		private ParticleSystem particles;
		private Dictionary<EffectBlockFactory.BlockEntry, int> generations = new Dictionary<EffectBlockFactory.BlockEntry, int>();
		private Map.CellState neutral;
		private Map.CellState powered;
		private Map.CellState temporary;
		private Map.CellState infected;
		private Map.CellState poweredSwitch;
		private Map.CellState permanentPowered;
		private Map.CellState switchState;

		public override void Awake()
		{
			base.Awake();

			this.EnabledWhenPaused = false;

			if (main.EditorEnabled)
				this.BlockQueue.Clear();

			this.neutral = Map.States[Map.t.Neutral];
			this.powered = Map.States[Map.t.Powered];
			this.temporary = Map.States[Map.t.Temporary];
			this.infected = Map.States[Map.t.Infected];
			this.poweredSwitch = Map.States[Map.t.PoweredSwitch];
			this.permanentPowered = Map.States[Map.t.PermanentPowered];
			this.switchState = Map.States[Map.t.Switch];

			this.particles = ParticleSystem.Get(main, "WhiteShatter");

			for (int i = 0; i < maxSparkLights; i++)
			{
				PointLight light = new PointLight();
				light.Serialize = false;
				light.Color.Value = new Vector3(1.0f);
				light.Enabled.Value = false;
				this.Entity.Add(light);
				sparkLights.Add(light);
			}

			this.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsFilled, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferredFromMap)
			{
				if (!main.EditorEnabled)
				{
					foreach (Map.Coordinate c in coords)
					{
						Map.t id = c.Data.ID;
						if (id == Map.t.Temporary || id == Map.t.Powered || id == Map.t.PoweredSwitch || id == Map.t.Infected || id == Map.t.Neutral)
						{
							Map.Coordinate newCoord = c;
							newCoord.Data = Map.EmptyState;
							int generation;
							EffectBlockFactory.BlockEntry generationsKey = new EffectBlockFactory.BlockEntry { Map = map, Coordinate = newCoord };
							if (generations.TryGetValue(generationsKey, out generation))
								generations.Remove(generationsKey);
							this.BlockQueue.Add(new ScheduledBlock
							{
								Map = map.Entity,
								Coordinate = newCoord,
								Time = propagateDelay,
								Generation = generation,
							});
						}
					}
				}
			}));

			this.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsEmptied, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferringToNewMap)
			{
				if (transferringToNewMap != null || main.EditorEnabled)
					return;
				
				bool handlePowered = false;
				foreach (Map.Coordinate coord in coords)
				{
					Map.t id = coord.Data.ID;
					if (id == Map.t.Powered || id == Map.t.PoweredSwitch)
						handlePowered = true;

					if (id == Map.t.Critical) // Critical. Explodes when destroyed.
						Explosion.Explode(main, map, coord);
					else if (id == Map.t.InfectedCritical) // Infected. Shatter effects.
					{
						ParticleSystem shatter = ParticleSystem.Get(main, "InfectedShatter");
						Vector3 pos = map.GetAbsolutePosition(coord);
						AkSoundEngine.PostEvent("Play_infected_shatter", pos);
						for (int i = 0; i < 50; i++)
						{
							Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
							shatter.AddParticle(pos + offset, offset);
						}
					}
					else if (id == Map.t.Powered || id == Map.t.Temporary || id == Map.t.Neutral || id == Map.t.Infected || id == Map.t.Floater)
					{
						int generation;
						Map.Coordinate c = coord;
						c.Data = Map.EmptyState;
						EffectBlockFactory.BlockEntry generationKey = new EffectBlockFactory.BlockEntry { Map = map, Coordinate = c };
						if (generations.TryGetValue(generationKey, out generation))
							generations.Remove(generationKey);

						if (generation == 0)
						{
							if (!isInQueue(map.Entity, coord, true))
							{
								this.BlockQueue.Add(new ScheduledBlock
								{
									Map = map.Entity,
									Coordinate = coord,
									Time = propagateDelay,
									Removing = true,
								});
							}
						}
						else if (generation < maxGenerations)
						{
							Direction down = map.GetRelativeDirection(Direction.NegativeY);
							foreach (Direction dir in DirectionExtensions.Directions)
							{
								Map.Coordinate adjacent = coord.Move(dir);
								if (!coords.Contains(adjacent))
								{
									Map.t adjacentID = map[adjacent].ID;
									bool adjacentIsFloater = adjacentID == Map.t.Floater;
									if (dir != down || adjacentIsFloater)
									{
										if (adjacentID == Map.t.Powered || adjacentID == Map.t.Temporary || adjacentID == Map.t.Neutral || adjacentID == Map.t.Infected || adjacentIsFloater)
										{
											if (!isInQueue(map.Entity, adjacent, true))
											{
												this.BlockQueue.Add(new ScheduledBlock
												{
													Map = map.Entity,
													Coordinate = adjacent,
													Time = propagateDelay,
													Removing = true,
													Generation = generation + 1,
												});
											}
										}
									}
								}
							}
						}
					}
					else if (id == Map.t.White) // White. Shatter effects.
					{
						ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
						Vector3 pos = map.GetAbsolutePosition(coord);
						AkSoundEngine.PostEvent("Play_white_shatter", pos);
						for (int i = 0; i < 50; i++)
						{
							Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
							shatter.AddParticle(pos + offset, offset);
						}
					}
				}

				if (handlePowered)
				{
					IEnumerable<IEnumerable<Map.Box>> poweredIslands = map.GetAdjacentIslands(coords.Where(x => x.Data.ID == Map.t.Powered), x => x.ID == Map.t.Powered || x.ID == Map.t.PoweredSwitch, permanentPowered);
					List<Map.Coordinate> poweredCoords = poweredIslands.SelectMany(x => x).SelectMany(x => x.GetCoords()).ToList();
					if (poweredCoords.Count > 0)
					{
						map.Empty(poweredCoords, true, true, null, false);
						foreach (Map.Coordinate coord in poweredCoords)
							map.Fill(coord, coord.Data.ID == Map.t.PoweredSwitch ? switchState : temporary);
						map.Regenerate();
					}
				}
			}));
		}

		private bool isInQueue(Entity m, Map.Coordinate c, bool removing)
		{
			foreach (ScheduledBlock b in this.BlockQueue)
			{
				if (b.Removing == removing && m == b.Map.Target && b.Coordinate.Equivalent(c))
					return true;
			}
			return false;
		}

		public void Update(float dt)
		{
			float sparkLightFade = sparkLightBrightness * dt / sparkLightFadeTime;
			for (int i = 0; i < activeSparkLights; i++)
			{
				PointLight light = sparkLights[i];
				float a = light.Color.Value.X - sparkLightFade;
				if (a < 0.0f)
				{
					light.Enabled.Value = false;
					PointLight swap = sparkLights[activeSparkLights - 1];
					sparkLights[i] = swap;
					sparkLights[activeSparkLights - 1] = light;
					activeSparkLights--;
					oldestSparkLight = activeSparkLights;
				}
				else
					light.Color.Value = new Vector3(a);
			}

			for (int i = 0; i < this.BlockQueue.Count; i++)
			{
				ScheduledBlock entry = this.BlockQueue[i];
				entry.Time -= dt;
				if (entry.Time < 0.0f)
				{
					this.BlockQueue.RemoveAt(i);
					i--;

					Entity mapEntity = entry.Map.Target;
					if (mapEntity != null && mapEntity.Active)
					{
						Map map = mapEntity.Get<Map>();
						Map.Coordinate c = entry.Coordinate;
						Map.t id = map[c].ID;

						bool isTemporary = id == Map.t.Temporary;
						bool isNeutral = id == Map.t.Neutral;
						bool isInfected = id == Map.t.Infected || id == Map.t.InfectedCritical;
						bool isPowered = id == Map.t.Powered || id == Map.t.PermanentPowered || id == Map.t.HardPowered || id == Map.t.PoweredSwitch;

						bool regenerate = false;

						if (entry.Removing)
						{
							if (entry.Generation == 0 && id == 0)
							{
								Direction down = map.GetRelativeDirection(Direction.NegativeY);
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									Map.t adjacentID = map[adjacent].ID;
									bool adjacentIsFloater = adjacentID == Map.t.Floater;
									if (dir != down || adjacentIsFloater)
									{
										if (adjacentID == Map.t.Powered || adjacentID == Map.t.Temporary || adjacentID == Map.t.Neutral || adjacentID == Map.t.Infected || adjacentIsFloater)
										{
											if (!this.isInQueue(map.Entity, adjacent, true))
											{
												this.BlockQueue.Add(new ScheduledBlock
												{
													Map = map.Entity,
													Coordinate = adjacent,
													Time = propagateDelay,
													Removing = true,
													Generation = 1,
												});
											}
										}
									}
								}
							}
							else if (entry.Generation > 0 && (isTemporary || isInfected || isPowered || id == Map.t.Neutral || id == Map.t.Floater))
							{
								generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = c }] = entry.Generation;
								map.Empty(c);
								this.Sparks(map.GetAbsolutePosition(c), Spark.Burn);
								regenerate = true;
							}
						}
						else if (isTemporary
							|| isInfected
							|| isPowered
							|| isNeutral)
						{
							if (isTemporary)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									Map.t adjacentID = map[adjacent].ID;

									if (adjacentID == Map.t.Powered || adjacentID == Map.t.PermanentPowered || adjacentID == Map.t.HardPowered || adjacentID == Map.t.PoweredSwitch)
									{
										map.Empty(c);
										map.Fill(c, powered);
										this.Sparks(map.GetAbsolutePosition(c), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Map.t.Neutral && entry.Generation < maxGenerations)
									{
										map.Empty(adjacent);
										generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = adjacent }] = entry.Generation + 1;
										map.Fill(adjacent, temporary);
										this.Sparks(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
								}
							}
							else if (isNeutral)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									Map.t adjacentID = map[adjacent].ID;
									if (adjacentID == Map.t.Infected || adjacentID == Map.t.Temporary)
									{
										map.Empty(adjacent);
										map.Fill(adjacent, neutral);
										this.Sparks(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
								}
							}
							else if (isPowered)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									Map.t adjacentID = map[adjacent].ID;

									if (adjacentID == Map.t.Temporary)
									{
										map.Empty(adjacent);
										map.Fill(adjacent, powered);
										this.Sparks(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Map.t.Switch)
									{
										map.Empty(adjacent, true);
										map.Fill(adjacent, poweredSwitch);
										this.Sparks(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Map.t.Critical)
									{
										map.Empty(adjacent);
										regenerate = true;
									}
								}
							}
							else if (isInfected)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									Map.t adjacentID = map[adjacent].ID;
									if (adjacentID == Map.t.Neutral && entry.Generation < maxGenerations)
									{
										map.Empty(adjacent);
										generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = adjacent }] = entry.Generation + 1;
										map.Fill(adjacent, infected);
										this.Sparks(map.GetAbsolutePosition(adjacent), Spark.Dangerous);
										regenerate = true;
									}
									else if (adjacentID == Map.t.Critical)
									{
										map.Empty(adjacent);
										regenerate = true;
									}
								}
							}
						}

						if (regenerate)
							map.Regenerate();
					}
				}
				i++;
			}
		}

		public void Sparks(Vector3 pos, Spark type)
		{
			for (int j = 0; j < 40; j++)
			{
				Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
				this.particles.AddParticle(pos + offset, offset);
			}

			if (this.random.Next(0, 2) == 0)
			{
				PointLight light;
				if (this.activeSparkLights < Propagator.maxSparkLights)
				{
					light = this.sparkLights[this.activeSparkLights];
					light.Enabled.Value = true;
					this.activeSparkLights++;
				}
				else
				{
					light = this.sparkLights[this.oldestSparkLight % this.activeSparkLights];
					this.oldestSparkLight = (this.oldestSparkLight + 1) % Propagator.maxSparkLights;
				}

				light.Color.Value = Vector3.One;
				light.Position.Value = pos;

				light.Attenuation.Value = type == Spark.Expander ? 10.0f : 5.0f;

				AkSoundEngine.PostEvent(type == Spark.Dangerous ? AK.EVENTS.PLAY_RED_BURN : AK.EVENTS.PLAY_ORANGE_BURN, pos);
			}
		}
	}
}
