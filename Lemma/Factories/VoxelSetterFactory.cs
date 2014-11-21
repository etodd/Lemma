using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class VoxelSetterFactory : Factory<Main>
	{
		public VoxelSetterFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "VoxelSetter");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;

			Transform transform = entity.GetOrCreate<Transform>("Transform");

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			attachable.Enabled.Value = true;

			VoxelSetter voxelSetter = entity.GetOrCreate<VoxelSetter>("VoxelSetter");

			voxelSetter.Add(new Binding<Entity.Handle>(voxelSetter.AttachedVoxel, attachable.AttachedVoxel));
			voxelSetter.Add(new Binding<Voxel.Coord>(voxelSetter.Coord, attachable.Coord));

			this.SetMain(entity, main);

			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("State", voxelSetter.State);
			entity.Add("Contiguous", voxelSetter.Contiguous);
			entity.Add("Set", voxelSetter.Set);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
