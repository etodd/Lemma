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
		[XmlIgnore]
		public Character Character;

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
		public Property<bool> EnableMoves = new Property<bool> { Editable = false, Value = true };
		[XmlIgnore]
		public Property<bool> SlowMotion = new Property<bool> { Editable = false };
		[XmlIgnore]
		public Property<WallRun> WallRunState = new Property<WallRun> { Editable = false, Value = WallRun.None };
		[XmlIgnore]
		public Property<bool> LastSupported = new Property<bool>();
		[XmlIgnore]
		public Property<Vector3> LastLinearVelocity = new Property<Vector3>();

		[XmlIgnore]
		public Command HealthDepleted = new Command();

		private float damageTimer = 0.0f;
		public Property<float> Health = new Property<float> { Value = 1.0f, Editable = false };

		private const float healthRegenerateDelay = 4.0f;
		private const float healthRegenerateRate = 0.1f;

		public override void Awake()
		{
			base.Awake();
			this.Editable = false;
			this.EnabledWhenPaused = false;
			this.Character = new Character(this.main, Vector3.Zero);
			this.Character.Body.Tag = this;
			this.main.Space.Add(this.Character);

			float lastDamagedHealthValue = this.Health;
			this.Health.Set = delegate(float value)
			{
				if (value < this.Health.InternalValue && this.damageTimer > 0.4f)
				{
					AkSoundEngine.PostEvent(value < lastDamagedHealthValue - 0.2f ? AK.EVENTS.PLAY_PLAYER_HURT : AK.EVENTS.PLAY_PLAYER_HURT, this.Entity);
					lastDamagedHealthValue = value;
					this.damageTimer = 0.0f;
				}
				this.Health.InternalValue = Math.Min(1.0f, Math.Max(0.0f, value));
				if (this.Health.InternalValue == 0.0f)
					this.HealthDepleted.Execute();
			};

			this.Add(new Binding<float>(this.main.TimeMultiplier, () => this.SlowMotion && !this.main.Paused ? 0.4f : 1.0f, this.SlowMotion, this.main.Paused));
		}

		public override void delete()
		{
			base.delete();
			this.main.TimeMultiplier.Value = 1.0f;
			this.main.Space.Remove(this.Character);
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
			this.Character.Transform.Changed();
			this.Character.LinearVelocity.Changed();
			this.LastSupported.Value = this.Character.IsSupported;
			this.LastLinearVelocity.Value = this.Character.LinearVelocity;
		}
	}
}