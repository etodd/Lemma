using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class LightingManager : Component<Main>, IGraphicsComponent
	{
		public enum DynamicShadowSetting { Off, Low, Medium, High, Ultra };
		private const int maxDirectionalLights = 3;

		private const int maxMaterials = 16;

		private int globalShadowMapSize;
		private int detailGlobalShadowMapSize;
		private const float lightShadowThreshold = 60.0f;
		private const float globalShadowFocusInterval = 10.0f;
		private const float detailGlobalShadowSizeRatio = 0.15f;
		private const float detailGlobalShadowFocusInterval = 1.0f;
		private int spotShadowMapSize;
		private int maxShadowedSpotLights;

		private Vector3[] directionalLightDirections = new Vector3[LightingManager.maxDirectionalLights];
		private Vector3[] directionalLightColors = new Vector3[LightingManager.maxDirectionalLights];
		private float directionalLightCloudShadow;
		private Vector2 directionalLightCloudVelocity;

		private Vector3 ambientLightColor = Vector3.Zero;

		public Property<Microsoft.Xna.Framework.Graphics.RenderTarget2D> GlobalShadowMap = new Property<Microsoft.Xna.Framework.Graphics.RenderTarget2D>();
		public Property<Microsoft.Xna.Framework.Graphics.RenderTarget2D> DetailGlobalShadowMap = new Property<Microsoft.Xna.Framework.Graphics.RenderTarget2D>();
		private Microsoft.Xna.Framework.Graphics.RenderTarget2D[] spotShadowMaps;
		private Dictionary<IComponent, int> shadowMapIndices = new Dictionary<IComponent, int>();
		private DirectionalLight globalShadowLight;
		public Property<Matrix> GlobalShadowViewProjection = new Property<Matrix>();
		public Property<Matrix> DetailGlobalShadowViewProjection = new Property<Matrix>();
		private bool globalShadowMapRenderedLastFrame;

		private Camera shadowCamera;

		public bool EnableGlobalShadowMap { get; private set; }
		public Property<bool> EnableDetailGlobalShadowMap = new Property<bool> { Value = true };

		public Property<bool> HasGlobalShadowLight = new Property<bool>();
		public Property<bool> HasGlobalShadowLightClouds = new Property<bool>();

		public Property<string> EnvironmentMap = new Property<string>();
		public Property<Vector3> EnvironmentColor = new Property<Vector3>();
		public Property<Color> BackgroundColor = new Property<Color>();

		public Property<DynamicShadowSetting> DynamicShadows = new Property<DynamicShadowSetting> { Value = DynamicShadowSetting.Off };

		private TextureCube environmentMap;

		private void loadEnvironmentMap(string file)
		{
			this.environmentMap = file == null ? (TextureCube)null : this.main.Content.Load<TextureCube>(file);
		}

		public override void Awake()
		{
			base.Awake();

			this.EnvironmentMap.Set = delegate(string file)
			{
				if (this.EnvironmentMap.InternalValue != file)
				{
					this.EnvironmentMap.InternalValue = file;
					this.loadEnvironmentMap(file);
				}
			};

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
						this.maxShadowedSpotLights = 0;
						break;
					case DynamicShadowSetting.Low:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.detailGlobalShadowMapSize = 512;
						this.maxShadowedSpotLights = 0;
						break;
					case DynamicShadowSetting.Medium:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.detailGlobalShadowMapSize = 1024;
						this.spotShadowMapSize = 256;
						this.maxShadowedSpotLights = 1;
						break;
					case DynamicShadowSetting.High:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.detailGlobalShadowMapSize = 1024;
						this.spotShadowMapSize = 512;
						this.maxShadowedSpotLights = 1;
						break;
					case DynamicShadowSetting.Ultra:
						this.EnableGlobalShadowMap = true;
						this.globalShadowMapSize = 1024;
						this.detailGlobalShadowMapSize = 2048;
						this.spotShadowMapSize = 1024;
						this.maxShadowedSpotLights = 1;
						break;
				}
				if (this.GlobalShadowMap.Value != null)
				{
					this.GlobalShadowMap.Value.Dispose();

					foreach (Microsoft.Xna.Framework.Graphics.RenderTarget2D target in this.spotShadowMaps)
						target.Dispose();
					
					this.DetailGlobalShadowMap.Value.Dispose();
				}

				if (this.EnableGlobalShadowMap)
				{
					this.GlobalShadowMap.Value = new Microsoft.Xna.Framework.Graphics.RenderTarget2D
					(
						this.main.GraphicsDevice,
						this.globalShadowMapSize,
						this.globalShadowMapSize,
						false,
						Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
						Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
						0,
						Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents
					);
					
					this.DetailGlobalShadowMap.Value = new Microsoft.Xna.Framework.Graphics.RenderTarget2D
					(
						this.main.GraphicsDevice,
						this.detailGlobalShadowMapSize,
						this.detailGlobalShadowMapSize,
						false,
						Microsoft.Xna.Framework.Graphics.SurfaceFormat.Single,
						Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24,
						0,
						Microsoft.Xna.Framework.Graphics.RenderTargetUsage.DiscardContents
					);
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

		public void LoadContent(bool reload)
		{
			if (reload)
				this.DynamicShadows.Reset();
		}

		private Dictionary<Model.Material, int> materials = new Dictionary<Model.Material, int>();
		private Vector2[] materialData = new Vector2[LightingManager.maxMaterials];

		public void ClearMaterials()
		{
			this.materials.Clear();
			this.materials[new Model.Material()] = 0; // Material with no lighting
		}

		public void SetMaterials()
		{
			foreach (KeyValuePair<Model.Material, int> pair in this.materials)
				this.materialData[pair.Value] = new Vector2(pair.Key.SpecularPower, pair.Key.SpecularIntensity);
		}

		public void UpdateGlobalLights()
		{
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
					this.directionalLightCloudShadow = light.CloudShadow;
					this.directionalLightCloudVelocity = light.CloudVelocity;
					index = 0;
					this.globalShadowLight = light;
				}
				this.directionalLightDirections[index] = Vector3.Transform(new Vector3(0, 0, 1), light.Quaternion);
				this.directionalLightColors[index] = light.Color;
				directionalLightIndex++;
			}
			while (directionalLightIndex < LightingManager.maxDirectionalLights)
			{
				this.directionalLightColors[directionalLightIndex] = Vector3.Zero;
				directionalLightIndex++;
			}

			bool hasGlobalLight = this.globalShadowLight != null;
			if (this.HasGlobalShadowLight != hasGlobalLight)
				this.HasGlobalShadowLight.Value = hasGlobalLight;

			bool hasClouds = hasGlobalLight && this.directionalLightCloudShadow > 0;
			if (this.HasGlobalShadowLightClouds != hasClouds)
				this.HasGlobalShadowLightClouds.Value = hasClouds;

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
		}

		public void RenderGlobalShadowMap(Camera camera)
		{
			Vector3 focus;
			Vector3 shadowCameraOffset;
			float size = camera.FarPlaneDistance * 1.5f;

			Vector3 globalShadowLightForward = Vector3.Transform(Vector3.Forward, this.globalShadowLight.Quaternion);

			if (this.globalShadowMapRenderedLastFrame)
				this.globalShadowMapRenderedLastFrame = false;
			else
			{
				focus = camera.Position;
				focus = new Vector3((float)Math.Round(focus.X / LightingManager.globalShadowFocusInterval), (float)Math.Round(focus.Y / LightingManager.globalShadowFocusInterval), (float)Math.Round(focus.Z / LightingManager.globalShadowFocusInterval)) * LightingManager.globalShadowFocusInterval;
				shadowCameraOffset = globalShadowLightForward * size * 1.25f;
				this.shadowCamera.View.Value = Matrix.CreateLookAt(focus + shadowCameraOffset, focus, Vector3.Up);

				this.shadowCamera.SetOrthographicProjection(size, size, 1.0f, size * 2.5f);

				this.main.GraphicsDevice.SetRenderTarget(this.GlobalShadowMap);
				this.main.GraphicsDevice.Clear(new Color(0, 0, 0));
				this.main.DrawScene(new RenderParameters { Camera = this.shadowCamera, Technique = Technique.Shadow });

				this.GlobalShadowViewProjection.Value = this.shadowCamera.ViewProjection;
				this.globalShadowMapRenderedLastFrame = true;
			}

			// Detail map
			if (this.EnableDetailGlobalShadowMap)
			{
				focus = camera.Position;
				focus = new Vector3((float)Math.Round(focus.X / LightingManager.detailGlobalShadowFocusInterval), (float)Math.Round(focus.Y / LightingManager.detailGlobalShadowFocusInterval), (float)Math.Round(focus.Z / LightingManager.detailGlobalShadowFocusInterval)) * LightingManager.detailGlobalShadowFocusInterval;
				shadowCameraOffset = globalShadowLightForward * camera.FarPlaneDistance;
				this.shadowCamera.View.Value = Matrix.CreateLookAt(focus + shadowCameraOffset, focus, Vector3.Up);

				float detailSize = size * LightingManager.detailGlobalShadowSizeRatio;
				this.shadowCamera.SetOrthographicProjection(detailSize, detailSize, 1.0f, size * 2.0f);

				this.main.GraphicsDevice.SetRenderTarget(this.DetailGlobalShadowMap);
				this.main.GraphicsDevice.Clear(new Color(0, 0, 0));
				this.main.DrawScene(new RenderParameters { Camera = this.shadowCamera, Technique = Technique.Shadow });

				this.DetailGlobalShadowViewProjection.Value = this.shadowCamera.ViewProjection;
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

			// Collect spot lights
			List<SpotLight> shadowedSpotLights = new List<SpotLight>();
			foreach (SpotLight light in SpotLight.All)
			{
				if (light.Enabled && !light.Suspended && light.Shadowed && light.Attenuation > 0.0f && camera.BoundingFrustum.Value.Intersects(light.BoundingFrustum))
					shadowedSpotLights.Add(light);
			}
			shadowedSpotLights = shadowedSpotLights.Select(x => new { Light = x, Score = (x.Position.Value - camera.Position.Value).LengthSquared() / x.Attenuation }).Where(x => x.Score < LightingManager.lightShadowThreshold).OrderBy(x => x.Score).Take(this.maxShadowedSpotLights).Select(x => x.Light).ToList();

			// Render spot shadow maps
			int index = 0;
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

		public void SetGlobalLightParameters(Microsoft.Xna.Framework.Graphics.Effect effect, Camera camera, Vector3 cameraPos)
		{
			effect.Parameters["DirectionalLightDirections"].SetValue(this.directionalLightDirections);
			effect.Parameters["DirectionalLightColors"].SetValue(this.directionalLightColors);
			effect.Parameters["EnvironmentColor"].SetValue(this.EnvironmentColor);
			effect.Parameters["Environment" + Model.SamplerPostfix].SetValue(this.environmentMap);
			effect.Parameters["CloudShadow"].SetValue(this.directionalLightCloudShadow);
			effect.Parameters["CloudOffset"].SetValue(this.directionalLightCloudVelocity * (this.main.TotalTime / 60.0f));
			effect.Parameters["CameraPosition"].SetValue(cameraPos);

			if (this.EnableGlobalShadowMap)
			{
				effect.Parameters["ShadowViewProjectionMatrix"].SetValue(Matrix.CreateTranslation(cameraPos) * this.GlobalShadowViewProjection);
				effect.Parameters["ShadowMapSize"].SetValue(this.globalShadowMapSize);
				effect.Parameters["ShadowMap" + Model.SamplerPostfix].SetValue(this.GlobalShadowMap);

				if (this.EnableDetailGlobalShadowMap)
				{
					effect.Parameters["DetailShadowViewProjectionMatrix"].SetValue(Matrix.CreateTranslation(cameraPos) * this.DetailGlobalShadowViewProjection);
					effect.Parameters["DetailShadowMapSize"].SetValue(this.detailGlobalShadowMapSize);
					effect.Parameters["DetailShadowMap" + Model.SamplerPostfix].SetValue(this.DetailGlobalShadowMap);
				}
			}
		}

		public void SetCompositeParameters(Microsoft.Xna.Framework.Graphics.Effect effect)
		{
			effect.Parameters["AmbientLightColor"].SetValue(this.ambientLightColor);
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

			if (this.GlobalShadowMap.Value != null)
				this.GlobalShadowMap.Value.Dispose();
		}
	}
}
