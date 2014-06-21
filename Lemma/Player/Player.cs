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
		[XmlIgnore]
		public Character Character;

		[XmlIgnore]
		public Property<bool> EnableCrouch = new Property<bool>();
		[XmlIgnore]
		public Property<bool> EnableSlowMotion = new Property<bool>();
		[XmlIgnore]
		public Property<bool> EnableMoves = new Property<bool> { Value = true };
		[XmlIgnore]
		public Property<bool> SlowMotion = new Property<bool>();

		public Property<ComponentBind.Entity.Handle> Note = new Property<ComponentBind.Entity.Handle>();
		public Property<ComponentBind.Entity.Handle> SignalTower = new Property<ComponentBind.Entity.Handle>();

		[XmlIgnore]
		public Command HealthDepleted = new Command();

		private const float damageSoundInterval = 0.4f;
		private float damageTimer = damageSoundInterval + 1.0f;
		public Property<float> Health = new Property<float> { Value = 1.0f };

		[XmlIgnore]
		public Command<float> Rumble = new Command<float>();

		private const float healthRegenerateDelay = 4.0f;
		private const float healthRegenerateRate = 0.1f;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Character = new Character(this.main, Vector3.Zero);
			this.Character.Body.Tag = this;
			this.main.Space.Add(this.Character);

			this.Health.Set = delegate(float value)
			{
				if (value < this.Health.InternalValue && this.damageTimer > damageSoundInterval)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_HURT, this.Entity);
					this.damageTimer = 0.0f;
					this.Rumble.Execute(Math.Min(0.3f, (this.Health.InternalValue - value) * 2.0f));
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
			this.damageTimer += dt;
			if (this.Health < 1.0f)
			{
				if (this.damageTimer > Player.healthRegenerateDelay)
					this.Health.Value += Player.healthRegenerateRate * dt;
			}

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

			this.Character.Transform.Changed();
			this.Character.LinearVelocity.Changed();
		}
	}
}