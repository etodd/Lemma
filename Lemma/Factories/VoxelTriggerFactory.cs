using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class VoxelTriggerFactory : Factory<Main>
	{
		public VoxelTriggerFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "VoxelTrigger");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;

			Transform transform = entity.GetOrCreate<Transform>("Transform");

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			attachable.Enabled.Value = true;

			VoxelTrigger trigger = entity.GetOrCreate<VoxelTrigger>("VoxelTrigger");
			trigger.Add(new Binding<Voxel.Coord>(trigger.Coord, attachable.Coord));
			trigger.Add(new Binding<Entity.Handle>(trigger.AttachedVoxel, attachable.AttachedVoxel));

			this.SetMain(entity, main);

			trigger.EditorProperties();
			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("AttachVector", attachable.Vector);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}