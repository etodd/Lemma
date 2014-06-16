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
using BEPUphysics.Constraints.TwoEntity;

namespace Lemma.Factories
{
	public class JointFactory
	{
		public static void Bind(Entity entity, Main main, Func<BEPUphysics.Entities.Entity, BEPUphysics.Entities.Entity, Vector3, Vector3, Vector3, ISpaceObject> createJoint, bool allowRotation, bool creating = false)
		{
			Transform mapTransform = entity.GetOrCreate<Transform>("MapTransform");

			Transform transform = entity.GetOrCreate<Transform>("Transform");

			Factory.Get<DynamicVoxelFactory>().InternalBind(entity, main, creating, mapTransform);

			DynamicVoxel map = entity.Get<DynamicVoxel>();

			Components.Joint jointData = entity.GetOrCreate<Components.Joint>("Joint");

			Action refreshMapTransform = delegate()
			{
				Entity parent = jointData.Parent.Value.Target;
				if (parent != null && parent.Active)
				{
					Voxel staticMap = parent.Get<Voxel>();
					jointData.Coord.Value = staticMap.GetCoordinate(transform.Position);
					mapTransform.Position.Value = staticMap.GetAbsolutePosition(staticMap.GetRelativePosition(jointData.Coord) - new Vector3(0.5f) + staticMap.Offset + map.Offset.Value);
					if (!allowRotation)
					{
						Matrix parentOrientation = staticMap.Transform;
						parentOrientation.Translation = Vector3.Zero;
						mapTransform.Orientation.Value = parentOrientation;
					}
				}
				else
					mapTransform.Matrix.Value = transform.Matrix;
			};

			if (main.EditorEnabled)
				entity.Add(new NotifyBinding(refreshMapTransform, transform.Matrix, map.Offset));

			ISpaceObject joint = null;
			CommandBinding jointDeleteBinding = null, parentPhysicsUpdateBinding = null;

			Action updateJoint = null;

			Action rebuildJoint = null;
			rebuildJoint = delegate()
			{
				if (jointDeleteBinding != null)
					entity.Remove(jointDeleteBinding);
				jointDeleteBinding = null;

				if (parentPhysicsUpdateBinding != null)
					entity.Remove(parentPhysicsUpdateBinding);
				parentPhysicsUpdateBinding = null;

				updateJoint();
			};

			updateJoint = delegate()
			{
				if (joint != null)
				{
					if (joint.Space != null)
						main.Space.Remove(joint);
					joint = null;
				}

				Entity parent = jointData.Parent.Value.Target;

				if (main.EditorEnabled)
					refreshMapTransform();
				else if (parent != null && parent.Active)
				{
					Voxel parentStaticMap = parent.Get<Voxel>();

					//map.PhysicsEntity.Position = mapTransform.Position;
					if (!allowRotation)
						map.PhysicsEntity.Orientation = mapTransform.Quaternion;

					if (jointData.Direction != Direction.None)
					{
						Vector3 relativeLineAnchor = parentStaticMap.GetRelativePosition(jointData.Coord) - new Vector3(0.5f) + parentStaticMap.Offset + map.Offset;
						Vector3 lineAnchor = parentStaticMap.GetAbsolutePosition(relativeLineAnchor);
						DynamicVoxel parentDynamicMap = parent.Get<DynamicVoxel>();
						joint = createJoint(map.PhysicsEntity, parentDynamicMap == null ? null : parentDynamicMap.PhysicsEntity, lineAnchor, parentStaticMap.GetAbsoluteVector(jointData.Direction.Value.GetVector()), parentStaticMap.GetAbsolutePosition(jointData.Coord));
						main.Space.Add(joint);
						map.PhysicsEntity.ActivityInformation.Activate();

						if (parentDynamicMap != null && parentPhysicsUpdateBinding == null)
						{
							parentPhysicsUpdateBinding = new CommandBinding(parentDynamicMap.PhysicsUpdated, updateJoint);
							entity.Add(parentPhysicsUpdateBinding);
						}

						if (jointDeleteBinding == null)
						{
							jointDeleteBinding = new CommandBinding(parent.Delete, delegate()
							{
								jointData.Parent.Value = null;
							});
							entity.Add(jointDeleteBinding);
						}
					}
				}
			};
			entity.Add(new CommandBinding(map.PhysicsUpdated, updateJoint));
			entity.Add(new NotifyBinding(rebuildJoint, jointData.Parent));
			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				if (joint != null && joint.Space != null)
				{
					main.Space.Remove(joint);
					joint = null;
				}
			}));
			
			entity.Add(new CommandBinding(map.OnSuspended, delegate()
			{
				if (joint != null && joint.Space != null)
					main.Space.Remove(joint);
			}));
			entity.Add(new CommandBinding(map.OnResumed, delegate()
			{
				if (joint != null && joint.Space == null)
					main.Space.Add(joint);
			}));
			
			entity.Add(new PostInitialization { rebuildJoint });

			if (main.EditorEnabled)
				JointFactory.attachEditorComponents(entity, main);
		}

		private static void attachEditorComponents(Entity entity, Main main)
		{
			Transform transform = entity.Get<Transform>();

			Components.Joint joint = entity.Get<Components.Joint>();
			EntityConnectable.AttachEditorComponents(entity, joint.Parent);
			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			model.Serialize = false;
			entity.Add("DirectionModel", model);

			Transform mapTransform = entity.Get<Transform>("MapTransform");
			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				Matrix m = Matrix.Identity;
				m.Translation = transform.Position;

				if (joint.Direction == Direction.None)
					m.Forward = m.Right = m.Up = Vector3.Zero;
				else
				{
					Vector3 normal = Vector3.TransformNormal(joint.Direction.Value.GetVector(), mapTransform.Matrix);

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
