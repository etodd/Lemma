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
			return new Entity(main, "Cloud");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			entity.CannotSuspendByDistance = true;

			Cloud settings = entity.GetOrCreate<Cloud>("Settings");

			ModelAlpha clouds = entity.GetOrCreate<ModelAlpha>("Clouds");
			clouds.Filename.Value = "AlphaModels\\clouds";
			clouds.DrawOrder.Value = -9;
			clouds.CullBoundingBox.Value = false;
			clouds.DisableCulling.Value = true;

			base.Bind(entity, main, creating);

			clouds.Add(new Binding<Matrix>(clouds.Transform, transform.Matrix));

			clouds.Add(new Binding<string, bool>(clouds.TechniquePostfix, x => x ? "Infinite" : "", settings.Infinite));

			clouds.Add(new Binding<float>(clouds.GetFloatParameter("Height"), settings.Height));

			clouds.Add(new Binding<Vector3>(clouds.GetVector3Parameter("CameraPosition"), main.Camera.Position));

			clouds.Add(new Binding<Vector2>(clouds.GetVector2Parameter("Velocity"), x => x * (1.0f / 60.0f), settings.Velocity));

			clouds.Add(new Binding<float>(clouds.GetFloatParameter("StartDistance"), settings.StartDistance));

			entity.Add("Height", settings.Height);
			entity.Add("Velocity", settings.Velocity);
			entity.Add("StartDistance", settings.StartDistance);
			entity.Add("Color", clouds.Color);
			entity.Add("Alpha", clouds.Alpha);
			entity.Add("Infinite", settings.Infinite);
			entity.Add("Shadowed", settings.Shadowed);
		}
	}
}
