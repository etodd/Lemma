using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Components
{
	public class LightingManager : Component<Main>
	{
		public enum DynamicShadowSetting { Off, Low, Medium, High };
		private const int maxDirectionalLights = 3;
		private const int maxMaterials = 16;
		private int globalShadowMapSize;
		private const float lightShadowThreshold = 60.0f;
		private const float globalShadowFocusInterval = 10.0f;
		private int spotShadowMapSize;
		private int maxShadowedSpotLights;
		private int pointShadowMapSize;
		private int maxShadowedPointLights;

		private Vector3[] directionalLightDirections = new Vector3[LightingManager.maxDirectionalLights];
		private Vector3[] directionalLightColors = new Vector3[LightingManager.maxDirectionalLights];

		private Vector3 ambientLightColor = Vector3.Zero;

		private Microsoft.Xna.Framework.Graphics.RenderTarget2D globalShadowMap;
#if MONOGAME
		private Microsoft.Xna.Framework.Graphics.RenderTarget2D[][] pointShadowMaps;
#else
		private Microsoft.Xna.Framework.Graphics.RenderTargetCube[] pointShadowMaps;
#endif
		private Microsoft.Xna.Framework.Graphics.RenderTarget2D[] spotShadowMaps;
		private Dictionary<IComponent, int> shadowMapIndices = new Dictionary<IComponent, int>();
		private DirectionalLight globalShadowLight;
		private Matrix globalShadowViewProjection;
		private PointLight currentPointLight;

		private Camera shadowCamera;

		public bool EnableGlobalShadowMap { get; private set; }

		public bool HasGlobalShadowLight
		{
			get
			{
				return this.globalShadowLight != null;
			}
		}

		public Property<DynamicShadowSetting> DynamicShadows = new Property<DynamicShadowSetting> { Value = DynamicShadowSetting.Off };

		public override void InitializeProperties()
		{
			this.shadowCamera = new Camera();
			this.main.AddComponent(this.shadowCamera);
			this.DynamicShadows.Set = delegate(DynamicShadowSetting value)
			{
				this.shadowMapIndices.Clear();
				this.DynamicShadows.InternalValue = value;
				switch (value)
				{
					case DynamicShadowSetting.Off:
						this.EnableGlobalShadowMap = false;
						this.maxShadowedPointLights = 0;
						this.maxShadowedSpotLights = 0;
						break;
					case DynamicShadowSetting.Low:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.spotShadowMapSize = 256;
						this.maxShadowedSpotLights = 1;
						this.maxShadowedPointLights = 0;
						break;
					case DynamicShadowSetting.Medium:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.spotShadowMapSize = 512;
						this.pointShadowMapSize = 512;
						this.maxShadowedPointLights = 1;
						this.maxShadowedSpotLights = 1;
						break;
					case DynamicShadowSetting.High:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 2048;
						this.spotShadowMapSize = 512;
						this.pointShadowMapSize = 512;
						this.maxShadowedPointLights = 2;
						this.maxShadowedSpotLights = 2;
						break;
				}
				if (this.globalShadowMap != null || this.pointShadowMaps != null)
				{
					this.globalShadowMap.Dispose();
#if MONOGAME
					foreach (Microsoft.Xna.Framework.Graphics.RenderTarget2D target in this.pointShadowMaps.SelectMany(x => x))
						target.Dispose();
#else
					foreach (Microsoft.Xna.Framework.Graphics.RenderTargetCube target in this.pointShadowMaps)
						target.Dispose();
#endif

					foreach (Microsoft.Xna.Framework.Graphics.RenderTarget2D target in this.spotShadowMaps)
						target.Dispose();
				}

				if (this.EnableGlobalShadowMap)
				{
					this.globalShadowMap = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(this.main.GraphicsDevice,
													this.globalShadowMapSize,
													this.globalShadowMapSize,
													false,
													Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
													Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
													0,
													Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);
				}

#if MONOGAME
				this.pointShadowMaps = new Microsoft.Xna.Framework.Graphics.RenderTarget2D[this.maxShadowedPointLights][];
#else
				this.pointShadowMaps = new Microsoft.Xna.Framework.Graphics.RenderTargetCube[this.maxShadowedPointLights];
#endif

				for (int i = 0; i < this.maxShadowedPointLights; i++)
				{
#if MONOGAME
					this.pointShadowMaps[i] = new Microsoft.Xna.Framework.Graphics.RenderTarget2D[6];
					for (int j = 0; j < 6; j++)
					{
						this.pointShadowMaps[i][j] = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(this.main.GraphicsDevice,
															this.pointShadowMapSize,
															this.pointShadowMapSize,
															false,
															Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
															Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
															0,
															Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);
					}
#else
					this.pointShadowMaps[i] = new Microsoft.Xna.Framework.Graphics.RenderTargetCube(this.main.GraphicsDevice,
															this.pointShadowMapSize,
															false,
															Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
															Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
															0,
															Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);
#endif
				}

				this.spotShadowMaps = new Microsoft.Xna.Framework.Graphics.RenderTarget2D[this.maxShadowedSpotLights];
				for (int i = 0; i < this.maxShadowedSpotLights; i++)
				{
					this.spotShadowMaps[i] = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(this.main.GraphicsDevice,
													this.spotShadowMapSize,
													this.spotShadowMapSize,
													false,
													Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
													Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
													0,
													Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents);
				}
			};
		}

		public override void LoadContent(bool reload)
		{
			if (reload)
				this.DynamicShadows.Reset();
		}

		private Dictionary<Model.Material, int> materials = new Dictionary<Model.Material, int>();
		private Vector2[] materialData = new Vector2[LightingManager.maxMaterials];

		public void UpdateGlobalLights()
		{
			foreach (KeyValuePair<Model.Material, int> pair in this.materials)
				this.materialData[pair.Value] = new Vector2(pair.Key.SpecularPower, pair.Key.SpecularIntensity);
			this.materials.Clear();
			this.materials[new Model.Material()] = 0; // Material with no lighting

			this.globalShadowLight = null;
			int directionalLightIndex = 0;
			foreach (DirectionalLight light in DirectionalLight.All.Where(x => x.Enabled && !x.Suspended).Take(LightingManager.maxDirectionalLights))
			{
				// Directional light
				int index = directionalLightIndex;
				if (light.Shadowed && this.globalShadowLight == null)
				{
					// This light is shadowed; swap it into the first slot.
					// By convention the first light is the shadow-caster, if there are any shadow-casting lights.
					this.directionalLightDirections[index] = this.directionalLightDirections[0];
					this.directionalLightColors[index] = this.directionalLightColors[0];
					index = 0;
					this.globalShadowLight = light;
				}
				this.directionalLightDirections[index] = light.Direction;
				this.directionalLightColors[index] = light.Color;
				directionalLightIndex++;
			}
			while (directionalLightIndex < LightingManager.maxDirectionalLights)
			{
				this.directionalLightColors[directionalLightIndex] = Vector3.Zero;
				directionalLightIndex++;
			}

			this.ambientLightColor = Vector3.Zero;
			foreach (AmbientLight light in AmbientLight.All.Where(x => x.Enabled))
				this.ambientLightColor += light.Color;
		}

		public int GetMaterialIndex(float specularPower, float specularIntensity)
		{
			return this.GetMaterialIndex(new Model.Material { SpecularPower = specularPower, SpecularIntensity = specularIntensity, });
		}

		public int GetMaterialIndex(Model.Material key)
		{
			int id;
			if (!this.materials.TryGetValue(key, out id))
			{
				if (this.materials.Count == LightingManager.maxMaterials)
					id = LightingManager.maxMaterials - 1;
				else
					id = this.materials[key] = this.materials.Count;
			}
			return id;
		}

		public void SetRenderParameters(Microsoft.Xna.Framework.Graphics.Effect effect, RenderParameters parameters)
		{
			if (parameters.Technique == Technique.PointLightShadow)
			{
				effect.Parameters["PointLightPosition"].SetValue(this.currentPointLight.Position);
				effect.Parameters["PointLightRadius"].SetValue(this.currentPointLight.Attenuation);
			}
		}

		public void RenderGlobalShadowMap(Camera camera)
		{
			Vector3 focus = camera.Position;
			focus = new Vector3((float)Math.Round(focus.X / LightingManager.globalShadowFocusInterval), (float)Math.Round(focus.Y / LightingManager.globalShadowFocusInterval), (float)Math.Round(focus.Z / LightingManager.globalShadowFocusInterval)) * LightingManager.globalShadowFocusInterval;
			Vector3 shadowCameraOffset = this.globalShadowLight.Direction.Value * -camera.FarPlaneDistance;
			this.shadowCamera.View.Value = Matrix.CreateLookAt(focus + shadowCameraOffset, focus, Vector3.Up);

			float size = camera.FarPlaneDistance;
			this.shadowCamera.SetOrthographicProjection(size, size, 1.0f, size * 2.0f);

			this.main.GraphicsDevice.SetRenderTarget(this.globalShadowMap);
			this.main.GraphicsDevice.Clear(new Color(0, 255, 255));
			this.main.DrawScene(new RenderParameters { Camera = this.shadowCamera, Technique = Technique.Shadow });

			this.globalShadowViewProjection = this.shadowCamera.ViewProjection;
		}

		public void RenderPointShadowMap(PointLight light, int index)
		{
			Matrix[] views = new Matrix[6];

			// Create view matrices
			Vector3 position = light.Position;
			views[0] = Matrix.CreateLookAt(position, position + Vector3.Right, Vector3.Up);
			views[1] = Matrix.CreateLookAt(position, position + Vector3.Left, Vector3.Up);
			views[2] = Matrix.CreateLookAt(position, position + Vector3.Up, Vector3.Backward);
			views[3] = Matrix.CreateLookAt(position, position + Vector3.Down, Vector3.Forward);
			views[4] = Matrix.CreateLookAt(position, position + Vector3.Forward, Vector3.Up);
			views[5] = Matrix.CreateLookAt(position, position + Vector3.Backward, Vector3.Up);

			// Projection matrix
			this.shadowCamera.Position.Value = position;
			this.shadowCamera.SetPerspectiveProjection((float)Math.PI / 2.0f, new Point(this.pointShadowMapSize, this.pointShadowMapSize), 1.0f, Math.Max(2.0f, light.Attenuation));

			this.currentPointLight = light;

			for (int i = 0; i < 6; i++)
			{
				this.shadowCamera.View.Value = views[i];
#if MONOGAME
				this.main.GraphicsDevice.SetRenderTarget(this.pointShadowMaps[index][i]);
#else
				this.main.GraphicsDevice.SetRenderTarget(this.pointShadowMaps[index], (Microsoft.Xna.Framework.Graphics.CubeMapFace)i);
#endif
				this.main.GraphicsDevice.Clear(new Color(0, 255, 255));
				this.main.DrawScene(new RenderParameters { Camera = this.shadowCamera, Technique = Technique.PointLightShadow });
			}
		}

		public void RenderSpotShadowMap(SpotLight light, int index)
		{
			this.shadowCamera.View.Value = light.View;
			this.shadowCamera.SetPerspectiveProjection(light.FieldOfView, new Point(this.spotShadowMapSize, this.spotShadowMapSize), 1.0f, Math.Max(2.0f, light.Attenuation));
			this.main.GraphicsDevice.SetRenderTarget(this.spotShadowMaps[index]);
			this.main.GraphicsDevice.Clear(new Color(0, 255, 255));
			this.main.DrawScene(new RenderParameters { Camera = this.shadowCamera, Technique = Technique.Shadow });
		}

		public void RenderShadowMaps(Camera camera)
		{
			Microsoft.Xna.Framework.Graphics.RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
			Microsoft.Xna.Framework.Graphics.RasterizerState reverseCullState = new Microsoft.Xna.Framework.Graphics.RasterizerState { CullMode = Microsoft.Xna.Framework.Graphics.CullMode.CullClockwiseFace };

			this.main.GraphicsDevice.RasterizerState = reverseCullState;

			this.shadowMapIndices.Clear();
			// Collect point lights
			List<PointLight> shadowedPointLights = new List<PointLight>();
			foreach (PointLight light in PointLight.All)
			{
				if (light.Enabled && !light.Suspended && light.Shadowed && light.Attenuation > 0.0f && camera.BoundingFrustum.Value.Intersects(light.BoundingSphere))
					shadowedPointLights.Add(light);
			}
			shadowedPointLights = shadowedPointLights.Select(x => new { Light = x, Score = (x.Position.Value - camera.Position.Value).LengthSquared() / x.Attenuation }).Where(x => x.Light.AlwaysShadow || x.Score < LightingManager.lightShadowThreshold).OrderBy(x => x.Score).Take(this.maxShadowedPointLights).Select(x => x.Light).ToList();

			// Render point shadow maps
			int index = 0;
			foreach (PointLight light in shadowedPointLights)
			{
				this.shadowMapIndices[light] = index;
				this.RenderPointShadowMap(light, index);
				index++;
			}

			// Collect spot lights
			List<SpotLight> shadowedSpotLights = new List<SpotLight>();
			foreach (SpotLight light in SpotLight.All)
			{
				if (light.Enabled && !light.Suspended && light.Shadowed && light.Attenuation > 0.0f && camera.BoundingFrustum.Value.Intersects(light.BoundingFrustum))
					shadowedSpotLights.Add(light);
			}
			shadowedSpotLights = shadowedSpotLights.Select(x => new { Light = x, Score = (x.Position.Value - camera.Position.Value).LengthSquared() / x.Attenuation }).Where(x => x.Score < LightingManager.lightShadowThreshold).OrderBy(x => x.Score).Take(this.maxShadowedSpotLights).Select(x => x.Light).ToList();

			// Render spot shadow maps
			index = 0;
			foreach (SpotLight light in shadowedSpotLights)
			{
				this.shadowMapIndices[light] = index;
				this.RenderSpotShadowMap(light, index);
				index++;
			}

			this.main.GraphicsDevice.RasterizerState = originalState;

			// Render global shadow map
			if (this.EnableGlobalShadowMap && this.globalShadowLight != null)
				this.RenderGlobalShadowMap(camera);
		}

		public void SetGlobalLightParameters(Microsoft.Xna.Framework.Graphics.Effect effect, Vector3 cameraPos)
		{
			effect.Parameters["DirectionalLightDirections"].SetValue(this.directionalLightDirections);
			effect.Parameters["DirectionalLightColors"].SetValue(this.directionalLightColors);
			effect.Parameters["AmbientLightColor"].SetValue(this.ambientLightColor);
			if (this.EnableGlobalShadowMap)
			{
				effect.Parameters["ShadowViewProjectionMatrix"].SetValue(Matrix.CreateTranslation(cameraPos) * this.globalShadowViewProjection);
				effect.Parameters["ShadowMapSize"].SetValue(this.globalShadowMapSize);
				effect.Parameters["ShadowMap" + Model.SamplerPostfix].SetValue(this.globalShadowMap);
			}
		}

		public void SetMaterialParameters(Microsoft.Xna.Framework.Graphics.Effect effect)
		{
			effect.Parameters["Materials"].SetValue(this.materialData);
		}

		public void SetSpotLightParameters(SpotLight light, Microsoft.Xna.Framework.Graphics.Effect effect, Vector3 cameraPos)
		{
			bool shadowed = light.Shadowed && this.shadowMapIndices.ContainsKey(light);
			effect.CurrentTechnique = effect.Techniques[shadowed ? "SpotLightShadowed" : "SpotLight"];
			if (shadowed)
			{
				effect.Parameters["ShadowMap" + Model.SamplerPostfix].SetValue(this.spotShadowMaps[this.shadowMapIndices[light]]);
				effect.Parameters["ShadowMapSize"].SetValue(this.spotShadowMapSize);
				effect.Parameters["ShadowBias"].SetValue(light.ShadowBias);
			}

			float horizontalScale = (float)Math.Sin(light.FieldOfView * 0.5f) * light.Attenuation;
			float depthScale = (float)Math.Cos(light.FieldOfView * 0.5f) * light.Attenuation;
			effect.Parameters["SpotLightViewProjectionMatrix"].SetValue(Matrix.CreateTranslation(cameraPos) * light.ViewProjection);
			effect.Parameters["SpotLightPosition"].SetValue(light.Position - cameraPos);

			Matrix rotation = Matrix.CreateFromQuaternion(light.Orientation);
			rotation.Forward *= -1.0f;
			effect.Parameters["SpotLightDirection"].SetValue(rotation.Forward);
			effect.Parameters["WorldMatrix"].SetValue(Matrix.CreateScale(horizontalScale, horizontalScale, depthScale) * rotation * Matrix.CreateTranslation(light.Position - cameraPos));

			effect.Parameters["SpotLightRadius"].SetValue(depthScale);
			effect.Parameters["SpotLightColor"].SetValue(light.Color);
			effect.Parameters["Cookie" + Model.SamplerPostfix].SetValue(light.CookieTexture);
		}

		public void SetPointLightParameters(PointLight light, Microsoft.Xna.Framework.Graphics.Effect effect, Vector3 cameraPos)
		{
			bool shadowed = light.Shadowed && this.shadowMapIndices.ContainsKey(light);
			effect.CurrentTechnique = effect.Techniques[shadowed ? "PointLightShadowed" : "PointLight"];
			if (shadowed)
			{
#if MONOGAME
				// TODO: MonoGame point light shadow maps
#else
				effect.Parameters["ShadowMap" + Model.SamplerPostfix].SetValue(this.pointShadowMaps[this.shadowMapIndices[light]]);
#endif
				effect.Parameters["ShadowMapSize"].SetValue(new Vector3(this.pointShadowMapSize));
			}

			effect.Parameters["WorldMatrix"].SetValue(Matrix.CreateScale(light.Attenuation) * Matrix.CreateTranslation(light.Position - cameraPos));
			effect.Parameters["PointLightPosition"].SetValue(light.Position - cameraPos);
			effect.Parameters["PointLightRadius"].SetValue(light.Attenuation);
			effect.Parameters["PointLightColor"].SetValue(light.Color);
		}

		public override void delete()
		{
			base.delete();
			this.shadowCamera.Delete.Execute();
			foreach (Microsoft.Xna.Framework.Graphics.RenderTarget2D shadowMap in this.spotShadowMaps)
				shadowMap.Dispose();
#if MONOGAME
			foreach (Microsoft.Xna.Framework.Graphics.RenderTarget2D shadowMap in this.pointShadowMaps.SelectMany(x => x))
				shadowMap.Dispose();
#else
			foreach (Microsoft.Xna.Framework.Graphics.RenderTargetCube shadowMap in this.pointShadowMaps)
				shadowMap.Dispose();
#endif

			if (this.globalShadowMap != null)
				this.globalShadowMap.Dispose();
		}
	}
}
