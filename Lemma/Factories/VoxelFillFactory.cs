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
			this.InternalBind(entity, main, creating, null, true);

			VoxelAttachable.MakeAttachable(entity, main);

			VoxelFill voxelFill = entity.GetOrCreate<VoxelFill>("VoxelFill");
			voxelFill.Add(new CommandBinding(voxelFill.Delete, entity.Delete));

			Voxel map = entity.Get<Voxel>();
			map.Editable = false;

			BindCommand(entity, voxelFill.Enable, "Enable");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
