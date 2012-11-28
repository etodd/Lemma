using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class AmbientLightFactory : Factory
	{
		public AmbientLightFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "AmbientLight");
			result.CannotSuspendByDistance = true;

			result.Add("Transform", new Transform());
			AmbientLight ambientLight = new AmbientLight();
			ambientLight.Color.Value = Vector3.One;
			result.Add("AmbientLight", ambientLight);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.IsInstanced.Value = false;
			model.Add(new Binding<Vector3>(model.Color, result.Get<AmbientLight>().Color));
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));
		}
	}
}
