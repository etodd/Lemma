using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class RaycastAI : Component<Main>
	{
		public Property<Entity.Handle> Voxel = new Property<Entity.Handle> { Editable = false };
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord> { Editable = false };
		public Property<Entity.Handle> LastVoxel = new Property<Entity.Handle> { Editable = false };
		public Property<Voxel.Coord> LastCoord = new Property<Voxel.Coord> { Editable = false };
		public Property<Direction> Normal = new Property<Direction> { Editable = false };
		public Property<float> Blend = new Property<float> { Editable = false, Value = 1.0f };
		public Property<float> BlendTime = new Property<float> { Editable = false, Value = 0.1f };
		public Property<float> MovementDistance = new Property<float> { Editable = false, Value = 15.0f };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<Matrix> Orientation = new Property<Matrix> { Editable = false };

		private Random random = new Random();

		public void Update()
		{
			Entity mapEntity = this.Voxel.Value.Target;
			if (mapEntity != null && mapEntity.Active)
			{
				Voxel currentMap = mapEntity.Get<Voxel>();
				Vector3 currentPosition = currentMap.GetAbsolutePosition(this.Coord);
				Entity lastMapEntity = this.LastVoxel.Value.Target;
				if (this.Blend < 1.0f && lastMapEntity != null && lastMapEntity.Active)
				{
					Voxel lastM = lastMapEntity.Get<Voxel>();
					this.Position.Value = Vector3.Lerp(lastM.GetAbsolutePosition(this.LastCoord), currentPosition, this.Blend);
					this.Orientation.Value = Matrix.Lerp(lastM.Transform, currentMap.Transform, this.Blend);
				}
				else
				{
					this.Position.Value = currentPosition;
					this.Orientation.Value = currentMap.Transform;
				}
				this.Blend.Value += this.main.ElapsedTime.Value / this.BlendTime;
			}
			else
				this.Voxel.Value = null;
		}

		public void MoveTo(Voxel.Coord coord, Voxel map = null)
		{
			this.LastCoord.Value = this.Coord;
			this.LastVoxel.Value = this.Voxel;
			if (map != null)
				this.Voxel.Value = map.Entity;
			this.Coord.Value = coord;
			this.Blend.Value = 0.0f;
		}

		public void Move(Vector3 dir)
		{
			float dirLength = dir.Length();
			if (dirLength == 0.0f)
				return; // We're already where we need to be
			else
				dir /= dirLength; // Normalize

			Vector3 pos;

			if (this.Voxel.Value.Target != null && this.Voxel.Value.Target.Active)
			{
				Voxel m = this.Voxel.Value.Target.Get<Voxel>();
				Voxel.Coord adjacent = this.Coord.Value.Move(this.Normal);
				if (m[adjacent].ID == 0)
					pos = m.GetAbsolutePosition(adjacent);
				else
					pos = this.Position;
			}
			else
				pos = this.Position;

			float radius = 0.0f;
			const int attempts = 20;

			Vector3 up = Vector3.Up;
			if ((float)Math.Abs(dir.Y) == 1.0f)
				up = Vector3.Right;

			Matrix mat = Matrix.Identity;
			mat.Forward = -dir;
			mat.Right = Vector3.Cross(dir, up);
			mat.Up = Vector3.Cross(mat.Right, dir);
			
			for (int i = 0; i < attempts; i++)
			{
				float x = ((float)Math.PI * 0.5f) + (((float)this.random.NextDouble() * 2.0f) - 1.0f) * radius;
				float y = (((float)this.random.NextDouble() * 2.0f) - 1.0f) * radius;
				Vector3 ray = new Vector3((float)Math.Cos(x) * (float)Math.Cos(y), (float)Math.Sin(y), (float)Math.Sin(x) * (float)Math.Cos(y));
				Voxel.GlobalRaycastResult hit = Lemma.Components.Voxel.GlobalRaycast(pos, Vector3.TransformNormal(ray, mat), this.MovementDistance);
				if (hit.Voxel != null && hit.Distance > 2.0f && hit.Coordinate.Value.Data.ID != Components.Voxel.t.AvoidAI)
				{
					bool skip = false;
					foreach (Water w in Water.ActiveInstances)
					{
						if (w.Fluid.BoundingBox.Contains(hit.Position) != ContainmentType.Disjoint)
						{
							skip = true;
							break;
						}
					}

					if (skip)
						break;

					Voxel.Coord newCoord = hit.Coordinate.Value.Move(hit.Normal);
					if (hit.Voxel[newCoord].ID == 0)
					{
						this.LastCoord.Value = this.Coord;
						this.Coord.Value = newCoord;
						this.LastVoxel.Value = this.Voxel;
						this.Voxel.Value = hit.Voxel.Entity;
						this.Normal.Value = hit.Normal;
						this.Blend.Value = 0.0f;
						break;
					}
				}
				radius += (float)Math.PI * 2.0f / (float)attempts;
			}
		}
	}
}
