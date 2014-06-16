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

		public Property<float> IntervalMultiplier = new Property<float> { Value = 1.0f };

		public Property<int> Index = new Property<int>();

		public Property<float> BlockLifetime = new Property<float> { Value = 0.25f };

		public ListProperty<CoordinateEntry> Coords = new ListProperty<CoordinateEntry>();

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

		public override void Start()
		{
			if (!this.main.EditorEnabled)
			{
				bool go = this.Coords.Count == 0;
				this.populateCoords();

				if (this.Enabled && go)
					this.sortCoords();
			}
		}

		private void populateCoords()
		{
			if (this.Coords.Count == 0)
			{
				Voxel map = this.Entity.Get<Voxel>();
				Entity targetEntity = this.Target.Value.Target;
				if (targetEntity != null && targetEntity.Active)
				{
					Voxel m = targetEntity.Get<Voxel>();
					foreach (CoordinateEntry e in map.Chunks.SelectMany(c => c.Boxes.SelectMany(x => x.GetCoords())).Select(delegate(Voxel.Coord y)
					{
						Voxel.Coord z = m.GetCoordinate(map.GetAbsolutePosition(y));
						z.Data = y.Data;
						return new CoordinateEntry { Coord = z, };
					}))
					this.Coords.Add(e);
				}
			}
		}

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

				List<CoordinateEntry> coordList = this.Coords.ToList();
				this.Coords.Clear();
				coordList.Sort(new LambdaComparer<CoordinateEntry>((x, y) => x.Distance.CompareTo(y.Distance)));
				foreach (CoordinateEntry e in coordList)
					this.Coords.Add(e);
			}
		}

		public void Update(float dt)
		{
			intervalTimer += dt;
			Entity targetEntity = this.Target.Value.Target;
			if (targetEntity != null && targetEntity.Active && this.Index < this.Coords.Count)
			{
				float interval = 0.03f * this.IntervalMultiplier;
				while (intervalTimer > interval && this.Index < this.Coords.Count)
				{
					EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
					Voxel m = targetEntity.Get<Voxel>();
					
					CoordinateEntry entry = this.Coords[this.Index];
					Entity blockEntity = factory.CreateAndBind(main);
					EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
					entry.Coord.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
					effectBlock.CheckAdjacent.Value = false;
					effectBlock.Offset.Value = m.GetRelativePosition(entry.Coord);
					effectBlock.DoScale.Value = true;

					effectBlock.StartPosition.Value = entry.Position + new Vector3(8.0f, 20.0f, 8.0f) * this.BlockLifetime.Value;
					effectBlock.StartOrientation.Value = Matrix.CreateRotationX(0.15f * this.Index) * Matrix.CreateRotationY(0.15f * this.Index);

					effectBlock.TotalLifetime.Value = this.BlockLifetime;
					effectBlock.Setup(targetEntity, entry.Coord, entry.Coord.Data.ID);
					main.Add(blockEntity);

					this.Index.Value++;
					intervalTimer -= interval;
				}
			}
			else
				this.Delete.Execute();
		}
	}
}
