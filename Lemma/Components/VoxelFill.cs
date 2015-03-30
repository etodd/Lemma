using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	[XmlInclude(typeof(CoordinateEntry))]
	[XmlInclude(typeof(Property<CoordinateEntry>))]
	[XmlInclude(typeof(ListProperty<CoordinateEntry>))]
	public class VoxelFill : Component<Main>, IUpdateableComponent
	{
		public class CoordinateEntry
		{
			public Voxel.Coord Coord;
			public Vector3 Position;
			public float Distance;
		}

		public Property<Entity.Handle> Target = new Property<Entity.Handle>();

		public Property<float> IntervalMultiplier = new Property<float> { Value = 0.1f };

		public Property<int> Index = new Property<int>();

		public Property<float> BlockLifetime = new Property<float> { Value = 0.5f };

		public ListProperty<CoordinateEntry> Coords = new ListProperty<CoordinateEntry>();

		// Output properties
		[XmlIgnore]
		public Property<Vector3> RumblePosition = new Property<Vector3>();

		private float intervalTimer;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.EnabledInEditMode = false;

			if (this.main.EditorEnabled)
				this.Coords.Clear();

			this.Add(new CommandBinding(this.Enable, delegate()
			{
				if (this.Index > 0)
					return; // We're already filling

				this.populateCoords();
				this.sortCoords();
			}));
		}

		public override void delete()
		{
			base.delete();
			if (this.rumbling)
				AkSoundEngine.PostEvent(AK.EVENTS.STOP_BLOCK_RUMBLE, this.Entity);
		}

		public override void Start()
		{
			if (!this.main.EditorEnabled)
				this.populateCoords();
		}

		private void populateCoords()
		{
			if (this.Coords.Length == 0)
			{
				Voxel map = this.Entity.Get<Voxel>();
				Entity targetEntity = this.Target.Value.Target;
				if (targetEntity != null && targetEntity.Active)
				{
					Voxel m = targetEntity.Get<Voxel>();
					lock (map.MutationLock)
					{
						foreach (CoordinateEntry e in map.Chunks.SelectMany(c => c.Boxes.SelectMany(x => x.GetCoords())).Select(delegate(Voxel.Coord y)
						{
							Voxel.Coord z = m.GetCoordinate(map.GetAbsolutePosition(y));
							z.Data = y.Data;
							return new CoordinateEntry { Coord = z, };
						}))
						{
							this.Coords.Add(e);
						}
					}
				}
			}
		}

		private static List<CoordinateEntry> coordSortCache = new List<CoordinateEntry>();

		private void sortCoords()
		{
			Entity targetEntity = this.Target.Value.Target;
			if (targetEntity != null && targetEntity.Active)
			{
				Voxel m = targetEntity.Get<Voxel>();
				Vector3 focusPoint = this.main.Camera.Position;
				foreach (CoordinateEntry entry in this.Coords)
				{
					entry.Position = m.GetAbsolutePosition(entry.Coord);
					entry.Distance = (focusPoint - entry.Position).LengthSquared();
				}

				List<CoordinateEntry> coordList = VoxelFill.coordSortCache;
				coordList.AddRange(this.Coords);
				this.Coords.Clear();
				coordList.Sort(new LambdaComparer<CoordinateEntry>((x, y) => x.Distance.CompareTo(y.Distance)));
				foreach (CoordinateEntry e in coordList)
					this.Coords.Add(e);
				coordList.Clear();
			}
		}

		private bool rumbling;
		private Vector3 rumbleSum;
		private const int rumbleCount = 50; // Average the last X block positions to get the position of the rumble
		public void Update(float dt)
		{
			intervalTimer += dt;
			Entity targetEntity = this.Target.Value.Target;
			if (targetEntity != null && targetEntity.Active && this.Index < this.Coords.Length)
			{
				EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
				Voxel m = targetEntity.Get<Voxel>();

				float interval = 0.03f * this.IntervalMultiplier;

				while (intervalTimer > interval && this.Index < this.Coords.Length)
				{
					CoordinateEntry entry = this.Coords[this.Index];
					Entity blockEntity = factory.CreateAndBind(main);
					EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
					entry.Coord.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
					effectBlock.CheckAdjacent = false;
					effectBlock.Offset.Value = m.GetRelativePosition(entry.Coord);
					effectBlock.DoScale = true;

					effectBlock.StartPosition = entry.Position + new Vector3(8.0f, 20.0f, 8.0f) * this.BlockLifetime.Value;
					effectBlock.StartOrientation = Quaternion.CreateFromYawPitchRoll(0.15f * this.Index, 0.15f * this.Index, 0);

					effectBlock.TotalLifetime = this.BlockLifetime;
					effectBlock.Setup(targetEntity, entry.Coord, entry.Coord.Data.ID);
					main.Add(blockEntity);

					this.rumbleSum += entry.Position;
					int lastIndex = this.Index - rumbleCount;
					if (lastIndex >= 0)
						this.rumbleSum -= this.Coords[lastIndex].Position;

					this.Index.Value++;
					intervalTimer -= interval;
				}
				if (this.Coords.Count > 200)
				{
					this.RumblePosition.Value = this.rumbleSum / Math.Min(this.Index + 1, rumbleCount);
					if (!this.rumbling)
					{
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_BLOCK_RUMBLE, this.Entity);
						this.rumbling = true;
					}
				}
			}
			else
				this.Delete.Execute();
		}
	}
}
