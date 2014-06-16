using System;
using ComponentBind;
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
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
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

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main, false, false, null);
			attachable.Enabled.Value = true;
			VoxelAttachable.BindTarget(entity, rift.Position);

			this.SetMain(entity, main);

			PointLight light = entity.GetOrCreate<PointLight>();
			light.Color.Value = new Vector3(1.2f, 1.4f, 1.6f);
			light.Add(new Binding<Vector3>(light.Position, rift.Position));
			light.Add(new Binding<bool>(light.Enabled, () => rift.Type == Rift.Style.In && rift.Enabled, rift.Type, rift.Enabled));
			light.Add(new Binding<float>(light.Attenuation, x => x * 2.0f, rift.CurrentRadius));

			rift.Add(new Binding<Entity.Handle>(rift.Voxel, attachable.AttachedVoxel));
			rift.Add(new Binding<Voxel.Coord>(rift.Coordinate, attachable.Coord));

			entity.Add("Enable", rift.Enable);
			entity.Add("Disable", rift.Disable);
			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("Enabled", rift.Enabled);
			entity.Add("Radius", rift.Radius);
			entity.Add("Style", rift.Type);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Rift.AttachEditorComponents(entity, main, this.Color);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
