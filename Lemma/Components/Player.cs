using System;
using BEPUphysics;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.UpdateableSystems;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.MathExtensions;
using Lemma.Util;
using BEPUphysics.CollisionTests;
using System.Xml.Serialization;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Components
{
	public class Player : Component, IUpdateableComponent
	{
		public enum WallRun { None, Left, Right, Straight, Down }

		protected Character character;
		[XmlIgnore]
		public Property<Vector2> MovementDirection = new Property<Vector2> { Editable = false };
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		[XmlIgnore]
		public Property<float> MaxSpeed = new Property<float> { Value = 8, Editable = false };
		public Property<float> NormalMaxSpeed = new Property<float> { Value = 8, Editable = false };
		[XmlIgnore]
		public Property<float> JumpSpeed = new Property<float> { Value = 10, Editable = false };
		[XmlIgnore]
		public Property<bool> IsLevitating = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableRoll = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableKick = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableBlockBuild = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableWallRun = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableAim = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableLevitation = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableSprint = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableSlowMotion = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> Sprint = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<bool> IsSupported = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<bool> IsSwimming = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<bool> EnableWalking = new Property<bool> { Editable = false, Value = true };
		[XmlIgnore]
		public Property<bool> HasTraction = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<int> Stamina = new Property<int> { Editable = false, Value = 100 };
		[XmlIgnore]
		public Property<Vector3> SupportLocation = new Property<Vector3> { Editable = false };
		[XmlIgnore]
		public Property<object> SupportEntityTag = new Property<object> { Editable = false };
		public Property<Vector3> LinearVelocity = new Property<Vector3> { Editable = false };
		[XmlIgnore]
		public Property<bool> AllowUncrouch = new Property<bool> { Editable = false, Value = true };
		public Property<bool> Crouched = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<float> Height = new Property<float> { Editable = false };
		[XmlIgnore]
		public Property<float> SupportHeight = new Property<float> { Editable = false, Value = 1.05f };
		[XmlIgnore]
		public Property<bool> SlowMotion = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<WallRun> WallRunState = new Property<WallRun> { Editable = false, Value = WallRun.None };

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();

		[XmlIgnore]
		public Command StaminaDepleted = new Command();

		private float damageTimer = 0.0f;
		public Property<float> Health = new Property<float> { Value = 1.0f, Editable = false };

		private const float healthRegenerateDelay = 4.0f;
		private const float healthRegenerateRate = 0.1f;

		private const float staminaDecayTime = 10.0f * 60.0f; // Time in seconds to drain all 100 stamina points
		private const float staminaDecayInterval = staminaDecayTime / 100.0f;

		private const float sprintStaminaDecayTime = 30.0f; // Time in seconds to drain all 100 stamina points while sprinting
		private const float sprintStaminaDecayInterval = sprintStaminaDecayTime / 100.0f;

		private float timeUntilNextStaminaDecay = staminaDecayInterval;

		[XmlIgnore]
		public BEPUphysics.Entities.Entity Body
		{
			get
			{
				return this.character.Body;
			}
		}

		public override void InitializeProperties()
		{
			this.Editable = false;
			this.EnabledWhenPaused.Value = false;
			this.character = new Character(this.main, Vector3.Zero, 4.0f, 1.75f, this.SupportHeight, 4.0f);
			this.character.IsUpdating = false;
			this.character.Body.Tag = this;
			this.main.Space.Add(this.character);

			float lastDamagedHealthValue = this.Health;
			this.Health.Set = delegate(float value)
			{
				if (value < this.Health.InternalValue)
				{
					if (Sound.PlayCue(this.main, value < lastDamagedHealthValue - 0.2f ? "Damage" : "Small Damage", 1.0f, 0.5f) != null)
						lastDamagedHealthValue = value;
					this.damageTimer = 0.0f;
				}
				this.Health.InternalValue = Math.Min(1.0f, Math.Max(0.0f, value));
				if (this.Health.InternalValue == 0.0f)
				{
					Sound.PlayCue(this.main, "Death");
					this.Entity.Delete.Execute();
				}
			};

			this.SlowMotion.Set = delegate(bool value)
			{
				if (this.EnableSlowMotion)
				{
					if (value && !this.SlowMotion.InternalValue)
						this.timeUntilNextStaminaDecay = 0.0f;
					else if (!value && this.SlowMotion.InternalValue)
						this.timeUntilNextStaminaDecay = staminaDecayInterval;
					this.SlowMotion.InternalValue = value;
				}
				else
					this.SlowMotion.InternalValue = false;
			};

			this.Stamina.Set = delegate(int value)
			{
				this.Stamina.InternalValue = Math.Max(0, Math.Min(100, value));
				if (this.Stamina.InternalValue == 0)
					this.StaminaDepleted.Execute();
			};

			this.Add(new Binding<float>(this.main.TimeMultiplier, () => this.SlowMotion && !this.main.Paused ? 0.4f : 1.0f, this.SlowMotion, this.main.Paused));
			this.Add(new TwoWayBinding<Vector2>(this.MovementDirection, this.character.MovementDirection));
			this.Add(new TwoWayBinding<float>(this.MaxSpeed, this.character.MaxSpeed));
			this.Add(new Binding<float>(this.MaxSpeed, () => this.NormalMaxSpeed * (this.Sprint ? 1.5f : 1.0f), this.NormalMaxSpeed, this.Sprint));
			this.Add(new TwoWayBinding<float>(this.JumpSpeed, this.character.JumpSpeed));
			this.Add(new TwoWayBinding<bool>(this.HasTraction, this.character.HasTraction));
			this.Add(new TwoWayBinding<bool>(this.IsSupported, this.character.IsSupported));
			this.Add(new TwoWayBinding<bool>(this.IsSwimming, this.character.IsSwimming));
			this.Add(new TwoWayBinding<WallRun>(this.WallRunState, this.character.WallRunState));
			this.Add(new TwoWayBinding<bool>(this.EnableWalking, this.character.EnableWalking));
			this.Add(new TwoWayBinding<Vector3>(this.character.SupportLocation, this.SupportLocation));
			this.Add(new TwoWayBinding<object>(this.character.SupportEntityTag, this.SupportEntityTag));
			this.Add(new TwoWayBinding<bool>(this.Crouched, this.character.Crouched));
			this.Add(new TwoWayBinding<bool>(this.AllowUncrouch, this.character.AllowUncrouch));
			this.Add(new TwoWayBinding<float>(this.SupportHeight, this.character.SupportHeight));
			this.Add(new CommandBinding<Collidable, ContactCollection>(this.character.Collided, this.Collided));
			this.Add(new Binding<float, bool>(this.Height, x => x ? 2.0f : 4.0f, this.Crouched));

			this.Add(new NotifyBinding(delegate()
			{
				this.Transform.Changed();
			}, this.Crouched));

			this.Transform.Set = delegate(Matrix value)
			{
				this.character.Body.WorldTransform = value;
			};
			this.Transform.Get = delegate()
			{
				return this.character.Body.BufferedStates.InterpolatedStates.WorldTransform;
			};

			this.LinearVelocity.Set = delegate(Vector3 value)
			{
				this.character.Body.LinearVelocity = value;
			};
			this.LinearVelocity.Get = delegate()
			{
				return this.character.Body.LinearVelocity;
			};

			this.Sprint.Set = delegate(bool value)
			{
				if (this.EnableSprint)
				{
					if (value && !this.Sprint.InternalValue)
						this.timeUntilNextStaminaDecay = 0.0f;
					else if (!value && this.Sprint.InternalValue)
						this.timeUntilNextStaminaDecay = staminaDecayInterval;
					this.Sprint.InternalValue = value;
				}
				else
					this.Sprint.InternalValue = false;
			};

			this.EnableSprint.Set = delegate(bool value)
			{
				this.EnableSprint.InternalValue = value;
				if (!value)
					this.Sprint.Value = false;
			};
		}

		protected override void delete()
		{
			base.delete();
			this.main.TimeMultiplier.Value = 1.0f;
			this.main.Space.Remove(this.character);
		}

		public void Update(float dt)
		{
			if (this.Health < 1.0f)
			{
				if (this.damageTimer < Player.healthRegenerateDelay)
					this.damageTimer += dt;
				else
					this.Health.Value += Player.healthRegenerateRate * dt;
			}
			if (!this.IsSupported || this.MovementDirection.Value.LengthSquared() > 0.0f || !this.EnableWalking || this.IsLevitating)
				this.timeUntilNextStaminaDecay -= dt;
			while (this.timeUntilNextStaminaDecay < 0.0f)
			{
				this.Stamina.Value--;
				this.timeUntilNextStaminaDecay += this.Sprint || this.SlowMotion ? sprintStaminaDecayInterval : staminaDecayInterval;
			}
			this.Transform.Changed();
			this.LinearVelocity.Changed();
		}
	}
}