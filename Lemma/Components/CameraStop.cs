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
		public static Property<bool> CinematicActive = new Property<bool>();

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
			Animation.Sequence sequence = new Animation.Sequence();

			bool originalCanPause = false;
			sequence.Add(new Animation.Execute(delegate()
			{
				Entity p = PlayerFactory.Instance;
				if (p != null)
				{
					p.Get<Model>("FirstPersonModel").Enabled.Value = false;
					p.Get<Model>("Model").Enabled.Value = false;
					p.Get<CameraController>().Enabled.Value = false;
					p.Get<FPSInput>().Enabled.Value = false;
					p.Get<UIRenderer>().Enabled.Value = false;
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_BREATHING_SOFT, p);
				}
				CameraStop.CinematicActive.Value = true;
				originalCanPause = this.main.Menu.CanPause;
#if !DEVELOPMENT
				this.main.Menu.CanPause.Value = false;
#endif
			}));

			sequence.Add(new Animation.Set<Matrix>(this.main.Camera.RotationMatrix, Matrix.CreateFromQuaternion(this.Entity.Get<Transform>().Quaternion)));
			sequence.Add(new Animation.Set<Vector3>(this.main.Camera.Position, Vector3.Transform(new Vector3(0, 0, this.Offset), this.Entity.Get<Transform>().Matrix)));

			Animation.Ease.EaseType lastEase = Animation.Ease.EaseType.None;
			BSpline spline = null;
			Entity current = this.Entity;
			float totalDuration = 0.0f;
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
				totalDuration += currentStop.Duration;

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

			Action done = delegate()
			{
				Entity p = PlayerFactory.Instance;
				if (p != null)
				{
					p.Get<Model>("FirstPersonModel").Enabled.Value = true;
					p.Get<Model>("Model").Enabled.Value = true;
					p.Get<CameraController>().Enabled.Value = true;
					p.Get<FPSInput>().Enabled.Value = true;
					p.Get<UIRenderer>().Enabled.Value = true;
				}
				CameraStop.CinematicActive.Value = false;
				this.main.Menu.CanPause.Value = originalCanPause;
			};

			Animation anim;
			if (totalDuration > 0.0f) // Fade in and out
			{
				anim = new Animation
				(
					new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.Zero, 0.5f),
					new Animation.Parallel(sequence, new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f)),
					new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.Zero, 0.5f),
					new Animation.Execute(done),
					new Animation.Vector3MoveTo(this.main.Renderer.Tint, Vector3.One, 0.5f)
				);
			}
			else
			{
				// Just do it
				anim = new Animation
				(
					sequence,
					new Animation.Execute(done)
				);
			}
			anim.EnabledWhenPaused = false;
			WorldFactory.Instance.Add(anim);
		}
	}
}