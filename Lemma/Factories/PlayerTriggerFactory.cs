using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PlayerTriggerFactory : Factory<Main>
	{
		public PlayerTriggerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PlayerTrigger");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Position");
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));

			entity.Add("Enable", trigger.Enable);
			entity.Add("Disable", trigger.Disable);
			entity.Add("PlayerEntered", trigger.PlayerEntered);
			entity.Add("PlayerExited", trigger.PlayerExited);

			entity.Add("Enabled", trigger.Enabled);
			entity.Add("Radius", trigger.Radius);
			entity.Add("DeleteOnTrigger", trigger.DeleteOnTrigger);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
