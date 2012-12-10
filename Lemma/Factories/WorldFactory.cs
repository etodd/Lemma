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
					Density = 1,
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
					Density = 1,
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

		private bool boxesContain(IEnumerable<NonAxisAlignedBoundingBox> boxes, Vector3 position)
		{
			foreach (NonAxisAlignedBoundingBox box in boxes)
			{
				if (box.BoundingBox.Contains(Vector3.Transform(position, box.Transform)) != ContainmentType.Disjoint)
					return true;
			}
			return false;
		}

		private void processEntity(Entity entity, Zone currentZone, IEnumerable<NonAxisAlignedBoundingBox> boxes)
		{
			if (!entity.CannotSuspend)
			{
				Map map = entity.Get<Map>();
				if (map != null && !typeof(DynamicMap).IsAssignableFrom(map.GetType()))
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
						suspended = !currentZone.ConnectedEntities.Contains(entity) && hasPosition && !this.boxesContain(boxes, pos);
					else
					{
						// Only suspend things that are connected or that are in other zones, or that are just too far away
						suspended = (currentZone != null && currentZone.ConnectedEntities.Contains(entity));
						if (!suspended && hasPosition)
						{
							if (!entity.CannotSuspendByDistance && !this.boxesContain(boxes, pos))
								suspended = true;
							else
							{
								foreach (Zone z in Zone.Zones)
								{
									if (z != currentZone && z.BoundingBox.Value.Contains(Vector3.Transform(pos, Matrix.Invert(z.Transform))) != ContainmentType.Disjoint)
									{
										suspended = true;
										break;
									}
								}
							}
						}
					}

					foreach (Component c in entity.ComponentList.ToList())
					{
						if (c.Suspended.Value != suspended)
							c.Suspended.Value = suspended;
					}
				}
			}
		}

		private class NonAxisAlignedBoundingBox
		{
			public BoundingBox BoundingBox;
			public Matrix Transform;
		}

		private IEnumerable<NonAxisAlignedBoundingBox> getActiveBoundingBoxes(Camera camera, Zone currentZone)
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
				this.processEntity(e, currentZone, this.getActiveBoundingBoxes(main.Camera, currentZone));
			}));

			IEnumerable<NonAxisAlignedBoundingBox> boxes = this.getActiveBoundingBoxes(main.Camera, currentZone);
			foreach (Entity e in main.Entities)
				this.processEntity(e, currentZone, boxes);

			Property<float> reverbAmount = result.GetProperty<float>("ReverbAmount");
			Property<float> reverbSize = result.GetProperty<float>("ReverbSize");

			Vector3 lastUpdatedCameraPosition = main.Camera.Position;
			bool lastFrameUpdated = true;
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
					{
						currentZone = newZone;

						if (newZone != null)
							Sound.ReverbSettings(main, newZone.ReverbAmount, newZone.ReverbSize);
						else
							Sound.ReverbSettings(main, reverbAmount, reverbSize);

						boxes = this.getActiveBoundingBoxes(main.Camera, newZone);
						foreach (Entity e in main.Entities)
							this.processEntity(e, newZone, boxes);

						lastUpdatedCameraPosition = main.Camera.Position;
					}
				}
			};
			update.EnabledInEditMode.Value = true;
			result.Add(update);

			this.SetMain(result, main);
			WorldFactory.instance = result;
		}
	}
}
