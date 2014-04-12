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
			Entity result = new Entity(main, "PlayerTrigger");

			Transform position = new Transform();

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 10.0f;
			result.Add("PlayerTrigger", trigger);

			result.Add("Position", position);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			ListProperty<Entity.Handle> targets = result.GetOrMakeListProperty<Entity.Handle>("Targets");
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

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			PlayerTrigger.AttachEditorComponents(result, main, this.Color);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(result, result.GetOrMakeListProperty<Entity.Handle>("Targets"));
		}
	}
}
