using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using Lemma.Util;

namespace Lemma.Factories
{
	public class DialogueFileFactory : Factory<Main>
	{
		public DialogueFileFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "DialogueFile");

			entity.Add("Transform", new Transform());

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);
			entity.CannotSuspend = true;

			DialogueFile file = entity.GetOrCreate<DialogueFile>("DialogueFile");
			file.EditorProperties();
		}
	}
}
