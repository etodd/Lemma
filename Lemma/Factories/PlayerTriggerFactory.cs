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
			this.SetMain(entity, main);
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(entity, main);

			ListProperty<Entity.Handle> targets = entity.GetOrMakeListProperty<Entity.Handle>("Targets");
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				foreach (Entity.Handle target in targets)
				{
					Entity t = target.Target;
					if (t != null && t.Active)
						t.GetCommand<Entity>("Trigger").Execute(p);
				}
			}));

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);

			MapAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, entity.GetOrMakeListProperty<Entity.Handle>("Targets"));
		}
	}
}
