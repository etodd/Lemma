using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class NoteFactory : Factory<Main>
	{
		public NoteFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Note");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>();
			Model model = entity.GetOrCreate<Model>("Model");
			this.SetMain(entity, main);
			model.Serialize = false;
			model.Filename.Value = "Models\\papers";
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			Property<string> text = entity.GetOrMakeProperty<string>("Text", true);
			Property<string> image = entity.GetOrMakeProperty<string>("Image", true);

			Property<bool> collected = entity.GetOrMakeProperty<bool>("Collected");
			entity.Add(new NotifyBinding(delegate()
			{
				if (collected)
				{
					GameMain gameMain = (GameMain)main;
					List<Entity> notes = main.Get("Note").ToList();
					Container msg = gameMain.Menu.ShowMessage
					(
						entity,
						delegate()
						{
							int notesCollected = notes.Where(x => x.GetOrMakeProperty<bool>("Collected")).Count();
							int total = notes.Count;
							return string.Format(main.Strings.Get("notes read"), notesCollected, total);
						},
						main.Strings.Language
					);
					gameMain.Menu.HideMessage(entity, msg, 4.0f);
				}
			}, collected));

			trigger.Serialize = false;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Radius.Value = 4.0f;

			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				p.GetOrMakeProperty<Entity.Handle>("Note").Value = entity;
			}));

			trigger.Add(new CommandBinding<Entity>(trigger.PlayerExited, delegate(Entity p)
			{
				if (p != null)
					p.GetOrMakeProperty<Entity.Handle>("Note").Value = null;
			}));

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			Property<bool> editorSelected = entity.GetOrMakeProperty<bool>("EditorSelected", false);
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !editorSelected, editorSelected));

			MapAttachable.AttachEditorComponents(entity, main, editorModel.Color);
			PlayerTrigger.AttachEditorComponents(entity, main, editorModel.Color);
		}
	}
}
