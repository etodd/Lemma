using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Scriptlike : Component<Main>
	{
		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Transform transform = entity.Get<Transform>();

			Property<bool> selected = entity.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Model model = new Model();
			model.Filename.Value = "Models\\pyramid";
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Editable = false;
			model.Serialize = false;
			model.Add(new Binding<bool>(model.Enabled, selected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
		}
	}
}
