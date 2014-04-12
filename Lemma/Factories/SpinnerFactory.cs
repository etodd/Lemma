using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;

namespace Lemma.Factories
{
	public class SpinnerFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Spinner");

			result.Add("Map", new DynamicMap(0, 0, 0));

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Property<Direction> dir = result.GetOrMakeProperty<Direction>("Direction", true);
			Property<float> minimum = result.GetOrMakeProperty<float>("Minimum", true);
			Property<float> maximum = result.GetOrMakeProperty<float>("Maximum", true);
			Property<bool> locked = result.GetOrMakeProperty<bool>("Locked", true);
			Property<bool> servo = result.GetOrMakeProperty<bool>("Servo", true, true);
			Property<float> speed = result.GetOrMakeProperty<float>("Speed", true, 5);
			Property<float> goal = result.GetOrMakeProperty<float>("Goal", true);

			RevoluteJoint joint = null;

			Action setLimits = delegate()
			{
				if (joint != null)
				{
					float min = minimum, max = maximum;
					if (max > min)
					{
						joint.Limit.IsActive = true;
						joint.Limit.MinimumAngle = minimum;
						joint.Limit.MaximumAngle = maximum;
					}
					else
						joint.Limit.IsActive = false;
				}
			};
			result.Add(new NotifyBinding(setLimits, minimum, maximum));

			Action setSpeed = delegate()
			{
				if (joint != null)
				{
					joint.Motor.Settings.Servo.BaseCorrectiveSpeed = joint.Motor.Settings.Servo.MaxCorrectiveVelocity = speed;
					joint.Motor.Settings.VelocityMotor.GoalVelocity = speed;
				}
			};
			result.Add(new NotifyBinding(setSpeed, speed));

			Action setGoal = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Servo.Goal = goal;
			};
			result.Add(new NotifyBinding(setGoal, goal));

			Action setLocked = delegate()
			{
				if (joint != null)
					joint.Motor.IsActive = locked;
			};
			result.Add(new NotifyBinding(setLocked, locked));

			DynamicMap map = result.Get<DynamicMap>();

			Action setServo = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Mode = servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
			};
			result.Add(new NotifyBinding(setServo, servo));

			Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint = delegate(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
			{
				joint = new RevoluteJoint(entity1, entity2, anchor, direction);
				float multiplier = Math.Max(1.0f, map.PhysicsEntity.Mass);
				joint.AngularJoint.SpringSettings.StiffnessConstant *= multiplier;
				joint.Limit.SpringSettings.StiffnessConstant *= multiplier;
				joint.Motor.Settings.Mode = MotorMode.Servomechanism;
				setLimits();
				setLocked();
				setSpeed();
				setServo();
				setGoal();
				return joint;
			};

			JointFactory.Bind(result, main, createJoint, true, creating);

			result.Add("On", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						servo.Value = false;
				},
			});

			result.Add("Off", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
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
						goal.Value = (float)Math.Atan2(y, x);
						servo.Value = true;
					}
				},
			});

			result.Add("Forward", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						joint.Motor.Settings.Servo.Goal = maximum;
				},
			});

			result.Add("Backward", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						joint.Motor.Settings.Servo.Goal = minimum;
				},
			});

			Command hitMax = new Command();
			result.Add("HitMax", hitMax);
			Command hitMin = new Command();
			result.Add("HitMin", hitMin);

			bool lastLimitExceeded = false;
			result.Add(new Updater
			{
				delegate(float dt)
				{
					if (joint != null)
					{
						bool limitExceeded = joint.Limit.IsLimitExceeded;
						if (limitExceeded && !lastLimitExceeded)
						{
							if (joint.Limit.Error.X > 0)
								hitMin.Execute();
							else
								hitMax.Execute();
						}
						lastLimitExceeded = limitExceeded;
					}
				}
			});
		}
	}
}
