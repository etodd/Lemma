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
	public class VoxelFillFactory : VoxelFactory
	{
		public override Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity entity = base.Create(main, offsetX, offsetY, offsetZ);
			entity.Type = "VoxelFill";
			entity.Create<VoxelFill>("VoxelFill").Enabled.Value = false;
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform mapTransform = entity.GetOrCreate<Transform>("MapTransform");
			mapTransform.Selectable.Value = false;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			VoxelFill voxelFill = entity.GetOrCreate<VoxelFill>("VoxelFill");
			voxelFill.Add(new CommandBinding(voxelFill.Delete, entity.Delete));

			Sound.AttachTracker(entity, voxelFill.RumblePosition);

			this.InternalBind(entity, main, creating, mapTransform, true);

			if (main.EditorEnabled)
			{
				Voxel voxel = entity.Get<Voxel>();

				Action refreshMapTransform = delegate()
				{
					Entity parent = voxelFill.Target.Value.Target;
					if (parent != null && parent.Active)
					{
						Voxel staticMap = parent.Get<Voxel>();
						if (staticMap == null)
							mapTransform.Matrix.Value = transform.Matrix;
						else
						{
							mapTransform.Position.Value = staticMap.GetAbsolutePosition(staticMap.GetRelativePosition(staticMap.GetCoordinate(transform.Matrix.Value.Translation)) - new Vector3(0.5f) + staticMap.Offset + voxel.Offset.Value);
							Matrix parentOrientation = staticMap.Transform;
							parentOrientation.Translation = Vector3.Zero;
							mapTransform.Quaternion.Value = Quaternion.CreateFromRotationMatrix(parentOrientation);
						}
					}
					else
						mapTransform.Matrix.Value = transform.Matrix;
				};

				entity.Add(new NotifyBinding(refreshMapTransform, transform.Matrix, voxel.Offset, voxelFill.Target));
				refreshMapTransform();
			}

			entity.Add("Enable", voxelFill.Enable);
			entity.Add("Disable", voxelFill.Disable);
			entity.Add("IntervalMultiplier", voxelFill.IntervalMultiplier);
			entity.Add("BlockLifetime", voxelFill.BlockLifetime);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, "Target", entity.Get<VoxelFill>().Target);
		}
	}
}
