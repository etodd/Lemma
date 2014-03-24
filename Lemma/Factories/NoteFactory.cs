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
			model.Filename.Value = "Models\\papers";
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			Property<string> text = result.GetOrMakeProperty<string>("Text", true);
			Property<string> image = result.GetOrMakeProperty<string>("Image", true);

			Property<bool> collected = result.GetOrMakeProperty<bool>("Collected");
			result.Add(new NotifyBinding(delegate()
			{
				if (collected)
				{
					GameMain gameMain = (GameMain)main;
					List<Entity> notes = main.Get("Note").ToList();
					Container msg = gameMain.ShowMessage
					(
						result,
						notes.Where(x => x.GetOrMakeProperty<bool>("Collected")).Count().ToString()
						+ " / " + notes.Count.ToString() + " notes collected"
					);
					gameMain.HideMessage(result, msg, 4.0f);
				}
			}, collected));

			trigger.Serialize = false;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Radius.Value = 4.0f;

			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				p.GetOrMakeProperty<Entity.Handle>("Note").Value = result;
			}));

			trigger.Add(new CommandBinding<Entity>(trigger.PlayerExited, delegate(Entity p)
			{
				if (p != null)
					p.GetOrMakeProperty<Entity.Handle>("Note").Value = null;
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
