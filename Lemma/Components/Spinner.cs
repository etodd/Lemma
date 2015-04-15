using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using BEPUphysics;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Constraints.TwoEntity.Motors;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Spinner : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Property<Direction> Direction = new Property<Direction>();

		public Property<float> Minimum = new Property<float>();
		public Property<float> Maximum = new Property<float>();
		public Property<bool> Locked = new Property<bool>();
		public Property<float> Speed = new Property<float>();
		public Property<float> Goal = new Property<float>();
		public Property<bool> Servo = new Property<bool>();
		public Property<uint> MovementLoop = new Property<uint> { Value = AK.EVENTS.SLIDER2_LOOP };
		public Property<uint> MovementStop = new Property<uint> { Value = AK.EVENTS.SLIDER2_STOP };

		public Property<Quaternion> OriginalRotation = new Property<Quaternion>();

		[XmlIgnore]
		public Command Forward = new Command();

		[XmlIgnore]
		public Command Backward = new Command();

		[XmlIgnore]
		public Command HitMin = new Command();

		[XmlIgnore]
		public Command HitMax = new Command();

		private void forward()
		{
			if (joint != null && this.Locked)
				joint.Motor.Settings.Servo.Goal = this.Maximum;
		}

		private void backward()
		{
			if (joint != null && this.Locked)
				joint.Motor.Settings.Servo.Goal = this.Minimum;
		}

		private RevoluteJoint joint = null;
		private void setLimits()
		{
			if (this.joint != null)
			{
				float min = this.Minimum, max = this.Maximum;
				if (max > min)
				{
					this.joint.Limit.IsActive = true;
					this.joint.Limit.MinimumAngle = min;
					this.joint.Limit.MaximumAngle = max;
				}
				else
					this.joint.Limit.IsActive = false;
			}
		}

		private void setSpeed()
		{
			if (this.joint != null)
			{
				this.joint.Motor.Settings.Servo.BaseCorrectiveSpeed = joint.Motor.Settings.Servo.MaxCorrectiveVelocity = this.Speed;
				this.joint.Motor.Settings.VelocityMotor.GoalVelocity = this.Speed;
			}
		}

		private void setLocked()
		{
			if (this.joint != null)
				this.joint.Motor.IsActive = this.Locked;
		}

		private void setGoal()
		{
			if (this.joint != null)
				this.joint.Motor.Settings.Servo.Goal = this.Goal;
		}

		private void setMode()
		{
			if (this.joint != null)
				this.joint.Motor.Settings.Mode = this.Servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
		}

		public void Move(int value)
		{
			if (this.Locked)
				this.Goal.Value = value;
		}

		private bool soundPlaying;
		private BEPUphysics.Entities.Entity physicsEntity;

		public ISpaceObject CreateJoint(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
		{
			// entity1 is us
			// entity2 is the main map we are attaching to
			this.physicsEntity = entity1;
			Vector3 originalPos = entity1.Position;
			Quaternion originalRotation = entity1.Orientation;
			entity1.Position = pos;
			entity1.Orientation = this.OriginalRotation;
			this.joint = new RevoluteJoint(entity1, entity2, anchor, direction);
			entity1.Position = originalPos;
			entity1.Orientation = originalRotation;
			float multiplier = Math.Max(1.0f, entity1.Mass);
			this.joint.AngularJoint.SpringSettings.StiffnessConstant *= multiplier;
			this.joint.Limit.SpringSettings.StiffnessConstant *= multiplier;
			this.joint.Motor.Settings.Mode = MotorMode.Servomechanism;
			this.setLimits();
			this.setLocked();
			this.setSpeed();
			this.setMode();
			this.setGoal();
			return joint;
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new NotifyBinding(this.setLimits, this.Minimum, this.Maximum));
			this.Add(new NotifyBinding(this.setSpeed, this.Speed));
			this.Add(new NotifyBinding(this.setLocked, this.Locked));
			this.Add(new NotifyBinding(this.setGoal, this.Goal));
			this.Add(new NotifyBinding(this.setMode, this.Servo));

			this.Forward.Action = (Action)this.forward;
			this.Backward.Action = (Action)this.backward;
			Action movementStop = delegate()
			{
				if (this.MovementStop.Value != 0)
					AkSoundEngine.PostEvent(this.MovementStop, this.Entity);
				this.soundPlaying = false;
			};
			this.Add(new CommandBinding(this.HitMax, movementStop));
			this.Add(new CommandBinding(this.HitMin, movementStop));
		}

		private bool lastLimitExceeded;
		public void Update(float dt)
		{
			if (this.joint != null)
			{
				bool limitExceeded = this.joint.Limit.IsLimitExceeded;
				if (limitExceeded && !this.lastLimitExceeded)
				{
					if (Math.Abs(joint.Limit.Error.X) > 0.0001f)
						this.HitMin.Execute();
					else if (Math.Abs(joint.Limit.Error.Y) > 0.0001f)
						this.HitMax.Execute();
				}
				else
				{
					bool moving = this.Locked && Math.Abs(Vector3.Dot(this.physicsEntity.AngularVelocity, this.joint.AngularJoint.WorldFreeAxisA)) > 0.1f;
					if (this.soundPlaying && !moving)
						AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL_OBJECT, this.Entity);
					else if (!this.soundPlaying && moving)
					{
						if (this.MovementLoop.Value != 0)
							AkSoundEngine.PostEvent(this.MovementLoop, this.Entity);
					}
					this.soundPlaying = moving;
				}
				this.lastLimitExceeded = limitExceeded;
			}
		}
	}
}