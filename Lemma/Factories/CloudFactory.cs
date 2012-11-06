using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CloudFactory : Factory
	{
		public CloudFactory()
		{
			this.Color = new Vector3(0.8f, 0.6f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Cloud");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			ModelAlpha clouds = new ModelAlpha();
			clouds.Filename.Value = "Models\\clouds";
			clouds.CullBoundingBox.Value = false;
			result.Add("Clouds", clouds);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			result.CannotSuspendByDistance = true;

			ModelAlpha clouds = result.Get<ModelAlpha>("Clouds");
			clouds.DrawOrder.Value = -9;

			Property<float> height = result.GetOrMakeProperty<float>("Height", true, 1.0f);
			result.Add(new Binding<float>(clouds.GetFloatParameter("Height"), height));

			Property<Vector2> velocity = result.GetOrMakeProperty<Vector2>("Velocity", true, Vector2.One);
			result.Add(new Binding<Vector2>(clouds.GetVector2Parameter("Velocity"), velocity));

			Property<float> time = clouds.GetFloatParameter("Time");

			result.Add(new CommandBinding(main.ReloadedContent, delegate()
			{
				height.Reset();
				velocity.Reset();
				time.Value = 0;
			}));

			result.Add(new Updater
			{
				delegate(float dt)
				{
					time.Value += dt * (1.0f / 60.0f);
				}
			});
		}
	}
}
