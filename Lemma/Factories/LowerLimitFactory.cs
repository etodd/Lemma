using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class LowerLimitFactory : Factory<Main>
	{
		public LowerLimitFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "LowerLimit");

			result.Add("Transform", new Transform());

			return result;
		}

		const float absoluteLimit = -20.0f;
		const float velocityThreshold = -40.0f;

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			result.CannotSuspendByDistance = true;
			Transform transform = result.Get<Transform>();
			transform.Editable = true;
			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);
			
			result.Add(new Updater
			{
				delegate(float dt)
				{
					Entity player = PlayerFactory.Instance;
					if (player != null && player.Active)
					{
						Player p = player.Get<Player>();
						float y = p.Character.Transform.Value.Translation.Y;
						float limit = transform.Position.Value.Y;
						if (y < limit + absoluteLimit || (y < limit && p.Character.LinearVelocity.Value.Y < velocityThreshold))
							player.Delete.Execute();
					}
					else
						player = null;
				}
			});
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);
		}
	}
}
