using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;

namespace Lemma.Factories
{
	public class SliderFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Slider");

			result.Add("Map", new DynamicMap(0, 0, 0));

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Property<Direction> dir = result.GetOrMakeProperty<Direction>("Direction", true);
			Property<int> minimum = result.GetOrMakeProperty<int>("Minimum", true);
			Property<int> maximum = result.GetOrMakeProperty<int>("Maximum", true);
			Property<bool> locked = result.GetOrMakeProperty<bool>("Locked", true);
			Property<float> speed = result.GetOrMakeProperty<float>("Speed", true, 5);
			Property<float> maxForce = result.GetOrMakeProperty<float>("MaxForce", true);

			PrismaticJoint joint = null;

			Action setLimits = delegate()
			{
				if (joint != null)
				{
					int min = minimum, max = maximum;
					if (max > min)
					{
						joint.Limit.IsActive = true;
						joint.Limit.Minimum = minimum;
						joint.Limit.Maximum = maximum;
					}
					else
						joint.Limit.IsActive = false;
				}
			};
			result.Add(new NotifyBinding(setLimits, minimum, maximum));

			Action setMaxForce = delegate()
			{
				if (joint != null)
				{
					if (maxForce > 0.001f)
						joint.Motor.Settings.MaximumForce = maxForce;
					else
						joint.Motor.Settings.MaximumForce = float.MaxValue;
				}
			};
			result.Add(new NotifyBinding(setMaxForce, maxForce));

			Action setSpeed = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Servo.BaseCorrectiveSpeed = speed;
			};
			result.Add(new NotifyBinding(setSpeed, speed));

			Action setLocked = delegate()
			{
				if (joint != null)
					joint.Motor.IsActive = locked;
			};
			result.Add(new NotifyBinding(setLocked, locked));

			Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint = delegate(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
			{
				joint = new PrismaticJoint(entity1, entity2, pos, -direction, anchor);
				joint.Motor.Settings.Mode = MotorMode.Servomechanism;
				setLimits();
				setLocked();
				setSpeed();
				setMaxForce();
				return joint;
			};

			JointFactory.Bind(result, main, createJoint, creating);

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
		}
	}
}
