using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
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
			bool originallyThirdPerson = false;

			List<Animation.Interval> animations = new List<Animation.Interval>();

			animations.Add(new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.Zero, 0.5f));

			animations.Add(new Animation.Execute(delegate()
			{
				if (PlayerFactory.Instance != null)
				{
					CameraController cameraController = PlayerFactory.Instance.Get<CameraController>();
					originallyThirdPerson = cameraController.ThirdPerson;
					cameraController.ThirdPerson.Value = true;
					cameraController.Enabled.Value = false;
					PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = false;
				}
			}));

			animations.Add(new Animation.Set<Matrix>(this.main.Camera.RotationMatrix, Matrix.CreateFromQuaternion(this.Entity.Get<Transform>().Quaternion)));
			animations.Add(new Animation.Set<Vector3>(this.main.Camera.Position, Vector3.Transform(new Vector3(0, 0, this.Offset), this.Entity.Get<Transform>().Matrix)));

			Animation.Sequence sequence = new Animation.Sequence();
			animations.Add(new Animation.Parallel(sequence, new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f)));

			Animation.Ease.EaseType lastEase = Animation.Ease.EaseType.None;
			BSpline spline = null;
			Entity current = this.Entity;
			while (current != null)
			{
				CameraStop currentStop = current.Get<CameraStop>();
				Transform currentTransform = current.Get<Transform>();

				Entity next = currentStop.Next.Value.Target;
				CameraStop nextStop = next == null ? null : next.Get<CameraStop>();

				if (!lastEase.BlendsInto(currentStop.Blend) || next == null)
				{
					if (spline != null)
						spline.Add(currentTransform.Position, currentTransform.Quaternion, currentStop.Offset);
					spline = new BSpline();
				}

				float currentTime = spline.Duration;
				spline.Duration += currentStop.Duration;

				if (currentStop.Blend != Animation.Ease.EaseType.None && next != null)
				{
					BSpline currentSpline = spline;
					currentSpline.Add(currentTransform.Position, currentTransform.Quaternion, currentStop.Offset);
					Transform nextTransform = next.Get<Transform>();
					sequence.Add
					(
						new Animation.Ease
						(
							new Animation.Custom
							(
								delegate(float x)
								{
									float lerpValue = (currentTime + x * currentStop.Duration) / currentSpline.Duration;
									BSpline.ControlPoint point = currentSpline.Evaluate(lerpValue);
									Matrix rotationMatrix = Matrix.CreateFromQuaternion(point.Orientation);
									this.main.Camera.RotationMatrix.Value = rotationMatrix;
									Matrix m = rotationMatrix * Matrix.CreateTranslation(point.Position);
									this.main.Camera.Position.Value = Vector3.Transform(new Vector3(0, 0, point.Offset), m);
								},
								currentStop.Duration
							),
							currentStop.Blend
						)
					);
				}
				else
				{
					sequence.Add
					(
						new Animation.Custom
						(
							delegate(float x)
							{
								this.main.Camera.RotationMatrix.Value = Matrix.CreateFromQuaternion(currentTransform.Quaternion);
								this.main.Camera.Position.Value = Vector3.Transform(new Vector3(0, 0, currentStop.Offset), currentTransform.Matrix);
							},
							currentStop.Duration
						)
					);
				}

				sequence.Add(new Animation.Execute(currentStop.OnDone));

				lastEase = currentStop.Blend;
				current = next;
			}

			animations.Add(new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.Zero, 0.5f));

			animations.Add(new Animation.Execute(delegate()
			{
				if (PlayerFactory.Instance != null)
				{
					CameraController cameraController = PlayerFactory.Instance.Get<CameraController>();
					cameraController.ThirdPerson.Value = originallyThirdPerson;
					cameraController.Enabled.Value = true;
					PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = true;
				}
			}));

			animations.Add(new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f));

			Animation anim = new Animation(animations.ToArray());
			anim.EnabledWhenPaused = false;
			WorldFactory.Instance.Add(anim);
		}
	}
}
