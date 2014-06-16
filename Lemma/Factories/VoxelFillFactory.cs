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
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			VoxelFill voxelFill = entity.GetOrCreate<VoxelFill>("VoxelFill");
			voxelFill.Add(new CommandBinding(voxelFill.Delete, entity.Delete));

			this.InternalBind(entity, main, creating, null, true);

			VoxelAttachable.MakeAttachable(entity, main);

			Voxel map = entity.Get<Voxel>();

			entity.Add("Enable", voxelFill.Enable);
			entity.Add("Disable", voxelFill.Disable);
			entity.Add("IntervalMultiplier", voxelFill.IntervalMultiplier);
			entity.Add("BlockLifetime", voxelFill.BlockLifetime);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, entity.Get<VoxelFill>().Target);
		}
	}
}
