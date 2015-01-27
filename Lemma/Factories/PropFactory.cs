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
	public class PropFactory : Factory<Main>
	{
		public PropFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Prop");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			Model model = entity.GetOrCreate<Model>("Model");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			model.EditorProperties();

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			entity.Add("Enabled", model.Enabled);
			entity.Add("Enable", model.Enable);
			entity.Add("Disable", model.Disable);
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

	public class PropAlphaFactory : PropFactory
	{
		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "PropAlpha");
			ModelAlpha model = new ModelAlpha();
			model.DrawOrder.Value = 11;
			entity.Add("Model", model);

			return entity;
		}
	}
}
