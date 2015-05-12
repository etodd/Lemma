using System;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.UpdateableSystems;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using Lemma.Util;
using BEPUphysics.CollisionTests;
using System.Xml.Serialization;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using ComponentBind;

namespace Lemma.Components
{
	public class Player : Component<Main>, IUpdateableComponent, IEarlyUpdateableComponent
	{
		public const float SlowmoTime = 1.5f;
		[XmlIgnore]
		public Character Character;

		[XmlIgnore]
		public Property<bool> EnableCrouch = new Property<bool>();
		[XmlIgnore]
		public Property<bool> PermanentEnableMoves = new Property<bool> { Value = true };
		[XmlIgnore]
		public Property<bool> TemporaryEnableMoves = new Property<bool> { Value = true };
		[XmlIgnore]
		public Property<bool> SlowMotion = new Property<bool>();
		[XmlIgnore]
		public Property<bool> EnableSlowMotion = new Property<bool>();

		public Property<ComponentBind.Entity.Handle> Note = new Property<ComponentBind.Entity.Handle>();
		public Property<ComponentBind.Entity.Handle> SignalTower = new Property<ComponentBind.Entity.Handle>();

		[XmlIgnore]
		public Command Die = new Command();

		private const float damageSoundInterval = 0.4f;
		private float damageTimer = damageSoundInterval + 1.0f;
		private float slowmoTimer;
		public Property<float> Health = new Property<float> { Value = 1.0f };

		private float lastSpeed = 0.0f;

		[XmlIgnore]
		public Command<float> Rumble = new Command<float>();

		[XmlIgnore]
		public Property<int> UpdateOrder { get; set; }

		private const float healthRegenerateDelay = 3.0f;
		private const float healthRegenerateRate = 0.1f;

		public Player()
		{
			this.UpdateOrder = new Property<int> { Value = 0 };
		}

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Character = new Character(this.main, this, Vector3.Zero);
			this.Add(new NotifyBinding(this.main.EarlyUpdateablesModified, this.UpdateOrder));
			this.Character.Body.Tag = this;
			this.main.Space.Add(this.Character);

			AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_FALL, this.Entity);
			AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_PLAYER_FALL, 0.0f);
			SoundKiller.Add(this.Entity, AK.EVENTS.STOP_PLAYER_FALL);

			this.Add(new ChangeBinding<float>(this.Health, delegate(float old, float value)
			{
				if (value < old && this.damageTimer > damageSoundInterval)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_HURT, this.Entity);
					this.damageTimer = 0.0f;
					this.Rumble.Execute(Math.Min(0.3f, (old - value) * 2.0f));
				}
				if (old > 0.0f && value <= 0.0f)
					this.Die.Execute();
			}));

			this.Add(new Binding<float>(this.main.TimeMultiplier, () => this.SlowMotion && !this.main.Paused ? 0.4f : 1.0f, this.SlowMotion, this.main.Paused));
			this.Add(new ChangeBinding<bool>(this.SlowMotion, delegate(bool old, bool value)
			{
				if (!old && value)
				{
					this.slowmoTimer = 0;
				}
			}));
		}

		public override void delete()
		{
			base.delete();
			this.main.TimeMultiplier.Value = 1.0f;
			this.main.Space.Remove(this.Character);
		}

		void IEarlyUpdateableComponent.Update(float dt)
		{
			this.Character.Transform.Value = this.Character.Body.WorldTransform;
			this.Character.LinearVelocity.Value = this.Character.Body.LinearVelocity;
		}

		void IUpdateableComponent.Update(float dt)
		{
			if (this.SlowMotion)
			{
				this.slowmoTimer += dt;
				if (this.slowmoTimer > Player.SlowmoTime)
					this.SlowMotion.Value = false;
			}

			this.damageTimer += dt;
			if (this.Health < 1.0f)
			{
				if (this.damageTimer > Player.healthRegenerateDelay)
					this.Health.Value += Player.healthRegenerateRate * dt;
			}

			Vector3 velocity = this.Character.LinearVelocity.Value;
			if (this.SlowMotion && velocity.Y < FallDamage.RollingDeathVelocity)
				this.SlowMotion.Value = false;

			float speed = velocity.Length();
			if (speed < this.lastSpeed)
				speed = Math.Max(speed, this.lastSpeed - 40.0f * dt);
			else if (speed > this.lastSpeed)
				speed = Math.Min(speed, this.lastSpeed + 10.0f * dt);
			this.lastSpeed = speed;
			float maxSpeed = this.Character.MaxSpeed * 1.3f;
			float volume;
			if (speed > maxSpeed)
				volume = (speed - maxSpeed) / (maxSpeed * 2.0f);
			else
				volume = 0.0f;
			AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_PLAYER_FALL, volume);

			// Determine if the player is swimming
			bool swimming = false;
			Vector3 pos = this.Character.Transform.Value.Translation + new Vector3(0, -1.0f, 0);
			foreach (Water w in Water.ActiveInstances)
			{
				if (w.Fluid.BoundingBox.Contains(pos) != ContainmentType.Disjoint)
				{
					swimming = true;
					break;
				}
			}
			this.Character.IsSwimming.Value = swimming;
		}
	}
}