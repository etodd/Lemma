using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PointLightFactory : Factory<Main>
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
			result.CannotSuspendByDistance = true;

			PointLight light = result.Get<PointLight>();
			Transform transform = result.Get<Transform>();
			light.Add(new TwoWayBinding<Vector3>(light.Position, transform.Position));

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			Model model = result.Get<Model>("EditorModel");

			Property<Vector3> color = result.Get<PointLight>().Color;
			model.Add(new Binding<Vector3>(model.Color, color));

			MapAttachable.AttachEditorComponents(result, main, color);
		}
	}
}
