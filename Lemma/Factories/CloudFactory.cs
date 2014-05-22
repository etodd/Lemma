using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CloudFactory : Factory<Main>
	{
		public CloudFactory()
		{
			this.Color = new Vector3(0.9f, 0.7f, 0.5f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Cloud");

			Transform transform = new Transform();
			entity.Add("Transform", transform);

			ModelAlpha clouds = new ModelAlpha();
			clouds.Filename.Value = "Models\\clouds";
			clouds.DrawOrder.Value = -9;
			entity.Add("Clouds", clouds);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			base.Bind(entity, main, creating);
			entity.CannotSuspendByDistance = true;

			ModelAlpha clouds = entity.Get<ModelAlpha>("Clouds");
			clouds.CullBoundingBox.Value = false;
			clouds.DisableCulling.Value = true;

			Property<float> height = entity.GetOrMakeProperty<float>("Height", true, 1.0f);
			entity.Add(new Binding<float>(clouds.GetFloatParameter("Height"), height));

			Property<Vector2> velocity = entity.GetOrMakeProperty<Vector2>("Velocity", true, Vector2.One);
			entity.Add(new Binding<Vector2>(clouds.GetVector2Parameter("Velocity"), x => x * (1.0f / 60.0f), velocity));

			entity.Add(new CommandBinding(main.ReloadedContent, delegate()
			{
				height.Reset();
				velocity.Reset();
			}));

			Property<float> startDistance = entity.GetOrMakeProperty<float>("StartDistance", true, 50);
			clouds.Add(new Binding<float>(clouds.GetFloatParameter("StartDistance"), startDistance));
		}
	}
}
