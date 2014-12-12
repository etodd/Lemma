using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public enum Technique { Render, Shadow, NonPostProcessed, Clip };

	public class RenderParameters
	{
		public Camera Camera;
		private Plane[] clipPlanes;
		public Plane[] ClipPlanes
		{
			get
			{
				return this.clipPlanes;
			}
			set
			{
				this.clipPlanes = value;
				if (value == null)
					this.ClipPlaneData = new Vector4[] { Vector4.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero };
				else
					this.ClipPlaneData = value.Select(x => new Vector4(x.Normal, x.D)).ToArray();
			}
		}
		public Vector4[] ClipPlaneData;
		private Technique technique;
		public Technique Technique
		{
			get
			{
				return this.technique;
			}
			set
			{
				this.technique = value;
				this.TechniqueString = value.ToString();
			}
		}
		public string TechniqueString;
		public bool ReverseCullOrder;
		public RenderTarget2D DepthBuffer;
		public RenderTarget2D FrameBuffer;
		public bool IsMainRender;
		public static readonly RenderParameters Default = new RenderParameters();
		public RenderParameters Clone()
		{
			return new RenderParameters
			{
				Camera = this.Camera,
				clipPlanes = (Plane[])this.clipPlanes.Clone(),
				ClipPlaneData = (Vector4[])this.ClipPlaneData.Clone(),
				Technique = this.Technique,
				ReverseCullOrder = this.ReverseCullOrder,
				DepthBuffer = this.DepthBuffer,
				FrameBuffer = this.FrameBuffer,
				IsMainRender = this.IsMainRender,
			};
		}
	}

	/// <summary>
	/// Deferred renderer
	/// </summary>
	public class Renderer : Component<Main>, IGraphicsComponent
	{
		private LightingManager lightingManager;

		// Geometry
		private static FullscreenQuad quad;
		private static Microsoft.Xna.Framework.Graphics.Model pointLightModel;
		private static Microsoft.Xna.Framework.Graphics.Model spotLightModel;

		public Property<float> BlurAmount = new Property<float>();
		public Property<float> SpeedBlurAmount = new Property<float>();
		public Property<Vector3> Tint = new Property<Vector3> { Value = Vector3.One };
		public Property<float> InternalGamma = new Property<float>();
		public Property<float> Gamma = new Property<float> { Value = 1.0f };
		public Property<float> Brightness = new Property<float>();
		public Property<float> MotionBlurAmount = new Property<float> { Value = 1.0f };
		private Texture2D lightRampTexture;
		public Property<string> LightRampTexture = new Property<string>();
		public Property<bool> EnableBloom = new Property<bool> { Value = true };
		public Property<bool> EnableSSAO = new Property<bool> { Value = true };

		private Point screenSize;

		// Effects
		private static Effect globalLightEffect;
		private static Effect pointLightEffect;
		private static Effect spotLightEffect;
		private Effect compositeEffect;
		private Effect motionBlurEffect;
		private Effect bloomEffect;
		private Effect blurEffect;
		private Effect clearEffect;
		private Effect downsampleEffect;
		private Effect ssaoEffect;

		// Render targets
		private RenderTarget2D lightingBuffer;
		private RenderTarget2D specularBuffer;
		private RenderTarget2D depthBuffer;
		private RenderTarget2D normalBuffer;
		private RenderTarget2D colorBuffer1;
		private RenderTarget2D colorBuffer2;
		private RenderTarget2D hdrBuffer1;
		private RenderTarget2D hdrBuffer2;
		private RenderTarget2D halfBuffer1;
		private RenderTarget2D halfBuffer2;
		private RenderTarget2D halfDepthBuffer;
		private Texture2D ssaoRandomTexture;
		private RenderTarget2D normalBufferLastFrame;
		private SpriteBatch spriteBatch;

		private bool allowSSAO;
		private bool allowBloom;
		private bool allowPostAlphaDrawables;
		private bool allowToneMapping;
		private bool justReallocatedBuffers;
		
		private static bool staticReloadBound = false;

		/// <summary>
		/// The class constructor
		/// </summary>
		/// <param name="graphicsDevice">The GraphicsDevice to use for rendering</param>
		/// <param name="contentManager">The ContentManager from which to load Effects</param>
		public Renderer(Main main, bool allowHdr, bool allowBloom, bool allowToneMapping, bool allowSSAO, bool allowPostAlphaDrawables)
		{
			this.allowBloom = allowBloom;
			this.allowSSAO = allowSSAO;
			this.allowPostAlphaDrawables = allowPostAlphaDrawables;
			this.allowToneMapping = allowToneMapping;
			this.hdr = allowHdr;
			this.lightingManager = main.LightingManager;
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new SetBinding<float>(this.BlurAmount, delegate(float value)
			{
				this.blurEffect.Parameters["BlurAmount"].SetValue(value);
			}));
			this.Add(new ChangeBinding<string>(this.LightRampTexture, delegate(string old, string file)
			{
				if (old != file)
					this.loadLightRampTexture(file);
			}));

			this.Add(new SetBinding<float>(this.InternalGamma, delegate(float value)
			{
				this.Gamma.Reset();
			}));

			this.Add(new SetBinding<float>(this.MotionBlurAmount, delegate(float value)
			{
				this.motionBlurEffect.Parameters["MotionBlurAmount"].SetValue(value);
			}));
			this.Add(new SetBinding<float>(this.SpeedBlurAmount, delegate(float value)
			{
				this.motionBlurEffect.Parameters["SpeedBlurAmount"].SetValue(value);
			}));
			this.SpeedBlurAmount.Value = 0.0f;

			this.Add(new SetBinding<float>(this.Gamma, delegate(float value)
			{
				this.bloomEffect.Parameters["Gamma"].SetValue(value + this.InternalGamma);
			}));
			this.Add(new SetBinding<Vector3>(this.Tint, delegate(Vector3 value)
			{
				this.bloomEffect.Parameters["Tint"].SetValue(value);
			}));
			this.Add(new SetBinding<float>(this.Brightness, delegate(float value)
			{
				this.bloomEffect.Parameters["Brightness"].SetValue(value);
			}));
		}

		public void Debug()
		{
			this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
			this.spriteBatch.Draw(this.lightingBuffer, Vector2.Zero, Color.White);
			this.spriteBatch.End();
		}

		private const string paramLightSampler = "Ramp" + Model.SamplerPostfix;
		private void loadLightRampTexture(string file)
		{
			this.lightRampTexture = file == null ? (Texture2D)null : this.main.Content.Load<Texture2D>(file);
			this.bloomEffect.Parameters[paramLightSampler].SetValue(this.lightRampTexture);
		}

		private const string paramCloudSampler = "Cloud" + Model.SamplerPostfix;
		private static string[] paramSourceSamplers = new[]
		{
			string.Format("Source{0}0", Model.SamplerPostfix),
			string.Format("Source{0}1", Model.SamplerPostfix),
			string.Format("Source{0}2", Model.SamplerPostfix),
			string.Format("Source{0}3", Model.SamplerPostfix),
		};
		private static string[] paramSourceDimensions = new[]
		{
			"SourceDimensions0",
			"SourceDimensions1",
			"SourceDimensions2",
			"SourceDimensions3",
		};

		private static void reload(Main m)
		{
			// Load static resources
			if (Renderer.quad == null)
			{
				Renderer.quad = new FullscreenQuad();
				Renderer.quad.SetMain(m);
				Renderer.quad.LoadContent(false);
				Renderer.quad.Awake();
			}
			else
				Renderer.quad.LoadContent(true);

			// Load light models
			Renderer.pointLightModel = m.Content.Load<Microsoft.Xna.Framework.Graphics.Model>("InternalModels\\pointlight");
			Renderer.spotLightModel = m.Content.Load<Microsoft.Xna.Framework.Graphics.Model>("InternalModels\\spotlight");

			Renderer.globalLightEffect = m.Content.Load<Effect>("Effects\\PostProcess\\GlobalLight").Clone();
			Renderer.pointLightEffect = m.Content.Load<Effect>("Effects\\PostProcess\\PointLight").Clone();
			Renderer.spotLightEffect = m.Content.Load<Effect>("Effects\\PostProcess\\SpotLight").Clone();
			Renderer.globalLightEffect.Parameters[paramCloudSampler].SetValue(m.Content.Load<Texture2D>("AlphaModels\\cloud_texture"));
		}

		public void LoadContent(bool reload)
		{
			if (!Renderer.staticReloadBound)
			{
				Main m = this.main;
				new CommandBinding(m.ReloadedContent, delegate()
				{
					Renderer.reload(m);
				});
				Renderer.staticReloadBound = true;
				Renderer.reload(m);
			}

			if (this.spriteBatch != null)
				this.spriteBatch.Dispose();
			this.spriteBatch = new SpriteBatch(this.main.GraphicsDevice);

			// Initialize our buffers
			this.ReallocateBuffers(this.screenSize);
		}

		private bool hdr;

		private SurfaceFormat hdrSurfaceFormat
		{
			get
			{
				return this.hdr ? SurfaceFormat.HdrBlendable : SurfaceFormat.Color;
			}
		}

		private const string paramRandomSampler = "Random" + Model.SamplerPostfix;
		public void ReallocateBuffers(Point size)
		{
			this.compositeEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Composite").Clone();
			this.compositeEffect.CurrentTechnique = this.compositeEffect.Techniques["Composite"];
			this.blurEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Blur").Clone();

			this.downsampleEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Downsample").Clone();
			this.ssaoEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\SSAO").Clone();
			this.ssaoRandomTexture = this.main.Content.Load<Texture2D>("Textures\\random");
			this.ssaoEffect.Parameters[paramRandomSampler].SetValue(this.ssaoRandomTexture);

			this.bloomEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Bloom").Clone();

			this.loadLightRampTexture(this.LightRampTexture);

			this.clearEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Clear").Clone();

			this.motionBlurEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\MotionBlur").Clone();

			this.screenSize = size;

			if (size.X > 0 && size.Y > 0)
			{
				// Lighting buffer
				if (this.lightingBuffer != null && !this.lightingBuffer.IsDisposed)
					this.lightingBuffer.Dispose();
				this.lightingBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);

				// Specular lighting buffer
				if (this.specularBuffer != null && !this.specularBuffer.IsDisposed)
					this.specularBuffer.Dispose();
				this.specularBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);

				// Depth buffer
				if (this.depthBuffer != null && !this.depthBuffer.IsDisposed)
					this.depthBuffer.Dispose();
				this.depthBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.HalfVector2,
													DepthFormat.Depth24,
													0,
													RenderTargetUsage.DiscardContents);

				// Normal buffer
				if (this.normalBuffer != null && !this.normalBuffer.IsDisposed)
					this.normalBuffer.Dispose();
				this.normalBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);

				// Color buffer 1
				if (this.colorBuffer1 != null && !this.colorBuffer1.IsDisposed)
					this.colorBuffer1.Dispose();
				this.colorBuffer1 = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.Depth24,
													0,
													RenderTargetUsage.DiscardContents);

				// Color buffer 2
				if (this.colorBuffer2 != null && !this.colorBuffer2.IsDisposed)
					this.colorBuffer2.Dispose();
				this.colorBuffer2 = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.Depth24,
													0,
													RenderTargetUsage.DiscardContents);

				if (this.hdr)
				{
					// HDR buffer 1
					if (this.hdrBuffer1 != null && !this.hdrBuffer1.IsDisposed)
						this.hdrBuffer1.Dispose();
					this.hdrBuffer1 = new RenderTarget2D(this.main.GraphicsDevice,
														size.X,
														size.Y,
														false,
														this.hdrSurfaceFormat,
														DepthFormat.Depth24,
														0,
														RenderTargetUsage.DiscardContents);

					// HDR buffer 2
					if (this.hdrBuffer2 != null && !this.hdrBuffer2.IsDisposed)
						this.hdrBuffer2.Dispose();
					this.hdrBuffer2 = new RenderTarget2D(this.main.GraphicsDevice,
														size.X,
														size.Y,
														false,
														this.hdrSurfaceFormat,
														DepthFormat.None,
														0,
														RenderTargetUsage.DiscardContents);
				}
				else
				{
					this.hdrBuffer1 = this.colorBuffer1;
					this.hdrBuffer2 = this.colorBuffer2;
				}

				if (this.normalBufferLastFrame != null)
				{
					if (!this.normalBufferLastFrame.IsDisposed)
						this.normalBufferLastFrame.Dispose();
					this.normalBufferLastFrame = null;
				}

				// Normal buffer from last frame
				this.normalBufferLastFrame = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);

				if (this.halfBuffer1 != null)
				{
					if (!this.halfBuffer1.IsDisposed)
						this.halfBuffer1.Dispose();
					this.halfBuffer1 = null;
				}
				if (this.halfBuffer2 != null)
				{
					if (!this.halfBuffer2.IsDisposed)
						this.halfBuffer2.Dispose();
					this.halfBuffer2 = null;
				}
				if (this.halfDepthBuffer != null)
				{
					if (!this.halfDepthBuffer.IsDisposed)
						this.halfDepthBuffer.Dispose();
					this.halfDepthBuffer = null;
				}

				if (this.allowBloom || this.allowSSAO)
				{
					this.halfBuffer1 = new RenderTarget2D(this.main.GraphicsDevice,
						size.X / 2,
						size.Y / 2,
						false,
						SurfaceFormat.Color,
						DepthFormat.None,
						0,
						RenderTargetUsage.DiscardContents);
					this.halfBuffer2 = new RenderTarget2D(this.main.GraphicsDevice,
						size.X / 2,
						size.Y / 2,
						false,
						SurfaceFormat.Color,
						DepthFormat.None,
						0,
						RenderTargetUsage.DiscardContents);
				}

				if (this.allowSSAO)
				{
					this.halfDepthBuffer = new RenderTarget2D(this.main.GraphicsDevice,
						size.X / 2,
						size.Y / 2,
						false,
						SurfaceFormat.Single,
						DepthFormat.None,
						0,
						RenderTargetUsage.DiscardContents);
				}
			}

			this.BlurAmount.Reset();
			this.SpeedBlurAmount.Reset();
			this.Tint.Reset();
			this.InternalGamma.Reset();
			this.Gamma.Reset();
			this.Brightness.Reset();
			this.MotionBlurAmount.Reset();
			this.LightRampTexture.Reset();

			this.justReallocatedBuffers = true;
		}

		private RenderTarget2D[] sources0 = new RenderTarget2D[0];
		private RenderTarget2D[] sources1 = new RenderTarget2D[1];
		private RenderTarget2D[] sources2 = new RenderTarget2D[2];
		private RenderTarget2D[] sources3 = new RenderTarget2D[3];
		private RenderTarget2D[] sources4 = new RenderTarget2D[4];
		private RenderTarget2D[] destinations1 = new RenderTarget2D[1];
		private RenderTarget2D[] destinations2 = new RenderTarget2D[2];

		public void SetRenderTargets(RenderParameters p)
		{
			if (this.needBufferReallocation())
				this.ReallocateBuffers(this.screenSize);

			p.Camera.ViewportSize.Value = this.screenSize;
			this.main.GraphicsDevice.SetRenderTargets(this.colorBuffer1, this.depthBuffer, this.normalBuffer);
			p.Camera.SetParameters(this.clearEffect);
			this.destinations1[0] = this.colorBuffer1;
			this.setTargetParameters(this.sources0, this.destinations1, this.clearEffect);
			Color color = this.lightingManager.BackgroundColor;
			this.clearEffect.Parameters["BackgroundColor"].SetValue(new Vector3((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f));
			this.main.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
			this.main.GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
			this.main.GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;
			this.main.GraphicsDevice.SamplerStates[4] = SamplerState.PointClamp;
			this.applyEffect(this.clearEffect);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
		}

		private bool needBufferReallocation()
		{
			return this.lightingBuffer.IsDisposed
				|| this.specularBuffer.IsDisposed
				|| this.depthBuffer.IsDisposed
				|| this.normalBuffer.IsDisposed
				|| this.colorBuffer1.IsDisposed
				|| this.colorBuffer2.IsDisposed
				|| (this.halfBuffer1 != null && this.halfBuffer1.IsDisposed)
				|| (this.halfBuffer2 != null && this.halfBuffer2.IsDisposed)
				|| (this.halfDepthBuffer != null && this.halfDepthBuffer.IsDisposed)
				|| (this.hdrBuffer1 != null && this.hdrBuffer1.IsDisposed)
				|| (this.hdrBuffer2 != null && this.hdrBuffer2.IsDisposed)
				|| (this.normalBufferLastFrame != null && this.normalBufferLastFrame.IsDisposed);
		}

		private RasterizerState reverseCullState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };

		public void PostProcess(RenderTarget2D result, RenderParameters parameters)
		{
			if (this.needBufferReallocation())
				return;

			Vector3 originalCameraPosition = parameters.Camera.Position;
			Matrix originalViewMatrix = parameters.Camera.View;
			BoundingFrustum originalBoundingFrustum = parameters.Camera.BoundingFrustum;

			parameters.Camera.Position.Value = Vector3.Zero;
			Matrix newViewMatrix = originalViewMatrix;
			newViewMatrix.Translation = Vector3.Zero;
			parameters.Camera.View.Value = newViewMatrix;

			RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;

			bool enableSSAO = this.allowSSAO && this.EnableSSAO;

			if (enableSSAO)
			{
				// Down-sample depth buffer
				this.downsampleEffect.CurrentTechnique = this.downsampleEffect.Techniques["DownsampleDepth"];
				this.sources2[0] = this.depthBuffer;
				this.sources2[1] = this.normalBuffer;
				this.destinations2[0] = this.halfDepthBuffer;
				this.destinations2[1] = this.halfBuffer1;
				if (!this.preparePostProcess(this.sources2, this.destinations2, this.downsampleEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Compute SSAO
				parameters.Camera.SetParameters(this.ssaoEffect);
				this.ssaoEffect.CurrentTechnique = this.ssaoEffect.Techniques["SSAO"];
				this.sources2[0] = this.halfDepthBuffer;
				this.sources2[1] = this.halfBuffer1;
				this.destinations1[0] = this.halfBuffer2;
				if (!this.preparePostProcess(this.sources2, this.destinations1, this.ssaoEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Blur
				this.ssaoEffect.CurrentTechnique = this.ssaoEffect.Techniques["BlurHorizontal"];
				this.sources2[0] = this.halfBuffer2;
				this.sources2[1] = this.halfDepthBuffer;
				this.destinations1[0] = this.halfBuffer1;
				if (!this.preparePostProcess(this.sources2, this.destinations1, this.ssaoEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				this.ssaoEffect.CurrentTechnique = this.ssaoEffect.Techniques["Composite"];
				this.sources2[0] = this.halfBuffer1;
				this.sources2[1] = this.halfDepthBuffer;
				this.destinations1[0] = this.halfBuffer2;
				if (!this.preparePostProcess(this.sources2, this.destinations1, this.ssaoEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
			}

			// Global lighting
			this.destinations2[0] = this.lightingBuffer;
			this.destinations2[1] = this.specularBuffer;
			if (!this.setTargets(this.destinations2))
				return;
			string globalLightTechnique = "GlobalLight";
			if (this.lightingManager.EnableGlobalShadowMap && this.lightingManager.HasGlobalShadowLight)
			{
				if (parameters.IsMainRender)
				{
					if (this.lightingManager.HasGlobalShadowLightClouds)
					{
						if (this.lightingManager.EnableDetailGlobalShadowMap)
							globalLightTechnique = "GlobalLightDetailShadowClouds";
						else
							globalLightTechnique = "GlobalLightShadowClouds";
					}
					else
					{
						if (this.lightingManager.EnableDetailGlobalShadowMap)
							globalLightTechnique = "GlobalLightDetailShadow";
						else
							globalLightTechnique = "GlobalLightShadow";
					}
				}
				else
					globalLightTechnique = "GlobalLightShadow";
			}
			Renderer.globalLightEffect.CurrentTechnique = Renderer.globalLightEffect.Techniques[globalLightTechnique];
			parameters.Camera.SetParameters(Renderer.globalLightEffect);
			this.lightingManager.SetGlobalLightParameters(Renderer.globalLightEffect, parameters.Camera, originalCameraPosition);
			this.lightingManager.SetMaterialParameters(Renderer.globalLightEffect);
			this.sources3[0] = this.depthBuffer;
			this.sources3[1] = this.normalBuffer;
			this.sources3[2] = this.colorBuffer1;
			this.destinations2[0] = this.lightingBuffer;
			this.destinations2[1] = this.specularBuffer;
			this.setTargetParameters(this.sources3, this.destinations2, Renderer.globalLightEffect);
			this.applyEffect(Renderer.globalLightEffect);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

			// Spot and point lights
			if (!parameters.ReverseCullOrder)
				this.main.GraphicsDevice.RasterizerState = this.reverseCullState;

			// HACK
			// Increase the far plane to prevent clipping back faces of huge lights
			float originalFarPlane = parameters.Camera.FarPlaneDistance;
			parameters.Camera.FarPlaneDistance.Value *= 4.0f;
			parameters.Camera.SetParameters(Renderer.pointLightEffect);
			parameters.Camera.SetParameters(Renderer.spotLightEffect);
			parameters.Camera.FarPlaneDistance.Value = originalFarPlane;

			// Spot lights
			this.lightingManager.SetMaterialParameters(Renderer.spotLightEffect);
			this.setTargetParameters(this.sources3, this.destinations2, Renderer.spotLightEffect);
			for (int i = 0; i < SpotLight.All.Count; i++)
			{
				SpotLight light = SpotLight.All[i];
				if (!light.Enabled || light.Suspended || light.Attenuation == 0.0f || light.Color.Value.LengthSquared() == 0.0f || !originalBoundingFrustum.Intersects(light.BoundingFrustum))
					continue;

				this.lightingManager.SetSpotLightParameters(light, Renderer.spotLightEffect, originalCameraPosition);
				this.applyEffect(Renderer.spotLightEffect);
				this.drawModel(Renderer.spotLightModel);
			}

			// Point lights
			this.lightingManager.SetMaterialParameters(Renderer.pointLightEffect);
			this.setTargetParameters(this.sources3, this.destinations2, Renderer.pointLightEffect);
			for (int i = 0; i < PointLight.All.Count; i++)
			{
				PointLight light = PointLight.All[i];
				if (!light.Enabled || light.Suspended || light.Attenuation == 0.0f || light.Color.Value.LengthSquared() == 0.0f || !originalBoundingFrustum.Intersects(light.BoundingSphere))
					continue;
				this.lightingManager.SetPointLightParameters(light, Renderer.pointLightEffect, originalCameraPosition);
				this.applyEffect(Renderer.pointLightEffect);
				this.drawModel(Renderer.pointLightModel);
			}

			if (!parameters.ReverseCullOrder)
				this.main.GraphicsDevice.RasterizerState = originalState;

			RenderTarget2D colorSource = this.colorBuffer1;
			RenderTarget2D colorDestination = this.hdrBuffer2;
			RenderTarget2D colorTemp = null;

			// Compositing
			this.compositeEffect.CurrentTechnique = this.compositeEffect.Techniques[enableSSAO ? "CompositeSSAO" : "Composite"];
			this.lightingManager.SetCompositeParameters(this.compositeEffect);
			parameters.Camera.SetParameters(this.compositeEffect);
			this.lightingManager.SetMaterialParameters(this.compositeEffect);
			RenderTarget2D[] compositeSources;
			if (enableSSAO)
			{
				compositeSources = this.sources4;
				compositeSources[3] = this.halfBuffer2;
			}
			else
				compositeSources = this.sources3;

			compositeSources[0] = colorSource;
			compositeSources[1] = this.lightingBuffer;
			compositeSources[2] = this.specularBuffer;

			this.destinations1[0] = colorDestination;

			if (!this.preparePostProcess(compositeSources, this.destinations1, this.compositeEffect))
				return;

			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

			bool enableBloom = this.allowBloom && this.EnableBloom;
			bool enableMotionBlur = this.MotionBlurAmount > 0.0f;
#if VR
			if (this.main.VR)
				enableMotionBlur = false;
#endif

			bool enableBlur = this.BlurAmount > 0.0f;

			// Swap the color buffers
			colorSource = this.hdrBuffer2;
			colorDestination = enableBloom || this.allowToneMapping || enableBlur || enableMotionBlur ? this.hdrBuffer1 : result;

			parameters.DepthBuffer = this.depthBuffer;
			parameters.FrameBuffer = colorSource;

			// Alpha components

			// Drawing to the color destination
			this.destinations1[0] = colorDestination;
			if (!this.setTargets(this.destinations1))
				return;

			// Copy the color source to the destination
			this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, originalState);
			this.spriteBatch.Draw(colorSource, Vector2.Zero, Color.White);
			this.spriteBatch.End();

			parameters.Camera.Position.Value = originalCameraPosition;
			parameters.Camera.View.Value = originalViewMatrix;

			this.main.DrawAlphaComponents(parameters);
			this.main.DrawPostAlphaComponents(parameters);

			// Swap the color buffers
			colorTemp = colorDestination;
			colorDestination = colorSource;
			parameters.FrameBuffer = colorSource = colorTemp;

			// Bloom
			if (enableBloom)
			{
				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["Downsample"];
				this.sources1[0] = colorSource;
				this.destinations1[0] = this.halfBuffer1;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.bloomEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["BlurHorizontal"];
				this.sources1[0] = this.halfBuffer1;
				this.destinations1[0] = this.halfBuffer2;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.bloomEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["BlurVertical"];
				this.sources1[0] = this.halfBuffer2;
				this.destinations1[0] = this.halfBuffer1;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.bloomEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["Composite"];
				this.sources2[0] = colorSource;
				this.sources2[1] = this.halfBuffer1;
				this.destinations1[0] = enableBlur || enableMotionBlur ? this.colorBuffer2 : result;
				if (!this.preparePostProcess(this.sources2, this.destinations1, this.bloomEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Swap the color buffers
				colorDestination = this.colorBuffer1;
				colorSource = this.colorBuffer2;
			}
			else if (this.allowToneMapping)
			{
				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["ToneMapOnly"];
				this.sources1[0] = colorSource;
				this.destinations1[0] = enableBlur || enableMotionBlur ? this.colorBuffer2 : result;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.bloomEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Swap the color buffers
				colorDestination = this.colorBuffer1;
				colorSource = this.colorBuffer2;
			}

			// Motion blur
			if (enableMotionBlur)
			{
				this.motionBlurEffect.CurrentTechnique = this.motionBlurEffect.Techniques["MotionBlur"];
				parameters.Camera.SetParameters(this.motionBlurEffect);

				// If we just reallocated our buffers, don't use the velocity buffer from the last frame because it will be empty
				this.sources3[0] = colorSource;
				this.sources3[1] = this.normalBuffer;
				this.sources3[2] = this.justReallocatedBuffers ? this.normalBuffer : this.normalBufferLastFrame;
				this.destinations1[0] = enableBlur ? colorDestination : result;
				if (!this.preparePostProcess(this.sources3, this.destinations1, this.motionBlurEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Swap the velocity buffers
				RenderTarget2D temp = this.normalBufferLastFrame;
				this.normalBufferLastFrame = this.normalBuffer;
				this.normalBuffer = temp;

				// Swap the color buffers
				colorTemp = colorDestination;
				colorDestination = colorSource;
				colorSource = colorTemp;
			}

			if (enableBlur)
			{
				// Blur
				this.blurEffect.CurrentTechnique = this.blurEffect.Techniques["BlurHorizontal"];
				parameters.Camera.SetParameters(this.blurEffect);
				this.sources1[0] = colorSource;
				this.destinations1[0] = colorDestination;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.blurEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
				this.blurEffect.CurrentTechnique = this.blurEffect.Techniques["Composite"];

				// Swap the color buffers
				colorTemp = colorDestination;
				colorDestination = colorSource;
				colorSource = colorTemp;

				this.sources1[0] = colorSource;
				this.destinations1[0] = result;
				if (!this.preparePostProcess(this.sources1, this.destinations1, this.blurEffect))
					return;
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
			}

			parameters.DepthBuffer = null;
			parameters.FrameBuffer = null;

			this.justReallocatedBuffers = false;
		}

		private void drawModel(Microsoft.Xna.Framework.Graphics.Model model)
		{
			foreach (ModelMesh mesh in model.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					if (part.NumVertices > 0)
					{
						this.main.GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
						this.main.GraphicsDevice.Indices = part.IndexBuffer;
						this.main.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
						Model.DrawCallCounter++;
						Model.TriangleCounter += part.PrimitiveCount;
					}
				}
			}
		}

		private RenderTargetBinding[] bindingsCache = new RenderTargetBinding[2];
		private bool setTargets(RenderTarget2D[] results)
		{
			if (results == null)
			{
				this.main.GraphicsDevice.SetRenderTarget(null);
				return true;
			}
			else if (results.Length == 1)
			{
				if (results[0] != null && results[0].IsDisposed)
					return false;
				this.main.GraphicsDevice.SetRenderTarget(results[0]);
				return true;
			}
			else if (results.Length == 2)
			{
				foreach (RenderTarget2D target in results)
				{
					if (target.IsDisposed)
						return false;
				}
				this.bindingsCache[0] = results[0];
				this.bindingsCache[1] = results[1];
				this.main.GraphicsDevice.SetRenderTargets(this.bindingsCache);
				return true;
			}
			else
				throw new Exception("Divide by cucumber error. Please reboot universe.");
		}

		private bool preparePostProcess(RenderTarget2D[] sources, RenderTarget2D[] results, Effect effect)
		{
			if (!this.setTargets(results))
				return false;
			this.setTargetParameters(sources, results, effect);
			this.applyEffect(effect);
			return true;
		}

		private void setTargetParameters(RenderTarget2D[] sources, RenderTarget2D[] results, Effect effect)
		{
			EffectParameter param;
			for (int i = 0; i < sources.Length; i++)
			{
				param = effect.Parameters[paramSourceSamplers[i]];
				if (param == null)
					break;
				param.SetValue(sources[i]);
				param = effect.Parameters[paramSourceDimensions[i]];
				if (param != null)
					param.SetValue(new Vector2(sources[i].Width, sources[i].Height));
			}
			param = effect.Parameters["DestinationDimensions"];
			if (param != null)
			{
				if (results == null || results.Length == 0 || results[0] == null)
					param.SetValue(new Vector2(this.screenSize.X, this.screenSize.Y));
				else
					param.SetValue(new Vector2(results[0].Width, results[0].Height));
			}
		}

		private void applyEffect(Effect effect)
		{
			effect.CurrentTechnique.Passes[0].Apply();
		}

		public override void delete()
		{
			base.delete();
			this.lightingBuffer.Dispose();
			this.normalBuffer.Dispose();
			this.normalBufferLastFrame.Dispose();
			this.depthBuffer.Dispose();
			this.colorBuffer1.Dispose();
			this.colorBuffer2.Dispose();
			if (this.hdr)
			{
				this.hdrBuffer1.Dispose();
				this.hdrBuffer2.Dispose();
			}
			this.specularBuffer.Dispose();

			this.compositeEffect.Dispose();
			this.blurEffect.Dispose();
			this.clearEffect.Dispose();

			if (this.motionBlurEffect != null)
				this.motionBlurEffect.Dispose();

			if (this.halfDepthBuffer != null)
				this.halfDepthBuffer.Dispose();
			if (this.halfBuffer1 != null)
				this.halfBuffer1.Dispose();
			if (this.halfBuffer2 != null)
				this.halfBuffer2.Dispose();

			if (this.bloomEffect != null)
				this.bloomEffect.Dispose();
		}
	}
}