using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Graphics;

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
			Entity entity = new Entity(main, "Skybox");

			ModelAlpha skybox = new ModelAlpha();
			skybox.Filename.Value = "Models\\skybox";
			entity.Add("Skybox", skybox);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			ModelAlpha skybox = entity.Get<ModelAlpha>("Skybox");
			skybox.MapContent.Value = true;
			skybox.MapContent.Editable = false;
			skybox.TechniquePostfix.Editable = false;
			base.Bind(entity, main, creating);
			entity.CannotSuspendByDistance = true;

			skybox.DisableCulling.Value = true;
			skybox.CullBoundingBox.Value = false;
			skybox.Add(new Binding<Matrix>(skybox.Transform, transform.Matrix));
			skybox.DrawOrder.Value = -10;

			Property<bool> vertical = entity.GetOrMakeProperty<bool>("VerticalLimit", true);
			Property<float> godRays = entity.GetOrMakeProperty<float>("GodRays", true, 0.25f);
			skybox.Add
			(
				new Binding<string>
				(
					skybox.TechniquePostfix,
					() => (vertical ? "Vertical" : "") + (main.LightingManager.HasGlobalShadowLight && godRays > 0.0f && main.Settings.EnableGodRays ? "GodRays" : ""),
					vertical, main.LightingManager.HasGlobalShadowLight, godRays, main.Settings.EnableGodRays
				)
			);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("VerticalSize"), entity.GetOrMakeProperty<float>("VerticalSize", true, 10.0f)));
			Property<float> verticalCenter = entity.GetOrMakeProperty<float>("VerticalCenter", true);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("VerticalCenter"), verticalCenter));
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("GodRayStrength"), godRays));
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("GodRayExtinction"), entity.GetOrMakeProperty<float>("GodRayExtinction", true, 1.0f)));
			skybox.Add(new Binding<Vector3>(skybox.GetVector3Parameter("CameraPosition"), main.Camera.Position));
			skybox.Add(new Binding<RenderTarget2D>(skybox.GetRenderTarget2DParameter("ShadowMap" + Components.Model.SamplerPostfix), () => main.LightingManager.GlobalShadowMap, main.LightingManager.GlobalShadowMap, main.ScreenSize));
			skybox.Add(new Binding<Matrix>(skybox.GetMatrixParameter("ShadowViewProjectionMatrix"), main.LightingManager.GlobalShadowViewProjection));
			skybox.GetTexture2DParameter("Random" + Lemma.Components.Model.SamplerPostfix).Value = main.Content.Load<Texture2D>("Images\\random");

			Property<float> startDistance = entity.GetOrMakeProperty<float>("StartDistance", true, 50);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("StartDistance"), startDistance));
		}
	}
}
