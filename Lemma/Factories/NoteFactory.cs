using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class NoteFactory : Factory
	{
		public NoteFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Note");
			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.GetOrCreate<Transform>("Transform");
			PlayerTrigger trigger = result.GetOrCreate<PlayerTrigger>();
			Model model = result.GetOrCreate<Model>("Model");
			this.SetMain(result, main);
			model.Serialize = false;
			model.Filename.Value = "Maps\\papers";
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			Property<string> text = result.GetOrMakeProperty<string>("Text", true);
			Property<string> image = result.GetOrMakeProperty<string>("Image", true);

			trigger.Serialize = false;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Radius.Value = 4.0f;

			Container msg = null;
			GameMain gameMain = (GameMain)main;
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				p.GetOrMakeProperty<string>("NoteText").Value = text;
				p.GetOrMakeProperty<string>("NoteImage").Value = image;
				msg = gameMain.ShowMessage("[" + gameMain.Settings.TogglePhone.Value.ToString() + "]");
			}));

			trigger.Add(new CommandBinding<Entity>(trigger.PlayerExited, delegate(Entity p)
			{
				p.GetOrMakeProperty<string>("NoteText").Value = null;
				p.GetOrMakeProperty<string>("NoteImage").Value = null;

				if (msg != null)
				{
					gameMain.HideMessage(msg);
					msg = null;
				}
			}));

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			Model editorModel = result.Get<Model>("EditorModel");
			Property<bool> editorSelected = result.GetOrMakeProperty<bool>("EditorSelected", false);
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !editorSelected, editorSelected));

			MapAttachable.AttachEditorComponents(result, main, editorModel.Color);
			PlayerTrigger.AttachEditorComponents(result, main, editorModel.Color);
		}
	}
}
