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

			Cloud settings = entity.GetOrCreate<Cloud>("Settings");

			ModelAlpha clouds = entity.Get<ModelAlpha>("Clouds");
			clouds.CullBoundingBox.Value = false;
			clouds.DisableCulling.Value = true;

			clouds.Add(new Binding<float>(clouds.GetFloatParameter("Height"), settings.Height));

			clouds.Add(new Binding<Vector2>(clouds.GetVector2Parameter("Velocity"), x => x * (1.0f / 60.0f), settings.Velocity));

			clouds.Add(new Binding<float>(clouds.GetFloatParameter("StartDistance"), settings.StartDistance));

			entity.Add("Height", settings.Height);
			entity.Add("Velocity", settings.Velocity);
			entity.Add("StartDistance", settings.StartDistance);
			entity.Add("Color", clouds.Color);
			entity.Add("Alpha", clouds.Alpha);
		}
	}
}
