using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

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
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> LastSupported = new Property<bool>();
		public Property<Player.WallRun> WallRunState = new Property<Player.WallRun>();
		public Property<bool> EnableWalking = new Property<bool>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector2> Movement = new Property<Vector2>();
		public Property<bool> Crouched = new Property<bool>();

		// Output
		public AnimatedModel Model;

		private float lastLandAnimationPlayed = 0.0f;

		// Animations and their priorities
		private Dictionary<string, AnimationInfo> movementAnimations = new Dictionary<string, AnimationInfo>
		{
			{ "Idle", new AnimationInfo { Priority = -1, DefaultStrength = 1.0f, } },
			{ "Run", new AnimationInfo { Priority = 0 } },
			{ "RunBackward", new AnimationInfo { Priority = 0 } },
			{ "RunLeft", new AnimationInfo { Priority = 0 } },
			{ "RunRight", new AnimationInfo { Priority = 0 } },
			{ "Sprint", new AnimationInfo { Priority = 1 } },
		};
		private Dictionary<string, AnimationInfo> crouchMovementAnimations = new Dictionary<string, AnimationInfo>
		{
			{ "CrouchIdle", new AnimationInfo { Priority = 1, DefaultStrength = 1.0f } },
			{ "CrouchWalk", new AnimationInfo { Priority = 2 } },
			{ "CrouchWalkBackward", new AnimationInfo { Priority = 2 } },
			{ "CrouchStrafeLeft", new AnimationInfo { Priority = 2 } },
			{ "CrouchStrafeRight", new AnimationInfo { Priority = 2 } },
		};

		public void Update(float dt)
		{
			if (this.WallRunState != Player.WallRun.None)
			{
				this.Model.Stop
				(
					"Jump",
					"JumpLeft",
					"JumpBackward",
					"JumpRight",
					"Fall",
					"Vault",
					"VaultLeft",
					"VaultRight"
				);
				return;
			}

			this.Model.Stop
			(
				"WallRunLeft",
				"WallRunRight",
				"WallRunStraight",
				"WallSlideDown",
				"WallSlideReverse"
			);

			if (this.IsSupported)
			{
				this.Model.Stop
				(
					"Jump",
					"JumpLeft",
					"JumpBackward",
					"JumpRight",
					"Fall",
					"Vault",
					"VaultLeft",
					"VaultRight"
				);

				Vector2 dir = this.Movement;

				string movementAnimation;

				Vector3 velocity = this.LinearVelocity;
				velocity.Y = 0;
				float speed = velocity.Length();

				if (this.EnableWalking)
				{
					if (this.Crouched)
					{
						if (dir.LengthSquared() == 0.0f)
							movementAnimation = "CrouchIdle";
						else
							movementAnimation = dir.Y < 0.0f ? "CrouchWalkBackward" : (dir.X > 0.0f ? "CrouchStrafeRight" : (dir.X < 0.0f ? "CrouchStrafeLeft" : "CrouchWalk"));
					}
					else
					{
						if (dir.LengthSquared() == 0.0f)
							movementAnimation = "Idle";
						else
							movementAnimation = dir.Y < 0.0f ? "RunBackward" : (dir.X > 0.0f ? "RunRight" : (dir.X < 0.0f ? "RunLeft" : "Run"));
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
						this.Model[animation.Key].Speed = this.Crouched ? (speed / 2.2f) : (speed / 6.5f);
					this.Model[animation.Key].TargetStrength = animation.Key == movementAnimation ? 1.0f : animation.Value.DefaultStrength;
				}

				if (movementAnimation == "Run")
				{
					SkinnedModel.Clip sprintAnimation = this.Model["Sprint"];
					sprintAnimation.TargetStrength = MathHelper.Clamp((speed - 6.0f) / 2.0f, 0.0f, 1.0f);
					this.Model["Run"].TargetStrength = Math.Min(MathHelper.Clamp(speed / 4.0f, 0.0f, 1.0f), 1.0f - sprintAnimation.TargetStrength);
				}

				if (!this.Model.IsPlaying(movementAnimation))
				{
					foreach (string anim in this.Crouched ? movementAnimations.Keys : crouchMovementAnimations.Keys)
						this.Model.Stop(anim);
					foreach (KeyValuePair<string, AnimationInfo> animation in this.Crouched ? crouchMovementAnimations : movementAnimations)
					{
						this.Model.StartClip(animation.Key, animation.Value.Priority, true, AnimatedModel.DefaultBlendTime);
						this.Model[animation.Key].Strength = animation.Value.DefaultStrength;
					}
				}
			}
			else
			{
				foreach (string anim in movementAnimations.Keys)
					this.Model.Stop(anim);
				foreach (string anim in crouchMovementAnimations.Keys)
					this.Model.Stop(anim);

				if (!this.Model.IsPlaying("Fall"))
					this.Model.StartClip("Fall", 0, true, AnimatedModel.DefaultBlendTime);
			}
		}
	}
}
