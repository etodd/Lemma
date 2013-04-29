using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PropFactory : Factory
	{
		public PropFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Prop");
			result.Add("Transform", new Transform());
			result.Add("Model", new Model());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>("Model");
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			Model editorModel = result.Get<Model>("EditorModel");
			Property<bool> editorSelected = result.GetOrMakeProperty<bool>("EditorSelected", false);
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, x => !x, editorSelected));
		}
	}

	public class PropAlphaFactory : PropFactory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PropAlpha");
			result.Add("Transform", new Transform());
			Model model = new ModelAlpha();
			model.DrawOrder.Value = 11;
			result.Add("Model", model);

			return result;
		}
	}
}
