using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PointLightFactory : Factory
	{
		public PointLightFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PointLight");

			result.Add("Transform", new Transform());
			PointLight pointLight = new PointLight();
			pointLight.Attenuation.Value = 10.0f;
			pointLight.Color.Value = Vector3.One;
			result.Add("PointLight", pointLight);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.Get<PointLight>();
			light.Add(new TwoWayBinding<Vector3>(result.Get<Transform>().Position, light.Position));

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.IsInstanced.Value = false;
			model.Add(new Binding<Vector3>(model.Color, result.Get<PointLight>().Color));
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));
		}
	}
}
