using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Smoke : Component<Main>, IUpdateableComponent
	{
		public Property<Vector3> Velocity = new Property<Vector3>();
		public Property<float> Lifetime = new Property<float>();

		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
		}

		public void Update(float dt)
		{
			this.Lifetime.Value += dt;
			if (this.Lifetime > 1.0f)
				this.Delete.Execute();
			else
			{
				this.Velocity.Value += new Vector3(0, dt * -11.0f, 0);
				this.Position.Value += this.Velocity.Value * dt;
			}
		}
	}
}
