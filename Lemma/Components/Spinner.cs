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

		[XmlIgnore]
		public Command On = new Command();

		[XmlIgnore]
		public Command Off = new Command();

		[XmlIgnore]
		public Command Forward = new Command();

		[XmlIgnore]
		public Command Backward = new Command();

		[XmlIgnore]
		public Command HitMin = new Command();

		[XmlIgnore]
		public Command HitMax = new Command();

		private void on()
		{
			if (this.joint != null && this.Locked)
				this.Servo.Value = false;
		}

		private void off()
		{
			if (joint != null && this.Locked)
			{
				BEPUphysics.Constraints.JointBasis2D basis = joint.Motor.Basis;
				basis.RotationMatrix = joint.Motor.ConnectionA.OrientationMatrix;

				Vector3 localTestAxis = joint.Motor.LocalTestAxis;
				Vector3 worldTestAxis;
				BEPUutilities.Matrix3x3 orientationMatrix = joint.Motor.ConnectionB.OrientationMatrix;
				BEPUutilities.Matrix3x3.Transform(ref localTestAxis, ref orientationMatrix, out worldTestAxis);

				float y, x;
				Vector3 yAxis = Vector3.Cross(basis.PrimaryAxis, basis.XAxis);
				Vector3.Dot(ref worldTestAxis, ref yAxis, out y);
				x = Vector3.Dot(worldTestAxis, basis.XAxis);
				this.Goal.Value = (float)Math.Atan2(y, x);
				this.Servo.Value = true;
			}
		}

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
		private float lastX;

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

		public ISpaceObject CreateJoint(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
		{
			// entity1 is us
			// entity2 is the main map we are attaching to
			this.joint = new RevoluteJoint(entity1, entity2, anchor, direction);
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
			this.On.Action = (Action)this.on;
			this.Off.Action = (Action)this.off;

			this.lastX = this.Minimum + (this.Maximum - this.Minimum) * 0.5f;
		}

		private bool lastLimitExceeded;
		public void Update(float dt)
		{
			if (this.joint != null)
			{
				bool limitExceeded = this.joint.Limit.IsLimitExceeded;
				if (limitExceeded && !this.lastLimitExceeded)
				{
					if (joint.Limit.Error.X > 0)
						this.HitMin.Execute();
					else
						this.HitMax.Execute();
				}
				this.lastLimitExceeded = limitExceeded;
			}
		}
	}
}
