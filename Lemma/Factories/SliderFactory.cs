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
			Entity result = Factory.Get<DynamicMapFactory>().Create(main);
			result.Type = "Slider";

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform mapTransform = result.GetOrCreate<Transform>("MapTransform");

			Transform transform = result.GetOrCreate<Transform>("Transform");

			Factory.Get<DynamicMapFactory>().InternalBind(result, main, creating, mapTransform);

			DynamicMap map = result.Get<DynamicMap>();

			Property<Entity.Handle> parentMap = result.GetOrMakeProperty<Entity.Handle>("Parent");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("Coord");
			Property<Direction> dir = result.GetOrMakeProperty<Direction>("Direction", true);
			Property<int> minimum = result.GetOrMakeProperty<int>("Minimum", true);
			Property<int> maximum = result.GetOrMakeProperty<int>("Maximum", true);
			Property<bool> locked = result.GetOrMakeProperty<bool>("Locked", true);
			Property<float> speed = result.GetOrMakeProperty<float>("Speed", true, 5);

			Action refreshMapTransform = delegate()
			{
				Entity parent = parentMap.Value.Target;
				if (parent != null)
				{
					if (!parent.Active)
						parent = null;
					else
					{
						Map staticMap = parent.Get<Map>();
						coord.Value = staticMap.GetCoordinate(transform.Position);
						mapTransform.Position.Value = staticMap.GetAbsolutePosition(staticMap.GetRelativePosition(coord) - new Vector3(0.5f) + staticMap.Offset + map.Offset);
						mapTransform.Orientation.Value = parent.Get<Transform>().Orientation;
					}
				}
				else
					mapTransform.Matrix.Value = transform.Matrix;
			};
			if (main.EditorEnabled)
				result.Add(new NotifyBinding(refreshMapTransform, transform.Matrix, map.Offset));

			PrismaticJoint joint = null;
			CommandBinding jointDeleteBinding = null, physicsUpdateBinding = null;

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

			Action rebuildJoint = null;
			rebuildJoint = delegate()
			{
				if (joint != null)
				{
					main.Space.Remove(joint);
					result.Remove(jointDeleteBinding);
					if (physicsUpdateBinding != null)
						result.Remove(physicsUpdateBinding);
					physicsUpdateBinding = null;
					joint = null;
					jointDeleteBinding = null;
				}

				Entity parent = parentMap.Value.Target;

				if (main.EditorEnabled)
					refreshMapTransform();

				if (parent != null)
				{
					if (!parent.Active)
						parent = null;
					else
					{
						Map staticMap = parent.Get<Map>();

						map.PhysicsEntity.Position = mapTransform.Position;
						map.PhysicsEntity.Orientation = mapTransform.Quaternion;

						if (dir != Direction.None && !main.EditorEnabled)
						{
							Vector3 relativeLineAnchor = staticMap.GetRelativePosition(coord) - new Vector3(0.5f) + staticMap.Offset + map.Offset;
							Vector3 lineAnchor = staticMap.GetAbsolutePosition(relativeLineAnchor);
							DynamicMap dynamicMap = parent.Get<DynamicMap>();
							joint = new PrismaticJoint(map.PhysicsEntity, dynamicMap == null ? null : dynamicMap.PhysicsEntity, map.PhysicsEntity.Position, staticMap.GetAbsoluteVector(dir.Value.GetVector()), lineAnchor);
							joint.Motor.Settings.Mode = MotorMode.Servomechanism;
							main.Space.Add(joint);
							setLimits();
							setLocked();
							setSpeed();

							if (dynamicMap != null)
							{
								physicsUpdateBinding = new CommandBinding(dynamicMap.PhysicsUpdated, rebuildJoint);
								result.Add(physicsUpdateBinding);
							}

							jointDeleteBinding = new CommandBinding(parent.Delete, delegate()
							{
								parentMap.Value = null;
							});
							result.Add(jointDeleteBinding);
						}
					}
				}
			};
			result.Add(new NotifyBinding(rebuildJoint, parentMap));
			rebuildJoint();

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

			if (main.EditorEnabled)
				this.AttachEditorComponents(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Transform transform = result.Get<Transform>();

			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			result.Add("EditorSelected", selected);

			Property<Entity.Handle> parentMap = result.GetOrMakeProperty<Entity.Handle>("Parent");

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					parentMap.Value = entity;
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			Action recalculateLine = delegate()
			{
				connectionLines.Lines.Clear();
				Entity parent = parentMap.Value.Target;
				if (parent != null)
				{
					connectionLines.Lines.Add(new LineDrawer.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(parent.Get<Transform>().Position, connectionLineColor)
					});
				}
			};

			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			model.Editable = false;
			model.Serialize = false;
			result.Add("DirectionModel", model);

			Property<Direction> dir = result.GetProperty<Direction>("Direction");
			Transform mapTransform = result.Get<Transform>("MapTransform");
			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				Matrix m = Matrix.Identity;
				m.Translation = transform.Position;

				if (dir == Direction.None)
					m.Forward = m.Right = m.Up = Vector3.Zero;
				else
				{
					Vector3 normal = Vector3.TransformNormal(dir.Value.GetVector(), mapTransform.Matrix);

					m.Forward = normal;
					if (normal.Equals(Vector3.Up))
						m.Right = Vector3.Left;
					else if (normal.Equals(Vector3.Down))
						m.Right = Vector3.Right;
					else
						m.Right = Vector3.Normalize(Vector3.Cross(normal, Vector3.Down));
					m.Up = Vector3.Cross(normal, m.Left);
				}
				return m;
			}, transform.Matrix, mapTransform.Matrix));

			NotifyBinding recalculateBinding = null;
			Action rebuildBinding = delegate()
			{
				if (recalculateBinding != null)
				{
					connectionLines.Remove(recalculateBinding);
					recalculateBinding = null;
				}
				if (parentMap.Value.Target != null)
				{
					recalculateBinding = new NotifyBinding(recalculateLine, parentMap.Value.Target.Get<Transform>().Matrix);
					connectionLines.Add(recalculateBinding);
				}
				recalculateLine();
			};
			connectionLines.Add(new NotifyBinding(rebuildBinding, parentMap));

			connectionLines.Add(new NotifyBinding(recalculateLine, selected));
			connectionLines.Add(new NotifyBinding(recalculateLine, () => selected, transform.Position));
			result.Add(connectionLines);
		}
	}
}
