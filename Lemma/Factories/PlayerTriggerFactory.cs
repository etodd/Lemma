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
			trigger.Radius.Editable = true;
			trigger.Enabled.Editable = true;
			this.SetMain(entity, main);

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				VoxelAttachable.MakeAttachable(entity, main);

			ListProperty<Entity.Handle> targets = entity.GetOrMakeListProperty<Entity.Handle>("Targets");
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				foreach (Entity.Handle target in targets)
				{
					Entity t = target.Target;
					if (t != null && t.Active)
						t.GetCommand("Trigger").Execute();
				}
			}));

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, entity.GetOrMakeListProperty<Entity.Handle>("Targets"));
		}
	}
}
