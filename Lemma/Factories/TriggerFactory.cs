using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class TriggerFactory : Factory<Main>
	{
		public TriggerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Trigger");

			Transform position = new Transform();

			Trigger trigger = new Trigger();
			trigger.Radius.Value = 10.0f;
			result.Add("Trigger", trigger);

			result.Add("Position", position);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			Trigger trigger = result.Get<Trigger>();

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			Trigger.AttachEditorComponents(result, main, this.Color);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(result, result.Get<Trigger>().Target);
		}
	}
}
