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
	public class Player : Component<Main>, IUpdateableComponent
	{
		public enum WallRun { None, Left, Right, Straight, Down, Reverse }
		public const float DefaultMaxSpeed = 8.8f;
		public const float DefaultJumpSpeed = 9.5f;

		protected Character character;
		[XmlIgnore]
		public Property<Vector2> MovementDirection = new Property<Vector2> { Editable = false };
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		[XmlIgnore]
		public Property<float> MaxSpeed = new Property<float> { Value = Player.DefaultMaxSpeed, Editable = false };
		[XmlIgnore]
		public Property<float> JumpSpeed = new Property<float> { Value = Player.DefaultJumpSpeed, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableRoll = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableCrouch = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableKick = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableWallRun = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableWallRunHorizontal = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableEnhancedWallRun = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableSlowMotion = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> EnableStamina = new Property<bool> { Value = false, Editable = false };
		[XmlIgnore]
		public Property<bool> IsSupported = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<bool> IsSwimming = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<bool> EnableWalking = new Property<bool> { Editable = false, Value = true };
		[XmlIgnore]
		public Property<bool> EnableMoves = new Property<bool> { Editable = false, Value = true };
		[XmlIgnore]
		public Property<bool> HasTraction = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<Vector3> SupportLocation = new Property<Vector3> { Editable = false };
		[XmlIgnore]
		public Property<BEPUphysics.Entities.Entity> SupportEntity = new Property<BEPUphysics.Entities.Entity> { Editable = false };
		public Property<Vector3> LinearVelocity = new Property<Vector3> { Editable = false };
		[XmlIgnore]
		public Property<bool> AllowUncrouch = new Property<bool> { Editable = false, Value = true };
		public Property<bool> Crouched = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<float> Height = new Property<float> { Editable = false };
		[XmlIgnore]
		public Property<float> SupportHeight = new Property<float> { Editable = false, Value = 1.5f };
		[XmlIgnore]
		public Property<bool> SlowMotion = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<WallRun> WallRunState = new Property<WallRun> { Editable = false, Value = WallRun.None };

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();

		[XmlIgnore]
		public Command HealthDepleted = new Command();

		private float damageTimer = 0.0f;
		public Property<float> Health = new Property<float> { Value = 1.0f, Editable = false };

		private const float healthRegenerateDelay = 4.0f;
		private const float healthRegenerateRate = 0.1f;

		[XmlIgnore]
		public BEPUphysics.Entities.Entity Body
		{
			get
			{
				return this.character.Body;
			}
		}

		public const float CharacterRadius = 1.75f;

		public const float DefaultCharacterHeight = 2.75f;

		public const float CrouchedCharacterHeight = 1.8f;

		public const float DefaultSupportHeight = 1.25f;

		public const float CrouchedSupportHeight = 0.5f;

		public override void InitializeProperties()
		{
			this.Editable = false;
			this.EnabledWhenPaused.Value = false;
			this.character = new Character(this.main, Vector3.Zero, DefaultCharacterHeight, CrouchedCharacterHeight, CharacterRadius, DefaultSupportHeight, CrouchedSupportHeight, 4.0f);
			this.character.IsUpdating = false;
			this.character.Body.Tag = this;
			this.main.Space.Add(this.character);

			float lastDamagedHealthValue = this.Health;
			this.Health.Set = delegate(float value)
			{
				if (value < this.Health.InternalValue && this.damageTimer > 0.1f)
				{
					AkSoundEngine.PostEvent(value < lastDamagedHealthValue - 0.2f ? "Play_damage" : "Play_small_damge", this.Entity);
					lastDamagedHealthValue = value;
					this.damageTimer = 0.0f;
				}
				this.Health.InternalValue = Math.Min(1.0f, Math.Max(0.0f, value));
				if (this.Health.InternalValue == 0.0f)
					this.HealthDepleted.Execute();
			};

			this.Add(new Binding<float>(this.main.TimeMultiplier, () => this.SlowMotion && !this.main.Paused ? 0.4f : 1.0f, this.SlowMotion, this.main.Paused));
			this.Add(new TwoWayBinding<Vector2>(this.MovementDirection, this.character.MovementDirection));
			this.Add(new TwoWayBinding<float>(this.MaxSpeed, this.character.MaxSpeed));
			this.Add(new TwoWayBinding<float>(this.JumpSpeed, this.character.JumpSpeed));
			this.Add(new TwoWayBinding<bool>(this.HasTraction, this.character.HasTraction));
			this.Add(new TwoWayBinding<bool>(this.IsSupported, this.character.IsSupported));
			this.Add(new TwoWayBinding<bool>(this.IsSwimming, this.character.IsSwimming));
			this.Add(new TwoWayBinding<WallRun>(this.WallRunState, this.character.WallRunState));
			this.Add(new TwoWayBinding<bool>(this.EnableWalking, this.character.EnableWalking));
			this.Add(new TwoWayBinding<Vector3>(this.character.SupportLocation, this.SupportLocation));
			this.Add(new TwoWayBinding<BEPUphysics.Entities.Entity>(this.character.SupportEntity, this.SupportEntity));
			this.Add(new TwoWayBinding<bool>(this.Crouched, this.character.Crouched));
			this.Add(new TwoWayBinding<bool>(this.AllowUncrouch, this.character.AllowUncrouch));
			this.Add(new TwoWayBinding<float>(this.SupportHeight, this.character.SupportHeight));
			this.Add(new CommandBinding<Collidable, ContactCollection>(this.character.Collided, this.Collided));
			this.Add(new Binding<float, bool>(this.Height, x => x ? CrouchedCharacterHeight : DefaultCharacterHeight, this.Crouched));

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
			this.Transform.Changed();
			this.LinearVelocity.Changed();
		}
	}
}