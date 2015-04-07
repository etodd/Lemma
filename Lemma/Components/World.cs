using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class World : Component<Main>, IUpdateableComponent, IGraphicsComponent
	{
		public static readonly Color DefaultBackgroundColor = new Color(16.0f / 255.0f, 26.0f / 255.0f, 38.0f / 255.0f, 1.0f);

		[XmlIgnore]
		public DialogueForest DialogueForest = new DialogueForest();

		public Property<string> LightRampTexture = new Property<string> { Value = "LightRamps\\default" };
		public Property<string> EnvironmentMap = new Property<string> { Value = "EnvironmentMaps\\env0" };
		public Property<Vector3> EnvironmentColor = new Property<Vector3> { Value = Vector3.One };
		public Property<Color> BackgroundColor = new Property<Color> { Value = World.DefaultBackgroundColor };
		public Property<float> FarPlaneDistance = new Property<float> { Value = 100.0f };
		public Property<Vector3> Gravity = new Property<Vector3> { Value = new Vector3(0.0f, -18.0f, -0.0f) };
		public Property<string> UUID = new Property<string>();
		public Property<Entity.Handle> ThumbnailCamera = new Property<Entity.Handle>();
		public Property<string> OverlayTexture = new Property<string>();
		public Property<float> OverlayTiling = new Property<float> { Value = 1.0f };
		public Property<float> CameraShakeAmount = new Property<float>();

		[XmlIgnore]
		public Property<Texture2D> OverlayTextureHandle = new Property<Texture2D>();

		private Vector3 lastUpdatedCameraPosition = new Vector3(float.MinValue);
		private bool lastFrameUpdated = false;

		[XmlIgnore]
		public Property<Zone> CurrentZone = new Property<Zone>();

		public void NewUUID()
		{
			this.UUID.Value = Guid.NewGuid().ToString().Replace("-", string.Empty);
		}

		public void LoadContent(bool reload)
		{
			if (reload)
				this.OverlayTexture.Changed();
		}

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;

			if (string.IsNullOrEmpty(this.UUID))
				this.NewUUID();

			this.Add(new Binding<Texture2D, string>(this.OverlayTextureHandle, file => file == null ? (Texture2D)null : this.main.MapContent.Load<Texture2D>(file), this.OverlayTexture));
			this.Add(new Binding<string>(this.main.Renderer.LightRampTexture, this.LightRampTexture));
			this.Add(new Binding<string>(this.main.LightingManager.EnvironmentMap, this.EnvironmentMap));
			this.Add(new Binding<Vector3>(this.main.LightingManager.EnvironmentColor, this.EnvironmentColor));
			this.Add(new Binding<Color>(this.main.LightingManager.BackgroundColor, this.BackgroundColor));
			this.Add(new Binding<float>(this.main.Camera.FarPlaneDistance, this.FarPlaneDistance));

			this.Add(new SetBinding<Vector3>(this.Gravity, delegate(Vector3 value)
			{
				this.main.Space.ForceUpdater.Gravity = value;
			}));

			this.Add(new CommandBinding<Entity>(this.main.EntityAdded, delegate(Entity e)
			{
				if (!e.CannotSuspend)
					processEntity(e, this.CurrentZone, getActiveBoundingBoxes(this.main.Camera, this.CurrentZone), this.main.Camera.Position, this.main.Camera.FarPlaneDistance);
			}));
		}

		public override void Start()
		{
			if (PlayerDataFactory.Instance == null && !this.main.EditorEnabled)
				this.main.Add(Factory.Get<PlayerDataFactory>().CreateAndBind(main));
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
			for (int i = 0; i < map.Chunks.Count; i++)
			{
				Voxel.Chunk chunk = map.Chunks[i];
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
							for (int i = 0; i < Zone.Zones.Count; i++)
							{
								Zone z = Zone.Zones[i];
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
				this.main.LightingManager.EnableDetailGlobalShadowMap.Value = true;
			else
				this.main.LightingManager.EnableDetailGlobalShadowMap.Value = newZone.DetailedShadows;

			IEnumerable<NonAxisAlignedBoundingBox> boxes = getActiveBoundingBoxes(this.main.Camera, newZone);
			Vector3 cameraPosition = this.main.Camera.Position;
			float suspendDistance = this.main.Camera.FarPlaneDistance;
			for (int i = 0; i < this.main.Entities.Count; i++)
			{
				Entity e = this.main.Entities[i];
				if (!e.CannotSuspend)
					processEntity(e, newZone, boxes, cameraPosition, suspendDistance);
			}

			this.lastUpdatedCameraPosition = this.main.Camera.Position;
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
