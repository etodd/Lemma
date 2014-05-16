using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class WorldFactory : Factory<Main>
	{
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

		private Random random = new Random();

		private static readonly Color defaultBackgroundColor = new Color(16.0f / 255.0f, 26.0f / 255.0f, 38.0f / 255.0f, 1.0f);

		private static Entity instance;

		public WorldFactory()
		{
			this.Color = new Vector3(0.1f, 0.1f, 0.1f);
			this.EditorCanSpawn = false;
		}

		public static Entity Instance
		{
			get
			{
				if (WorldFactory.instance == null)
					return null;

				if (!WorldFactory.instance.Active)
					WorldFactory.instance = null;

				return WorldFactory.instance;
			}
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "World");
			result.Add("Transform", new Transform());

			result.Add("LightRampTexture", new Property<string> { Editable = true, Value = "Images\\default-ramp" });
			result.Add("BackgroundColor", new Property<Color> { Editable = true, Value = WorldFactory.defaultBackgroundColor });
			result.Add("FarPlaneDistance", new Property<float> { Editable = true, Value = 100.0f });

			ListProperty<Map.CellState> additionalMaterials = new ListProperty<Map.CellState>();

			result.Add("AdditionalMaterials", additionalMaterials);

			result.Add("ReverbAmount", new Property<float> { Value = 0.0f, Editable = true });

			result.Add("ReverbSize", new Property<float> { Value = 0.0f, Editable = true });

			return result;
		}

		private static bool boxesContain(IEnumerable<NonAxisAlignedBoundingBox> boxes, Vector3 position)
		{
			foreach (NonAxisAlignedBoundingBox box in boxes)
			{
				if (box.BoundingBox.Contains(Vector3.Transform(position, box.Transform)) != ContainmentType.Disjoint)
					return true;
			}
			return false;
		}

		private static void processMap(Map map, IEnumerable<NonAxisAlignedBoundingBox> boxes)
		{
			foreach (Map.Chunk chunk in map.Chunks)
			{
				BoundingBox absoluteChunkBoundingBox = chunk.RelativeBoundingBox.Transform(map.Transform);
				bool active = false;
				foreach (NonAxisAlignedBoundingBox box in boxes)
				{
					if (box.BoundingBox.Intersects(absoluteChunkBoundingBox.Transform(box.Transform)))
					{
						active = true;
						break;
					}
				}
				if (chunk.Active && !active)
					chunk.Deactivate();
				else if (!chunk.Active && active)
					chunk.Activate();
			}
		}

		private static void processEntity(Entity entity, Zone currentZone, IEnumerable<NonAxisAlignedBoundingBox> boxes, Vector3 cameraPosition, float suspendDistance)
		{
			Map map = entity.Get<Map>();
			if (map != null && !typeof(DynamicMap).IsAssignableFrom(map.GetType()))
				processMap(map, boxes);
			else
			{
				Transform transform = entity.Get<Transform>();
				bool hasPosition = transform != null;

				Vector3 pos = Vector3.Zero;

				if (hasPosition)
					pos = transform.Position;

				if (map != null)
				{
					// Dynamic map
					hasPosition = true;
					pos = Vector3.Transform(Vector3.Zero, map.Transform);
					suspendDistance += Math.Max(Math.Max(map.MaxX - map.MinX, map.MaxY - map.MinY), map.MaxZ - map.MinZ) * 0.5f;
				}

				bool suspended;
				if (currentZone != null && currentZone.Exclusive) // Suspend everything outside the current zone, unless it's connected
				{
					suspended = hasPosition && !boxesContain(boxes, pos);
					// Allow the editor to reverse the decision
					if (currentZone.ConnectedEntities.Contains(entity))
						suspended = !suspended;
				}
				else
				{
					// Only suspend things that are in a different (exclusive) zone, or that are just too far away
					suspended = false;
					if (hasPosition)
					{
						if (!entity.CannotSuspendByDistance && !boxesContain(boxes, pos) && (pos - cameraPosition).Length() > suspendDistance)
							suspended = true;
						else
						{
							foreach (Zone z in Zone.Zones)
							{
								if (z != currentZone && z.Exclusive && z.BoundingBox.Value.Contains(Vector3.Transform(pos, Matrix.Invert(z.Transform))) != ContainmentType.Disjoint)
								{
									suspended = true;
									break;
								}
							}
						}
					}

					// Allow the editor to reverse the decision
					if (currentZone != null && currentZone.ConnectedEntities.Contains(entity))
						suspended = !suspended;
				}

				entity.SetSuspended(suspended);
			}
		}

		private class NonAxisAlignedBoundingBox
		{
			public BoundingBox BoundingBox;
			public Matrix Transform;
		}

		private static IEnumerable<NonAxisAlignedBoundingBox> getActiveBoundingBoxes(Camera camera, Zone currentZone)
		{
			if (currentZone == null)
			{
				Vector3 pos = camera.Position;
				float radius = camera.FarPlaneDistance;
				return new[] { new NonAxisAlignedBoundingBox { BoundingBox = new BoundingBox(pos - new Vector3(radius), pos + new Vector3(radius)), Transform = Matrix.Identity } };
			}
			else
				return Zone.GetConnectedZones(currentZone).Select(x => new NonAxisAlignedBoundingBox { BoundingBox = x.BoundingBox, Transform = Matrix.Invert(x.Transform) }).ToList();
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			result.EditorCanDelete = false;

			ListProperty<DialogueForest> dialogue = result.GetOrMakeListProperty<DialogueForest>("Dialogue");
			dialogue.Serialize = false;
			dialogue.Editable = false;

			result.Add(new TwoWayBinding<string>(result.GetProperty<string>("LightRampTexture"), main.Renderer.LightRampTexture));
			result.Add(new TwoWayBinding<string>(result.GetOrMakeProperty<string>("EnvironmentMap", true, "Images\\env0"), main.Renderer.EnvironmentMap));
			result.Add(new TwoWayBinding<Vector3>(result.GetOrMakeProperty<Vector3>("EnvironmentColor", true, Vector3.One), main.Renderer.EnvironmentColor));
			result.Add(new TwoWayBinding<Color>(result.GetProperty<Color>("BackgroundColor"), main.Renderer.BackgroundColor));
			result.Add(new TwoWayBinding<float>(result.GetProperty<float>("FarPlaneDistance"), main.Camera.FarPlaneDistance));

			Property<Vector3> gravity = result.GetOrMakeProperty<Vector3>("Gravity", true, new Vector3(0.0f, -18.0f, -0.0f));
			gravity.Set = delegate(Vector3 value)
			{
				main.Space.ForceUpdater.Gravity = value;
			};
			gravity.Get = delegate()
			{
				return main.Space.ForceUpdater.Gravity;
			};

			// Zone management
			Property<Zone> currentZone = result.GetOrMakeProperty<Zone>("CurrentZone");
			currentZone.Serialize = false;

			result.Add(new CommandBinding<Entity>(main.EntityAdded, delegate(Entity e)
			{
				if (!e.CannotSuspend)
					processEntity(e, currentZone, getActiveBoundingBoxes(main.Camera, currentZone), main.Camera.Position, main.Camera.FarPlaneDistance);
			}));

			IEnumerable<NonAxisAlignedBoundingBox> boxes = getActiveBoundingBoxes(main.Camera, currentZone);
			Vector3 cameraPosition = main.Camera.Position;
			float suspendDistance = main.Camera.FarPlaneDistance;
			foreach (Entity e in main.Entities)
			{
				if (!e.CannotSuspend)
					processEntity(e, currentZone, boxes, cameraPosition, suspendDistance);
			}

			result.Add("ProcessMap", new Command<Map>
			{
				Action = delegate(Map map)
				{
					processMap(map, boxes);
				}
			});

			Property<float> reverbAmount = result.GetProperty<float>("ReverbAmount");
			Property<float> reverbSize = result.GetProperty<float>("ReverbSize");
			// TODO: figure out Wwise environment settings

			//Sound.ReverbSettings(main, reverbAmount, reverbSize);

			Vector3 lastUpdatedCameraPosition = new Vector3(float.MinValue);
			bool lastFrameUpdated = false;

			Action<Zone> updateZones = delegate(Zone newZone)
			{
				currentZone.Value = newZone;

				/*
				if (newZone != null)
					Sound.ReverbSettings(main, newZone.ReverbAmount, newZone.ReverbSize);
				else
					Sound.ReverbSettings(main, reverbAmount, reverbSize);
				*/

				if (newZone == null)
					main.LightingManager.EnableDetailGlobalShadowMap = true;
				else
					main.LightingManager.EnableDetailGlobalShadowMap = newZone.DetailedShadows;

				boxes = getActiveBoundingBoxes(main.Camera, newZone);
				cameraPosition = main.Camera.Position;
				suspendDistance = main.Camera.FarPlaneDistance;
				foreach (Entity e in main.Entities)
				{
					if (!e.CannotSuspend)
						processEntity(e, newZone, boxes, cameraPosition, suspendDistance);
				}

				lastUpdatedCameraPosition = main.Camera.Position;
			};

			result.Add("UpdateZones", new Command
			{
				Action = delegate() { updateZones(Zone.Get(main.Camera.Position)); },
			});

			Updater update = new Updater
			{
				delegate(float dt)
				{
					// Update every other frame
					if (lastFrameUpdated)
					{
						lastFrameUpdated = false;
						return;
					}
					lastFrameUpdated = true;

					Zone newZone = Zone.Get(main.Camera.Position);

					if (newZone != currentZone.Value || (newZone == null && (main.Camera.Position - lastUpdatedCameraPosition).Length() > 10.0f))
						updateZones(newZone);
				}
			};
			update.EnabledInEditMode = true;
			result.Add(update);

			this.SetMain(result, main);
			WorldFactory.instance = result;
			AkSoundEngine.DefaultGameObject = result;

			Random random = new Random();

			Entity player = main.Get("Player").FirstOrDefault();

			List<PointLight> sparkLights = new List<PointLight>();
			const float sparkLightAttenuation = 5.0f;
			const float sparkLightFadeTime = 0.5f;
			const float sparkLightBrightness = 2.0f;
			const int maxSparkLights = 10;
			int activeSparkLights = 0;
			int oldestSparkLight = 0;
			for (int i = 0; i < maxSparkLights; i++)
			{
				PointLight light = new PointLight();
				light.Serialize = false;
				light.Color.Value = new Vector3(1.0f);
				light.Enabled.Value = false;
				result.Add(light);
				sparkLights.Add(light);
			}

			Action<Vector3, float> sparks = delegate(Vector3 pos, float size)
			{
				ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
				for (int j = 0; j < 40; j++)
				{
					Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
					shatter.AddParticle(pos + offset, offset);
				}

				if (this.random.Next(0, 2) == 0)
				{
					PointLight light;
					if (activeSparkLights < maxSparkLights)
					{
						light = sparkLights[activeSparkLights];
						light.Enabled.Value = true;
						activeSparkLights++;
					}
					else
					{
						light = sparkLights[oldestSparkLight % activeSparkLights];
						oldestSparkLight = (oldestSparkLight + 1) % maxSparkLights;
					}

					light.Attenuation.Value = size;
					light.Color.Value = Vector3.One;
					light.Position.Value = pos;

					AkSoundEngine.PostEvent("Play_sparks", pos);
				}
			};

			const float propagateDelay = 0.07f;
			const int maxGenerations = 4;

			ListProperty<ScheduledBlock> blockQueue = result.GetOrMakeListProperty<ScheduledBlock>("PowerQueue");
			if (main.EditorEnabled)
				blockQueue.Clear();

			Func<Entity, Map.Coordinate, bool, bool> isInQueue = delegate(Entity m, Map.Coordinate c, bool removing)
			{
				foreach (ScheduledBlock b in blockQueue)
				{
					if (b.Removing == removing && m == b.Map.Target && b.Coordinate.Equivalent(c))
						return true;
				}
				return false;
			};

			Dictionary<EffectBlockFactory.BlockEntry, int> generations = new Dictionary<EffectBlockFactory.BlockEntry, int>();

			result.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsFilled, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferredFromMap)
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
							blockQueue.Add(new ScheduledBlock
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

			Map.CellState neutral = Map.States[Map.t.Neutral],
				powered = Map.States[Map.t.Powered],
				temporary = Map.States[Map.t.Temporary],
				infected = Map.States[Map.t.Infected],
				poweredSwitch = Map.States[Map.t.PoweredSwitch],
				permanentPowered = Map.States[Map.t.PermanentPowered],
				switchState = Map.States[Map.t.Switch];

			result.Add(new Updater
			{
				delegate(float dt)
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

					for (int i = 0; i < blockQueue.Count; i++)
					{
						ScheduledBlock entry = blockQueue[i];
						entry.Time -= dt;
						if (entry.Time < 0.0f)
						{
							blockQueue.RemoveAt(i);
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
													if (!isInQueue(map.Entity, adjacent, true))
													{
														blockQueue.Add(new ScheduledBlock
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
										sparks(map.GetAbsolutePosition(c), sparkLightAttenuation);
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
												sparks(map.GetAbsolutePosition(c), sparkLightAttenuation);
												regenerate = true;
											}
											else if (adjacentID == Map.t.Neutral && entry.Generation < maxGenerations)
											{
												map.Empty(adjacent);
												generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = adjacent }] = entry.Generation + 1;
												map.Fill(adjacent, temporary);
												sparks(map.GetAbsolutePosition(adjacent), sparkLightAttenuation);
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
												sparks(map.GetAbsolutePosition(adjacent), sparkLightAttenuation);
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
												sparks(map.GetAbsolutePosition(adjacent), sparkLightAttenuation);
												regenerate = true;
											}
											else if (adjacentID == Map.t.Switch)
											{
												map.Empty(adjacent, true);
												map.Fill(adjacent, poweredSwitch);
												sparks(map.GetAbsolutePosition(adjacent), sparkLightAttenuation);
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
												sparks(map.GetAbsolutePosition(adjacent), sparkLightAttenuation);
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
			});

			result.Add(new CommandBinding<Map, IEnumerable<Map.Coordinate>, Map>(Map.GlobalCellsEmptied, delegate(Map map, IEnumerable<Map.Coordinate> coords, Map transferringToNewMap)
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
								blockQueue.Add(new ScheduledBlock
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
												blockQueue.Add(new ScheduledBlock
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
	}
}
