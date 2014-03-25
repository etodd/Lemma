using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class WorldFactory : Factory
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

		public static Dictionary<int, Map.CellState> States = new Dictionary<int, Map.CellState>();
		public static Dictionary<string, Map.CellState> StatesByName = new Dictionary<string, Map.CellState>();
		public static List<Map.CellState> StateList = new List<Map.CellState>();

		public static void AddState(params Map.CellState[] states)
		{
			foreach (Map.CellState state in states)
			{
				WorldFactory.States[state.ID] = state;
				WorldFactory.StatesByName[state.Name] = state;
				WorldFactory.StateList.Add(state);
			}
		}

		public static void RemoveState(params Map.CellState[] states)
		{
			foreach (Map.CellState state in states)
			{
				WorldFactory.States.Remove(state.ID);
				WorldFactory.StatesByName.Remove(state.Name);
				WorldFactory.StateList.Remove(state);
			}
		}

		static WorldFactory()
		{
			WorldFactory.AddState
			(
				new Map.CellState
				{
					ID = 0,
					Name = "Empty"
				},
				new Map.CellState
				{
					ID = 1,
					Name = "Rock",
					Permanent = true,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\rock",
					NormalMap = "Textures\\rock-normal",
					FootstepCue = "RockFootsteps",
					RubbleCue = "Rubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f,
				},
				new Map.CellState
				{
					ID = 2,
					Name = "Temporary",
					Permanent = false,
					Hard = false,
					Density = 2,
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Tint = new Vector3(0.3f, 0.5f, 0.7f),
				},
				new Map.CellState
				{
					ID = 3,
					Name = "AvoidAI",
					Permanent = true,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\dirty",
					NormalMap = "Textures\\dirty-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tint = new Vector3(0.15f),
				},
				new Map.CellState
				{
					ID = 4,
					Name = "Dirt",
					Permanent = false,
					Hard = true,
					Density = 0.5f,
					DiffuseMap = "Textures\\dirt",
					NormalMap = "Textures\\dirt-normal",
					FootstepCue = "DirtFootsteps",
					RubbleCue = "DirtRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
				},
				new Map.CellState
				{
					ID = 6,
					Name = "Critical",
					Permanent = false,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\danger",
					NormalMap = "Textures\\plain-normal",
					FootstepCue = "CriticalFootsteps",
					RubbleCue = "CriticalRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
				},
				new Map.CellState
				{
					ID = 7,
					Name = "Foliage",
					Permanent = false,
					Hard = false,
					Density = 0.5f,
					DiffuseMap = "Textures\\foliage",
					NormalMap = "Textures\\plain-normal",
					FootstepCue = "FoliageFootsteps",
					RubbleCue = "FoliageRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					AllowAlpha = true,
					Tiling = 3.0f,
				},
				new Map.CellState
				{
					ID = 8,
					Name = "Hard",
					Permanent = false,
					Hard = true,
					Density = 0.5f,
					DiffuseMap = "Textures\\dirty",
					NormalMap = "Textures\\metal-channels-normal",
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f,
					Tint = new Vector3(0.4f),
				},
				new Map.CellState
				{
					ID = 9,
					Name = "Floater",
					Permanent = false,
					Hard = false,
					Density = 0.5f,
					DiffuseMap = "Textures\\dirty",
					NormalMap = "Textures\\metal-channels-normal",
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tint = new Vector3(0.9f, 0.3f, 0.0f),
				},
				new Map.CellState
				{
					ID = 13,
					Name = "HardPowered",
					Permanent = false,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\powered-permanent",
					NormalMap = "Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 18,
					Name = "Neutral",
					Permanent = false,
					Hard = false,
					Density = 1,
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Tint = new Vector3(0.7f),
				},
				new Map.CellState
				{
					ID = 23,
					Name = "RockChunky",
					Permanent = true,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\rock-chunky",
					NormalMap = "Textures\\rock-chunky-normal",
					FootstepCue = "RockFootsteps",
					RubbleCue = "Rubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tiling = 0.25f,
				},
				new Map.CellState
				{
					ID = 30,
					Name = "White",
					Permanent = false,
					Hard = false,
					ShadowCast = false,
					Density = 0.5f,
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 31,
					Name = "Metal",
					Permanent = true,
					Hard = true,
					Density = 1,
					DiffuseMap = "Textures\\dirty",
					NormalMap = "Textures\\metal-channels2-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f,
					Tint = new Vector3(0.25f),
				},
				new Map.CellState
				{
					ID = 32,
					Name = "MetalSwirl",
					Permanent = true,
					Hard = true,
					Density = 1,
					DiffuseMap = "Textures\\dirty",
					NormalMap = "Textures\\metal-swirl-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f,
					Tint = new Vector3(0.25f),
				},
				new Map.CellState
				{
					ID = 34,
					Name = "Invisible",
					Permanent = true,
					Hard = true,
					Invisible = true,
					AllowAlpha = true,
					Density = 1,
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\plain-normal",
					Tint = new Vector3(0.5f),
				},
				new Map.CellState
				{
					ID = 35,
					Name = "WhitePermanent",
					Permanent = true,
					Hard = true,
					ShadowCast = false,
					Density = 0.5f,
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 36,
					Name = "Switch",
					Permanent = true,
					Hard = true,
					Density = 1,
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					DiffuseMap = "Textures\\switch",
					NormalMap = "Textures\\switch-normal",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Tiling = 4.0f,
					Tint = new Vector3(0.3f, 0.6f, 0.8f),
				},
				new Map.CellState
				{
					ID = 37,
					Name = "PoweredSwitch",
					Permanent = true,
					Hard = true,
					Density = 1,
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					DiffuseMap = "Textures\\powered-switch",
					NormalMap = "Textures\\switch-normal",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
					Tiling = 4.0f,
				},
				new Map.CellState
				{
					ID = 38,
					Name = "Powered",
					Permanent = false,
					Hard = false,
					Density = 2,
					DiffuseMap = "Textures\\powered",
					NormalMap = "Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 39,
					Name = "PermanentPowered",
					Permanent = true,
					Hard = true,
					Density = 2,
					DiffuseMap = "Textures\\powered-permanent",
					NormalMap = "Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 40,
					Name = "InfectedCritical",
					Permanent = false,
					Hard = true,
					Density = 3,
					FootstepCue = "InfectedFootsteps",
					RubbleCue = "InfectedRubble",
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\temporary-normal",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Tint = new Vector3(0.4f, 0.0f, 0.0f),
				},
				new Map.CellState
				{
					ID = 41,
					Name = "Infected",
					Permanent = false,
					Hard = false,
					Density = 3,
					FootstepCue = "InfectedFootsteps",
					RubbleCue = "InfectedRubble",
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\temporary-normal",
					SpecularPower = 250.0f,
					SpecularIntensity = 0.4f,
					Tint = new Vector3(0.8f, 0.1f, 0.1f),
				},
				new Map.CellState
				{
					ID = 42,
					Name = "Black",
					Permanent = true,
					Hard = true,
					ShadowCast = true,
					Density = 0.5f,
					DiffuseMap = "Textures\\white",
					NormalMap = "Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tint = Vector3.Zero,
				}
			);
		}

		private static readonly Color defaultBackgroundColor = new Color(8.0f / 255.0f, 13.0f / 255.0f, 19.0f / 255.0f, 1.0f);

		private static Entity instance;

		public WorldFactory()
		{
			this.Color = new Vector3(0.1f, 0.1f, 0.1f);
		}

		public static Entity Get()
		{
			if (WorldFactory.instance == null)
				return null;

			if (!WorldFactory.instance.Active)
				WorldFactory.instance = null;

			return WorldFactory.instance;
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "World");
			result.Add("Transform", new Transform());
			result.Add("Gravity",
				new Property<Vector3>
				{
					Get = delegate()
					{
						return main.Space.ForceUpdater.Gravity;
					},
					Set = delegate(Vector3 value)
					{
						main.Space.ForceUpdater.Gravity = value;
					}
				}
			);

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
					suspended = !currentZone.ConnectedEntities.Contains(entity) && hasPosition && !boxesContain(boxes, pos);
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

			result.Add(new TwoWayBinding<string>(result.GetProperty<string>("LightRampTexture"), main.Renderer.LightRampTexture));
			result.Add(new TwoWayBinding<string>(result.GetOrMakeProperty<string>("EnvironmentMap", true, "Images\\env0"), main.Renderer.EnvironmentMap));
			result.Add(new TwoWayBinding<Vector3>(result.GetOrMakeProperty<Vector3>("EnvironmentColor", true, Vector3.One), main.Renderer.EnvironmentColor));
			result.Add(new TwoWayBinding<Color>(result.GetProperty<Color>("BackgroundColor"), main.Renderer.BackgroundColor));
			result.Add(new TwoWayBinding<float>(result.GetProperty<float>("FarPlaneDistance"), main.Camera.FarPlaneDistance));

			WorldFactory.AddState(result.GetListProperty<Map.CellState>("AdditionalMaterials").ToArray());
			result.Add(new CommandBinding(result.Delete, delegate()
			{
				WorldFactory.RemoveState(result.GetListProperty<Map.CellState>("AdditionalMaterials").ToArray());
			}));

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

			Sound.ReverbSettings(main, reverbAmount, reverbSize);

			Vector3 lastUpdatedCameraPosition = new Vector3(float.MinValue);
			bool lastFrameUpdated = false;

			Action<Zone> updateZones = delegate(Zone newZone)
			{
				currentZone.Value = newZone;

				if (newZone != null)
					Sound.ReverbSettings(main, newZone.ReverbAmount, newZone.ReverbSize);
				else
					Sound.ReverbSettings(main, reverbAmount, reverbSize);

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
			update.EnabledInEditMode.Value = true;
			result.Add(update);

			this.SetMain(result, main);
			WorldFactory.instance = result;

			// Materials
			Map.CellState emptyState = WorldFactory.States[0];

			Random random = new Random();

			Entity player = main.Get("Player").FirstOrDefault();

			int criticalID = WorldFactory.StatesByName["Critical"].ID,
				infectedCriticalID = WorldFactory.StatesByName["InfectedCritical"].ID,
				whiteID = WorldFactory.StatesByName["White"].ID,
				temporaryID = WorldFactory.StatesByName["Temporary"].ID,
				neutralID = WorldFactory.StatesByName["Neutral"].ID,
				poweredID = WorldFactory.StatesByName["Powered"].ID,
				permanentPoweredID = WorldFactory.StatesByName["PermanentPowered"].ID,
				hardPoweredID = WorldFactory.StatesByName["HardPowered"].ID,
				switchID = WorldFactory.StatesByName["Switch"].ID,
				poweredSwitchID = WorldFactory.StatesByName["PoweredSwitch"].ID,
				infectedID = WorldFactory.StatesByName["Infected"].ID,
				floaterID = WorldFactory.StatesByName["Floater"].ID;

			Action<Entity> bindPlayer = delegate(Entity p)
			{
				player = p;
				
				Player playerComponent = player.Get<Player>();

				AnimatedModel playerModel = player.Get<AnimatedModel>("Model");

				Updater lavaDamager = new Updater
				{
					delegate(float dt)
					{
						if (!playerModel.IsPlaying("Kick") && (playerComponent.IsSupported || playerComponent.WallRunState.Value != Player.WallRun.None))
							playerComponent.Health.Value -= 0.6f * dt;
					}
				};
				lavaDamager.Enabled.Value = false;

				player.Add(lavaDamager);
				
				player.Add(new CommandBinding<Map, Map.Coordinate?, Direction>(player.GetCommand<Map, Map.Coordinate?, Direction>("WalkedOn"), delegate(Map map, Map.Coordinate? coord, Direction dir)
				{
					int groundType = map == null ? 0 : map[coord.Value].ID;

					// Lava. Damage the player character if it steps on lava.
					bool isLava = groundType == infectedID || groundType == infectedCriticalID;
					if (isLava)
						playerComponent.Health.Value -= 0.2f;
					lavaDamager.Enabled.Value = isLava;

					// Floater. Delete the block after a delay.
					if (groundType == floaterID)
					{
						Vector3 pos = map.GetAbsolutePosition(coord.Value);
						ParticleEmitter.Emit(main, "Smoke", pos, 1.0f, 10);
						Sound.PlayCue(main, "FragileDirt Crumble", pos);
						result.Add(new Animation
						(
							new Animation.Delay(1.0f),
							new Animation.Execute(delegate()
							{
								if (map[coord.Value].ID == floaterID)
								{
									map.Empty(coord.Value);
									map.Regenerate();
								}
							})
						));
					}
				}));
			};

			if (player != null)
				bindPlayer(player);

			result.Add(new CommandBinding<Entity>(main.EntityAdded, delegate(Entity e)
			{
				if (e.Type == "Player")
					bindPlayer(e);
			}));

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
						int id = c.Data.ID;
						if (id == temporaryID || id == poweredID || id == poweredSwitchID || id == infectedID)
						{
							Map.Coordinate newCoord = c;
							newCoord.Data = emptyState;
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

			List<PointLight> sparkLights = new List<PointLight>();
			const float startSparkLightAttenuation = 5.0f;
			const float sparkLightFadeTime = 0.5f;
			int activeSparkLights = 0;

			Action<Vector3> sparks = delegate(Vector3 pos)
			{
				ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
				for (int j = 0; j < 30; j++)
				{
					Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
					shatter.AddParticle(pos + offset, offset);
				}

				PointLight light;
				if (activeSparkLights < sparkLights.Count)
				{
					light = sparkLights[activeSparkLights];
					light.Enabled.Value = true;
				}
				else
				{
					light = new PointLight();
					result.Add(light);
					sparkLights.Add(light);
				}
				activeSparkLights++;

				light.Serialize = false;
				light.Shadowed.Value = false;
				light.Color.Value = new Vector3(1.0f);
				light.Attenuation.Value = startSparkLightAttenuation;
				light.Position.Value = pos;

				Sound.PlayCue(main, "Sparks", pos, 1.0f, 0.05f);
			};

			result.Add(new Updater
			{
				delegate(float dt)
				{
					float sparkLightFade = startSparkLightAttenuation * dt / sparkLightFadeTime;
					for (int i = 0; i < activeSparkLights; i++)
					{
						PointLight light = sparkLights[i];
						float a = light.Attenuation - sparkLightFade;
						if (a < 0.0f)
						{
							light.Enabled.Value = false;
							PointLight swap = sparkLights[activeSparkLights - 1];
							sparkLights[i] = swap;
							sparkLights[activeSparkLights - 1] = light;
							activeSparkLights--;
						}
						else
							light.Attenuation.Value = a;
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
								int id = map[c].ID;

								bool isTemporary = id == temporaryID;
								bool isInfected = id == infectedID || id == infectedCriticalID;
								bool isPowered = id == poweredID || id == permanentPoweredID || id == hardPoweredID || id == poweredSwitchID;

								bool regenerate = false;

								if (entry.Removing)
								{
									if (entry.Generation == 0 && id == 0)
									{
										Direction down = map.GetRelativeDirection(Direction.NegativeY);
										foreach (Direction dir in DirectionExtensions.Directions)
										{
											Map.Coordinate adjacent = c.Move(dir);
											int adjacentID = map[adjacent].ID;
											bool adjacentIsFloater = adjacentID == floaterID;
											if (dir != down || adjacentIsFloater)
											{
												if (adjacentID == poweredID || adjacentID == temporaryID || adjacentID == neutralID || adjacentID == infectedID || adjacentIsFloater)
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
									else if (entry.Generation > 0 && (isTemporary || isInfected || isPowered || id == neutralID || id == floaterID))
									{
										generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = c }] = entry.Generation;
										map.Empty(c);
										sparks(map.GetAbsolutePosition(c));
										regenerate = true;
									}
								}
								else if (isTemporary
									|| isInfected
									|| isPowered)
								{
									if (isTemporary)
									{
										foreach (Direction dir in DirectionExtensions.Directions)
										{
											Map.Coordinate adjacent = c.Move(dir);
											int adjacentID = map[adjacent].ID;

											if (adjacentID == poweredID || adjacentID == permanentPoweredID || adjacentID == hardPoweredID || adjacentID == poweredSwitchID)
											{
												map.Empty(c);
												map.Fill(c, WorldFactory.States[poweredID]);
												sparks(map.GetAbsolutePosition(c));
												regenerate = true;
											}
											else if (adjacentID == neutralID && entry.Generation < maxGenerations)
											{
												map.Empty(adjacent);
												generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = adjacent }] = entry.Generation + 1;
												map.Fill(adjacent, WorldFactory.States[temporaryID]);
												sparks(map.GetAbsolutePosition(adjacent));
												regenerate = true;
											}
										}
									}
									else if (isPowered)
									{
										foreach (Direction dir in DirectionExtensions.Directions)
										{
											Map.Coordinate adjacent = c.Move(dir);
											int adjacentID = map[adjacent].ID;

											if (adjacentID == temporaryID)
											{
												map.Empty(adjacent);
												map.Fill(adjacent, WorldFactory.States[poweredID]);
												sparks(map.GetAbsolutePosition(adjacent));
												regenerate = true;
											}
											else if (adjacentID == switchID)
											{
												map.Empty(adjacent, true);
												map.Fill(adjacent, WorldFactory.States[poweredSwitchID]);
												sparks(map.GetAbsolutePosition(adjacent));
												regenerate = true;
											}
											else if (adjacentID == criticalID)
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
											int adjacentID = map[adjacent].ID;
											if (adjacentID == neutralID && entry.Generation < maxGenerations)
											{
												map.Empty(adjacent);
												generations[new EffectBlockFactory.BlockEntry { Map = map, Coordinate = adjacent }] = entry.Generation + 1;
												map.Fill(adjacent, WorldFactory.States[infectedID]);
												sparks(map.GetAbsolutePosition(adjacent));
												regenerate = true;
											}
											else if (adjacentID == criticalID)
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
					int id = coord.Data.ID;
					if (id == poweredID || id == poweredSwitchID)
						handlePowered = true;

					if (id == criticalID) // Critical. Explodes when destroyed.
						Explosion.Explode(main, map, coord);
					else if (id == infectedCriticalID) // Infected. Shatter effects.
					{
						ParticleSystem shatter = ParticleSystem.Get(main, "InfectedShatter");
						Vector3 pos = map.GetAbsolutePosition(coord);
						Sound.PlayCue(main, "InfectedShatter", pos, 1.0f, 0.05f);
						for (int i = 0; i < 50; i++)
						{
							Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
							shatter.AddParticle(pos + offset, offset);
						}
					}
					else if (id == poweredID || id == temporaryID || id == neutralID || id == infectedID || id == floaterID)
					{
						int generation;
						Map.Coordinate c = coord;
						c.Data = emptyState;
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
									int adjacentID = map[adjacent].ID;
									bool adjacentIsFloater = adjacentID == floaterID;
									if (dir != down || adjacentIsFloater)
									{
										if (adjacentID == poweredID || adjacentID == temporaryID || adjacentID == neutralID || adjacentID == infectedID || adjacentIsFloater)
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
					else if (id == whiteID) // White. Shatter effects.
					{
						ParticleSystem shatter = ParticleSystem.Get(main, "WhiteShatter");
						Vector3 pos = map.GetAbsolutePosition(coord);
						Sound.PlayCue(main, "WhiteShatter", pos, 1.0f, 0.05f);
						for (int i = 0; i < 50; i++)
						{
							Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
							shatter.AddParticle(pos + offset, offset);
						}
					}
				}

				if (handlePowered)
				{
					IEnumerable<IEnumerable<Map.Box>> poweredIslands = map.GetAdjacentIslands(coords.Where(x => x.Data.ID == poweredID), x => x.ID == poweredID || x.ID == poweredSwitchID, WorldFactory.StatesByName["PermanentPowered"]);
					List<Map.Coordinate> poweredCoords = poweredIslands.SelectMany(x => x).SelectMany(x => x.GetCoords()).ToList();
					if (poweredCoords.Count > 0)
					{
						Map.CellState temporaryState = WorldFactory.StatesByName["Temporary"];
						Map.CellState switchState = WorldFactory.StatesByName["Switch"];
						map.Empty(poweredCoords, true, true, null, false);
						foreach (Map.Coordinate coord in poweredCoords)
							map.Fill(coord, coord.Data.ID == poweredSwitchID ? switchState : temporaryState);
						map.Regenerate();
					}
				}
			}));
		}
	}
}
