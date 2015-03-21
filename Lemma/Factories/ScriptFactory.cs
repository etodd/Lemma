using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Reflection;

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
			entity.Add("ExecuteOnLoad", script.ExecuteOnLoad);
			entity.Add("DeleteOnExecute", script.DeleteOnExecute);
			entity.Add("Script", script.Name, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.MapDirectory, null, ".cs", delegate()
				{
					List<string> scripts = new List<string>();
					Assembly assembly = Assembly.GetExecutingAssembly();
					foreach (Type type in assembly.GetTypes())
					{
						if (type.Namespace == Script.ScriptNamespace && type.IsClass && type.BaseType == typeof(GameScripts.ScriptBase))
						{
							bool available = false;
							if (main.Settings.GodModeProperty)
								available = true;
							else
							{
								FieldInfo prop = type.GetField("AvailableInReleaseEditor", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
								if ((bool)prop.GetValue(type) == true)
									available = true;
							}
							if (available)
								scripts.Add(type.Name);
						}
					}
					return scripts;
				}),
				RefreshOnChange = true,
			});
			entity.Add("Errors", script.Errors, null, true);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Model model = entity.Get<Model>("EditorModel");
			model.Add(new Binding<Vector3, string>(model.Color, x => string.IsNullOrEmpty(x) ? Vector3.One : new Vector3(1.0f, 0.0f, 0.0f), entity.Get<Script>().Errors));
		}
	}
}
