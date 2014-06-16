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
			return new Entity(main, "AmbientSound");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			AmbientSound ambientSound = entity.GetOrCreate<AmbientSound>("AmbientSound");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();
			VoxelAttachable.BindTarget(entity, ambientSound.Position);

			entity.Add("Play", ambientSound.Enable);
			entity.Add("Stop", ambientSound.Disable);
			entity.Add("Playing", ambientSound.Enabled);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
