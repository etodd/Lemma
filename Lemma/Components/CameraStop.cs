using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class CameraStop : Component<Main>
	{
		public Property<float> Offset = new Property<float>();

		public Property<Entity.Handle> Next = new Property<Entity.Handle>();

		public Property<Animation.Ease.EaseType> Blend = new Property<Animation.Ease.EaseType>();

		public Property<float> Duration = new Property<float>();

		[XmlIgnore]
		public Command OnDone = new Command();

		[XmlIgnore]
		public Command Go = new Command();

		public override void Awake()
		{
			base.Awake();
			this.Go.Action = (Action)this.animate;
		}

		private void animate()
		{
			if (PlayerFactory.Instance != null)
			{
				CameraController cameraController = PlayerFactory.Instance.Get<CameraController>();
				cameraController.ThirdPerson.Value = true;
				cameraController.Enabled.Value = false;
				PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = false;
			}

			List<Animation.Interval> animations = new List<Animation.Interval>();
			Entity current = this.Entity;
			while (current != null)
			{
				CameraStop currentStop = current.Get<CameraStop>();
				Transform currentTransform = current.Get<Transform>();

				animations.Add(new Animation.Set<Vector3>(main.Camera.Position, Vector3.Transform(new Vector3(0, 0, currentStop.Offset), currentTransform.Matrix)));
				animations.Add(new Animation.Set<Matrix>(main.Camera.RotationMatrix, currentTransform.Orientation));

				Entity next = currentStop.Next.Value.Target;
				CameraStop nextStop = next == null ? null : next.Get<CameraStop>();

				if (currentStop.Blend != Animation.Ease.EaseType.None && next != null)
				{
					Transform nextTransform = next.Get<Transform>();
					animations.Add
					(
						new Animation.Ease
						(
							new Animation.Custom
							(
								delegate(float x)
								{
									Quaternion q = Quaternion.Lerp(currentTransform.Quaternion, nextTransform.Quaternion, x);
									float offset = MathHelper.Lerp(currentStop.Offset, nextStop.Offset, x);
									Vector3 pos = Vector3.Lerp(currentTransform.Position, nextTransform.Position, x);

									Matrix rotationMatrix = Matrix.CreateFromQuaternion(q);
									main.Camera.RotationMatrix.Value = rotationMatrix;
									Matrix m = rotationMatrix * Matrix.CreateTranslation(pos);
									main.Camera.Position.Value = Vector3.Transform(new Vector3(0, 0, offset), m);
								},
								currentStop.Duration
							),
							currentStop.Blend
						)
					);
				}
				else
				{
					animations.Add
					(
						new Animation.Custom
						(
							delegate(float x)
							{
								main.Camera.RotationMatrix.Value = currentTransform.Orientation;
								main.Camera.Position.Value = Vector3.Transform(new Vector3(0, 0, currentStop.Offset), currentTransform.Matrix);
							},
							currentStop.Duration
						)
					);
				}

				animations.Add(new Animation.Execute(currentStop.OnDone));

				current = next;
			}
			Animation anim = new Animation(animations.ToArray());
			anim.EnabledWhenPaused = false;
			WorldFactory.Instance.Add(anim);
		}
	}
}
