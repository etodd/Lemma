using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Rift : Component<Main>, IUpdateableComponent
	{
		public enum Style
		{
			In, Up
		}

		private const float damageTime = 2.0f; // How long the player can stand in a rift before they die
		private const float interval = 0.015f; // A coordinate is emptied every x seconds
		private const int batch = 8; // Empty coordinates in batches
		private const float batchInterval = interval * batch;
		private const float particleInterval = 0.015f; // A particle is emitted every x seconds
		private const float inSoundInterval = 0.25f; // A sound is played every x seconds (In style)
		public Property<int> Radius = new Property<int> { Value = 10 };
		public Property<float> CurrentRadius = new Property<float>();
		public Property<int> CurrentIndex = new Property<int>();
		public Property<Entity.Handle> Voxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coordinate = new Property<Voxel.Coord>();
		public Property<Vector3> Position = new Property<Vector3>();
		public ListProperty<Voxel.Coord> Coords = new ListProperty<Voxel.Coord>();
		public Property<Style> Type = new Property<Style>();

		private Voxel voxel;
		private float intervalTimer;
		private float particleIntervalTimer;
		private float soundIntervalTimer;
		private ParticleSystem particles;

		private static List<VoxelFill.CoordinateEntry> coordSortCache = new List<VoxelFill.CoordinateEntry>();
		private static List<Rift> rifts = new List<Rift>();

		public override void Awake()
		{
			base.Awake();
			Sound.AttachTracker(this.Entity, this.Position);
			this.particles = ParticleSystem.Get(this.main, "Rift");
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Add(new CommandBinding(this.Enable, (Action)this.go));
			rifts.Add(this);
		}

		private void go()
		{
			if (this.Coords.Length == 0)
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_RIFT_OPEN, this.Entity);
				if (PlayerFactory.Instance != null)
					PlayerFactory.Instance.Get<CameraController>().Shake.Execute(this.Position, 30.0f);
				Entity voxelEntity = this.Voxel.Value.Target;
				if (voxelEntity != null && voxelEntity.Active)
				{
					Voxel v = voxelEntity.Get<Voxel>();
					Voxel.Coord center = this.Coordinate;
					Vector3 pos = v.GetRelativePosition(center);
					int radius = this.Radius;
					List<VoxelFill.CoordinateEntry> coords = Rift.coordSortCache;
					for (Voxel.Coord x = center.Move(Direction.NegativeX, radius); x.X < center.X + radius; x.X++)
					{
						for (Voxel.Coord y = x.Move(Direction.NegativeY, radius); y.Y < center.Y + radius; y.Y++)
						{
							for (Voxel.Coord z = y.Move(Direction.NegativeZ, radius); z.Z < center.Z + radius; z.Z++)
							{
								float distance = (pos - v.GetRelativePosition(z)).Length();
								if (distance <= radius && v[z] != Components.Voxel.States.Empty)
									coords.Add(new VoxelFill.CoordinateEntry { Coord = z.Clone(), Distance = distance });
							}
						}
					}
					if (coords.Count == 0)
						this.Entity.Delete.Execute();
					else
					{
						coords.Sort(new LambdaComparer<VoxelFill.CoordinateEntry>((x, y) => x.Distance.CompareTo(y.Distance)));
						this.Coords.AddAll(coords.Select(x => x.Coord));
						coords.Clear();
					}
				}
			}
		}

		public override void delete()
		{
			base.delete();
			rifts.Remove(this);
		}

		public static Rift Query(Vector3 pos)
		{
			for (int i = 0; i < rifts.Count; i++)
			{
				Rift r = rifts[i];
				if ((pos - r.Position).Length() < r.CurrentRadius)
					return r;
			}
			return null;
		}

		private ImplodeBlockFactory blockFactory = Factory.Get<ImplodeBlockFactory>();

		private List<Voxel.Coord> removals = new List<Voxel.Coord>();
		private Random random = new Random();
		public void Update(float dt)
		{
			if (this.Coords.Length == 0)
				this.go();
			else if (this.CurrentIndex < this.Coords.Length)
			{
				if (this.voxel == null)
				{
					Entity v = this.Voxel.Value.Target;
					if (v != null && v.Active)
						this.voxel = v.Get<Voxel>();
				}
				else if (this.voxel.Active)
				{
					this.intervalTimer += dt;

					if (this.Type == Style.In)
					{
						this.particleIntervalTimer += dt;
						while (this.particleIntervalTimer > particleInterval)
						{
							this.particleIntervalTimer -= particleInterval;
							this.particles.AddParticle(this.Position, Vector3.Zero, -1.0f, this.CurrentRadius * 2.0f);
						}
					}

					this.soundIntervalTimer += dt;
					float soundInterval = this.Type == Style.In ? inSoundInterval : 0.35f + (float)random.NextDouble() * 0.35f;
					while (this.soundIntervalTimer > soundInterval)
					{
						this.soundIntervalTimer -= soundInterval;
						AkSoundEngine.PostEvent(this.Type == Style.In ? AK.EVENTS.PLAY_RIFT : AK.EVENTS.PLAY_RIFT_BLOCK, this.Entity);
					}

					bool regenerate = false;
					if (this.intervalTimer > batchInterval)
					{
						for (int i = 0; i < batch && this.CurrentIndex < this.Coords.Length; i++)
						{
							Voxel.Coord c = this.Coords[this.CurrentIndex];
							if ((c.Data = this.voxel[c]) != Components.Voxel.States.Empty)
							{
								regenerate = true;
								this.removals.Add(c);
							}
							this.CurrentIndex.Value++;
						}
					}

					if (regenerate)
					{
						this.intervalTimer -= batchInterval;
						this.voxel.Empty(this.removals, true, true);
						foreach (Voxel.Coord c in this.removals)
						{
							if (this.Type == Style.In)
								this.blockFactory.Implode(main, this.voxel, c, c.Data, this.Position);
							else
								this.blockFactory.BlowAway(main, this.voxel, c, c.Data);
						}
						this.removals.Clear();
						this.voxel.Regenerate();
					}

					this.CurrentRadius.Value = (this.voxel.GetRelativePosition(this.Coords[Math.Max(0, this.CurrentIndex - 1)]) - this.voxel.GetRelativePosition(this.Coordinate)).Length();
				}
				else
					this.Entity.Delete.Execute();
			}
			else
				this.Entity.Delete.Execute();

			if (this.Type.Value == Style.In)
			{
				Entity player = PlayerFactory.Instance;
				if (player != null && (player.Get<Transform>().Position.Value - this.Position.Value).Length() < this.CurrentRadius)
					player.Get<Agent>().Damage.Execute(dt * damageTime);
			}
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Rift rift = entity.Get<Rift>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3, int>(model.Scale, x => new Vector3(x), rift.Radius));
			model.Serialize = false;
			model.DrawOrder.Value = 11; // In front of water
			model.Add(new Binding<bool>(model.Enabled, entity.EditorSelected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), rift.Position));
		}
	}
}