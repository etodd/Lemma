using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class ScriptFactory : Factory
	{
		public ScriptFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Script");

			result.Add("Transform", new Transform());

			result.Add("Script", new Script());

			result.Add("ExecuteOnLoad", new Property<bool> { Editable = true, Value = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			if (result.GetProperty<bool>("ExecuteOnLoad") && !main.EditorEnabled)
			{
				Script script = result.Get<Script>();
				result.Add("Executor", new PostInitialization
				{
					delegate()
					{
						script.Execute.Execute();
					}
				});
			}
			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			Model model = result.Get<Model>("EditorModel");
			model.Add(new Binding<Vector3, string>(model.Color, x => string.IsNullOrEmpty(x) ? Vector3.One : new Vector3(1.0f, 0.0f, 0.0f), result.Get<Script>().Errors));
		}
	}
}
