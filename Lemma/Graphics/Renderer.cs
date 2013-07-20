using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public enum Technique { Render, MotionBlur, Shadow, PointLightShadow, NonPostProcessed, Clip };

	public struct RenderParameters
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
		public Technique Technique;
		public bool ReverseCullOrder;
		public RenderTarget2D DepthBuffer;
		public RenderTarget2D FrameBuffer;
		public bool IsMainRender;
		public static readonly RenderParameters Default = new RenderParameters();
	}

	/// <summary>
	/// Deferred renderer
	/// </summary>
	public class Renderer : Component
	{
		public delegate void DrawStageDelegate(RenderParameters parameters);

		private LightingManager lightingManager;

		// Geometry
		private static FullscreenQuad quad;
		private static Microsoft.Xna.Framework.Graphics.Model pointLightModel;
		private static Microsoft.Xna.Framework.Graphics.Model spotLightModel;

		public Property<float> BlurAmount = new Property<float>();
		public Property<Vector3> Tint = new Property<Vector3> { Value = Vector3.One };
		public Property<float> InternalGamma = new Property<float> { Value = 0.0f };
		public Property<float> Gamma = new Property<float> { Value = 1.0f };
		public Property<float> MotionBlurAmount = new Property<float> { Value = 1.0f };
		private Texture2D lightRampTexture;
		private TextureCube environmentMap;
		public Property<string> LightRampTexture = new Property<string>();
		public Property<string> EnvironmentMap = new Property<string>();
		public Property<Vector3> EnvironmentColor = new Property<Vector3> { Value = Vector3.One };
		public Property<bool> EnableBloom = new Property<bool> { Value = true };
		public Property<bool> EnableSSAO = new Property<bool> { Value = true };
		public Property<float> BloomThreshold = new Property<float> { Value = 0.9f };

		private static readonly Color defaultBackgroundColor = new Color(8.0f / 255.0f, 13.0f / 255.0f, 19.0f / 255.0f, 0.0f);
		public Property<Color> BackgroundColor = new Property<Color> { Value = Renderer.defaultBackgroundColor };

		private Point screenSize;

		// Effects
		private static Effect globalLightEffect;
		private static Effect pointLightEffect;
		private static Effect spotLightEffect;
		private Effect compositeEffect;
		private Effect toneMapEffect;
		private Effect motionBlurEffect;
		private Effect bloomEffect;
		private Effect blurEffect;
		private Effect clearEffect;
		private Effect depthDownsampleEffect;
		private Effect ssaoEffect;

		// Render targets
		private RenderTarget2D lightingBuffer;
		private RenderTarget2D specularBuffer;
		private RenderTarget2D depthBuffer;
		private RenderTarget2D normalBuffer;
		private RenderTarget2D colorBuffer1;
		private RenderTarget2D colorBuffer2;
		private RenderTarget2D halfBuffer1;
		private RenderTarget2D halfBuffer2;
		private RenderTarget2D halfDepthBuffer;
		private Texture2D ssaoRandomTexture;
		private bool allowMotionBlur;
		private bool allowSSAO;
		private RenderTarget2D velocityBuffer;
		private RenderTarget2D velocityBufferLastFrame;
		private bool allowBloom;
		private RenderTarget2D bloomBuffer;
		private SpriteBatch spriteBatch;

		/// <summary>
		/// The class constructor
		/// </summary>
		/// <param name="graphicsDevice">The GraphicsDevice to use for rendering</param>
		/// <param name="contentManager">The ContentManager from which to load Effects</param>
		public Renderer(Main main, Point size, bool allowMotionBlur, bool allowBloom, bool allowSSAO)
		{
			this.allowMotionBlur = allowMotionBlur;
			this.allowBloom = allowBloom;
			this.allowSSAO = allowSSAO;
			this.lightingManager = main.LightingManager;
			this.screenSize = size;
		}

		public override void InitializeProperties()
		{
			this.BlurAmount.Set = delegate(float value)
			{
				this.BlurAmount.InternalValue = value;
				this.blurEffect.Parameters["BlurAmount"].SetValue(value);
			};
			this.Tint.Set = delegate(Vector3 value)
			{
				this.Tint.InternalValue = value;
				this.toneMapEffect.Parameters["Tint"].SetValue(value);
			};
			this.LightRampTexture.Set = delegate(string file)
			{
				if (this.LightRampTexture.InternalValue != file)
				{
					this.LightRampTexture.InternalValue = file;
					this.loadLightRampTexture(file);
				}
			};

			this.EnvironmentMap.Set = delegate(string file)
			{
				if (this.EnvironmentMap.InternalValue != file)
				{
					this.EnvironmentMap.InternalValue = file;
					this.loadEnvironmentMap(file);
				}
			};

			this.InternalGamma.Set = delegate(float value)
			{
				this.InternalGamma.InternalValue = value;
				this.Gamma.Reset();
			};

			this.Gamma.Set = delegate(float value)
			{
				this.Gamma.InternalValue = value;
				this.toneMapEffect.Parameters["Gamma"].SetValue(value + this.InternalGamma);
			};

			this.EnvironmentColor.Set = delegate(Vector3 value)
			{
				this.EnvironmentColor.InternalValue = value;
				Renderer.globalLightEffect.Parameters["EnvironmentColor"].SetValue(value);
			};

			if (this.allowMotionBlur)
			{
				this.MotionBlurAmount.Set = delegate(float value)
				{
					this.MotionBlurAmount.InternalValue = value;
					this.motionBlurEffect.Parameters["MotionBlurAmount"].SetValue(value);
				};
			}

			if (this.allowBloom)
			{
				this.BloomThreshold.Set = delegate(float value)
				{
					this.BloomThreshold.InternalValue = value;
					this.bloomEffect.Parameters["BloomThreshold"].SetValue(value);
				};
			}
		}

		private void loadLightRampTexture(string file)
		{
			this.lightRampTexture = file == null ? (Texture2D)null : this.main.Content.Load<Texture2D>(file);
			this.toneMapEffect.Parameters["Ramp" + Model.SamplerPostfix].SetValue(this.lightRampTexture);
		}

		private void loadEnvironmentMap(string file)
		{
			this.environmentMap = file == null ? (TextureCube)null : this.main.Content.Load<TextureCube>(file);
			Renderer.globalLightEffect.Parameters["Environment" + Model.SamplerPostfix].SetValue(this.environmentMap);
		}

		public override void LoadContent(bool reload)
		{
			// Load static resources
			if (reload)
				Renderer.quad.LoadContent(true);
			else
			{
				Renderer.quad = new FullscreenQuad();
				Renderer.quad.SetMain(this.main);
			}

			this.spriteBatch = new SpriteBatch(this.main.GraphicsDevice);

			if (Renderer.globalLightEffect == null || reload)
			{
				Renderer.globalLightEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\GlobalLight");
				this.loadEnvironmentMap(this.EnvironmentMap);
				Renderer.pointLightEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\PointLight");
				Renderer.spotLightEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\SpotLight");
			}

			if (Renderer.pointLightModel == null || reload)
			{
				// Load light models
				Renderer.pointLightModel = this.main.Content.Load<Microsoft.Xna.Framework.Graphics.Model>("Models\\sphere");
				Renderer.spotLightModel = this.main.Content.Load<Microsoft.Xna.Framework.Graphics.Model>("Models\\spotlight");
			}

			this.compositeEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Composite").Clone();
			this.compositeEffect.CurrentTechnique = this.compositeEffect.Techniques["Composite"];
			this.blurEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Blur").Clone();

			this.depthDownsampleEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\DepthDownsample").Clone();
			this.ssaoEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\SSAO").Clone();
			this.ssaoRandomTexture = this.main.Content.Load<Texture2D>("Images\\random");
			this.ssaoEffect.Parameters["Random" + Model.SamplerPostfix].SetValue(this.ssaoRandomTexture);

			this.toneMapEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\ToneMap").Clone();
			this.loadLightRampTexture(this.LightRampTexture);

			this.clearEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Clear").Clone();

			if (this.allowMotionBlur)
				this.motionBlurEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\MotionBlur").Clone();

			if (this.allowBloom)
				this.bloomEffect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Bloom").Clone();

			// Initialize our buffers
			this.ReallocateBuffers(this.screenSize);
		}

		public void ReallocateBuffers(Point size)
		{
			this.screenSize = size;
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
												DepthFormat.Depth24,
												0,
												RenderTargetUsage.DiscardContents);

			// Depth buffer
			if (this.depthBuffer != null && !this.depthBuffer.IsDisposed)
				this.depthBuffer.Dispose();
			this.depthBuffer = new RenderTarget2D(this.main.GraphicsDevice,
												size.X,
												size.Y,
												false,
												SurfaceFormat.Single,
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
												DepthFormat.Depth24,
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
												DepthFormat.None,
												0,
												RenderTargetUsage.DiscardContents);

			if (this.velocityBuffer != null)
			{
				if (!this.velocityBuffer.IsDisposed)
					this.velocityBuffer.Dispose();
				this.velocityBuffer = null;
			}
			if (this.velocityBufferLastFrame != null)
			{
				if (!this.velocityBufferLastFrame.IsDisposed)
					this.velocityBufferLastFrame.Dispose();
				this.velocityBufferLastFrame = null;
			}
			if (this.allowMotionBlur)
			{
				// Velocity for motion blur
				this.velocityBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);

				// Velocity from last frame
				this.velocityBufferLastFrame = new RenderTarget2D(this.main.GraphicsDevice,
													size.X,
													size.Y,
													false,
													SurfaceFormat.Color,
													DepthFormat.None,
													0,
													RenderTargetUsage.DiscardContents);
			}

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

			if (this.bloomBuffer != null)
			{
				if (!this.bloomBuffer.IsDisposed)
					this.bloomBuffer.Dispose();
				this.bloomBuffer = null;
			}
			if (this.allowBloom)
			{
				// Bloom buffer
				this.bloomBuffer = new RenderTarget2D(this.main.GraphicsDevice,
													size.X / 2,
													size.Y / 2,
													false,
													SurfaceFormat.Color,
													DepthFormat.Depth24,
													0,
													RenderTargetUsage.DiscardContents);
			}
		}

		public void SetRenderTargets(RenderParameters p)
		{
			bool motionBlur = this.allowMotionBlur && !this.main.Paused;
			if (motionBlur)
				this.main.GraphicsDevice.SetRenderTargets(this.colorBuffer1, this.depthBuffer, this.normalBuffer, this.velocityBuffer);
			else
				this.main.GraphicsDevice.SetRenderTargets(this.colorBuffer1, this.depthBuffer, this.normalBuffer);

			this.clearEffect.CurrentTechnique = this.clearEffect.Techniques[motionBlur ? "ClearMotionBlur" : "Clear"];
			Color color = this.BackgroundColor;
			p.Camera.SetParameters(this.clearEffect);
			this.setTargetParameters(new RenderTarget2D[] { }, new RenderTarget2D[] { this.colorBuffer1 }, this.clearEffect);
			this.clearEffect.Parameters["BackgroundColor"].SetValue(new Vector3((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f));
			this.applyEffect(this.clearEffect);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
		}

		public void PostProcess(RenderTarget2D result, RenderParameters parameters, DrawStageDelegate alphaStageDelegate)
		{
			RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
			RasterizerState reverseCullState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };

			bool enableSSAO = this.allowSSAO && this.EnableSSAO;

			if (enableSSAO)
			{
				// Down-sample depth buffer
				this.preparePostProcess(new[] { this.depthBuffer }, new[] { this.halfDepthBuffer }, this.depthDownsampleEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
			}

			// Global lighting
			this.setTargets(this.lightingBuffer, this.specularBuffer);
			Renderer.globalLightEffect.CurrentTechnique = Renderer.globalLightEffect.Techniques["GlobalLight" + (this.lightingManager.EnableGlobalShadowMap && this.lightingManager.HasGlobalShadowLight ? "Shadow" : "")];
			parameters.Camera.SetParameters(Renderer.globalLightEffect);
			this.lightingManager.SetGlobalLightParameters(Renderer.globalLightEffect);
			this.setTargetParameters(new RenderTarget2D[] { this.depthBuffer, this.normalBuffer, this.colorBuffer1 }, new RenderTarget2D[] { this.lightingBuffer, this.specularBuffer }, Renderer.globalLightEffect);
			this.applyEffect(Renderer.globalLightEffect);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

			// Point lights
			if (!parameters.ReverseCullOrder)
				this.main.GraphicsDevice.RasterizerState = reverseCullState;
			parameters.Camera.FarPlaneDistance.Value *= 1.5f;
			parameters.Camera.SetParameters(Renderer.pointLightEffect);
			parameters.Camera.FarPlaneDistance.Value /= 1.5f;

			this.setTargetParameters(new RenderTarget2D[] { this.depthBuffer, this.normalBuffer, this.colorBuffer1 }, new RenderTarget2D[] { this.lightingBuffer, this.specularBuffer }, Renderer.pointLightEffect);
			foreach (PointLight light in PointLight.All)
			{
				if (!light.Enabled || light.Suspended || light.Attenuation == 0.0f || !parameters.Camera.BoundingFrustum.Value.Intersects(light.BoundingSphere))
					continue;

				this.lightingManager.SetPointLightParameters(light, Renderer.pointLightEffect);
				this.applyEffect(Renderer.pointLightEffect);
				this.drawModel(Renderer.pointLightModel);
			}

			// Spot lights

			// HACK
			parameters.Camera.FarPlaneDistance.Value *= 1.5f;
			parameters.Camera.SetParameters(Renderer.spotLightEffect);
			parameters.Camera.FarPlaneDistance.Value /= 1.5f;

			this.setTargetParameters(new RenderTarget2D[] { this.depthBuffer, this.normalBuffer, this.colorBuffer1 }, new RenderTarget2D[] { this.lightingBuffer, this.specularBuffer }, Renderer.spotLightEffect);
			foreach (SpotLight light in SpotLight.All)
			{
				if (!light.Enabled || light.Suspended || light.Attenuation == 0.0f || !parameters.Camera.BoundingFrustum.Value.Intersects(light.BoundingFrustum))
					continue;

				this.lightingManager.SetSpotLightParameters(light, Renderer.spotLightEffect);
				this.applyEffect(Renderer.spotLightEffect);
				this.drawModel(Renderer.spotLightModel);
			}
			if (!parameters.ReverseCullOrder)
				this.main.GraphicsDevice.RasterizerState = originalState;

			// SSAO
			if (enableSSAO)
			{
				// Compute SSAO
				parameters.Camera.SetParameters(this.ssaoEffect);
				this.preparePostProcess(new[] { this.halfDepthBuffer, this.normalBuffer }, new[] { this.halfBuffer1 }, this.ssaoEffect);
				this.main.GraphicsDevice.Clear(Color.Black);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Blur
				/*this.blurEffect.CurrentTechnique = this.blurEffect.Techniques["BlurHorizontal"];
				parameters.Camera.SetParameters(this.blurEffect);
				this.preparePostProcess(new RenderTarget2D[] { this.halfBuffer1 }, new RenderTarget2D[] { this.halfBuffer2 }, this.blurEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
				this.blurEffect.CurrentTechnique = this.blurEffect.Techniques["Composite"];

				this.preparePostProcess(new RenderTarget2D[] { this.halfBuffer2 }, new RenderTarget2D[] { this.halfBuffer1 }, this.blurEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);*/
			}

			RenderTarget2D colorSource = this.colorBuffer1;
			RenderTarget2D colorDestination = this.colorBuffer2;
			RenderTarget2D colorTemp = null;

			// Compositing
			this.compositeEffect.CurrentTechnique = this.compositeEffect.Techniques["Composite" + (enableSSAO ? "SSAO" : "")];
			parameters.Camera.SetParameters(this.compositeEffect);
			this.preparePostProcess
			(
				enableSSAO
				? new RenderTarget2D[] { colorSource, this.lightingBuffer, this.specularBuffer, this.halfBuffer1 }
				: new RenderTarget2D[] { colorSource, this.lightingBuffer, this.specularBuffer },
				new RenderTarget2D[] { colorDestination },
				this.compositeEffect
			);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

			// Swap the color buffers
			colorTemp = colorDestination;
			colorDestination = colorSource;
			colorSource = colorTemp;

			parameters.DepthBuffer = this.depthBuffer;
			parameters.FrameBuffer = colorSource;

			// Alpha components

			// Drawing to the color destination
			this.setTargets(colorDestination);

			// Copy the color source to the destination
			this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
			this.spriteBatch.Draw(colorSource, Vector2.Zero, Color.White);
			this.spriteBatch.End();

			if (alphaStageDelegate != null)
				alphaStageDelegate(parameters);

			// Swap the color buffers
			colorTemp = colorDestination;
			colorDestination = colorSource;
			colorSource = colorTemp;

			// Motion blur
			if (this.allowMotionBlur && this.MotionBlurAmount > 0.0f)
			{
				this.motionBlurEffect.CurrentTechnique = this.motionBlurEffect.Techniques["MotionBlur"];
				parameters.Camera.SetParameters(this.motionBlurEffect);
				this.preparePostProcess(new RenderTarget2D[] { colorSource, this.velocityBuffer, this.velocityBufferLastFrame }, new RenderTarget2D[] { colorDestination }, this.motionBlurEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

				// Swap the velocity buffers
				RenderTarget2D temp = this.velocityBufferLastFrame;
				this.velocityBufferLastFrame = this.velocityBuffer;
				this.velocityBuffer = temp;

				// Swap the color buffers
				colorTemp = colorDestination;
				colorDestination = colorSource;
				colorSource = colorTemp;
			}

			bool enableBloom = this.allowBloom && this.EnableBloom && this.BloomThreshold < 1.0f;
			bool enableBlur = this.BlurAmount > 0.0f;

			// Tone mapping
			this.toneMapEffect.CurrentTechnique = this.toneMapEffect.Techniques[enableBloom ? "ToneMap" : "ToneMapDecode"];
			parameters.Camera.SetParameters(this.toneMapEffect);
			this.preparePostProcess(new RenderTarget2D[] { colorSource }, new RenderTarget2D[] { enableBloom || enableBlur ? colorDestination : result }, this.toneMapEffect);
			Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

			// Swap the color buffers
			colorTemp = colorDestination;
			colorDestination = colorSource;
			colorSource = colorTemp;

			// Bloom
			if (enableBloom)
			{
				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["BlurHorizontal"];
				parameters.Camera.SetParameters(this.bloomEffect);
				this.preparePostProcess(new RenderTarget2D[] { colorSource }, new RenderTarget2D[] { this.bloomBuffer }, this.bloomEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
				this.bloomEffect.CurrentTechnique = this.bloomEffect.Techniques["Composite"];
				this.preparePostProcess(new RenderTarget2D[] { colorSource, this.bloomBuffer }, new RenderTarget2D[] { enableBlur ? colorDestination : result }, this.bloomEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);

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
				this.preparePostProcess(new RenderTarget2D[] { colorSource }, new RenderTarget2D[] { colorDestination }, this.blurEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
				this.blurEffect.CurrentTechnique = this.blurEffect.Techniques["Composite"];

				// Swap the color buffers
				colorTemp = colorDestination;
				colorDestination = colorSource;
				colorSource = colorTemp;

				this.preparePostProcess(new RenderTarget2D[] { colorSource }, new RenderTarget2D[] { result }, this.blurEffect);
				Renderer.quad.DrawAlpha(this.main.GameTime, RenderParameters.Default);
			}
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
					}
				}
			}
		}

		private void setTargets(params RenderTarget2D[] results)
		{
			if (results == null)
				this.main.GraphicsDevice.SetRenderTarget(null);
			else if (results.Length == 1)
				this.main.GraphicsDevice.SetRenderTarget(results[0]);
			else
				this.main.GraphicsDevice.SetRenderTargets(results.Select(x => new RenderTargetBinding(x)).ToArray());
		}

		private void preparePostProcess(RenderTarget2D[] sources, RenderTarget2D[] results, Effect effect)
		{
			this.setTargets(results);
			this.setTargetParameters(sources, results, effect);
			this.applyEffect(effect);
		}

		private void setTargetParameters(RenderTarget2D[] sources, RenderTarget2D[] results, Effect effect)
		{
			EffectParameter param;
			for (int i = 0; i < sources.Length; i++)
			{
				param = effect.Parameters["Source" + Model.SamplerPostfix + i.ToString()];
				if (param == null)
					break;
				param.SetValue(sources[i]);
				param = effect.Parameters["SourceDimensions" + i.ToString()];
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

		protected override void delete()
		{
			base.delete();
			this.lightingBuffer.Dispose();
			this.normalBuffer.Dispose();
			this.depthBuffer.Dispose();
			this.colorBuffer1.Dispose();
			this.colorBuffer2.Dispose();
			this.specularBuffer.Dispose();

			this.compositeEffect.Dispose();
			this.blurEffect.Dispose();
			this.toneMapEffect.Dispose();
			this.clearEffect.Dispose();

			if (this.velocityBuffer != null)
				this.velocityBuffer.Dispose();
			if (this.velocityBufferLastFrame != null)
				this.velocityBufferLastFrame.Dispose();
			if (this.motionBlurEffect != null)
				this.motionBlurEffect.Dispose();

			if (this.halfDepthBuffer != null)
				this.halfDepthBuffer.Dispose();
			if (this.halfBuffer1 != null)
				this.halfBuffer1.Dispose();
			if (this.halfBuffer2 != null)
				this.halfBuffer2.Dispose();

			if (this.bloomBuffer != null)
				this.bloomBuffer.Dispose();
			if (this.bloomEffect != null)
				this.bloomEffect.Dispose();
		}
	}
}
