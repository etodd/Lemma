using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Rotator : Component<Main>, IUpdateableComponent
	{
		public Property<Vector3> Velocity = new Property<Vector3>();
		public ListProperty<Entity.Handle> Targets = new ListProperty<Entity.Handle>();

		public Rotator()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
		}

		public void EditorProperties()
		{
			this.Entity.Add("Velocity", this.Velocity);
		}

		private List<Transform> transforms = new List<Transform>();
		private List<Voxel> voxels = new List<Voxel>();
		public override void Start()
		{
			if (!this.main.EditorEnabled)
			{
				foreach (Entity.Handle handle in this.Targets)
				{
					Entity e = handle.Target;
					if (e != null && e.Active)
					{
						this.transforms.Add(e.Get<Transform>());
						Voxel v = e.Get<Voxel>();
						if (v != null)
							this.voxels.Add(v);
					}
				}
			}
		}

		public void Update(float dt)
		{
			if (this.transforms.Count == 0)
				this.Delete.Execute();
			else
			{
				Vector3 v = this.Velocity.Value * dt;
				Quaternion diff = Microsoft.Xna.Framework.Quaternion.CreateFromYawPitchRoll(v.Y, v.X, v.Z);
				for (int i = 0; i < this.transforms.Count; i++)
				{
					Transform t = this.transforms[i];
					if (t.Active)
						t.Quaternion.Value *= diff;
					else
					{
						this.transforms.RemoveAt(i);
						i--;
					}
				}

				v = this.Velocity;
				for (int i = 0; i < this.voxels.Count; i++)
				{
					Voxel voxel = this.voxels[i];
					if (voxel.Active)
						voxel.AngularVelocity.Value = v;
					else
					{
						this.voxels.RemoveAt(i);
						i--;
					}
				}
			}
		}
	}
}
