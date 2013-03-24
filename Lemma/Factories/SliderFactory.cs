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

			PrismaticJoint joint = null;

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

			Action rebuildMotor = delegate()
			{
				if (joint != null)
					main.Space.Remove(joint);

				if (main.EditorEnabled)
					refreshMapTransform();

				Entity parent = parentMap.Value.Target;
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
							DynamicMap dynamicMap = parent.Get<DynamicMap>();
							if (dynamicMap != null)
								joint = new PrismaticJoint(map.PhysicsEntity, dynamicMap.PhysicsEntity, map.PhysicsEntity.Position, dynamicMap.GetAbsoluteVector(dir.Value.GetVector()), map.PhysicsEntity.Position);
							else
								joint = new PrismaticJoint(map.PhysicsEntity, null, map.PhysicsEntity.Position, staticMap.GetAbsoluteVector(dir.Value.GetVector()), map.PhysicsEntity.Position);
							main.Space.Add(joint);
						}
					}
				}
			};
			result.Add(new NotifyBinding(rebuildMotor, parentMap));
			rebuildMotor();

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
					Vector3 normal = Vector3.TransformNormal(dir.Value.GetVector(), transform.Matrix);

					m.Forward = -normal;
					if (normal.Equals(Vector3.Up))
						m.Right = Vector3.Right;
					else if (normal.Equals(Vector3.Down))
						m.Right = Vector3.Left;
					else
						m.Right = Vector3.Normalize(Vector3.Cross(normal, Vector3.Up));
					m.Up = Vector3.Cross(normal, m.Right);
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
