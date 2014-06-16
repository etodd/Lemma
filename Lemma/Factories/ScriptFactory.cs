using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class ScriptFactory : Factory<Main>
	{
		public ScriptFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Script");
			entity.Add("Transform", new Transform());
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;

			Script script = entity.GetOrCreate<Script>("Script");
			script.Add(new CommandBinding(script.Delete, entity.Delete));

			this.SetMain(entity, main);

			entity.Add("Execute", script.Execute, Command.Perms.LinkableAndExecutable);
			entity.Add("Name", script.Name);
			entity.Add("DeleteOnExecute", script.DeleteOnExecute);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Model model = entity.Get<Model>("EditorModel");
			model.Add(new Binding<Vector3, string>(model.Color, x => string.IsNullOrEmpty(x) ? Vector3.One : new Vector3(1.0f, 0.0f, 0.0f), entity.Get<Script>().Errors));

			EntityConnectable.AttachEditorComponents(entity, entity.Get<Script>().ConnectedEntities);
		}
	}
}
