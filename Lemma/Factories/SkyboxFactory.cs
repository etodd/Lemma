using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SkyboxFactory : Factory
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

			Model skybox = new Model();
			skybox.Filename.Value = "Models\\skybox";
			skybox.CullBoundingBox.Value = false;
			result.Add("Skybox", skybox);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			result.CannotSuspendByDistance = true;

			Model skybox = result.Get<Model>("Skybox");
			skybox.Add(new Binding<Matrix>(skybox.Transform, result.Get<Transform>().Matrix));
			skybox.DrawOrder.Value = -10;
		}
	}
}
