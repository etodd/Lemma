using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class World : Component<Main>, IUpdateableComponent
	{
		private static readonly Color defaultBackgroundColor = new Color(16.0f / 255.0f, 26.0f / 255.0f, 38.0f / 255.0f, 1.0f);

		[XmlIgnore]
		public DialogueForest DialogueForest = new DialogueForest();

		public EditorProperty<string> LightRampTexture = new EditorProperty<string> { Value = "Images\\default-ramp" };
		public EditorProperty<string> EnvironmentMap = new EditorProperty<string> { Value = "Images\\env0" };
		public EditorProperty<Vector3> EnvironmentColor = new EditorProperty<Vector3> { Value = Vector3.One };
		public EditorProperty<Color> BackgroundColor = new EditorProperty<Color> { Value = World.defaultBackgroundColor };
		public EditorProperty<float> FarPlaneDistance = new EditorProperty<float> { Value = 100.0f };
		public EditorProperty<Vector3> Gravity = new EditorProperty<Vector3> { Value = new Vector3(0.0f, -18.0f, -0.0f) };

		private Vector3 lastUpdatedCameraPosition = new Vector3(float.MinValue);
		private bool lastFrameUpdated = false;

		[XmlIgnore]
		public Property<Zone> CurrentZone = new Property<Zone>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;

			this.Add(new TwoWayBinding<string>(this.LightRampTexture, this.main.Renderer.LightRampTexture));
			this.Add(new TwoWayBinding<string>(this.EnvironmentMap, this.main.Renderer.EnvironmentMap));
			this.Add(new TwoWayBinding<Vector3>(this.EnvironmentColor, this.main.Renderer.EnvironmentColor));
			this.Add(new TwoWayBinding<Color>(this.BackgroundColor, this.main.Renderer.BackgroundColor));
			this.Add(new TwoWayBinding<float>(this.FarPlaneDistance, this.main.Camera.FarPlaneDistance));

			this.Gravity.Set = delegate(Vector3 value)
			{
				main.Space.ForceUpdater.Gravity = value;
			};
			this.Gravity.Get = delegate()
			{
				return main.Space.ForceUpdater.Gravity;
			};

			this.Add(new CommandBinding<Entity>(this.main.EntityAdded, delegate(Entity e)
			{
				if (!e.CannotSuspend)
					processEntity(e, this.CurrentZone, getActiveBoundingBoxes(main.Camera, this.CurrentZone), main.Camera.Position, main.Camera.FarPlaneDistance);
			}));
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

		private static void processMap(Voxel map, IEnumerable<NonAxisAlignedBoundingBox> boxes)
		{
			foreach (Voxel.Chunk chunk in map.Chunks)
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
			Voxel map = entity.Get<Voxel>();
			if (map != null && !typeof(DynamicVoxel).IsAssignableFrom(map.GetType()))
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

		private void updateZones(Zone newZone)
		{
			this.CurrentZone.Value = newZone;

			if (newZone == null)
				main.LightingManager.EnableDetailGlobalShadowMap.Value = true;
			else
				main.LightingManager.EnableDetailGlobalShadowMap.Value = newZone.DetailedShadows;

			IEnumerable<NonAxisAlignedBoundingBox> boxes = getActiveBoundingBoxes(main.Camera, newZone);
			Vector3 cameraPosition = main.Camera.Position;
			float suspendDistance = main.Camera.FarPlaneDistance;
			foreach (Entity e in main.Entities)
			{
				if (!e.CannotSuspend)
					processEntity(e, newZone, boxes, cameraPosition, suspendDistance);
			}

			this.lastUpdatedCameraPosition = main.Camera.Position;
		}

		public void UpdateZones()
		{
			this.updateZones(Zone.Get(this.main.Camera.Position));
		}

		public void Update(float dt)
		{
			// Update every other frame
			if (this.lastFrameUpdated)
			{
				this.lastFrameUpdated = false;
				return;
			}
			this.lastFrameUpdated = true;

			Zone newZone = Zone.Get(this.main.Camera.Position);

			if (newZone != this.CurrentZone.Value || (newZone == null && (this.main.Camera.Position - this.lastUpdatedCameraPosition).Length() > 10.0f))
				this.updateZones(newZone);
		}
	}
}
