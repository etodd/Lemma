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
		public static Dictionary<int, Map.CellState> States = new Dictionary<int, Map.CellState>();
		public static Dictionary<string, Map.CellState> StatesByName = new Dictionary<string, Map.CellState>();
		public static List<Map.CellState> StateList = new List<Map.CellState>();

		private static void addState(params Map.CellState[] states)
		{
			foreach (Map.CellState state in states)
			{
				WorldFactory.States[state.ID] = state;
				WorldFactory.StatesByName[state.Name] = state;
				WorldFactory.StateList.Add(state);
			}
		}

		private static void removeState(params Map.CellState[] states)
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
			WorldFactory.addState
			(
				new Map.CellState
				{
					ID = 0,
					Name = "Empty"
				},
				new Map.CellState
				{
					ID = 1,
					Name = "Concrete",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\concrete",
					NormalMap = "Maps\\Textures\\concrete-normal",
					FootstepCue = "ConcreteFootsteps",
					RubbleCue = "ConcreteRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f
				},
				new Map.CellState
				{
					ID = 2,
					Name = "Temporary",
					Permanent = false,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\temporary",
					NormalMap = "Maps\\Textures\\temporary-normal",
					FootstepCue = "TemporaryFootsteps",
					RubbleCue = "TemporaryRubble",
					SpecularPower = 50.0f,
					SpecularIntensity = 0.2f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 3,
					Name = "Dirt",
					Permanent = false,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\dirt",
					NormalMap = "Maps\\Textures\\dirt-normal",
					FootstepCue = "DirtFootsteps",
					RubbleCue = "DirtRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 4,
					Name = "Gravel",
					Permanent = false,
					Density = 6,
					DiffuseMap = "Maps\\Textures\\gravel",
					NormalMap = "Maps\\Textures\\gravel-normal",
					FootstepCue = "GravelFootsteps",
					RubbleCue = "GravelRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 5,
					Name = "Metal",
					Permanent = false,
					Density = 4,
					DiffuseMap = "Maps\\Textures\\metal",
					NormalMap = "Maps\\Textures\\metal-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f
				},
				new Map.CellState
				{
					ID = 6,
					Name = "Critical",
					Permanent = false,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\danger",
					NormalMap = "Maps\\Textures\\danger-normal",
					FootstepCue = "CriticalFootsteps",
					RubbleCue = "CriticalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 7,
					Name = "Foliage",
					Permanent = false,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\foliage",
					NormalMap = "Maps\\Textures\\plain-normal",
					FootstepCue = "FoliageFootsteps",
					RubbleCue = "FoliageRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					AllowAlpha = true
				},
				new Map.CellState
				{
					ID = 8,
					Name = "Wood",
					Permanent = false,
					Density = 0.1f,
					DiffuseMap = "Maps\\Textures\\wood",
					NormalMap = "Maps\\Textures\\wood-normal",
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
				},
				new Map.CellState
				{
					ID = 9,
					Name = "Ice",
					Permanent = false,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\ice",
					NormalMap = "Maps\\Textures\\ice-normal",
					FootstepCue = "IceFootsteps",
					RubbleCue = "IceRubble",
					SpecularPower = 100.0f,
					SpecularIntensity = 1.0f,
				},
				new Map.CellState
				{
					ID = 10,
					Name = "Quartz",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\quartz",
					NormalMap = "Maps\\Textures\\quartz-normal",
					FootstepCue = "QuartzFootsteps",
					RubbleCue = "QuartzRubble",
					SpecularPower = 100.0f,
					SpecularIntensity = 1.0f,
				},
				new Map.CellState
				{
					ID = 11,
					Name = "Lava",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\lava",
					NormalMap = "Maps\\Textures\\lava-normal",
					FootstepCue = "LavaFootsteps",
					RubbleCue = "LavaRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
					Tint = new Vector3(1.5f, 0.5f, 0.5f),
				},
				new Map.CellState
				{
					ID = 12,
					Name = "FragileDirt",
					Permanent = false,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\fragile-dirt",
					NormalMap = "Maps\\Textures\\fragile-dirt-normal",
					FootstepCue = "DirtFootsteps",
					RubbleCue = "DirtRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
				},
				new Map.CellState
				{
					ID = 13,
					Name = "Marble",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\marble",
					NormalMap = "Maps\\Textures\\marble-normal",
					FootstepCue = "MarbleFootsteps",
					RubbleCue = "MarbleRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.2f,
				},
				new Map.CellState
				{
					ID = 14,
					Name = "Infected",
					Permanent = false,
					Density = 3,
					DiffuseMap = "Maps\\Textures\\lava",
					NormalMap = "Maps\\Textures\\lava-normal",
					FootstepCue = "InfectedFootsteps",
					RubbleCue = "InfectedRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
					Tint = new Vector3(0.0f, 1.0f, 0.0f),
				},
				new Map.CellState
				{
					ID = 15,
					Name = "Grass",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\grass",
					NormalMap = "Maps\\Textures\\grass-normal",
					FootstepCue = "GrassFootsteps",
					RubbleCue = "GrassRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tiling = 1.0f,
				},
				new Map.CellState
				{
					ID = 16,
					Name = "Brick",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\brick",
					NormalMap = "Maps\\Textures\\brick-normal",
					FootstepCue = "BrickFootsteps",
					RubbleCue = "BrickRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 17,
					Name = "Tile",
					Permanent = false,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\tile",
					NormalMap = "Maps\\Textures\\tile-normal",
					FootstepCue = "TileFootsteps",
					RubbleCue = "TileRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 18,
					Name = "Windows",
					Permanent = false,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\windows",
					NormalMap = "Maps\\Textures\\windows-normal",
					FootstepCue = "WindowsFootsteps",
					RubbleCue = "WindowsRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.7f,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 19,
					Name = "Bulkhead1",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\bulkhead1",
					NormalMap = "Maps\\Textures\\bulkhead1-normal",
					FootstepCue = "Bulkhead1Footsteps",
					RubbleCue = "Bulkhead1Rubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
					Tiling = 1.0f,
				},
				new Map.CellState
				{
					ID = 20,
					Name = "Bulkhead2",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\bulkhead2",
					NormalMap = "Maps\\Textures\\bulkhead2-normal",
					FootstepCue = "Bulkhead1Footsteps",
					RubbleCue = "Bulkhead1Rubble",
					SpecularPower = 100.0f,
					SpecularIntensity = 0.1f,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 21,
					Name = "RockRough",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\rough-rock",
					NormalMap = "Maps\\Textures\\rough-rock-normal",
					FootstepCue = "RockRoughFootsteps",
					RubbleCue = "RockRoughRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 22,
					Name = "RockGrass",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\rock-grass",
					NormalMap = "Maps\\Textures\\rock-grass-normal",
					FootstepCue = "RockGrassFootsteps",
					RubbleCue = "RockGrassRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 23,
					Name = "RockChunky",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\rock-chunky",
					NormalMap = "Maps\\Textures\\rock-chunky-normal",
					FootstepCue = "RockChunkyFootsteps",
					RubbleCue = "RockChunkyRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 24,
					Name = "RockRed",
					Permanent = true,
					Density = 2,
					DiffuseMap = "Maps\\Textures\\rock-red",
					NormalMap = "Maps\\Textures\\rock-red-normal",
					FootstepCue = "RockRedFootsteps",
					RubbleCue = "RockRedRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 25,
					Name = "MetalChannels",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\metal-channels",
					NormalMap = "Maps\\Textures\\metal-channels-normal",
					FootstepCue = "MetalChannelsFootsteps",
					RubbleCue = "MetalChannelsRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 26,
					Name = "InfectedPermanent",
					Permanent = true,
					Density = 3,
					DiffuseMap = "Maps\\Textures\\lava",
					NormalMap = "Maps\\Textures\\lava-normal",
					FootstepCue = "InfectedFootsteps",
					RubbleCue = "InfectedRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
					Tint = new Vector3(0.5f, 1.5f, 0.5f),
				},
				new Map.CellState
				{
					ID = 27,
					Name = "MetalRibs",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\metal-ribs",
					NormalMap = "Maps\\Textures\\metal-ribs-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 28,
					Name = "MetalRed",
					Permanent = true,
					Density = 0.1f,
					DiffuseMap = "Maps\\Textures\\metal-red",
					NormalMap = "Maps\\Textures\\metal-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 29,
					Name = "MetalGrate",
					Permanent = true,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\metal-grate",
					NormalMap = "Maps\\Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularPower = 200.0f,
					SpecularIntensity = 0.5f,
					AllowAlpha = true,
					Tiling = 2.0f,
				},
				new Map.CellState
				{
					ID = 30,
					Name = "White",
					Permanent = false,
					ShadowCast = false,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\white",
					NormalMap = "Maps\\Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 31,
					Name = "MetalChannels2",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\metal-channels2",
					NormalMap = "Maps\\Textures\\metal-channels2-normal",
					FootstepCue = "MetalChannelsFootsteps",
					RubbleCue = "MetalChannelsRubble",
					SpecularPower = 10.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 32,
					Name = "MetalSwirl",
					Permanent = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\metal-swirl",
					NormalMap = "Maps\\Textures\\metal-swirl-normal",
					FootstepCue = "MetalChannelsFootsteps",
					RubbleCue = "MetalChannelsRubble",
					SpecularPower = 10.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 33,
					Name = "FakeMetalChannels2",
					Permanent = true,
					Fake = true,
					Density = 1,
					DiffuseMap = "Maps\\Textures\\metal-channels2",
					NormalMap = "Maps\\Textures\\metal-channels2-normal",
					FootstepCue = "MetalChannelsFootsteps",
					RubbleCue = "MetalChannelsRubble",
					SpecularPower = 10.0f,
					SpecularIntensity = 0.5f,
				},
				new Map.CellState
				{
					ID = 34,
					Name = "Invisible",
					Permanent = true,
					Invisible = true,
					Density = 1,
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					DiffuseMap = "Maps\\Textures\\debug",
					NormalMap = "Maps\\Textures\\plain-normal",
				},
				new Map.CellState
				{
					ID = 35,
					Name = "WhitePermanent",
					Permanent = true,
					ShadowCast = false,
					Density = 0.5f,
					DiffuseMap = "Maps\\Textures\\white",
					NormalMap = "Maps\\Textures\\plain-normal",
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 36,
					Name = "Debug",
					Permanent = true,
					Density = 1,
					FootstepCue = "MetalFootsteps",
					RubbleCue = "MetalRubble",
					DiffuseMap = "Maps\\Textures\\debug",
					NormalMap = "Maps\\Textures\\plain-normal",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
				},
				new Map.CellState
				{
					ID = 37,
					Name = "DebugTemporary",
					Permanent = false,
					Density = 1,
					FootstepCue = "WoodFootsteps",
					RubbleCue = "WoodRubble",
					DiffuseMap = "Maps\\Textures\\debug2",
					NormalMap = "Maps\\Textures\\plain-normal",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f
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

				if (map != null && typeof(DynamicMap).IsAssignableFrom(map.GetType()))
				{
					hasPosition = true;
					pos = Vector3.Transform(Vector3.Zero, map.Transform);
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
			result.Add(new TwoWayBinding<Color>(result.GetProperty<Color>("BackgroundColor"), main.Renderer.BackgroundColor));
			result.Add(new TwoWayBinding<float>(result.GetProperty<float>("FarPlaneDistance"), main.Camera.FarPlaneDistance));

			WorldFactory.addState(result.GetListProperty<Map.CellState>("AdditionalMaterials").ToArray());
			result.Add(new CommandBinding(result.Delete, delegate()
			{
				WorldFactory.removeState(result.GetListProperty<Map.CellState>("AdditionalMaterials").ToArray());
			}));

			// Zone management
			Zone currentZone = null;

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
				currentZone = newZone;

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

					if (newZone != currentZone || (newZone == null && (main.Camera.Position - lastUpdatedCameraPosition).Length() > 10.0f))
						updateZones(newZone);
				}
			};
			update.EnabledInEditMode.Value = true;
			result.Add(update);

			this.SetMain(result, main);
			WorldFactory.instance = result;
		}
	}
}
