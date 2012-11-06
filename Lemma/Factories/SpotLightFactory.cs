using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SpotLightFactory : Factory
	{
		public SpotLightFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "SpotLight");

			Transform transform = new Transform();
			result.Add("Transform", transform);
			SpotLight spotLight = new SpotLight();
			result.Add("SpotLight", spotLight);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			SpotLight spotLight = result.Get<SpotLight>();

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			this.SetMain(result, main);

			spotLight.Add(new TwoWayBinding<Vector3>(spotLight.Position, transform.Position));
			spotLight.Add(new TwoWayBinding<Quaternion>(spotLight.Orientation, transform.Quaternion));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Add(new Binding<Vector3>(model.Color, result.Get<SpotLight>().Color));
			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, delegate(Matrix x)
				{
					x.Forward *= -1.0f;
					return x;
				}, result.Get<Transform>().Matrix));
		}
	}
}
