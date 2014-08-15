using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class VoxelChaseAI : Component<Main>, IUpdateableComponent
	{
		private Random random = new Random();

		private static bool filter(Voxel.State state)
		{
			return state != Components.Voxel.EmptyState && !state.Hard;
		}

		public Property<bool> EnableMovement = new Property<bool> { Value = true };
		public Property<Entity.Handle> Voxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> LastCoord = new Property<Voxel.Coord>();
		public Property<float> Blend = new Property<float>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public ListProperty<Voxel.Coord> History = new ListProperty<Voxel.Coord>();
		public Property<bool> EnablePathfinding = new Property<bool> { Value = true };
		public Property<float> Speed = new Property<float> { Value = 8.0f };

		public bool HasPath
		{
			get
			{
				return this.broadphasePath.Count > 0 || this.narrowphasePath.Count > 0;
			}
		}

		[XmlIgnore]
		public Func<Voxel.State, bool> Filter = VoxelChaseAI.filter;
		[XmlIgnore]
		public Command<Voxel, Voxel.Coord> Moved = new Command<Voxel, Voxel.Coord>();

		public Property<Vector3> Target = new Property<Vector3>();
		public Property<Vector3> Position = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Serialize = true;
		}

		private Stack<Voxel.Box> broadphasePath = new Stack<Voxel.Box>();
		private Stack<Voxel.Coord> narrowphasePath = new Stack<Voxel.Coord>();
		private float lastPathCalculation;
		public void Update(float dt)
		{
			Entity mapEntity = this.Voxel.Value.Target;
			if (mapEntity == null || !mapEntity.Active)
			{
				// Find closest map
				int closest = 5;
				Voxel.Coord newCoord = default(Voxel.Coord);
				foreach (Voxel m in Lemma.Components.Voxel.Voxels)
				{
					Voxel.Coord mCoord = m.GetCoordinate(this.Position);
					Voxel.Coord? c = m.FindClosestFilledCell(mCoord, closest);
					if (c.HasValue)
					{
						mapEntity = m.Entity;
						newCoord = c.Value;
						closest = Math.Min(Math.Abs(mCoord.X - newCoord.X), Math.Min(Math.Abs(mCoord.Y - newCoord.Y), Math.Abs(mCoord.Z - newCoord.Z)));
					}
				}
				if (mapEntity == null)
					this.Delete.Execute();
				else
				{
					this.Voxel.Value = mapEntity;
					this.Coord.Value = this.LastCoord.Value = newCoord;
					this.Blend.Value = 1.0f;
				}
			}
			else
			{
				Voxel m = mapEntity.Get<Voxel>();

				if (this.EnableMovement)
					this.Blend.Value += dt * this.Speed;

				if (this.Blend > 1.0f)
				{
					this.Blend.Value = 0.0f;

					Voxel.Coord c = this.Coord.Value;

					this.Moved.Execute(m, c);

					this.LastCoord.Value = c;

					if (this.EnablePathfinding)
					{
						if (this.broadphasePath.Count == 0 || this.main.TotalTime - this.lastPathCalculation > 1.0f)
						{
							this.lastPathCalculation = this.main.TotalTime;
							Voxel.Coord? targetCoord = m.FindClosestFilledCell(m.GetCoordinate(this.Target));
							if (targetCoord.HasValue)
							{
								this.narrowphasePath.Clear();
								this.broadphasePath.Clear();
								Voxel.Box box = m.GetBox(c);
								VoxelAStar.Broadphase(m, box, targetCoord.Value, this.Filter, this.broadphasePath);
								if (this.broadphasePath.Count > 0)
									this.broadphasePath.Pop(); // First box is the current one
								//this.debugBroadphase(m, this.broadphasePath);
							}
						}

						if (this.narrowphasePath.Count == 0 && this.broadphasePath.Count > 0)
						{
							VoxelAStar.Narrowphase(m, this.Coord, this.broadphasePath.Pop(), this.narrowphasePath);
							if (this.narrowphasePath.Count <= 1)
							{
								this.broadphasePath.Clear();
								this.narrowphasePath.Clear();
								this.Blend.Value = 1.0f;
							}
							else
								this.narrowphasePath.Pop(); // First coordinate is the current one
							//this.debugNarrowphase(m, this.narrowphasePath);
						}

						if (this.narrowphasePath.Count > 0)
						{
							Voxel.Coord newCoord = this.narrowphasePath.Pop();
							if (this.Filter(m[newCoord]))
								this.Coord.Value = newCoord;
							else
							{
								this.broadphasePath.Clear();
								this.narrowphasePath.Clear();
								this.Blend.Value = 1.0f;
							}
						}
					}
				}

				Vector3 last = m.GetAbsolutePosition(this.LastCoord), current = m.GetAbsolutePosition(this.Coord);
				this.Position.Value = Vector3.Lerp(last, current, this.Blend);
			}
		}

		private void debugBroadphase(Voxel m, Stack<Voxel.Box> path)
		{
			int i = 0;
			foreach (Voxel.Box b in path)
			{
				Vector3 start = m.GetRelativePosition(b.X, b.Y, b.Z) - new Vector3(0.1f), end = m.GetRelativePosition(b.X + b.Width, b.Y + b.Height, b.Z + b.Depth) + new Vector3(0.1f);

				Matrix matrix = Matrix.CreateScale(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y), Math.Abs(end.Z - start.Z)) * Matrix.CreateTranslation(new Vector3(-0.5f) + (start + end) * 0.5f);

				ModelAlpha model = new ModelAlpha();
				model.Filename.Value = "AlphaModels\\box";
				model.Color.Value = new Vector3((float)i / (float)path.Count);
				model.Alpha.Value = 1.0f;
				model.IsInstanced.Value = false;
				model.Serialize = false;
				model.DrawOrder.Value = 11; // In front of water
				model.CullBoundingBox.Value = false;
				model.DisableCulling.Value = true;
				model.Add(new Binding<Matrix>(model.Transform, () => matrix * Matrix.CreateTranslation(-m.Offset.Value) * m.Transform, m.Transform, m.Offset));
				this.main.AddComponent(model);

				this.main.AddComponent(new Animation
				(
					new Animation.FloatMoveTo(model.Alpha, 0.0f, 3.0f),
					new Animation.Execute(model.Delete)
				));
				i++;
			}
		}

		private void debugNarrowphase(Voxel m, Stack<Voxel.Coord> path)
		{
			int i = 0;
			foreach (Voxel.Coord b in path)
			{
				Vector3 start = m.GetRelativePosition(b.X, b.Y, b.Z) - new Vector3(0.1f), end = m.GetRelativePosition(b.X + 1, b.Y + 1, b.Z + 1) + new Vector3(0.1f);

				Matrix matrix = Matrix.CreateScale(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y), Math.Abs(end.Z - start.Z)) * Matrix.CreateTranslation(new Vector3(-0.5f) + (start + end) * 0.5f);

				ModelAlpha model = new ModelAlpha();
				model.Filename.Value = "AlphaModels\\box";
				model.Color.Value = new Vector3((float)i / (float)path.Count);
				model.Alpha.Value = 1.0f;
				model.IsInstanced.Value = false;
				model.Serialize = false;
				model.DrawOrder.Value = 11; // In front of water
				model.CullBoundingBox.Value = false;
				model.DisableCulling.Value = true;
				model.Add(new Binding<Matrix>(model.Transform, () => matrix * Matrix.CreateTranslation(-m.Offset.Value) * m.Transform, m.Transform, m.Offset));
				this.main.AddComponent(model);

				this.main.AddComponent(new Animation
				(
					new Animation.FloatMoveTo(model.Alpha, 0.0f, 3.0f),
					new Animation.Execute(model.Delete)
				));
				i++;
			}
		}
	}
}
