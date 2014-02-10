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
using BEPUphysics.Constraints.TwoEntity;

namespace Lemma.Factories
{
	public class JointFactory
	{
		public static void Bind(Entity result, Main main, Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint, bool allowRotation, bool creating = false)
		{
			Transform mapTransform = result.GetOrCreate<Transform>("MapTransform");

			Transform transform = result.GetOrCreate<Transform>("Transform");

			Factory.Get<DynamicMapFactory>().InternalBind(result, main, creating, mapTransform);

			DynamicMap map = result.Get<DynamicMap>();

			Property<Entity.Handle> parentMap = result.GetOrMakeProperty<Entity.Handle>("Parent");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("Coord");
			Property<Direction> dir = result.GetOrMakeProperty<Direction>("Direction", true);

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
						mapTransform.Position.Value = staticMap.GetAbsolutePosition(staticMap.GetRelativePosition(coord) - new Vector3(0.5f) + staticMap.Offset + map.Offset.Value);
						if (!allowRotation)
							mapTransform.Orientation.Value = parent.Get<Transform>().Orientation;
					}
				}
				else
					mapTransform.Matrix.Value = transform.Matrix;
			};
			if (main.EditorEnabled)
				result.Add(new NotifyBinding(refreshMapTransform, transform.Matrix, map.Offset));

			ISpaceObject joint = null;
			CommandBinding jointDeleteBinding = null, physicsUpdateBinding = null, parentPhysicsUpdateBinding = null;

			Action rebuildJoint = null;
			rebuildJoint = delegate()
			{
				if (joint != null)
				{
					if (joint.Space != null)
						main.Space.Remove(joint);
					result.Remove(jointDeleteBinding);

					if (parentPhysicsUpdateBinding != null)
						result.Remove(parentPhysicsUpdateBinding);
					parentPhysicsUpdateBinding = null;

					if (physicsUpdateBinding != null)
						result.Remove(physicsUpdateBinding);
					physicsUpdateBinding = null;

					joint = null;
					jointDeleteBinding = null;
				}

				Entity parent = parentMap.Value.Target;

				if (main.EditorEnabled)
				{
					refreshMapTransform();
					return;
				}

				if (parent != null)
				{
					if (!parent.Active)
						parent = null;
					else
					{
						Map parentStaticMap = parent.Get<Map>();

						map.PhysicsEntity.Position = mapTransform.Position;
						if (!allowRotation)
							map.PhysicsEntity.Orientation = mapTransform.Quaternion;

						if (dir != Direction.None)
						{
							Vector3 relativeLineAnchor = parentStaticMap.GetRelativePosition(coord) - new Vector3(0.5f) + parentStaticMap.Offset + map.Offset;
							Vector3 lineAnchor = parentStaticMap.GetAbsolutePosition(relativeLineAnchor);
							DynamicMap parentDynamicMap = parent.Get<DynamicMap>();
							joint = createJoint(map.PhysicsEntity, parentDynamicMap == null ? null : parentDynamicMap.PhysicsEntity, lineAnchor, parentStaticMap.GetAbsoluteVector(dir.Value.GetVector()), parentStaticMap.GetAbsolutePosition(coord));
							main.Space.Add(joint);
							map.PhysicsEntity.ActivityInformation.Activate();

							if (parentDynamicMap != null)
							{
								parentPhysicsUpdateBinding = new CommandBinding(parentDynamicMap.PhysicsUpdated, rebuildJoint);
								result.Add(parentPhysicsUpdateBinding);
							}

							physicsUpdateBinding = new CommandBinding(map.PhysicsUpdated, rebuildJoint);
							result.Add(physicsUpdateBinding);

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
			result.Add(new CommandBinding(result.Delete, delegate()
			{
				if (joint != null && joint.Space != null)
				{
					main.Space.Remove(joint);
					joint = null;
				}
			}));
			result.Add(new CommandBinding(map.OnSuspended, delegate()
			{
				if (joint != null && joint.Space != null)
					main.Space.Remove(joint);
			}));
			result.Add(new CommandBinding(map.OnResumed, delegate()
			{
				if (joint != null && joint.Space == null)
					main.Space.Add(joint);
			}));
			rebuildJoint();
			Command rebuildJointCommand = new Command
			{
				Action = rebuildJoint,
			};
			result.Add("RebuildJoint", rebuildJointCommand);

			if (main.EditorEnabled)
				JointFactory.attachEditorComponents(result, main);
		}

		private static void attachEditorComponents(Entity result, Main main)
		{
			Transform transform = result.Get<Transform>();

			Property<Entity.Handle> parentMap = result.GetOrMakeProperty<Entity.Handle>("Parent");

			EntityConnectable.AttachEditorComponents(result, main, parentMap);
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

					m.Forward = -normal;
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
		}
	}
}
