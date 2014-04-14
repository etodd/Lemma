using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Water : Component<Main>, IDrawableAlphaComponent, IDrawablePreFrameComponent, IUpdateableComponent
	{
		private static List<Water> instances = new List<Water>();

		public static IEnumerable<Water> ActiveInstances
		{
			get
			{
				return instances.Where(x => !x.Suspended);
			}
		}

		/// <summary>
		/// A struct that represents a single vertex in the
		/// vertex buffer.
		/// </summary>
		private struct QuadVertex : IVertexType
		{
			public Vector3 Position;
			public Vector2 TexCoord;
			public Vector3 Normal;
			public VertexDeclaration VertexDeclaration
			{
				get
				{
					return Water.VertexDeclaration;
				}
			}
		}

		public Property<int> DrawOrder { get; set; }

		private VertexBuffer surfaceVertexBuffer;
		private VertexBuffer underwaterVertexBuffer;

		private static VertexDeclaration vertexDeclaration;
		public static VertexDeclaration VertexDeclaration
		{
			get
			{
				if (Water.vertexDeclaration == null)
				{
					Microsoft.Xna.Framework.Graphics.VertexElement[] declElements = new VertexElement[3];
					declElements[0].Offset = 0;
					declElements[0].UsageIndex = 0;
					declElements[0].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[0].VertexElementUsage = VertexElementUsage.Position;
					declElements[1].Offset = sizeof(float) * 3;
					declElements[1].UsageIndex = 0;
					declElements[1].VertexElementFormat = VertexElementFormat.Vector2;
					declElements[1].VertexElementUsage = VertexElementUsage.TextureCoordinate;
					declElements[2].Offset = sizeof(float) * 5;
					declElements[2].UsageIndex = 0;
					declElements[2].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[2].VertexElementUsage = VertexElementUsage.Normal;
					Water.vertexDeclaration = new VertexDeclaration(declElements);
				}
				return Water.vertexDeclaration;
			}
		}

		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<Vector3> Color = new Property<Vector3> { Value = new Vector3(0.7f, 0.9f, 1.0f), Editable = true };
		public Property<Vector3> UnderwaterColor = new Property<Vector3> { Value = new Vector3(0.0f, 0.07f, 0.13f), Editable = true };
		public Property<float> Fresnel = new Property<float> { Value = 0.6f, Editable = true };
		public Property<float> Speed = new Property<float> { Value = 0.075f, Editable = true };
		public Property<float> RippleDensity = new Property<float> { Value = 1.0f, Editable = true };
		public Property<bool> EnableReflection = new Property<bool> { Value = true, Editable = false };
		public Property<float> Distortion = new Property<float> { Value = 0.25f, Editable = true };
		public Property<float> Brightness = new Property<float> { Value = 0.1f, Editable = true };
		public Property<float> Clearness = new Property<float> { Value = 0.25f, Editable = true };
		public Property<float> Depth = new Property<float> { Value = 100.0f, Editable = true };
		public Property<float> Refraction = new Property<float> { Value = 0.0f, Editable = true };
		public Property<Vector2> Scale = new Property<Vector2> { Value = new Vector2(100.0f, 100.0f), Editable = true };

		private Renderer renderer;
		private RenderTarget2D buffer;
		private Effect effect;
		private RenderParameters parameters;
		private Camera camera;

		[XmlIgnore]
		public Util.CustomFluidVolume Fluid;

		private bool needResize = false;

		private Random random = new Random();

		public Water()
		{
			this.DrawOrder = new Property<int> { Editable = true, Value = 10 };
		}

		private void resize()
		{
			Point size = this.main.ScreenSize;
			size.X = (int)((float)size.X * 0.5f);
			size.Y = (int)((float)size.Y * 0.5f);
			if (this.renderer == null)
			{
				this.renderer = new Renderer(this.main, size, false, false, false);
				this.renderer.LightRampTexture.Value = "Images\\default-ramp";
				this.main.AddComponent(this.renderer);
			}
			else
				this.renderer.ReallocateBuffers(size);

			if (this.buffer != null)
				this.buffer.Dispose();
			this.buffer = new RenderTarget2D(this.main.GraphicsDevice, size.X, size.Y);

			this.needResize = false;
		}

		public override void LoadContent(bool reload)
		{
			this.effect = this.main.Content.Load<Effect>("Effects\\Water").Clone();
			this.effect.Parameters["NormalMap" + Model.SamplerPostfix].SetValue(this.main.Content.Load<Texture2D>("Images\\water-normal"));

			this.Color.Reset();
			this.Fresnel.Reset();
			this.Speed.Reset();
			this.RippleDensity.Reset();
			this.Distortion.Reset();
			this.Brightness.Reset();
			this.Clearness.Reset();
			this.Refraction.Reset();
			this.UnderwaterColor.Reset();

			// Surface
			this.surfaceVertexBuffer = new VertexBuffer(this.main.GraphicsDevice, typeof(QuadVertex), Water.VertexDeclaration.VertexStride * 4, BufferUsage.None);
			QuadVertex[] surfaceData = new QuadVertex[4];

			// Upper right
			const float scale = 0.5f;
			surfaceData[0].Position = new Vector3(scale, 0, scale);
			surfaceData[0].TexCoord = new Vector2(1, 0);

			// Upper left
			surfaceData[1].Position = new Vector3(-scale, 0, scale);
			surfaceData[1].TexCoord = new Vector2(0, 0);

			// Lower right
			surfaceData[2].Position = new Vector3(scale, 0, -scale);
			surfaceData[2].TexCoord = new Vector2(1, 1);

			// Lower left
			surfaceData[3].Position = new Vector3(-scale, 0, -scale);
			surfaceData[3].TexCoord = new Vector2(0, 1);

			surfaceData[0].Normal = surfaceData[1].Normal = surfaceData[2].Normal = surfaceData[3].Normal = new Vector3(0, 1, 0);

			this.surfaceVertexBuffer.SetData(surfaceData);

			// Underwater
			this.underwaterVertexBuffer = new VertexBuffer(this.main.GraphicsDevice, typeof(QuadVertex), Water.VertexDeclaration.VertexStride * 4, BufferUsage.None);

			QuadVertex[] underwaterData = new QuadVertex[4];

			// Upper right
			underwaterData[0].Position = new Vector3(1, 1, 1);
			underwaterData[0].TexCoord = new Vector2(1, 0);

			// Lower right
			underwaterData[1].Position = new Vector3(1, -1, 1);
			underwaterData[1].TexCoord = new Vector2(1, 1);

			// Upper left
			underwaterData[2].Position = new Vector3(-1, 1, 1);
			underwaterData[2].TexCoord = new Vector2(0, 0);

			// Lower left
			underwaterData[3].Position = new Vector3(-1, -1, 1);
			underwaterData[3].TexCoord = new Vector2(0, 1);

			underwaterData[0].Normal = underwaterData[1].Normal = underwaterData[2].Normal = underwaterData[3].Normal = new Vector3(0, 0, -1);

			this.underwaterVertexBuffer.SetData(underwaterData);

			this.resize();
		}

		public override void InitializeProperties()
		{
			this.EnabledWhenPaused.Value = true;
			this.Add(new NotifyBinding(delegate() { this.needResize = true; }, this.main.ScreenSize));
			this.Add(new Binding<bool>(this.EnableReflection, ((GameMain)this.main).Settings.EnableReflections));

			Action removeFluid = delegate()
			{
				if (this.Fluid.Space != null)
					this.main.Space.Remove(this.Fluid);
			};

			Action addFluid = delegate()
			{
				if (this.Fluid.Space == null && this.Enabled && !this.Suspended)
					this.main.Space.Add(this.Fluid);
			};

			this.Add(new CommandBinding(this.OnSuspended, removeFluid));
			this.Add(new CommandBinding(this.OnDisabled, removeFluid));
			this.Add(new CommandBinding(this.OnResumed, addFluid));
			this.Add(new CommandBinding(this.OnEnabled, addFluid));

			this.camera = new Camera();
			this.main.AddComponent(this.camera);
			this.parameters = new RenderParameters
			{
				Camera = this.camera,
				Technique = Technique.Clip,
				ReverseCullOrder = true,
			};

			this.Color.Set = delegate(Vector3 value)
			{
				this.Color.InternalValue = value;
				this.effect.Parameters["Color"].SetValue(value);
			};

			this.Scale.Set = delegate(Vector2 value)
			{
				this.Scale.InternalValue = value;
				this.effect.Parameters["Scale"].SetValue(value);
				this.updatePhysics();
			};

			this.UnderwaterColor.Set = delegate(Vector3 value)
			{
				this.UnderwaterColor.InternalValue = value;
				this.effect.Parameters["UnderwaterColor"].SetValue(value);
			};

			this.Fresnel.Set = delegate(float value)
			{
				this.Fresnel.InternalValue = value;
				this.effect.Parameters["Fresnel"].SetValue(value);
			};

			this.Speed.Set = delegate(float value)
			{
				this.Speed.InternalValue = value;
				this.effect.Parameters["Speed"].SetValue(value);
			};

			this.RippleDensity.Set = delegate(float value)
			{
				this.RippleDensity.InternalValue = value;
				this.effect.Parameters["RippleDensity"].SetValue(value);
			};

			this.Distortion.Set = delegate(float value)
			{
				this.Distortion.InternalValue = value;
				this.effect.Parameters["Distortion"].SetValue(value);
			};

			this.Brightness.Set = delegate(float value)
			{
				this.Brightness.InternalValue = value;
				this.effect.Parameters["Brightness"].SetValue(value);
			};

			this.Clearness.Set = delegate(float value)
			{
				this.Clearness.InternalValue = value;
				this.effect.Parameters["Clearness"].SetValue(value);
			};

			this.Refraction.Set = delegate(float value)
			{
				this.Refraction.InternalValue = value;
				this.effect.Parameters["Refraction"].SetValue(value);
			};

			this.Position.Set = delegate(Vector3 value)
			{
				this.Position.InternalValue = value;
				this.effect.Parameters["Position"].SetValue(this.Position);
				this.updatePhysics();
			};

			this.Depth.Set = delegate(float value)
			{
				 this.Depth.InternalValue = value;
				 this.updatePhysics();
			};

			instances.Add(this);
		}

		private void updatePhysics()
		{
			if (this.Fluid != null)
				this.main.Space.Remove(this.Fluid);

			List<Vector3[]> tris = new List<Vector3[]>();
			float width = this.Scale.Value.X;
			float length = this.Scale.Value.Y;
			Vector3 pos = this.Position;

			tris.Add(new[]
			{
				pos + new Vector3(width / -2, 0, length / -2),
				pos + new Vector3(width / 2, 0, length / -2),
				pos + new Vector3(width / -2, 0, length / 2)
			});
			tris.Add(new[]
			{
				pos + new Vector3(width / -2, 0, length / 2),
				pos + new Vector3(width / 2, 0, length / -2),
				pos + new Vector3(width / 2, 0, length / 2)
			});

			this.Fluid = new Util.CustomFluidVolume(Vector3.Up, this.main.Space.ForceUpdater.Gravity.Y, tris, this.Depth, 1.25f, 0.997f, 0.2f, this.main.Space.BroadPhase.QueryAccelerator, this.main.Space.ThreadManager);
			this.main.Space.Add(this.Fluid);
		}

		void IDrawableAlphaComponent.DrawAlpha(Microsoft.Xna.Framework.GameTime time, RenderParameters p)
		{
			if (!p.IsMainRender)
				return;

			Vector3 cameraPos = p.Camera.Position;
			Vector3 pos = this.Position;
			bool underwater = this.Fluid.BoundingBox.Contains(cameraPos) != ContainmentType.Disjoint;
			if (!underwater && cameraPos.Y < pos.Y)
				return;

			RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
			this.main.GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };

			float oldFarPlane = p.Camera.FarPlaneDistance;
			p.Camera.FarPlaneDistance.Value = 1000.0f;

			p.Camera.SetParameters(this.effect);
			this.effect.Parameters["ActualFarPlaneDistance"].SetValue(oldFarPlane);
			this.effect.Parameters["Reflection" + Model.SamplerPostfix].SetValue(this.buffer);
			this.effect.Parameters["Time"].SetValue(this.main.TotalTime);
			this.effect.Parameters["Depth" + Model.SamplerPostfix].SetValue(p.DepthBuffer);
			this.effect.Parameters["Frame" + Model.SamplerPostfix].SetValue(p.FrameBuffer);

			// Draw surface
			this.effect.CurrentTechnique = this.effect.Techniques[underwater || !this.EnableReflection ? "Surface" : "SurfaceReflection"];
			this.effect.CurrentTechnique.Passes[0].Apply();
			this.main.GraphicsDevice.SetVertexBuffer(this.surfaceVertexBuffer);
			this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

			this.main.GraphicsDevice.RasterizerState = originalState;

			p.Camera.FarPlaneDistance.Value = oldFarPlane;

			if (underwater)
			{
				// Draw underwater stuff
				this.effect.CurrentTechnique = this.effect.Techniques["Underwater"];
				this.effect.CurrentTechnique.Passes[0].Apply();
				this.main.GraphicsDevice.SetVertexBuffer(this.underwaterVertexBuffer);
				this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
			}
		}

		void IDrawablePreFrameComponent.DrawPreFrame(GameTime time, RenderParameters p)
		{
			if (!this.EnableReflection)
				return;

			if (this.needResize)
				this.resize();

			float waterHeight = this.Position.Value.Y;
			if (p.Camera.Position.Value.Y > waterHeight)
			{
				this.parameters.ClipPlanes = new[] { new Plane(Vector3.Up, -waterHeight) };
				this.renderer.SetRenderTargets(this.parameters);
				this.camera.Position.Value = p.Camera.Position;
				Matrix reflect = Matrix.CreateTranslation(0.0f, -waterHeight, 0.0f) * Matrix.CreateScale(1.0f, -1.0f, 1.0f) * Matrix.CreateTranslation(0.0f, waterHeight, 0.0f);
				this.camera.Position.Value = Vector3.Transform(this.camera.Position, reflect);
				this.camera.View.Value = reflect * p.Camera.View;
				this.camera.SetPerspectiveProjection(p.Camera.FieldOfView, new Point(this.buffer.Width, this.buffer.Height), p.Camera.NearPlaneDistance, p.Camera.FarPlaneDistance);

				this.main.DrawScene(this.parameters);

				this.renderer.PostProcess(this.buffer, this.parameters, this.main.DrawAlphaComponents);
			}
		}

		void IUpdateableComponent.Update(float dt)
		{
			if (this.main.Paused)
				return;

			float waterHeight = this.Position.Value.Y;

			lock (this.Fluid.NotifyEntries)
			{
				foreach (BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable collidable in this.Fluid.NotifyEntries)
				{
					if (collidable.Entity == null)
						continue;

					float speed = collidable.Entity.LinearVelocity.Length();

					if (speed > 9.0f)
					{
						float volume = Math.Min(speed * collidable.Entity.Mass / 50.0f, 1.0f);
						if (volume > 0.25f)
						{
							// TODO: Figure out Wwise volume parameter
							AkSoundEngine.PostEvent(collidable.Entity.LinearVelocity.Y > 0.0f ? "Splash Out" : "Splash", collidable.Entity.Position);
						}
					}

					if (speed > 5.0f)
					{
						collidable.UpdateBoundingBox();
						BoundingBox boundingBox = collidable.BoundingBox;
						Vector3[] particlePositions = new Vector3[30];

						for (int i = 0; i < particlePositions.Length; i++)
							particlePositions[i] = new Vector3(boundingBox.Min.X + ((float)this.random.NextDouble() * (boundingBox.Max.X - boundingBox.Min.X)),
								waterHeight,
								boundingBox.Min.Z + ((float)this.random.NextDouble() * (boundingBox.Max.Z - boundingBox.Min.Z)));

						ParticleEmitter.Emit(this.main, "Splash", particlePositions);
					}
				}
				this.Fluid.NotifyEntries.Clear();
			}
		}

		protected override void delete()
		{
			this.camera.Delete.Execute();
			this.effect.Dispose();
			this.renderer.Delete.Execute();
			this.buffer.Dispose();
			this.surfaceVertexBuffer.Dispose();
			this.underwaterVertexBuffer.Dispose();
			if (this.Fluid.Space != null)
				this.main.Space.Remove(this.Fluid);
			instances.Remove(this);
			base.delete();
		}
	}
}
