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

			Property<bool> vertical = result.GetOrMakeProperty<bool>("VerticalLimit", true);
			Property<float> godRays = result.GetOrMakeProperty<float>("GodRays", true, 0.25f);
			skybox.Add
			(
				new Binding<string>
				(
					skybox.TechniquePostfix,
					() => (vertical ? "Vertical" : "") + (main.LightingManager.HasGlobalShadowLight && godRays > 0.0f && ((GameMain)main).Settings.EnableGodRays ? "GodRays" : ""),
					vertical, main.LightingManager.HasGlobalShadowLight, godRays, ((GameMain)main).Settings.EnableGodRays
				)
			);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("VerticalSize"), result.GetOrMakeProperty<float>("VerticalSize", true, 10.0f)));
			Property<float> verticalCenter = result.GetOrMakeProperty<float>("VerticalCenter", true);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("VerticalCenter"), verticalCenter));
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("GodRayStrength"), godRays));
			skybox.Add(new Binding<Vector3>(skybox.GetVector3Parameter("CameraPosition"), main.Camera.Position));
			skybox.Add(new Binding<RenderTarget2D>(skybox.GetRenderTarget2DParameter("ShadowMap" + Components.Model.SamplerPostfix), main.LightingManager.GlobalShadowMap));
			skybox.Add(new Binding<Matrix>(skybox.GetMatrixParameter("ShadowViewProjectionMatrix"), main.LightingManager.GlobalShadowViewProjection));
			skybox.Add(new Binding<Matrix>(skybox.GetMatrixParameter("ShadowViewProjectionMatrix"), main.LightingManager.GlobalShadowViewProjection));

			Property<float> startDistance = result.GetOrMakeProperty<float>("StartDistance", true, 50);
			skybox.Add(new Binding<float>(skybox.GetFloatParameter("StartDistance"), startDistance));
		}
	}
}
