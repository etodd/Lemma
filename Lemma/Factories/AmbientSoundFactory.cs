using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class AmbientSoundFactory : Factory<Main>
	{
		public AmbientSoundFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "AmbientSound");

			entity.Add("Transform", new Transform());

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);

			Transform transform = entity.Get<Transform>();

			Property<bool> is3D = entity.GetOrMakeProperty<bool>("Is3D", true);

			Property<string> play = entity.GetOrMakeProperty<string>("Play", true);
			Property<string> stop = entity.GetOrMakeProperty<string>("Stop", true);

			entity.CannotSuspendByDistance = !is3D;
			entity.Add(new NotifyBinding(delegate()
			{
				entity.CannotSuspendByDistance = !is3D;
			}, is3D));

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				VoxelAttachable.MakeAttachable(entity, main);

			if (!main.EditorEnabled)
			{
				AkSoundEngine.PostEvent(play, entity);
				SoundKiller.Add(entity, stop);
			}
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
