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
			public Entity.Handle Voxel;
			public Voxel.Coord Coordinate;
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
		private Dictionary<EffectBlock.Entry, int> generations = new Dictionary<EffectBlock.Entry, int>();
		private Voxel.State neutral;
		private Voxel.State powered;
		private Voxel.State temporary;
		private Voxel.State infected;
		private Voxel.State poweredSwitch;
		private Voxel.State permanentPowered;
		private Voxel.State switchState;

		public override void Awake()
		{
			base.Awake();

			this.EnabledWhenPaused = false;

			if (main.EditorEnabled)
				this.BlockQueue.Clear();

			this.neutral = Voxel.States[Voxel.t.Neutral];
			this.powered = Voxel.States[Voxel.t.Powered];
			this.temporary = Voxel.States[Voxel.t.Temporary];
			this.infected = Voxel.States[Voxel.t.Infected];
			this.poweredSwitch = Voxel.States[Voxel.t.PoweredSwitch];
			this.permanentPowered = Voxel.States[Voxel.t.PermanentPowered];
			this.switchState = Voxel.States[Voxel.t.Switch];

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

			if (!this.main.EditorEnabled)
			{
				this.Add(new CommandBinding<Voxel, IEnumerable<Voxel.Coord>, Voxel>(Voxel.GlobalCellsFilled, delegate(Voxel map, IEnumerable<Voxel.Coord> coords, Voxel transferredFromMap)
				{
					foreach (Voxel.Coord c in coords)
					{
						Voxel.t id = c.Data.ID;
						if (id == Voxel.t.Temporary || id == Voxel.t.Powered || id == Voxel.t.PoweredSwitch || id == Voxel.t.Infected || id == Voxel.t.Neutral)
						{
							Voxel.Coord newCoord = c;
							newCoord.Data = Voxel.EmptyState;
							int generation;
							EffectBlock.Entry generationsKey = new EffectBlock.Entry { Voxel = map, Coordinate = newCoord };
							if (generations.TryGetValue(generationsKey, out generation))
								generations.Remove(generationsKey);
							this.BlockQueue.Add(new ScheduledBlock
							{
								Voxel = map.Entity,
								Coordinate = newCoord,
								Time = propagateDelay,
								Generation = generation,
							});
						}
					}
				}));

				this.Add(new CommandBinding<Voxel, IEnumerable<Voxel.Coord>, Voxel>(Voxel.GlobalCellsEmptied, delegate(Voxel map, IEnumerable<Voxel.Coord> coords, Voxel transferringToNewMap)
				{
					if (transferringToNewMap != null)
						return;
					
					bool handlePowered = false;
					foreach (Voxel.Coord coord in coords)
					{
						Voxel.t id = coord.Data.ID;
						if (id == Voxel.t.Powered || id == Voxel.t.PoweredSwitch)
							handlePowered = true;

						if (id == Voxel.t.Critical) // Critical. Explodes when destroyed.
							Explosion.Explode(main, map, coord);
						else if (id == Voxel.t.InfectedCritical) // Infected. Shatter effects.
						{
							ParticleSystem shatter = ParticleSystem.Get(main, "InfectedShatter");
							Vector3 pos = map.GetAbsolutePosition(coord);
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_INFECTED_CRITICAL_SHATTER, pos);
							for (int i = 0; i < 50; i++)
							{
								Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
								shatter.AddParticle(pos + offset, offset);
							}
						}
						else if (id == Voxel.t.Powered || id == Voxel.t.Temporary || id == Voxel.t.Neutral || id == Voxel.t.Infected || id == Voxel.t.Floater)
						{
							int generation;
							Voxel.Coord c = coord;
							c.Data = Voxel.EmptyState;
							EffectBlock.Entry generationKey = new EffectBlock.Entry { Voxel = map, Coordinate = c };
							if (generations.TryGetValue(generationKey, out generation))
								generations.Remove(generationKey);

							if (generation == 0)
							{
								if (!this.isInQueue(map.Entity, coord, true))
								{
									this.BlockQueue.Add(new ScheduledBlock
									{
										Voxel = map.Entity,
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
									Voxel.Coord adjacent = coord.Move(dir);
									if (!coords.Contains(adjacent))
									{
										Voxel.t adjacentID = map[adjacent].ID;
										bool adjacentIsFloater = adjacentID == Voxel.t.Floater;
										if (dir != down || adjacentIsFloater)
										{
											if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.Temporary || adjacentID == Voxel.t.Neutral || adjacentID == Voxel.t.Infected || adjacentIsFloater)
											{
												if (!this.isInQueue(map.Entity, adjacent, true))
												{
													this.BlockQueue.Add(new ScheduledBlock
													{
														Voxel = map.Entity,
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
						else if (id == Voxel.t.White || id == Voxel.t.WhitePermanent) // White. Shatter effects.
						{
							ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
							Vector3 pos = map.GetAbsolutePosition(coord);
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WHITE_SHATTER, pos);
							for (int i = 0; i < 50; i++)
							{
								Vector3 offset = new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f);
								shatter.AddParticle(pos + offset, offset);
							}
						}
					}

					if (handlePowered)
					{
						IEnumerable<IEnumerable<Voxel.Box>> poweredIslands = map.GetAdjacentIslands(coords.Where(x => x.Data.ID == Voxel.t.Powered), x => x.ID == Voxel.t.Powered || x.ID == Voxel.t.PoweredSwitch, permanentPowered);
						List<Voxel.Coord> poweredCoords = poweredIslands.SelectMany(x => x).SelectMany(x => x.GetCoords()).ToList();
						if (poweredCoords.Count > 0)
						{
							map.Empty(poweredCoords, true, true, map, false);
							foreach (Voxel.Coord coord in poweredCoords)
								map.Fill(coord, coord.Data.ID == Voxel.t.PoweredSwitch ? switchState : temporary);
							map.Regenerate();
						}
					}
				}));
			}
		}

		private bool isInQueue(Entity m, Voxel.Coord c, bool removing)
		{
			foreach (ScheduledBlock b in this.BlockQueue)
			{
				if (b.Removing == removing && m == b.Voxel.Target && b.Coordinate.Equivalent(c))
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

					Entity mapEntity = entry.Voxel.Target;
					if (mapEntity != null && mapEntity.Active)
					{
						Voxel map = mapEntity.Get<Voxel>();
						Voxel.Coord c = entry.Coordinate;
						Voxel.t id = map[c].ID;

						bool isTemporary = id == Voxel.t.Temporary;
						bool isNeutral = id == Voxel.t.Neutral;
						bool isInfected = id == Voxel.t.Infected || id == Voxel.t.InfectedCritical;
						bool isPowered = id == Voxel.t.Powered || id == Voxel.t.PermanentPowered || id == Voxel.t.HardPowered || id == Voxel.t.PoweredSwitch;

						bool regenerate = false;

						if (entry.Removing)
						{
							if (entry.Generation == 0 && id == 0)
							{
								Direction down = map.GetRelativeDirection(Direction.NegativeY);
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;
									bool adjacentIsFloater = adjacentID == Voxel.t.Floater;
									if (dir != down || adjacentIsFloater)
									{
										if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.Temporary || adjacentID == Voxel.t.Neutral || adjacentID == Voxel.t.Infected || adjacentIsFloater)
										{
											if (!this.isInQueue(map.Entity, adjacent, true))
											{
												this.BlockQueue.Add(new ScheduledBlock
												{
													Voxel = map.Entity,
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
							else if (entry.Generation > 0 && (isTemporary || isInfected || isPowered || id == Voxel.t.Neutral || id == Voxel.t.Floater))
							{
								generations[new EffectBlock.Entry { Voxel = map, Coordinate = c }] = entry.Generation;
								map.Empty(c);
								this.sparksLowPriority(map.GetAbsolutePosition(c), Spark.Burn);
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
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;

									if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.PermanentPowered || adjacentID == Voxel.t.HardPowered || adjacentID == Voxel.t.PoweredSwitch)
									{
										map.Empty(c, false, true, map);
										map.Fill(c, powered);
										this.sparksLowPriority(map.GetAbsolutePosition(c), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Voxel.t.Neutral && entry.Generation < maxGenerations)
									{
										map.Empty(adjacent, false, true, map);
										generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
										map.Fill(adjacent, temporary);
										this.sparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
								}
							}
							else if (isNeutral)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;
									if (adjacentID == Voxel.t.Infected || adjacentID == Voxel.t.Temporary)
									{
										map.Empty(adjacent, false, true, map);
										map.Fill(adjacent, neutral);
										this.sparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
								}
							}
							else if (isPowered)
							{
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;

									if (adjacentID == Voxel.t.Temporary)
									{
										map.Empty(adjacent, false, true, map);
										map.Fill(adjacent, powered);
										this.sparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Voxel.t.Switch)
									{
										map.Empty(adjacent, true, true, map);
										map.Fill(adjacent, poweredSwitch);
										this.sparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
										regenerate = true;
									}
									else if (adjacentID == Voxel.t.Critical)
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
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;
									if (adjacentID == Voxel.t.Neutral && entry.Generation < maxGenerations)
									{
										map.Empty(adjacent, false, true, map);
										generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
										map.Fill(adjacent, infected);
										this.sparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Dangerous);
										regenerate = true;
									}
									else if (adjacentID == Voxel.t.Critical)
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

		public void sparksLowPriority(Vector3 pos, Spark type)
		{
			this.sparks(pos, type, this.random.Next(0, 2) == 0, this.random.Next(0, 2) == 0);
		}

		public void Sparks(Vector3 pos, Spark type)
		{
			this.sparks(pos, type, true, true);
		}

		private void sparks(Vector3 pos, Spark type, bool showLight, bool playSound)
		{
			for (int j = 0; j < 40; j++)
			{
				Vector3 offset = new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f);
				this.particles.AddParticle(pos + offset, offset);
			}

			if (showLight)
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

				if (playSound)
				{
					uint sound;
					switch (type)
					{
						case Spark.Dangerous:
							sound = AK.EVENTS.PLAY_RED_BURN;
							break;
						case Spark.Burn:
							sound = AK.EVENTS.PLAY_ORANGE_BURN;
							break;
						default:
							sound = AK.EVENTS.PLAY_BLUE_BURN;
							break;
					}
					AkSoundEngine.PostEvent(sound, pos);
				}
			}
		}
	}
}
