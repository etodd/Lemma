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
			return new Entity(main, "Trigger");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Trigger trigger = entity.GetOrCreate<Trigger>("Trigger");

			VoxelAttachable.MakeAttachable(entity, main);

			this.SetMain(entity, main);

			trigger.EditorProperties();
			entity.Add("Enable", trigger.Enable);
			entity.Add("Disable", trigger.Disable);
			entity.Add("Entered", trigger.Entered);
			entity.Add("Exited", trigger.Exited);

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Trigger.AttachEditorComponents(entity, main, this.Color);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, "Target", entity.Get<Trigger>().Target);
		}
	}
}
