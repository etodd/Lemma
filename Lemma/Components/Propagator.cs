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
		private EffectBlockFactory blockFactory;

		private List<Voxel.Coord> removedPoweredCoords = new List<Voxel.Coord>();

		private float lastShatterSound;

		public override void Awake()
		{
			base.Awake();

			this.blockFactory = Factory.Get<EffectBlockFactory>();

			this.EnabledWhenPaused = false;

			if (main.EditorEnabled)
				this.BlockQueue.Clear();

			this.particles = ParticleSystem.Get(main, "WhiteShatter");

			for (int i = 0; i < maxSparkLights; i++)
			{
				PointLight light = new PointLight();
				light.Serialize = false;
				light.Color.Value = new Vector3(1.0f);
				light.Enabled.Value = false;
				this.Entity.Add(light);
				this.sparkLights.Add(light);
			}

			if (!this.main.EditorEnabled)
			{
				this.Add(new CommandBinding<Voxel, IEnumerable<Voxel.Coord>, Voxel>(Voxel.GlobalCellsFilled, delegate(Voxel map, IEnumerable<Voxel.Coord> coords, Voxel transferredFromMap)
				{
					foreach (Voxel.Coord c in coords)
					{
						Voxel.t id = c.Data.ID;
						if (id == Voxel.t.Blue || id == Voxel.t.Powered || id == Voxel.t.PoweredSwitch || id == Voxel.t.Infected || id == Voxel.t.Neutral || id == Voxel.t.HardPowered || id == Voxel.t.Hard || id == Voxel.t.HardInfected)
						{
							Voxel.Coord newCoord = c;
							newCoord.Data = Voxel.States.Empty;
							int generation;
							EffectBlock.Entry generationsKey = new EffectBlock.Entry { Voxel = map, Coordinate = newCoord };
							if (this.generations.TryGetValue(generationsKey, out generation))
								this.generations.Remove(generationsKey);
							if (!this.isInQueue(map.Entity, newCoord, false))
							{
								this.BlockQueue.Add(new ScheduledBlock
								{
									Voxel = map.Entity,
									Coordinate = newCoord,
									Time = propagateDelay,
									Generation = generation,
								});
							}
						}
					}
				}));

				this.Add(new CommandBinding<Voxel, IEnumerable<Voxel.Coord>, Voxel>(Voxel.GlobalCellsEmptied, delegate(Voxel map, IEnumerable<Voxel.Coord> coords, Voxel transferringToNewMap)
				{
					foreach (Voxel.Coord coord in coords)
					{
						Voxel.t id = coord.Data.ID;
						if (id == Voxel.t.Powered || id == Voxel.t.PoweredSwitch || id == Voxel.t.HardPowered || id == Voxel.t.PermanentPowered)
							this.removedPoweredCoords.Add(coord);
						if (transferringToNewMap != null)
							continue;

						if (id == Voxel.t.Critical) // Critical. Explodes when destroyed.
							Explosion.Explode(main, map, coord);
						else if (id == Voxel.t.HardInfected) // Infected. Shatter effects.
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
						else if (id == Voxel.t.Powered || id == Voxel.t.Blue || id == Voxel.t.Neutral || id == Voxel.t.Infected || id == Voxel.t.Floater)
						{
							int generation;
							Voxel.Coord c = coord;
							c.Data = Voxel.States.Empty;
							EffectBlock.Entry generationKey = new EffectBlock.Entry { Voxel = map, Coordinate = c };
							if (this.generations.TryGetValue(generationKey, out generation))
								this.generations.Remove(generationKey);

							if (id == Voxel.t.Floater)
							{
								Entity blockEntity = this.blockFactory.CreateAndBind(main);
								EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
								coord.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
								effectBlock.Delay = 4.0f;
								effectBlock.Offset.Value = map.GetRelativePosition(coord);
								effectBlock.StartPosition = map.GetAbsolutePosition(coord) + new Vector3(2.5f, 5.0f, 2.5f);
								effectBlock.StartOrientation = Quaternion.CreateFromYawPitchRoll(1.0f, 1.0f, 0);
								effectBlock.TotalLifetime = 0.5f;
								effectBlock.Setup(map.Entity, coord, coord.Data.ID);
								main.Add(blockEntity);
							}

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
								for (int i = 0; i < 6; i++)
								{
									Direction dir = DirectionExtensions.Directions[i];
									Voxel.Coord adjacent = coord.Move(dir);
									if (!coords.Contains(adjacent))
									{
										Voxel.t adjacentID = map[adjacent].ID;
										bool adjacentIsFloater = adjacentID == Voxel.t.Floater;
										if (dir != down || adjacentIsFloater)
										{
											if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.Blue || adjacentID == Voxel.t.Neutral || adjacentID == Voxel.t.Infected || adjacentIsFloater)
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
						else if (id == Voxel.t.White || id == Voxel.t.Glass) // Shatter effects.
						{
							ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
							Vector3 pos = map.GetAbsolutePosition(coord);
							for (int i = 0; i < 50; i++)
							{
								Vector3 offset = new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f);
								shatter.AddParticle(pos + offset, offset);
							}
							float time = this.main.TotalTime;
							if (time - this.lastShatterSound > 0.3f)
							{
								this.lastShatterSound = time;
								AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WHITE_SHATTER, pos);
							}
						}
					}

					if (this.removedPoweredCoords.Count > 0)
					{
						IEnumerable<IEnumerable<Voxel.Box>> poweredIslands = map.GetAdjacentIslands(this.removedPoweredCoords, x => x.ID == Voxel.t.Powered || x.ID == Voxel.t.HardPowered, x => x == Voxel.States.PermanentPowered || x == Voxel.States.PoweredSwitch);
						List<Voxel.Coord> poweredCoords = poweredIslands.SelectMany(x => x).SelectMany(x => x.GetCoords()).ToList();
						if (poweredCoords.Count > 0)
						{
							lock (map.MutationLock)
							{
								map.Empty(poweredCoords, true, true, map, false);
								for (int i = 0; i < poweredCoords.Count; i++)
								{
									Voxel.Coord coord = poweredCoords[i];
									if (coord.Data.ID == Voxel.t.HardPowered)
										map.Fill(coord, Voxel.States.Hard, false);
									else
										map.Fill(coord, Voxel.States.Blue, false);
								}
							}
							this.toRegenerate.Add(map);
						}
						this.removedPoweredCoords.Clear();
					}
				}));
			}
		}

		private bool isInQueue(Entity m, Voxel.Coord c, bool removing)
		{
			for (int i = 0; i < this.BlockQueue.Count; i++)
			{
				ScheduledBlock b = this.BlockQueue[i];
				if (b.Removing == removing && m == b.Voxel.Target && b.Coordinate.Equivalent(c))
					return true;
			}
			return false;
		}

		private List<Voxel> toRegenerate = new List<Voxel>();
		public void Update(float dt)
		{
			float sparkLightFade = sparkLightBrightness * dt / sparkLightFadeTime;
			for (int i = 0; i < activeSparkLights; i++)
			{
				PointLight light = this.sparkLights[i];
				float a = light.Color.Value.X - sparkLightFade;
				if (a < 0.0f)
				{
					light.Enabled.Value = false;
					PointLight swap = this.sparkLights[activeSparkLights - 1];
					this.sparkLights[i] = swap;
					this.sparkLights[activeSparkLights - 1] = light;
					activeSparkLights--;
					oldestSparkLight = activeSparkLights;
				}
				else
					light.Color.Value = new Vector3(a);
			}

			for (int i = 0; i < this.BlockQueue.Length; i++)
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

						bool regenerate = false;

						if (entry.Removing)
						{
							if (entry.Generation == 0 && id == 0)
							{
								Direction down = map.GetRelativeDirection(Direction.NegativeY);
								for (int j = 0; j < 6; j++)
								{
									Direction dir = DirectionExtensions.Directions[j];
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = map[adjacent].ID;
									bool adjacentIsFloater = adjacentID == Voxel.t.Floater;
									if (dir != down || adjacentIsFloater)
									{
										if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.Blue || adjacentID == Voxel.t.Neutral || adjacentID == Voxel.t.Infected || adjacentIsFloater)
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
							else if (entry.Generation > 0 && (id == Voxel.t.Blue || id == Voxel.t.Infected || id == Voxel.t.Powered || id == Voxel.t.PermanentPowered || id == Voxel.t.HardPowered || id == Voxel.t.PoweredSwitch || id == Voxel.t.Neutral || id == Voxel.t.Floater))
							{
								this.generations[new EffectBlock.Entry { Voxel = map, Coordinate = c }] = entry.Generation;
								map.Empty(c);
								this.SparksLowPriority(map.GetAbsolutePosition(c), Spark.Burn);
								regenerate = true;
							}
						}
						else if (id == Voxel.t.Blue)
						{
							for (int j = 0; j < 6; j++)
							{
								Direction dir = DirectionExtensions.Directions[j];
								Voxel.Coord adjacent = c.Move(dir);
								Voxel.t adjacentID = map[adjacent].ID;

								if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.PermanentPowered || adjacentID == Voxel.t.HardPowered || adjacentID == Voxel.t.PoweredSwitch)
								{
									map.Empty(c, false, true, map);
									map.Fill(c, Voxel.States.Powered);
									this.SparksLowPriority(map.GetAbsolutePosition(c), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Neutral && entry.Generation < maxGenerations)
								{
									map.Empty(adjacent, false, true, map);
									this.generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
									map.Fill(adjacent, Voxel.States.Blue);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
							}
						}
						else if (id == Voxel.t.Neutral)
						{
							for (int j = 0; j < 6; j++)
							{
								Direction dir = DirectionExtensions.Directions[j];
								Voxel.Coord adjacent = c.Move(dir);
								Voxel.t adjacentID = map[adjacent].ID;
								if (adjacentID == Voxel.t.Infected || adjacentID == Voxel.t.Blue || adjacentID == Voxel.t.Powered)
								{
									map.Empty(adjacent, false, true, map);
									map.Fill(adjacent, Voxel.States.Neutral);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.HardInfected)
								{
									map.Empty(adjacent, false, true, map);
									map.Fill(adjacent, Voxel.States.Hard);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
							}
						}
						else if (id == Voxel.t.Hard)
						{
							for (int j = 0; j < 6; j++)
							{
								Direction dir = DirectionExtensions.Directions[j];
								Voxel.Coord adjacent = c.Move(dir);
								Voxel.t adjacentID = map[adjacent].ID;
								if (adjacentID == Voxel.t.HardInfected)
								{
									map.Empty(adjacent, false, true, map);
									map.Fill(adjacent, Voxel.States.Hard);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
							}
						}
						else if (id == Voxel.t.Powered || id == Voxel.t.PermanentPowered || id == Voxel.t.HardPowered || id == Voxel.t.PoweredSwitch)
						{
							for (int j = 0; j < 6; j++)
							{
								Direction dir = DirectionExtensions.Directions[j];
								Voxel.Coord adjacent = c.Move(dir);
								Voxel.t adjacentID = map[adjacent].ID;

								if (id == Voxel.t.Powered && adjacentID == Voxel.t.Neutral && entry.Generation < maxGenerations)
								{
									map.Empty(adjacent, false, true, map);
									this.generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
									map.Fill(adjacent, Voxel.States.Powered);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Blue)
								{
									map.Empty(adjacent, false, true, map);
									map.Fill(adjacent, Voxel.States.Powered);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Switch)
								{
									map.Empty(adjacent, true, true, map);
									map.Fill(adjacent, Voxel.States.PoweredSwitch);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Hard)
								{
									map.Empty(adjacent, true, true, map);
									map.Fill(adjacent, Voxel.States.HardPowered);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Normal);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Critical)
								{
									map.Empty(adjacent);
									regenerate = true;
								}
							}
						}
						else if (id == Voxel.t.Infected || id == Voxel.t.HardInfected)
						{
							for (int j = 0; j < 6; j++)
							{
								Direction dir = DirectionExtensions.Directions[j];
								Voxel.Coord adjacent = c.Move(dir);
								Voxel.t adjacentID = map[adjacent].ID;
								if (adjacentID == Voxel.t.Neutral && entry.Generation < maxGenerations)
								{
									map.Empty(adjacent, false, true, map);
									this.generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
									map.Fill(adjacent, Voxel.States.Infected);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Dangerous);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Hard && entry.Generation < maxGenerations)
								{
									map.Empty(adjacent, false, true, map);
									this.generations[new EffectBlock.Entry { Voxel = map, Coordinate = adjacent }] = entry.Generation + 1;
									map.Fill(adjacent, Voxel.States.HardInfected);
									this.SparksLowPriority(map.GetAbsolutePosition(adjacent), Spark.Dangerous);
									regenerate = true;
								}
								else if (adjacentID == Voxel.t.Critical)
								{
									map.Empty(adjacent);
									regenerate = true;
								}
							}
						}

						if (regenerate)
							this.toRegenerate.Add(map);
					}
				}
			}
			for (int i = 0; i < this.toRegenerate.Count; i++)
				this.toRegenerate[i].Regenerate();
			this.toRegenerate.Clear();
		}

		public void SparksLowPriority(Vector3 pos, Spark type)
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