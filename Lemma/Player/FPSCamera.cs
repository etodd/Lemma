using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;
using Lemma.Util;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class FPSCamera : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Property<Vector2> Mouse = new Property<Vector2>();
		[XmlIgnore]
		public Property<Vector2> Movement = new Property<Vector2>();
		[XmlIgnore]
		public Property<bool> Up = new Property<bool>();
		[XmlIgnore]
		public Property<bool> Down = new Property<bool>();
		[XmlIgnore]
		public Property<bool> SpeedMode = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Serialize = false;
			this.Add(new CommandBinding(this.Enable, delegate()
			{
				this.main.Renderer.SpeedBlurAmount.Value = 0;
			}));
		}

		public void Update(float dt)
		{
			Vector2 movement2 = this.Movement;
			Vector3 movement3 = Vector3.TransformNormal(new Vector3(movement2.X, 0, -movement2.Y), this.main.Camera.RotationMatrix);
			if (this.Up)
				movement3 = movement3.SetComponent(Direction.PositiveY, 1.0f);
			else if (this.Down)
				movement3 = movement3.SetComponent(Direction.NegativeY, 1.0f);
			this.main.Camera.Position.Value += movement3 * (this.SpeedMode ? 30.0f : 15.0f) * dt;

			Vector2 mouse = this.Mouse;
			this.main.Camera.Angles.Value = new Vector3(-mouse.Y, mouse.X, 0.0f);
		}
	}
}