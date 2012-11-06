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
					DiffuseMap = "EnvironmentTextures\\concrete",
					NormalMap = "EnvironmentTextures\\concrete-normal",
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
					DiffuseMap = "EnvironmentTextures\\temporary",
					NormalMap = "EnvironmentTextures\\temporary-normal",
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
					DiffuseMap = "EnvironmentTextures\\dirt",
					NormalMap = "EnvironmentTextures\\dirt-normal",
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
					DiffuseMap = "EnvironmentTextures\\gravel",
					NormalMap = "EnvironmentTextures\\gravel-normal",
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
					DiffuseMap = "EnvironmentTextures\\metal",
					NormalMap = "EnvironmentTextures\\metal-normal",
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
					DiffuseMap = "EnvironmentTextures\\danger",
					NormalMap = "EnvironmentTextures\\danger-normal",
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
					DiffuseMap = "EnvironmentTextures\\foliage",
					NormalMap = "EnvironmentTextures\\plain-normal",
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
					DiffuseMap = "EnvironmentTextures\\wood",
					NormalMap = "EnvironmentTextures\\wood-normal",
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
					DiffuseMap = "EnvironmentTextures\\ice",
					NormalMap = "EnvironmentTextures\\ice-normal",
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
					DiffuseMap = "EnvironmentTextures\\quartz",
					NormalMap = "EnvironmentTextures\\quartz-normal",
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
					DiffuseMap = "EnvironmentTextures\\lava",
					NormalMap = "EnvironmentTextures\\lava-normal",
					FootstepCue = "LavaFootsteps",
					RubbleCue = "LavaRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 12,
					Name = "FragileDirt",
					Permanent = false,
					Density = 0.5f,
					DiffuseMap = "EnvironmentTextures\\fragile-dirt",
					NormalMap = "EnvironmentTextures\\fragile-dirt-normal",
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
					DiffuseMap = "EnvironmentTextures\\marble",
					NormalMap = "EnvironmentTextures\\marble-normal",
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
					DiffuseMap = "EnvironmentTextures\\infected",
					NormalMap = "EnvironmentTextures\\lava-normal",
					FootstepCue = "InfectedFootsteps",
					RubbleCue = "InfectedRubble",
					SpecularPower = 1.0f,
					SpecularIntensity = 0.0f,
					Glow = true,
				},
				new Map.CellState
				{
					ID = 15,
					Name = "Grass",
					Permanent = true,
					Density = 1,
					DiffuseMap = "EnvironmentTextures\\grass",
					NormalMap = "EnvironmentTextures\\grass-normal",
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
					DiffuseMap = "EnvironmentTextures\\brick",
					NormalMap = "EnvironmentTextures\\brick-normal",
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
					DiffuseMap = "EnvironmentTextures\\tile",
					NormalMap = "EnvironmentTextures\\tile-normal",
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
					DiffuseMap = "EnvironmentTextures\\windows",
					NormalMap = "EnvironmentTextures\\windows-normal",
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
					DiffuseMap = "EnvironmentTextures\\bulkhead1",
					NormalMap = "EnvironmentTextures\\bulkhead1-normal",
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
					Permanent = false,
					Density = 1,
					DiffuseMap = "EnvironmentTextures\\bulkhead2",
					NormalMap = "EnvironmentTextures\\bulkhead2-normal",
					FootstepCue = "Bulkhead1Footsteps",
					RubbleCue = "Bulkhead1Rubble",
					SpecularPower = 100.0f,
					SpecularIntensity = 0.1f,
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

		private bool boxesContain(IEnumerable<BoundingBox> boxes, Vector3 position)
		{
			foreach (BoundingBox box in boxes)
			{
				if (box.Contains(position) != ContainmentType.Disjoint)
					return true;
			}
			return false;
		}

		private void processEntity(Entity entity, Zone currentZone, IEnumerable<BoundingBox> boxes)
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
						foreach (BoundingBox box in boxes)
						{
							if (absoluteChunkBoundingBox.Intersects(box))
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
									if (z != currentZone && z.AbsoluteBoundingBox.Value.Contains(pos) != ContainmentType.Disjoint)
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

		private IEnumerable<BoundingBox> getActiveBoundingBoxes(Camera camera, Zone currentZone)
		{
			if (currentZone == null)
			{
				Vector3 pos = camera.Position;
				float radius = camera.FarPlaneDistance;
				return new[] { new BoundingBox(pos - new Vector3(radius), pos + new Vector3(radius)) };
			}
			else
				return Zone.GetConnectedBoundingBoxes(currentZone);
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;

			Model skybox = result.Get<Model>("Skybox");
			if (skybox != null)
				skybox.DrawOrder.Value = -10;

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

			IEnumerable<BoundingBox> boxes = this.getActiveBoundingBoxes(main.Camera, currentZone);
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
