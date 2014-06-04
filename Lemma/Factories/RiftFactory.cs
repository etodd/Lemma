using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class RiftFactory : Factory<Main>
	{
		public RiftFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Rift");
			Rift rift = new Rift();
			rift.Enabled.Value = false;
			entity.Add("Rift", rift);
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Position");
			Rift rift = entity.GetOrCreate<Rift>("Rift");
			rift.Enabled.Editable = true;
			this.SetMain(entity, main);

			Property<Matrix> targetTransform = new Property<Matrix>();
			VoxelAttachable.MakeAttachable(entity, main, false, false, null);
			VoxelAttachable.BindTarget(entity, rift.Position);

			Property<Entity.Handle> voxel = entity.GetOrMakeProperty<Entity.Handle>("AttachedVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("AttachedCoordinate");

			rift.Add(new Binding<Entity.Handle>(rift.Voxel, voxel));
			rift.Add(new Binding<Voxel.Coord>(rift.Coordinate, coord));

			entity.Add("Trigger", new Command
			{
				Action = delegate()
				{
					rift.Enabled.Value = true;
				}
			});
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Rift.AttachEditorComponents(entity, main, this.Color);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
