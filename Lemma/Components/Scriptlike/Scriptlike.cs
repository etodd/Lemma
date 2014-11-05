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

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\pyramid";
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Serialize = false;

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));
		}
	}
}
