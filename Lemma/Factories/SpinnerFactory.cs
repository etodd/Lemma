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
			Entity entity = new Entity(main, "Spinner");

			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			entity.Add("Voxel", new DynamicVoxel(0, 0, 0));

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Property<Direction> dir = entity.GetOrMakeProperty<Direction>("Direction", true);
			Property<float> minimum = entity.GetOrMakeProperty<float>("Minimum", true);
			Property<float> maximum = entity.GetOrMakeProperty<float>("Maximum", true);
			Property<bool> locked = entity.GetOrMakeProperty<bool>("Locked", true);
			Property<bool> servo = entity.GetOrMakeProperty<bool>("Servo", true, true);
			Property<float> speed = entity.GetOrMakeProperty<float>("Speed", true, 5);
			Property<float> goal = entity.GetOrMakeProperty<float>("Goal", true);

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
			entity.Add(new NotifyBinding(setLimits, minimum, maximum));

			Action setSpeed = delegate()
			{
				if (joint != null)
				{
					joint.Motor.Settings.Servo.BaseCorrectiveSpeed = joint.Motor.Settings.Servo.MaxCorrectiveVelocity = speed;
					joint.Motor.Settings.VelocityMotor.GoalVelocity = speed;
				}
			};
			entity.Add(new NotifyBinding(setSpeed, speed));

			Action setGoal = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Servo.Goal = goal;
			};
			entity.Add(new NotifyBinding(setGoal, goal));

			Action setLocked = delegate()
			{
				if (joint != null)
					joint.Motor.IsActive = locked;
			};
			entity.Add(new NotifyBinding(setLocked, locked));

			DynamicVoxel map = entity.Get<DynamicVoxel>();

			Action setServo = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Mode = servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
			};
			entity.Add(new NotifyBinding(setServo, servo));

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

			JointFactory.Bind(entity, main, createJoint, true, creating);

			entity.Add("On", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						servo.Value = false;
				},
			});

			entity.Add("Off", new Command
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

			entity.Add("Forward", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						joint.Motor.Settings.Servo.Goal = maximum;
				},
			});

			entity.Add("Backward", new Command
			{
				Action = delegate()
				{
					if (joint != null && locked)
						joint.Motor.Settings.Servo.Goal = minimum;
				},
			});

			Command hitMax = new Command();
			entity.Add("HitMax", hitMax);
			Command hitMin = new Command();
			entity.Add("HitMin", hitMin);

			bool lastLimitExceeded = false;
			entity.Add(new Updater
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
