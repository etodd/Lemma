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

			result.Add(new PostInitialization
			{
				delegate()
				{
					if (Factory.Get<PlayerDataFactory>().Instance == null)
						Factory.Get<PlayerDataFactory>().CreateAndBind(main);
				}
			});

			Property<DialogueForest> dialogue = result.GetOrMakeProperty<DialogueForest>("DialogueForest");
			dialogue.Value = new DialogueForest();
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
					main.LightingManager.EnableDetailGlobalShadowMap.Value = true;
				else
					main.LightingManager.EnableDetailGlobalShadowMap.Value = newZone.DetailedShadows;

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

			result.GetOrCreate<Propagator>("Propagator");

			this.SetMain(result, main);
			WorldFactory.instance = result;
			AkSoundEngine.DefaultGameObject = result;
		}
	}
}
