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
	public class CameraController : Component<Main>, IUpdateableComponent
	{
		private Noise3D noise;

		private float shakeTime = 0.0f;
		private const float totalShakeTime = 0.5f;
		private float shakeAmount;

		// Input commands
		[XmlIgnore]
		public Command<Vector3, float> Shake = new Command<Vector3, float>();

		// Input properties
		[XmlIgnore]
		public Property<float> BaseCameraShakeAmount = new Property<float>();
		public Property<float> CameraShakeAmount = new Property<float>();
		[XmlIgnore]
		public Property<Matrix> CameraBone = new Property<Matrix>();
		[XmlIgnore]
		public Property<Matrix> HeadBone = new Property<Matrix>();
		[XmlIgnore]
		public Property<Matrix> ModelTransform = new Property<Matrix>();
		[XmlIgnore]
		public Property<Vector2> Mouse = new Property<Vector2>();
		[XmlIgnore]
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		[XmlIgnore]
		public Property<float> MaxSpeed = new Property<float>();
		[XmlIgnore]
		public Property<bool> ThirdPerson = new Property<bool>();
		[XmlIgnore]
		public Property<float> Lean = new Property<float>();
		[XmlIgnore]
		public Vector3 Offset;

		// Output properties
		public Property<float> TotalCameraShake = new Property<float>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Serialize = false;
			this.noise = new Noise3D();
			this.Add(new CommandBinding(this.Disable, delegate()
			{
				this.main.Renderer.SpeedBlurAmount.Value = 0;
			}));

			this.Shake.Action = delegate(Vector3 pos, float size)
			{
				float targetShake = Math.Max(0.0f, 1.0f - ((pos - main.Camera.Position).Length() / size));
				this.shakeAmount = Math.Max(this.shakeAmount * this.blendShake(), targetShake);
				this.shakeTime = totalShakeTime;
			};
		}

		private float blendShake() // hehe
		{
			float x = this.shakeTime / CameraController.totalShakeTime;
			// x starts at 1 and goes down to 0
			if (x > 0.75f)
				return 1.0f - ((x - 0.75f) / 0.25f);
			else
				return x / 0.75f;
		}

		public void Update(float dt)
		{
			Vector2 mouse = this.Mouse;
			Vector3 shake = Vector3.Zero;
			float finalShakeAmount = this.BaseCameraShakeAmount + this.CameraShakeAmount;
			if (this.shakeTime > 0.0f)
			{
				finalShakeAmount += this.shakeAmount * this.blendShake();
				this.shakeTime -= dt;
			}
			else
				this.shakeAmount = 0.0f;

			this.TotalCameraShake.Value = finalShakeAmount;
			if (finalShakeAmount > 0.0f)
			{
				float offset = main.TotalTime * 15.0f;
				shake = new Vector3(this.noise.Sample(new Vector3(offset)), this.noise.Sample(new Vector3(offset + 64)), noise.Sample(new Vector3(offset + 128))) * finalShakeAmount * 0.3f;
			}

			Vector3 cameraPosition = Vector3.Transform(this.Offset, Matrix.CreateRotationY((float)Math.PI) * this.HeadBone.Value * this.ModelTransform);

			if (this.ThirdPerson)
			{
				cameraPosition = Vector3.Transform(new Vector3(0.0f, 3.0f, 0.0f), this.ModelTransform);

				main.Camera.Angles.Value = new Vector3(-mouse.Y + shake.X, mouse.X + (float)Math.PI * 1.0f + shake.Y, shake.Z);

				Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(cameraPosition, -main.Camera.Forward.Value, 5.0f);

				float cameraDistance = 4.0f;
				if (hit.Voxel != null)
					cameraDistance = (hit.Position - cameraPosition).Length() - 1.0f;
				main.Camera.Position.Value = cameraPosition + (main.Camera.Right.Value * cameraDistance * -0.25f) + (main.Camera.Forward.Value * -cameraDistance);
			}
			else
			{
				Matrix cameraOrientation = this.CameraBone.Value * Matrix.CreateRotationY(mouse.X + shake.X);

				main.Camera.Position.Value = cameraPosition;

				Matrix rot = Matrix.Identity;
				rot.Forward = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 1.0f, 0), cameraOrientation));
				rot.Up = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0.0f, 0, 1.0f), cameraOrientation));
				rot.Right = Vector3.Normalize(Vector3.Cross(rot.Forward, rot.Up));

				main.Camera.RotationMatrix.Value = rot * Matrix.CreateFromAxisAngle(rot.Forward, shake.Z + this.Lean) * Matrix.CreateFromAxisAngle(rot.Right, -mouse.Y + shake.Y);
			}

			float minBlur = 4.0f;
			float maxBlur = this.MaxSpeed.Value + 2.0f;
			float speed = Math.Abs(Vector3.Dot(this.LinearVelocity.Value, main.Camera.Forward));
			main.Renderer.SpeedBlurAmount.Value = Math.Min(1.0f, Math.Max(0.0f, (speed - minBlur) / (maxBlur - minBlur)));
		}
	}
}
