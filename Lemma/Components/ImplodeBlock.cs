using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class ImplodeBlock : Component<Main>, IUpdateableComponent
	{
		private const float totalLifetimeIn = 0.4f;
		private const float totalLifetimeUp = 2.0f;

		public Property<Rift.Style> Type = new Property<Rift.Style>();

		public Property<float> Lifetime = new Property<float>();
		public Property<bool> DoScale = new Property<bool>();
		public Property<Vector3> StartPosition = new Property<Vector3>();
		public Property<Vector3> EndPosition = new Property<Vector3>();
		public Property<Matrix> StartOrientation = new Property<Matrix>();
		public Property<Matrix> EndOrientation = new Property<Matrix>();

		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<Vector3> Scale = new Property<Vector3>();
		public Property<Vector3> Offset = new Property<Vector3>();

		private Quaternion startQuat = Quaternion.Identity;
		private Quaternion endQuat = Quaternion.Identity;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;

			this.Add(new SetBinding<Vector3>(this.StartPosition, delegate(Vector3 value)
			{
				this.Position.Value = value;
			}));

			this.Add(new SetBinding<Matrix>(this.StartOrientation, delegate(Matrix value)
			{
				this.startQuat = Quaternion.CreateFromRotationMatrix(this.StartOrientation);
				this.Orientation.Value = this.startQuat;
			}));

			this.Add(new SetBinding<Matrix>(this.EndOrientation, delegate(Matrix value)
			{
				this.endQuat = Quaternion.CreateFromRotationMatrix(this.EndOrientation);
			}));
		}

		public void Update(float dt)
		{
			this.Lifetime.Value += dt;

			float blend = this.Lifetime / (this.Type == Rift.Style.In ? totalLifetimeIn : totalLifetimeUp);

			if (blend > 1.0f)
				this.Delete.Execute();
			else
			{
				this.Scale.Value = new Vector3(1.0f - blend);
				this.Orientation.Value = Quaternion.Lerp(this.startQuat, this.endQuat, blend);
				this.Position.Value = Vector3.Lerp(this.StartPosition, this.EndPosition, blend);
			}
		}
	}
}
