using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SkyboxFactory : Factory<Main>
	{
		public SkyboxFactory()
		{
			this.Color = new Vector3(0.8f, 0.6f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Skybox");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			ModelAlpha skybox = new ModelAlpha();
			skybox.Filename.Value = "Models\\skybox";
			result.Add("Skybox", skybox);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			result.CannotSuspendByDistance = true;

			ModelAlpha skybox = result.Get<ModelAlpha>("Skybox");
			skybox.DisableCulling.Value = true;
			skybox.CullBoundingBox.Value = false;
			skybox.Add(new Binding<Matrix>(skybox.Transform, result.Get<Transform>().Matrix));
			skybox.DrawOrder.Value = -10;

			Property<float> startDistance = result.GetOrMakeProperty<float>("StartDistance", true, 50);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("StartDistance"), startDistance));
		}
	}
}
