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

		public void EditorProperties()
		{
			this.Entity.Add("Velocity", this.Velocity);
		}

		private List<Transform> transforms = new List<Transform>();
		public override void Start()
		{
			foreach (Entity.Handle handle in this.Targets)
			{
				Entity e = handle.Target;
				if (e != null && e.Active)
					transforms.Add(e.Get<Transform>());
			}
		}

		public void Update(float dt)
		{
			if (this.transforms.Count == 0)
				this.Delete.Execute();
			else
			{
				Vector3 v = this.Velocity.Value * dt;
				Quaternion diff = Microsoft.Xna.Framework.Quaternion.CreateFromYawPitchRoll(v.X, v.Y, v.Z);
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
			}
		}
	}
}
