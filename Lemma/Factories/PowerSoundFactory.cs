using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class PowerSoundFactory : Factory<Main>
	{
		public PowerSoundFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PowerSound");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.SetMain(entity, main);

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			attachable.Enabled.Value = true;

			entity.Add("AttachOffset", attachable.Offset);

			if (!main.EditorEnabled)
			{
				Property<Vector3> soundPosition = new Property<Vector3>();
				VoxelAttachable.BindTarget(entity, soundPosition);
				AkGameObjectTracker.Attach(entity, soundPosition);
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WHITE_LIGHT, entity);
				SoundKiller.Add(entity, AK.EVENTS.STOP_WHITE_LIGHT);
			}
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
