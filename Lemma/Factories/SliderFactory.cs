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
	public class SliderFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Slider");
			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			entity.Add("Voxel", new DynamicVoxel(0, 0, 0));
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Property<Direction> dir = entity.GetOrMakeProperty<Direction>("Direction", true);
			Property<int> minimum = entity.GetOrMakeProperty<int>("Minimum", true);
			Property<int> maximum = entity.GetOrMakeProperty<int>("Maximum", true);
			Property<bool> locked = entity.GetOrMakeProperty<bool>("Locked", true);
			Property<float> speed = entity.GetOrMakeProperty<float>("Speed", true, 5);
			Property<float> maxForce = entity.GetOrMakeProperty<float>("MaxForce", true);
			Property<float> damping = entity.GetOrMakeProperty<float>("Damping", true);
			Property<float> stiffness = entity.GetOrMakeProperty<float>("Stiffness", true);
			Property<int> goal = entity.GetOrMakeProperty<int>("Goal", true);
			Property<bool> servo = entity.GetOrMakeProperty<bool>("Servo", true, true);

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
			entity.Add(new NotifyBinding(setLimits, minimum, maximum));

			Action updateMaterial = delegate()
			{
				DynamicVoxel map = entity.Get<DynamicVoxel>();
				if (map != null)
				{
					bool active = locked && (!servo || (servo && goal.Value != minimum.Value));

					Voxel.State slider = Voxel.States[Voxel.t.Slider];
					Voxel.State powered = Voxel.States[Voxel.t.SliderPowered];
					Voxel.State desired = active ? powered : slider;
					Voxel.t currentID = map[0, 0, 0].ID;
					if (currentID != desired.ID & (currentID == Voxel.t.Slider || currentID == Voxel.t.SliderPowered))
					{
						List<Voxel.Coord> coords = map.GetContiguousByType(new[] { map.GetBox(0, 0, 0) }).SelectMany(x => x.GetCoords()).ToList();
						map.Empty(coords, true, true, null, false);
						foreach (Voxel.Coord c in coords)
							map.Fill(c, desired);
						map.Regenerate();
					}
				}
			};

			Action setMaxForce = delegate()
			{
				if (joint != null)
				{
					if (maxForce > 0.001f)
						joint.Motor.Settings.MaximumForce = maxForce * entity.Get<DynamicVoxel>().PhysicsEntity.Mass;
					else
						joint.Motor.Settings.MaximumForce = float.MaxValue;
				}
			};
			entity.Add(new NotifyBinding(setMaxForce, maxForce));

			Action setSpeed = delegate()
			{
				if (joint != null)
				{
					joint.Motor.Settings.Servo.BaseCorrectiveSpeed = speed;
					joint.Motor.Settings.VelocityMotor.GoalVelocity = speed;
				}
			};
			entity.Add(new NotifyBinding(setSpeed, speed));

			Action setLocked = delegate()
			{
				if (joint != null)
					joint.Motor.IsActive = locked;
				updateMaterial();
			};
			entity.Add(new NotifyBinding(setLocked, locked));

			Action setGoal = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Servo.Goal = goal;
				updateMaterial();
			};
			entity.Add(new NotifyBinding(setGoal, goal));

			Action setDamping = delegate()
			{
				if (joint != null && damping != 0)
					joint.Motor.Settings.Servo.SpringSettings.DampingConstant = damping;
			};
			entity.Add(new NotifyBinding(setDamping, damping));

			Action setStiffness = delegate()
			{
				if (joint != null && stiffness != 0)
					joint.Motor.Settings.Servo.SpringSettings.StiffnessConstant = stiffness;
			};
			entity.Add(new NotifyBinding(setStiffness, stiffness));

			Action setMode = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Mode = servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
				updateMaterial();
			};
			entity.Add(new NotifyBinding(setMode, servo));

			BEPUphysics.Entities.Entity a = null, b = null;
			Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint = delegate(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
			{
				// entity1 is us
				// entity2 is the main map we are attaching to
				a = entity1;
				b = entity2;
				Vector3 originalPos = entity1.Position;
				entity1.Position = pos;
				joint = new PrismaticJoint(entity1, entity2, pos, -direction, pos);
				entity1.Position = originalPos;
				setLimits();
				setLocked();
				setSpeed();
				setMaxForce();
				setGoal();
				setDamping();
				setStiffness();
				setMode();
				return joint;
			};

			JointFactory.Bind(entity, main, createJoint, false, creating);

			Action<int> move = delegate(int value)
			{
				if (locked)
					goal.Value = value;
			};
			entity.Add("Forward", new Command { Action = delegate() { move(maximum); } });
			entity.Add("Trigger", new Command { Action = delegate() { move(maximum); } });
			entity.Add("Backward", new Command { Action = delegate() { move(minimum); } });

			Command hitMax = new Command();
			entity.Add("HitMax", hitMax);
			Command hitMin = new Command();
			entity.Add("HitMin", hitMin);

			float lastX = minimum + (maximum - minimum) * 0.5f;
			entity.Add(new Updater
			{
				delegate(float dt)
				{
					if (joint != null)
					{
						Vector3 separation = joint.Limit.AnchorB - joint.Limit.AnchorA;

						float x = Vector3.Dot(separation, joint.Limit.Axis);

						if (x > maximum - 0.5f)
						{
							if (lastX <= maximum - 0.5f)
								hitMax.Execute();
						}
						
						if (x < minimum + 0.5f)
						{
							if (lastX >= minimum + 0.5f)
								hitMin.Execute();
						}

						lastX = x;
					}
				}
			});

			Property<bool> startAtMinimum = entity.GetOrMakeProperty<bool>("StartAtMinimum", true);

			if (!main.EditorEnabled && startAtMinimum)
			{
				startAtMinimum.Value = false;
				entity.Add(new PostInitialization
				{
					delegate()
					{
						Transform transform = entity.GetOrCreate<Transform>("MapTransform");
						DynamicVoxel map = entity.Get<DynamicVoxel>();
						transform.Position.Value = map.GetAbsolutePosition(new Voxel.Coord().Move(dir, minimum));
					}
				});
			}
		}
	}
}
