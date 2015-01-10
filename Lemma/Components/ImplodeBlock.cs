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

		public Rift.Style Type;

		public Voxel.t StateId;
		public float Lifetime;
		public bool DoScale;
		public Vector3 StartPosition;
		public Vector3 EndPosition;
		public Quaternion StartOrientation;
		public Quaternion EndOrientation;

		public Property<Matrix> Transform = new Property<Matrix>();
		public Property<Vector3> Offset = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
		}

		public void Update(float dt)
		{
			this.Lifetime += dt;

			float blend = this.Lifetime / (this.Type == Rift.Style.In ? totalLifetimeIn : totalLifetimeUp);

			if (blend > 1.0f)
				this.Delete.Execute();
			else
			{
				Matrix result = Matrix.CreateFromQuaternion(Quaternion.Lerp(this.StartOrientation, this.EndOrientation, blend));
				float scale = 1.0f - blend;
				result.Right *= scale;
				result.Up *= scale;
				result.Forward *= scale;
				result.Translation = Vector3.Lerp(this.StartPosition, this.EndPosition, blend);
				this.Transform.Value = result;
			}
		}
	}
}