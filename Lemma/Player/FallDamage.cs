using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class FallDamage : Component<Main>, IUpdateableComponent
	{
		public const float DamageVelocity = -20.0f; // Vertical velocity below which damage occurs
		public const float RollingDamageVelocity = -28.0f; // Damage velocity when rolling
		public const float GruntVelocity = -13.0f; // Vertical velocity below which grunting occurs
		public const float DamageMultiplier = 0.2f;
		public const float DeathVelocity = DamageVelocity - (1.0f / DamageMultiplier);
		public const float RollingDeathVelocity = RollingDamageVelocity - (1.0f / DamageMultiplier);

		// Input commands
		public Command<float> Apply = new Command<float>();
		public Command ApplyJump = new Command();
		public Command<BEPUphysics.BroadPhaseEntries.Collidable, ContactCollection> Collided = new Command<BEPUphysics.BroadPhaseEntries.Collidable,ContactCollection>();

		// Output commands
		public Command<float> Rumble = new Command<float>();
		public Command LockRotation = new Command();
		public Property<bool> Landing = new Property<bool>();

		// Input properties
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> PhoneOrNoteActive = new Property<bool>();

		private bool lastSupported;

		private float landingTimer;

		private const float landingTime = 0.75f;

		// Animated model
		public void Bind(AnimatedModel m)
		{
			this.model = m;
			m["Land"].Strength = m["Land"].TargetStrength = 0.7f;
			this.landAnimation = m["LandHard"];
			this.landAnimation.GetChannel(m.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix hips)
			{
				float t = (float)this.landAnimation.CurrentTime.TotalSeconds;
				hips.Translation += new Vector3(0, Math.Max(0.0f, 1.0f - (t > 0.75f ? t - 0.75f : 0)), 0);
				return hips;
			};
			this.landAnimation.Speed = 1.5f;
		}

		private AnimatedModel model;
		private SkinnedModel.Clip landAnimation;

		// Output properties
		public Property<float> Health = new Property<float>();
		public Property<bool> EnableWalking = new Property<bool>();
		public Property<bool> EnableMoves = new Property<bool>();
		public Command<float> PhysicsDamage = new Command<float>(); // Damage incurred from physics stuff smashing us

		// Input/output properties
		public Property<Vector3> LinearVelocity = new Property<Vector3>();

		private Vector3 lastLinearVelocity;
		private bool disabledMoves;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
			this.ApplyJump.Action = delegate() { this.apply(this.lastLinearVelocity.Y - this.LinearVelocity.Value.Y, true); };
			this.Apply.Action = delegate(float verticalAcceleration) { this.apply(verticalAcceleration, false); };

			// Damage the player if they hit something too hard
			this.Collided.Action = delegate(BEPUphysics.BroadPhaseEntries.Collidable other, ContactCollection contacts)
			{
				DynamicVoxel map = other.Tag as DynamicVoxel;
				if (map != null)
				{
					float force = contacts[contacts.Count - 1].NormalImpulse;
					float threshold = map.Dangerous ? 20.0f : 50.0f;
					float playerLastSpeed = Vector3.Dot(this.lastLinearVelocity, Vector3.Normalize(-contacts[contacts.Count - 1].Contact.Normal)) * 2.5f;
					if (force > threshold + playerLastSpeed + 4.0f)
						this.PhysicsDamage.Execute((force - threshold - playerLastSpeed) * 0.04f);
				}
			};
		}

		private void apply(float verticalAcceleration, bool jumping)
		{
			bool rolling = this.model.IsPlaying("Roll") || this.model.IsPlaying("Kick");
			float v = rolling ? RollingDamageVelocity : DamageVelocity;
			if (verticalAcceleration < v)
			{
				float damage = (verticalAcceleration - v) * DamageMultiplier;
				this.Health.Value += damage;
				// Health component will take care of rumble
				if (this.Health.Value <= 0.0f)
				{
					main.Spawner.RespawnDistance = Spawner.DefaultRespawnDistance;
					main.Spawner.RespawnInterval = Spawner.DefaultRespawnInterval;
				}
				else
				{
					if (!rolling && !jumping)
					{
						this.LinearVelocity.Value = new Vector3(0, this.LinearVelocity.Value.Y, 0);
						this.Landing.Value = true;
						this.LockRotation.Execute();
						this.landingTimer = 0;
						this.model.StartClip("LandHard", 0, false, 0.1f);
						this.EnableWalking.Value = false;
						this.EnableMoves.Value = false;
						this.disabledMoves = true;
					}
				}
			}
			else if (verticalAcceleration < GruntVelocity)
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_LAND, this.Entity);
				if (!rolling)
					this.model.StartClip("Land", 4);
				this.Rumble.Execute(0.2f);
			}
		}

		public void Update(float dt)
		{
			if (this.Landing)
			{
				this.landingTimer += dt;
				if (this.landingTimer > landingTime)
					this.Landing.Value = false;
			}

			if (!this.lastSupported && this.IsSupported)
			{
				// Damage the player if they fall too hard and they're not smashing or rolling
				float accel = this.lastLinearVelocity.Y - this.LinearVelocity.Value.Y;
				this.Apply.Execute(accel);
			}

			if (this.disabledMoves && (!this.landAnimation.Active || this.landAnimation.CurrentTime.TotalSeconds > 1.0f))
			{
				// We disabled walking while the land animation was playing.
				// Now re-enable it
				if (!this.PhoneOrNoteActive)
				{
					this.EnableWalking.Value = true;
					this.EnableMoves.Value = true;
				}
				this.disabledMoves = false;
			}

			this.lastSupported = this.IsSupported;
			this.lastLinearVelocity = this.LinearVelocity;
		}

		public override void delete()
		{
			base.delete();
			if (!this.PhoneOrNoteActive)
			{
				this.EnableWalking.Value = true;
				this.EnableMoves.Value = true;
			}
		}
	}
}