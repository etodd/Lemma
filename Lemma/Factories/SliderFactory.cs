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
			Property<float> damping = result.GetOrMakeProperty<float>("Damping", true);
			Property<float> stiffness = result.GetOrMakeProperty<float>("Stiffness", true);
			Property<int> goal = result.GetOrMakeProperty<int>("Goal", true);
			Property<bool> servo = result.GetOrMakeProperty<bool>("Servo", true, true);

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

			Action updateMaterial = delegate()
			{
				DynamicMap map = result.Get<DynamicMap>();
				if (map != null)
				{
					bool active = locked && (!servo || (servo && goal.Value != minimum.Value));

					Map.CellState slider = WorldFactory.StatesByName["Slider"];
					Map.CellState powered = WorldFactory.StatesByName["SliderPowered"];
					Map.CellState desired = active ? powered : slider;
					int currentID = map[0, 0, 0].ID;
					if (currentID != desired.ID & (currentID == slider.ID || currentID == powered.ID))
					{
						List<Map.Coordinate> coords = map.GetContiguousByType(new[] { map.GetBox(0, 0, 0) }).SelectMany(x => x.GetCoords()).ToList();
						map.Empty(coords, true, true, null, false);
						foreach (Map.Coordinate c in coords)
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
						joint.Motor.Settings.MaximumForce = maxForce * result.Get<DynamicMap>().PhysicsEntity.Mass;
					else
						joint.Motor.Settings.MaximumForce = float.MaxValue;
				}
			};
			result.Add(new NotifyBinding(setMaxForce, maxForce));

			Action setSpeed = delegate()
			{
				if (joint != null)
				{
					joint.Motor.Settings.Servo.BaseCorrectiveSpeed = speed;
					joint.Motor.Settings.VelocityMotor.GoalVelocity = speed;
				}
			};
			result.Add(new NotifyBinding(setSpeed, speed));

			Action setLocked = delegate()
			{
				if (joint != null)
					joint.Motor.IsActive = locked;
				updateMaterial();
			};
			result.Add(new NotifyBinding(setLocked, locked));

			Action setGoal = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Servo.Goal = goal;
				updateMaterial();
			};
			result.Add(new NotifyBinding(setGoal, goal));

			Action setDamping = delegate()
			{
				if (joint != null && damping != 0)
					joint.Motor.Settings.Servo.SpringSettings.DampingConstant = damping;
			};
			result.Add(new NotifyBinding(setDamping, damping));

			Action setStiffness = delegate()
			{
				if (joint != null && stiffness != 0)
					joint.Motor.Settings.Servo.SpringSettings.StiffnessConstant = stiffness;
			};
			result.Add(new NotifyBinding(setStiffness, stiffness));

			Action setMode = delegate()
			{
				if (joint != null)
					joint.Motor.Settings.Mode = servo ? MotorMode.Servomechanism : MotorMode.VelocityMotor;
				updateMaterial();
			};
			result.Add(new NotifyBinding(setMode, servo));

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

			JointFactory.Bind(result, main, createJoint, false, creating);

			Action<int> move = delegate(int value)
			{
				if (joint != null && locked)
					goal.Value = value;
			};
			result.Add("Forward", new Command { Action = delegate() { move(maximum); } });
			result.Add("Trigger", new Command<Entity> { Action = delegate(Entity p) { move(maximum); } });
			result.Add("Backward", new Command { Action = delegate() { move(minimum); } });

			Command hitMax = new Command();
			result.Add("HitMax", hitMax);
			Command hitMin = new Command();
			result.Add("HitMin", hitMin);

			float lastX = minimum + (maximum - minimum) * 0.5f;
			result.Add(new Updater
			{
				delegate(float dt)
				{
					if (joint != null)
					{
						// HACK
						a.Orientation = b != null ? b.Orientation : Quaternion.Identity;

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

			Property<bool> startAtMinimum = result.GetOrMakeProperty<bool>("StartAtMinimum", true);

			if (!main.EditorEnabled && startAtMinimum)
			{
				startAtMinimum.Value = false;
				result.Add(new PostInitialization
				{
					delegate()
					{
						Transform transform = result.GetOrCreate<Transform>("MapTransform");
						DynamicMap map = result.Get<DynamicMap>();
						transform.Position.Value = map.GetAbsolutePosition(new Map.Coordinate().Move(dir, minimum));
					}
				});
			}
		}
	}
}
