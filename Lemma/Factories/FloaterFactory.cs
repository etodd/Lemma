using System;
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
	public class FloaterFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Floater");

			result.Add("Map", new DynamicMap(0, 0, 0));

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Property<float> maxForce = result.GetOrMakeProperty<float>("MaxForce", true);
			Property<float> damping = result.GetOrMakeProperty<float>("Damping", true);
			Property<float> stiffness = result.GetOrMakeProperty<float>("Stiffness", true);

			NoRotationJoint joint = null;
			EntityMover mover = null;

			Action setMaxForce = delegate()
			{
				if (mover != null)
				{
					if (maxForce > 0.001f)
						mover.LinearMotor.Settings.MaximumForce = maxForce * result.Get<DynamicMap>().PhysicsEntity.Mass;
					else
						mover.LinearMotor.Settings.MaximumForce = float.MaxValue;
				}
			};
			result.Add(new NotifyBinding(setMaxForce, maxForce));

			Action setDamping = delegate()
			{
				if (mover != null && damping != 0)
					mover.LinearMotor.Settings.Servo.SpringSettings.DampingConstant = damping;
			};
			result.Add(new NotifyBinding(setDamping, damping));

			Action setStiffness = delegate()
			{
				if (mover != null && stiffness != 0)
					mover.LinearMotor.Settings.Servo.SpringSettings.StiffnessConstant = stiffness;
			};
			result.Add(new NotifyBinding(setStiffness, stiffness));

			Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint = delegate(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
			{
				joint = new NoRotationJoint(entity1, entity2);
				if (mover != null && mover.Space != null)
					main.Space.Remove(mover);
				mover = new EntityMover(entity1);
				main.Space.Add(mover);
				setMaxForce();
				setDamping();
				setStiffness();
				return joint;
			};

			JointFactory.Bind(result, main, createJoint, true, creating);

			result.Add(new CommandBinding(result.Get<DynamicMap>().OnSuspended, delegate()
			{
				if (mover != null && mover.Space != null)
					main.Space.Remove(mover);
			}));

			result.Add(new CommandBinding(result.Get<DynamicMap>().OnResumed, delegate()
			{
				if (mover != null && mover.Space == null)
					main.Space.Add(mover);
			}));

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				if (mover != null && mover.Space != null)
				{
					main.Space.Remove(mover);
					mover = null;
				}
			}));

			Property<Entity.Handle> parent = result.GetOrMakeProperty<Entity.Handle>("Parent");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("Coord");
			Updater updater = null;
			updater = new Updater
			{
				delegate(float dt)
				{
					Entity parentEntity = parent.Value.Target;
					if (parentEntity != null && parentEntity.Active)
						mover.TargetPosition = parentEntity.Get<Map>().GetAbsolutePosition(coord) + new Vector3(0, -0.01f, 0);
					else
					{
						updater.Delete.Execute();
						parent.Value = null;
						if (mover != null && mover.Space != null)
						{
							main.Space.Remove(mover);
							mover = null;
						}
					}
				}
			};
			result.Add(updater);
		}
	}
}
