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
					List<Entity> notes = main.Get("Note").ToList();
					int notesCollected = notes.Where(x => x.GetOrMakeProperty<bool>("Collected")).Count();
					int total = notes.Count;
					Container msg = main.Menu.ShowMessageFormat
					(
						entity,
						"\\notes read",
						notesCollected, total
					);
					main.Menu.HideMessage(entity, msg, 4.0f);
				}
			}, collected));

			trigger.Serialize = false;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Radius.Value = 3.5f;

			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				PlayerFactory.Instance.GetOrMakeProperty<Entity.Handle>("Note").Value = entity;
			}));

			trigger.Add(new CommandBinding(trigger.PlayerExited, delegate()
			{
				if (PlayerFactory.Instance != null)
					PlayerFactory.Instance.GetOrMakeProperty<Entity.Handle>("Note").Value = null;
			}));

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				VoxelAttachable.MakeAttachable(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			Property<bool> editorSelected = entity.GetOrMakeProperty<bool>("EditorSelected", false);
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !editorSelected, editorSelected));

			VoxelAttachable.AttachEditorComponents(entity, main, editorModel.Color);
			PlayerTrigger.AttachEditorComponents(entity, main, editorModel.Color);
		}
	}
}
