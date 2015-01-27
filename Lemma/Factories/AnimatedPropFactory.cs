using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using Lemma.IO;

namespace Lemma.Factories
{
	public class AnimatedPropFactory : Factory<Main>
	{
		public AnimatedPropFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "AnimatedProp");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			AnimatedModel model = entity.GetOrCreate<AnimatedModel>("Model");

			AnimatedProp prop = entity.GetOrCreate<AnimatedProp>("AnimatedProp");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			model.EditorProperties();

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			entity.Add("Visible", model.Enabled);
			ListProperty<string> clips = null;
			if (main.EditorEnabled)
			{
				clips = new ListProperty<string>();
				model.Add(new ChangeBinding<string>(model.Filename, delegate(string old, string value)
				{
					if (model.IsValid)
					{
						clips.Clear();
						clips.AddAll(model.Clips.Keys);
					}
				}));
			}
			entity.Add("Clip", prop.Clip, new PropertyEntry.EditorData { Options = clips, });
			entity.Add("Loop", prop.Loop);
			entity.Add("Enabled", prop.Enabled);
			entity.Add("Enable", prop.Enable);
			entity.Add("Disable", prop.Disable);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model model = entity.Get<Model>("Model");
			Model editorModel = entity.Get<Model>("EditorModel");
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => Editor.EditorModelsVisible && (!entity.EditorSelected || !model.IsValid), entity.EditorSelected, model.IsValid, Editor.EditorModelsVisible));

			VoxelAttachable.AttachEditorComponents(entity, main, editorModel.Color);
		}
	}
}
