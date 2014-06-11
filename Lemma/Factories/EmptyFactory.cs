using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class EmptyFactory : Factory<Main>
	{
		public EmptyFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Empty");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.GetOrCreate<Transform>("Transform").Editable = true;

			Command detach = new Command
			{
				Action = delegate()
				{
					entity.Delete.Execute();
				},
			};
			VoxelAttachable.MakeAttachable(entity, main, true, false, detach);

			this.SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
