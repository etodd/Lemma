using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Components
{
	public class AnimationController : Component<Main>, IUpdateableComponent
	{
		private class AnimationInfo
		{
			public int Priority;
			public float DefaultStrength;
		}

		// Input properties
		public Property<Vector3> SupportVelocity = new Property<Vector3>();
		public Property<bool> Kicking = new Property<bool>();
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> IsSwimming = new Property<bool>();
		public Property<WallRun.State> WallRunState = new Property<WallRun.State>();
		public Property<bool> EnableWalking = new Property<bool>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector2> Movement = new Property<Vector2>();
		public Property<bool> Crouched = new Property<bool>();
		public Property<Vector2> Mouse = new Property<Vector2>();
		public Property<float> Rotation = new Property<float>();
		public Property<bool> EnableLean = new Property<bool>();
		public Property<Voxel> WallRunMap = new Property<Voxel>();
		public Property<Direction> WallDirection = new Property<Direction>();

		private Random random = new Random();

		// Output
		public Property<float> Lean = new Property<float>();
		private AnimatedModel model;
		private SkinnedModel.Clip sprintAnimation;
		private SkinnedModel.Clip runAnimation;
		private SkinnedModel.Clip fallAnimation;
		private Property<Matrix> relativeHeadBone;

		private float lastRotation;

		private float breathing;

		private float idleRotation;
		private bool idling;
		private float idleRotationBlend;
		private const float idleRotationBlendTime = 0.3f;

		private const float animationSpeedCoefficient = 1.0f / 4.5f;
		private const float animationSpeedCoefficientWallRun = 1.0f / 3.0f;
		private const float maxAnimationSpeed = 1.45f;
		private const float maxAnimationSpeedWallRun = 2.0f;
		private const float animationSpeedCoefficientCrouched = 1.0f / 2.2f;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Serialize = false;
			this.lastRotation = this.idleRotation = this.Rotation;
			SoundKiller.Add(this.Entity, AK.EVENTS.STOP_PLAYER_BREATHING_SOFT);
		}

		public void Bind(AnimatedModel m)
		{
			this.model = m;
			this.sprintAnimation = m["Sprint"];
			this.runAnimation = m["Run"];
			this.fallAnimation = m["Fall"];

			m["Idle"].GetChannel(m.GetBoneIndex("ORG-spine")).Filter = delegate(Matrix spine)
			{
				float x;
				if (this.idleRotationBlend < 1.0f)
					x = (this.Rotation - this.idleRotation.ClosestAngle(this.Rotation)) * Math.Max(0.0f, 1.0f - this.idleRotationBlend);
				else if (this.idling)
					x = this.Rotation - this.idleRotation.ClosestAngle(this.Rotation);
				else
					x = 0.0f;
				return spine * Matrix.CreateRotationY(x);
			};

			int headIndex = m.GetBoneIndex("ORG-head");
			Matrix headBindPose = m.GetWorldBindTransform(headIndex) * Matrix.CreateTranslation(0, 0.1f, 0.25f);
			Func<SkinnedModel.Clip, Func<Matrix, Matrix>> cancelHeadBob = delegate(SkinnedModel.Clip clip)
			{
				int bone = headIndex;
				List<SkinnedModel.Channel> channels = new List<SkinnedModel.Channel>();
				channels.Add(new SkinnedModel.Channel { BoneIndex = bone });
				while (true)
				{
					int parent = m.SkeletonHierarchy[bone];
					if (parent == -1)
						break;
					else
						channels.Add(clip.GetChannel(parent) ?? new SkinnedModel.Channel { BoneIndex = bone });
					bone = parent;
				}
				return delegate(Matrix x)
				{
					if (main.MinimizeCameraMovement)
					{
						Matrix world = Matrix.Identity;
						for (int i = 0; i < channels.Count; i++)
						{
							SkinnedModel.Channel channel = channels[i];
							if (channel.Count == 0)
								world = world * m.BindPose[channel.BoneIndex];
							else
								world = world * channel.CurrentMatrix;
						}
						return headBindPose * Matrix.Invert(world);
					}
					else
						return x;
				};
			};

			int cameraIndex = m.GetBoneIndex("Camera");
			Func<SkinnedModel.Clip, Func<Matrix, Matrix>> cancelRotation = delegate(SkinnedModel.Clip clip)
			{
				int bone = cameraIndex;
				List<SkinnedModel.Channel> channels = new List<SkinnedModel.Channel>();
				channels.Add(new SkinnedModel.Channel { BoneIndex = bone });
				while (true)
				{
					int parent = m.SkeletonHierarchy[bone];
					if (parent == -1)
						break;
					else
						channels.Add(clip.GetChannel(parent) ?? new SkinnedModel.Channel { BoneIndex = bone });
					bone = parent;
				}
				return delegate(Matrix x)
				{
					if (
#if VR
						this.main.VR ||
#endif
						this.main.MinimizeCameraMovement)
					{
						Matrix world = Matrix.Identity;
						for (int i = 0; i < channels.Count; i++)
						{
							SkinnedModel.Channel channel = channels[i];
							if (channel.Count == 0)
								world = world * m.BindPose[channel.BoneIndex];
							else
								world = world * channel.CurrentMatrix;
						}
						Matrix rot = Matrix.Invert(world) * Matrix.CreateRotationX((float)Math.PI * 0.5f);
						rot.Translation = x.Translation;
						return rot;
					}
					else
						return x;
				};
			};

			m["Run"].GetChannel(headIndex).Filter = cancelHeadBob(m["Run"]);
			m["RunLeft"].GetChannel(headIndex).Filter = cancelHeadBob(m["RunLeft"]);
			m["RunRight"].GetChannel(headIndex).Filter = cancelHeadBob(m["RunRight"]);
			m["RunLeftForward"].GetChannel(headIndex).Filter = cancelHeadBob(m["RunLeftForward"]);
			m["RunRightForward"].GetChannel(headIndex).Filter = cancelHeadBob(m["RunRightForward"]);
			m["Sprint"].GetChannel(headIndex).Filter = cancelHeadBob(m["Sprint"]);
			m["Slide"].GetChannel(cameraIndex).Filter = cancelRotation(m["Slide"]);
			m["WallRunLeft"].GetChannel(cameraIndex).Filter = cancelRotation(m["WallRunLeft"]);
			m["WallRunRight"].GetChannel(cameraIndex).Filter = cancelRotation(m["WallRunRight"]);
			m["TopOut"].GetChannel(cameraIndex).Filter = cancelRotation(m["TopOut"]);
			m["Land"].GetChannel(cameraIndex).Filter = cancelRotation(m["Land"]);
			m["LandHard"].GetChannel(cameraIndex).Filter = cancelRotation(m["LandHard"]);

			m["Idle"].GetChannel(m.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix hips)
			{
				float x;
				if (this.idleRotationBlend < 1.0f)
					x = (this.idleRotation.ClosestAngle(this.Rotation) - this.Rotation) * Math.Max(0.0f, 1.0f - this.idleRotationBlend);
				else if (this.idling)
					x = this.idleRotation.ClosestAngle(this.Rotation) - this.Rotation;
				else
					x = 0.0f;
				return hips * Matrix.CreateRotationZ(x);
			};

			this.relativeHeadBone = m.GetRelativeBoneTransform("ORG-head");
			m["Swim"].Speed = 2.0f;
			m["SwimForward"].Speed = 2.0f;
			m["TurnLeft"].Speed = 2.0f;
			m["TurnRight"].Speed = 2.0f;

			m["WallRunStraight"].GetChannel(m.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix hips)
			{
				hips.Translation += new Vector3(0, 1, 0);
				return hips;
			};
		}

		// Animations and their priorities
		private static Dictionary<string, AnimationInfo> movementAnimations = new Dictionary<string, AnimationInfo>
		{
			{ "Idle", new AnimationInfo { Priority = -1, DefaultStrength = 1.0f, } },
			{ "Run", new AnimationInfo { Priority = 0 } },
			{ "RunBackward", new AnimationInfo { Priority = 0 } },
			{ "RunLeft", new AnimationInfo { Priority = 0 } },
			{ "RunRight", new AnimationInfo { Priority = 0 } },
			{ "RunLeftForward", new AnimationInfo { Priority = 0 } },
			{ "RunRightForward", new AnimationInfo { Priority = 0 } },
			{ "Sprint", new AnimationInfo { Priority = 1 } },
		};

		// Split the unit circle into 8 pie slices, starting with positive X
		private static string[] directions = new[]
		{
			"RunRight",
			"RunRightForward",
			"Run",
			"RunLeftForward",
			"RunLeft",
			"RunBackward",
			"RunBackward",
			"RunBackward",
		};

		private static Dictionary<string, AnimationInfo> crouchMovementAnimations = new Dictionary<string, AnimationInfo>
		{
			{ "CrouchIdle", new AnimationInfo { Priority = 1, DefaultStrength = 1.0f } },
			{ "CrouchWalk", new AnimationInfo { Priority = 2 } },
			{ "CrouchWalkBackward", new AnimationInfo { Priority = 2 } },
			{ "CrouchStrafeLeft", new AnimationInfo { Priority = 2 } },
			{ "CrouchStrafeRight", new AnimationInfo { Priority = 2 } },
		};

		private static string[] crouchDirections = new[]
		{
			"CrouchStrafeRight",
			"CrouchStrafeRight",
			"CrouchWalk",
			"CrouchStrafeLeft",
			"CrouchStrafeLeft",
			"CrouchWalkBackward",
			"CrouchWalkBackward",
			"CrouchWalkBackward",
		};

		const float sprintRange = 1.0f;
		const float sprintThreshold = Character.DefaultMaxSpeed - sprintRange;

		private float lastHorizontalSpeed;

		public void Update(float dt)
		{
			Vector2 mouse = this.Mouse;

			Vector3 relativeVelocity = this.LinearVelocity.Value - this.SupportVelocity.Value;
			Vector3 horizontalRelativeVelocity = relativeVelocity;
			horizontalRelativeVelocity.Y = 0;
			float horizontalSpeed = horizontalRelativeVelocity.Length();
			if (horizontalSpeed < this.lastHorizontalSpeed)
				horizontalSpeed = Math.Max(horizontalSpeed, this.lastHorizontalSpeed - 40.0f * dt);
			else if (horizontalSpeed > this.lastHorizontalSpeed)
				horizontalSpeed = Math.Min(horizontalSpeed, this.lastHorizontalSpeed + 10.0f * dt);
			this.lastHorizontalSpeed = horizontalSpeed;

			if (this.WallRunState == WallRun.State.None)
			{
				this.model.Stop
				(
					"WallRunLeft",
					"WallRunRight",
					"WallRunStraight",
					"WallSlideDown",
					"WallSlideReverse"
				);

				if (this.IsSupported)
				{
					this.model.Stop
					(
						"Jump",
						"Jump02",
						"Jump03",
						"JumpLeft",
						"JumpBackward",
						"JumpRight",
						"Fall",
						"Swim",
						"SwimForward"
					);

					Vector2 dir = this.Movement;
					float angle = (float)Math.Atan2(dir.Y, dir.X);
					if (angle < 0.0f)
						angle += (float)Math.PI * 2.0f;

					string movementAnimation;

					if (this.EnableWalking)
					{
						if (this.Crouched)
						{
							if (dir.LengthSquared() == 0.0f)
								movementAnimation = "CrouchIdle";
							else
								movementAnimation = AnimationController.crouchDirections[(int)Math.Round(angle / ((float)Math.PI * 0.25f)) % 8];
						}
						else
						{
							if (dir.LengthSquared() == 0.0f)
								movementAnimation = "Idle";
							else
								movementAnimation = AnimationController.directions[(int)Math.Round(angle / ((float)Math.PI * 0.25f)) % 8];
						}
					}
					else
					{
						if (this.Crouched)
							movementAnimation = "CrouchIdle";
						else
							movementAnimation = "Idle";
					}

					foreach (KeyValuePair<string, AnimationInfo> animation in this.Crouched ? crouchMovementAnimations : movementAnimations)
					{
						if (animation.Key != "Idle" && animation.Key != "CrouchIdle")
							this.model[animation.Key].Speed = Math.Min(horizontalSpeed * (this.Crouched ? animationSpeedCoefficientCrouched : animationSpeedCoefficient), maxAnimationSpeed);
						this.model[animation.Key].TargetStrength = animation.Key == movementAnimation ? 1.0f : animation.Value.DefaultStrength;
					}

					bool nowIdling = false;
					if (movementAnimation == "Run")
					{
						this.sprintAnimation.TargetStrength = MathHelper.Clamp((horizontalSpeed - sprintThreshold) / sprintRange, 0.0f, 1.0f);
						this.runAnimation.TargetStrength = Math.Min(MathHelper.Clamp(horizontalSpeed / sprintThreshold, 0.0f, 1.0f), 1.0f - this.sprintAnimation.TargetStrength);
					}
					else if (movementAnimation != "Idle" && movementAnimation != "CrouchIdle")
						this.model[movementAnimation].TargetStrength = MathHelper.Clamp(this.Crouched ? horizontalSpeed / 2.0f : horizontalSpeed / sprintThreshold, 0.0f, 1.0f);
					else if (movementAnimation == "Idle")
						nowIdling = true;

					if (nowIdling)
					{
						if (this.idling)
						{
							// We're already idling. Blend to new idle rotation if necessary
							if (this.idleRotationBlend < 1.0f)
							{
								this.idleRotationBlend += dt / idleRotationBlendTime;
								if (this.idleRotationBlend >= 1.0f)
									this.idleRotation = this.Rotation; // We're done blending
							}
							else
							{
								float rotationDiff = this.Rotation - this.idleRotation.ClosestAngle(this.Rotation);
								if (Math.Abs(rotationDiff) > Math.PI * 0.25)
								{
									this.idleRotationBlend = 0.0f; // Start blending to new rotation
									this.model.StartClip(rotationDiff > 0.0f ? "TurnLeft" : "TurnRight", 1);
								}
							}
						}
						else // We just started idling.
						{
							if (this.idleRotationBlend >= 1.0f) // Save the current rotation.
								this.idleRotation = this.Rotation;
						}
					}
					else
					{
						if (this.idling) // We're just now coming out of idle state
						{
							if (this.idleRotationBlend > 1.0f)
								this.idleRotationBlend = 0.0f;
						}
						if (this.idleRotationBlend < 1.0f)
						{
							this.idleRotationBlend += dt / idleRotationBlendTime;
							if (this.idleRotationBlend >= 1.0f)
								this.idleRotation = this.Rotation; // We're done blending
						}
					}
					this.idling = nowIdling;

					if (!this.model.IsPlaying(movementAnimation))
					{
						foreach (string anim in this.Crouched ? movementAnimations.Keys : crouchMovementAnimations.Keys)
							this.model.Stop(anim);
						Dictionary<string, AnimationInfo> animations = this.Crouched ? crouchMovementAnimations : movementAnimations;
						TimeSpan currentTime = TimeSpan.FromSeconds(this.random.NextDouble() * this.model[animations.Keys.First()].Duration.TotalSeconds);
						foreach (KeyValuePair<string, AnimationInfo> animation in animations)
						{
							this.model.StartClip(animation.Key, animation.Value.Priority, true, AnimatedModel.DefaultBlendTime);
							SkinnedModel.Clip clip = this.model[animation.Key];
							clip.CurrentTime = currentTime;
							clip.Strength = animation.Value.DefaultStrength;
						}
					}
				}
				else
				{
					this.idling = false;
					foreach (string anim in movementAnimations.Keys)
						this.model.Stop(anim);
					foreach (string anim in crouchMovementAnimations.Keys)
						this.model.Stop(anim);

					if (this.IsSwimming)
					{
						this.model.Stop("Fall");
						if (this.Movement.Value.Y > 0.25f)
						{
							this.model.Stop("Swim");
							if (!this.model.IsPlaying("SwimForward"))
							{
								this.model.StartClip("SwimForward", 0, true, AnimatedModel.DefaultBlendTime);
								SkinnedModel.Clip clip = this.model["SwimForward"];
								clip.CurrentTime = TimeSpan.FromSeconds(this.random.NextDouble() * clip.Duration.TotalSeconds);
							}
						}
						else
						{
							this.model.Stop("SwimForward");
							if (!this.model.IsPlaying("Swim"))
							{
								this.model.StartClip("Swim", 0, true, AnimatedModel.DefaultBlendTime);
								SkinnedModel.Clip clip = this.model["Swim"];
								clip.CurrentTime = TimeSpan.FromSeconds(this.random.NextDouble() * clip.Duration.TotalSeconds);
							}
						}
					}
					else
					{
						this.model.Stop("SwimForward");
						if (!this.model.IsPlaying("Fall"))
						{
							this.model.StartClip("Fall", 0, true, AnimatedModel.DefaultBlendTime);
							this.fallAnimation.CurrentTime = TimeSpan.FromSeconds(random.NextDouble() * this.fallAnimation.Duration.TotalSeconds);
						}
					}

					this.fallAnimation.Speed = MathHelper.Clamp(this.LinearVelocity.Value.Length() * (1.0f / 20.0f), 0, 2);
				}
			}
			else
			{
				this.idling = false;
				this.model.Stop
				(
					"Jump",
					"Jump02",
					"Jump03",
					"JumpLeft",
					"JumpBackward",
					"JumpRight",
					"Fall"
				);
				foreach (string anim in movementAnimations.Keys)
					this.model.Stop(anim);

				string wallRunAnimation;
				switch (this.WallRunState.Value)
				{
					case WallRun.State.Straight:
						wallRunAnimation = "WallRunStraight";
						break;
					case WallRun.State.Down:
						wallRunAnimation = "WallSlideDown";
						break;
					case WallRun.State.Left:
						wallRunAnimation = "WallRunLeft";
						break;
					case WallRun.State.Right:
						wallRunAnimation = "WallRunRight";
						break;
					default:
						wallRunAnimation = null;
						break;
				}
				if (!this.model.IsPlaying(wallRunAnimation))
				{
					this.model.Stop
					(
						"WallRunStraight",
						"WallSlideDown",
						"WallRunLeft",
						"WallRunRight",
						"WallSlideReverse"
					);
					this.model.StartClip(wallRunAnimation, 1, true);
				}

				if (wallRunAnimation != null)
				{
					Vector3 wallNormal = this.WallRunMap.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());
					float animationSpeed = (relativeVelocity - wallNormal * Vector3.Dot(relativeVelocity, wallNormal)).Length();
					this.model[wallRunAnimation].Speed = Math.Min(maxAnimationSpeedWallRun, animationSpeed * animationSpeedCoefficientWallRun);
				}
			}

			// Rotate head to match mouse
			this.relativeHeadBone.Value *= Matrix.CreateRotationX(mouse.Y * 0.6f);
			this.model.UpdateWorldTransforms();

			float l = 0.0f;
			if (this.EnableLean
				&& !this.main.MinimizeCameraMovement
#if VR
				&& !this.main.VR
#endif
				)
			{
				l = horizontalSpeed * (this.lastRotation.ClosestAngle(this.Rotation) - this.Rotation) * (1.0f / 60.0f) / dt;
			}

			this.Lean.Value += (l - this.Lean) * 20.0f * dt;

			this.lastRotation = this.Rotation;

			const float timeScale = 5.0f;
			const float softBreathingThresholdPercentage = 0.75f;
			float newBreathing;
			if (!this.Crouched && this.Movement.Value.LengthSquared() > 0.0f && horizontalSpeed > Character.DefaultMaxSpeed * 0.75f)
			{
				newBreathing = Math.Min(this.breathing + (dt / timeScale), 1.0f);
				if (this.breathing < softBreathingThresholdPercentage && newBreathing > softBreathingThresholdPercentage)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_BREATHING_SOFT, this.Entity);
					newBreathing = 1.0f;
				}
			}
			else
			{
				newBreathing = Math.Max(0, this.breathing - dt / timeScale);
				if (this.breathing > softBreathingThresholdPercentage && newBreathing < softBreathingThresholdPercentage)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_BREATHING_SOFT, this.Entity);
					newBreathing = 0.0f;
				}
			}
			this.breathing = newBreathing;
			AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_PLAYER_SLIDE, MathHelper.Clamp(relativeVelocity.Length() / 8.0f, 0.0f, 1.0f) * (this.Kicking ? 1.0f : 0.25f));
		}
	}
}