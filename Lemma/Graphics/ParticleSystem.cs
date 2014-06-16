using System; using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.Collections.Generic;

namespace Lemma.Components
{
	/// <summary>
	/// The main component in charge of displaying particles.
	/// </summary>
	public class ParticleSystem : Component<Main>, IUpdateableComponent, IDrawablePostAlphaComponent, IDrawableAlphaComponent, IDrawableComponent
	{
		public Property<int> DrawOrder { get; set; }

		public bool IsVisible(BoundingFrustum frustum)
		{
			return true;
		}

		public float GetDistance(Vector3 camera)
		{
			return 0.0f;
		}

		#region ParticleVertex
		/// <summary>
		/// Custom vertex structure for drawing particles.
		/// </summary>
		protected struct ParticleVertex
		{
			// Stores which corner of the particle quad this vertex represents.
			public Short2 Corner;

			// Stores the starting position of the particle.
			public Vector3 Position;

			// Stores the starting velocity of the particle.
			public Vector3 Velocity;

			// Four random values, used to make each particle look slightly different.
			public Color Random;

			// The time (in seconds) at which this particle was created.
			public float Time;

			// Lifetime (in seconds) of this particle.
			public float Lifetime;

			public float StartSize;

			// Describe the layout of this vertex structure.
			public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
			(
				new VertexElement(0, VertexElementFormat.Short2, VertexElementUsage.Position, 0),
				new VertexElement(4, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
				new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
				new VertexElement(28, VertexElementFormat.Color, VertexElementUsage.Color, 0),
				new VertexElement(32, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0),
				new VertexElement(36, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
				new VertexElement(40, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2)
			);


			// Describe the size of this vertex structure.
			public const int SizeInBytes = 44;
		}
		#endregion

		#region ParticleSettings
		/// <summary>
		/// Settings class describes all the tweakable options used
		/// to control the appearance of a particle system.
		/// </summary>
		public class ParticleSettings
		{
			// Name of the texture used by this particle system.
			public string TextureName = null;

			public string EffectFile = null;

			public int DrawOrder = 11; // In front of water and fog

			// Maximum number of particles that can be displayed at one time.
			public int MaxParticles = 100;

			public bool PostAlpha = false;


			// How long these particles will last.
			public TimeSpan Duration = TimeSpan.FromSeconds(1);


			// If greater than zero, some particles will last a shorter time than others.
			public float DurationRandomness = 0;


			// Controls how much particles are influenced by the velocity of the object
			// which created them. You can see this in action with the explosion effect,
			// where the flames continue to move in the same direction as the source
			// projectile. The projectile trail particles, on the other hand, set this
			// value very low so they are less affected by the velocity of the projectile.
			public float EmitterVelocitySensitivity = 0.0f;


			// Range of values controlling how much X and Z axis velocity to give each
			// particle. Values for individual particles are randomly chosen from somewhere
			// between these limits.
			public float MinHorizontalVelocity = 0;
			public float MaxHorizontalVelocity = 0;

			public Model.Material Material;

			// Range of values controlling how much Y axis velocity to give each particle.
			// Values for individual particles are randomly chosen from somewhere between
			// these limits.
			public float MinVerticalVelocity = 0;
			public float MaxVerticalVelocity = 0;


			// Direction and strength of the gravity effect. Note that this can point in any
			// direction, not just down! The fire effect points it upward to make the flames
			// rise, and the smoke plume points it sideways to simulate wind.
			public Vector3 Gravity = Vector3.Zero;


			// Controls how the particle velocity will change over their lifetime. If set
			// to 1, particles will keep going at the same speed as when they were created.
			// If set to 0, particles will come to a complete stop right before they die.
			// Values greater than 1 make the particles speed up over time.
			public float EndVelocity = 1;


			// Range of values controlling the particle color and alpha. Values for
			// individual particles are randomly chosen from somewhere between these limits.
			public Vector4 MinColor = Vector4.One;
			public Vector4 MaxColor = Vector4.One;


			// Range of values controlling how fast the particles rotate. Values for
			// individual particles are randomly chosen from somewhere between these
			// limits. If both these values are set to 0, the particle system will
			// automatically switch to an alternative shader technique that does not
			// support rotation, and thus requires significantly less GPU power. This
			// means if you don't need the rotation effect, you may get a performance
			// boost from leaving these values at 0.
			public float MinRotateSpeed = 0;
			public float MaxRotateSpeed = 0;


			// Range of values controlling how big the particles are when first created.
			// Values for individual particles are randomly chosen from somewhere between
			// these limits.
			public float MinStartSize = 100;
			public float MaxStartSize = 100;


			// Range of values controlling how big particles become at the end of their
			// life. Values for individual particles are randomly chosen from somewhere
			// between these limits.
			public float MinEndSize = 100;
			public float MaxEndSize = 100;


			// Alpha blending settings.
			public BlendState BlendState = BlendState.NonPremultiplied;

			public List<Technique> UnsupportedTechniques = new List<Technique>();
		}
		#endregion

		public static ParticleSystem Add(Main main, string type, ParticleSystem.ParticleSettings settings)
		{
			if (ParticleSystem.systems == null)
			{
				ParticleSystem.initialize(main);
			}

			return ParticleSystem.add(main, type, settings);
		}

		protected static ParticleSystem add(Main main, string type, ParticleSystem.ParticleSettings settings)
		{
			ParticleSystem system = new ParticleSystem();
			system.Type.Value = type;
			system.Settings.Value = settings;

			ParticleSystem.systems.Add(system.Type, system);

			main.AddComponent(system);
			return system;
		}

		private static void initialize(Main main)
		{
			ParticleSystem.systems = new Dictionary<string, ParticleSystem>();
			ParticleSystem.add(main, "Splash",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\splash",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(1.0f),
				MinHorizontalVelocity = -4.0f,
				MaxHorizontalVelocity = 4.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 5.0f,
				Gravity = new Vector3(0.0f, -10.0f, 0.0f),
				MinRotateSpeed = -2.0f,
				MaxRotateSpeed = 2.0f,
				MinStartSize = 0.1f,
				MaxStartSize = 0.3f,
				MinEndSize = 0.0f,
				MaxEndSize = 0.0f,
				BlendState = BlendState.AlphaBlend,
				MinColor = new Vector4(0.7f, 0.75f, 0.8f, 1.0f),
				MaxColor = new Vector4(0.7f, 0.75f, 0.8f, 1.0f),
			});

			ParticleSystem.add(main, "BigSplash",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\big-splash",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(0.5f),
				MinHorizontalVelocity = -4.0f,
				MaxHorizontalVelocity = 4.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 2.0f,
				Gravity = new Vector3(0.0f, 0.0f, 0.0f),
				MinRotateSpeed = -1.0f,
				MaxRotateSpeed = 1.0f,
				MinStartSize = 0.5f,
				MaxStartSize = 1.0f,
				MinEndSize = 1.0f,
				MaxEndSize = 2.0f,
				BlendState = BlendState.AlphaBlend,
				MinColor = new Vector4(0.7f, 0.75f, 0.8f, 0.5f),
				MaxColor = new Vector4(0.7f, 0.75f, 0.8f, 0.5f),
			});

#if DEVELOPMENT
			ParticleSystem.add(main, "Debug",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\debug",
				MaxParticles = 10000,
				Duration = TimeSpan.FromSeconds(1.0f),
				MinHorizontalVelocity = 0.0f,
				MaxHorizontalVelocity = 0.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 0.0f,
				Gravity = Vector3.Zero,
				MinRotateSpeed = 0.0f,
				MaxRotateSpeed = 0.0f,
				MinStartSize = 0.5f,
				MaxStartSize = 0.5f,
				MinEndSize = 0.5f,
				MaxEndSize = 0.5f,
				BlendState = BlendState.AlphaBlend,
				MinColor = Vector4.One,
				MaxColor = Vector4.One,
			});
#endif

			ParticleSystem.add(main, "Distortion",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\distortion",
				EffectFile = "Effects\\ParticleFrameBufferDistortion",
				MaxParticles = 1000,
				DrawOrder = 0,
				Duration = TimeSpan.FromSeconds(5.0f),
				MinHorizontalVelocity = -0.5f,
				MaxHorizontalVelocity = 0.5f,
				MinVerticalVelocity = -0.5f,
				MaxVerticalVelocity = 0.5f,
				Gravity = new Vector3(0.0f, 0.0f, 0.0f),
				MinRotateSpeed = 0.0f,
				MaxRotateSpeed = 0.0f,
				MinStartSize = 0.5f,
				MaxStartSize = 1.0f,
				MinEndSize = 2.0f,
				MaxEndSize = 4.0f,
				BlendState = BlendState.AlphaBlend,
				MinColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				MaxColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				PostAlpha = true,
			});

			ParticleSystem.add(main, "Rift",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\rift",
				EffectFile = "Effects\\ParticleFrameBufferDistortion",
				MaxParticles = 1000,
				DrawOrder = 0,
				Duration = TimeSpan.FromSeconds(0.6f),
				MinHorizontalVelocity = 0.0f,
				MaxHorizontalVelocity = 0.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 0.0f,
				Gravity = new Vector3(0.0f, 0.0f, 0.0f),
				MinRotateSpeed = 0.0f,
				MaxRotateSpeed = 0.0f,
				MinStartSize = 8.0f,
				MaxStartSize = 8.0f,
				MinEndSize = 0.0f,
				MaxEndSize = 0.0f,
				BlendState = BlendState.AlphaBlend,
				MinColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				MaxColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				PostAlpha = true,
			});

			ParticleSystem.Add(main, "Purple",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\spark",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(10.0f),
				MinHorizontalVelocity = -0.2f,
				MaxHorizontalVelocity = 0.2f,
				MinVerticalVelocity = -0.2f,
				MaxVerticalVelocity = 0.2f,
				Gravity = new Vector3(0.0f, 0.0f, 0.0f),
				MinRotateSpeed = -4.0f,
				MaxRotateSpeed = 4.0f,
				MinStartSize = 0.05f,
				MaxStartSize = 0.1f,
				MinEndSize = 0.05f,
				MaxEndSize = 0.1f,
				BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
				MinColor = new Vector4(0.8f, 0.3f, 1.5f, 1.0f),
				MaxColor = new Vector4(1.0f, 0.5f, 2.0f, 1.0f),
			});

			ParticleSystem.add(main, "DistortionSmall",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\distortion",
				EffectFile = "Effects\\ParticleFrameBufferDistortion",
				MaxParticles = 1000,
				DrawOrder = 0,
				Duration = TimeSpan.FromSeconds(1.0f),
				MinHorizontalVelocity = -0.15f,
				MaxHorizontalVelocity = 0.15f,
				MinVerticalVelocity = -0.15f,
				MaxVerticalVelocity = 0.15f,
				Gravity = new Vector3(0.0f, 0.0f, 0.0f),
				MinRotateSpeed = 0.0f,
				MaxRotateSpeed = 0.0f,
				MinStartSize = 0.1f,
				MaxStartSize = 0.25f,
				MinEndSize = 0.3f,
				MaxEndSize = 0.4f,
				BlendState = BlendState.AlphaBlend,
				MinColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				MaxColor = new Vector4(1.2f, 1.4f, 1.6f, 1.0f),
				PostAlpha = true,
			});

			ParticleSystem.add(main, "Smoke",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\smoke",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(3.0f),
				MinHorizontalVelocity = -1.0f,
				MaxHorizontalVelocity = 1.0f,
				MinVerticalVelocity = 1.0f,
				MaxVerticalVelocity = 3.0f,
				Gravity = new Vector3(0.0f, -2.0f, 0.0f),
				MinRotateSpeed = 0.0f,
				MaxRotateSpeed = 0.0f,
				MinStartSize = 0.5f,
				MaxStartSize = 1.0f,
				MinEndSize = 2.0f,
				MaxEndSize = 4.0f,
				BlendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
				MinColor = new Vector4(1.0f, 1.0f, 1.0f, 0.8f),
				MaxColor = new Vector4(1.0f, 1.0f, 1.0f, 0.8f),
			});

			ParticleSystem.add(main, "InfectedShatter",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\spark",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(1.0f),
				MinHorizontalVelocity = -4.0f,
				MaxHorizontalVelocity = 4.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 5.0f,
				Gravity = new Vector3(0.0f, -8.0f, 0.0f),
				MinRotateSpeed = -2.0f,
				MaxRotateSpeed = 2.0f,
				MinStartSize = 0.1f,
				MaxStartSize = 0.3f,
				MinEndSize = 0.0f,
				MaxEndSize = 0.0f,
				BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
				MinColor = new Vector4(2.0f, 0.75f, 0.75f, 1.0f),
				MaxColor = new Vector4(2.0f, 0.75f, 0.75f, 1.0f),
			});

			ParticleSystem.add(main, "WhiteShatter",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\spark",
				MaxParticles = 1000,
				Duration = TimeSpan.FromSeconds(1.0f),
				MinHorizontalVelocity = -4.0f,
				MaxHorizontalVelocity = 4.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 5.0f,
				Gravity = new Vector3(0.0f, -8.0f, 0.0f),
				MinRotateSpeed = -2.0f,
				MaxRotateSpeed = 2.0f,
				MinStartSize = 0.1f,
				MaxStartSize = 0.3f,
				MinEndSize = 0.0f,
				MaxEndSize = 0.0f,
				BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
				MinColor = new Vector4(1.5f, 1.25f, 1.0f, 1.0f),
				MaxColor = new Vector4(1.5f, 1.25f, 1.0f, 1.0f),
			});

			ParticleSystem.add(main, "Electricity",
			new ParticleSystem.ParticleSettings
			{
				TextureName = "Particles\\spark",
				MaxParticles = 2000,
				Duration = TimeSpan.FromSeconds(0.3f),
				MinHorizontalVelocity = -1.0f,
				MaxHorizontalVelocity = 1.0f,
				MinVerticalVelocity = 0.0f,
				MaxVerticalVelocity = 1.5f,
				Gravity = new Vector3(0.0f, -1.2f, 0.0f),
				MinRotateSpeed = -10.0f,
				MaxRotateSpeed = 10.0f,
				MinStartSize = 0.05f,
				MaxStartSize = 0.1f,
				MinEndSize = 0.0f,
				MaxEndSize = 0.0f,
				BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
				MinColor = new Vector4(1.0f, 1.25f, 1.5f, 1.0f),
				MaxColor = new Vector4(1.0f, 1.25f, 1.5f, 1.0f),
			});
		}

		private static Dictionary<string, ParticleSystem> systems = null;

		public Property<string> Type = new Property<string>();

		// Settings class controls the appearance and animation of this particle system.
		public Property<ParticleSettings> Settings = new Property<ParticleSettings>();
		private ParticleSettings settings;

		// Custom effect for drawing particles. This computes the particle
		// animation entirely in the vertex shader: no per-particle CPU work required!
		private Effect particleEffect;

		// Shortcuts for accessing frequently changed effect parameters.
		private EffectParameter effectMaterialIdParameter;
		private EffectParameter effectViewParameter;
		private EffectParameter effectInverseViewParameter;
		private EffectParameter effectCameraPositionParameter;
		private EffectParameter effectProjectionParameter;
		private EffectParameter effectViewportScaleParameter;
		private EffectParameter effectTimeParameter;
		private EffectParameter effectDepthBufferParameter;
		private EffectParameter effectFrameBufferParameter;

		// An array of particles, treated as a circular queue.
		private ParticleVertex[] particles;

		// A vertex buffer holding our particles. This contains the same data as
		// the particles array, but copied across to where the GPU can access it.
		private DynamicVertexBuffer vertexBuffer;

		// Index buffer turns sets of four vertices into particle quads (pairs of triangles).
		private IndexBuffer indexBuffer;

		// The particles array and vertex buffer are treated as a circular queue.
		// Initially, the entire contents of the array are free, because no particles
		// are in use. When a new particle is created, this is allocated from the
		// beginning of the array. If more than one particle is created, these will
		// always be stored in a consecutive block of array elements. Because all
		// particles last for the same amount of time, old particles will always be
		// removed in order from the start of this active particle region, so the
		// active and free regions will never be intermingled. Because the queue is
		// circular, there can be times when the active particle region wraps from the
		// end of the array back to the start. The queue uses modulo arithmetic to
		// handle these cases. For instance with a four entry queue we could have:
		//
		//      0
		//      1 - first active particle
		//      2 
		//      3 - first free particle
		//
		// In this case, particles 1 and 2 are active, while 3 and 4 are free.
		// Using modulo arithmetic we could also have:
		//
		//      0
		//      1 - first free particle
		//      2 
		//      3 - first active particle
		//
		// Here, 3 and 0 are active, while 1 and 2 are free.
		//
		// But wait! The full story is even more complex.
		//
		// When we create a new particle, we add them to our managed particles array.
		// We also need to copy this new data into the GPU vertex buffer, but we don't
		// want to do that straight away, because setting new data into a vertex buffer
		// can be an expensive operation. If we are going to be adding several particles
		// in a single frame, it is faster to initially just store them in our managed
		// array, and then later upload them all to the GPU in one single call. So our
		// queue also needs a region for storing new particles that have been added to
		// the managed array but not yet uploaded to the vertex buffer.
		//
		// Another issue occurs when old particles are retired. The CPU and GPU run
		// asynchronously, so the GPU will often still be busy drawing the previous
		// frame while the CPU is working on the next frame. This can cause a
		// synchronization problem if an old particle is retired, and then immediately
		// overwritten by a new one, because the CPU might try to change the contents
		// of the vertex buffer while the GPU is still busy drawing the old data from
		// it. Normally the graphics driver will take care of this by waiting until
		// the GPU has finished drawing inside the VertexBuffer.SetData call, but we
		// don't want to waste time waiting around every time we try to add a new
		// particle! To avoid this delay, we can specify the SetDataOptions.NoOverwrite
		// flag when we write to the vertex buffer. This basically means "I promise I
		// will never try to overwrite any data that the GPU might still be using, so
		// you can just go ahead and update the buffer straight away". To keep this
		// promise, we must avoid reusing vertices immediately after they are drawn.
		//
		// So in total, our queue contains four different regions:
		//
		// Vertices between firstActiveParticle and firstNewParticle are actively
		// being drawn, and exist in both the managed particles array and the GPU
		// vertex buffer.
		//
		// Vertices between firstNewParticle and firstFreeParticle are newly created,
		// and exist only in the managed particles array. These need to be uploaded
		// to the GPU at the start of the next draw call.
		//
		// Vertices between firstFreeParticle and firstRetiredParticle are free and
		// waiting to be allocated.
		//
		// Vertices between firstRetiredParticle and firstActiveParticle are no longer
		// being drawn, but were drawn recently enough that the GPU could still be
		// using them. These need to be kept around for a few more frames before they
		// can be reallocated.

		private int firstActiveParticle;
		private int firstNewParticle;
		private int firstFreeParticle;
		private int firstRetiredParticle;

		private RasterizerState noCullRasterizerState = new RasterizerState { CullMode = CullMode.None };

		// Store the current time, in seconds.
		private float currentTime;

		// Count how many times Draw has been called. This is used to know
		// when it is safe to retire old particles back into the free list.
		private int drawCounter;

		// Shared random number generator.
		private static Random random = new Random();

		public static ParticleSystem Get(Main main, string type)
		{
			if (ParticleSystem.systems == null)
				ParticleSystem.initialize(main);
			ParticleSystem value;
			if (ParticleSystem.systems.TryGetValue(type, out value))
				return value;
			else
				return null;
		}

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = true;
			this.DrawOrder = new Property<int> { Value = 11 };
			this.Settings.Get = delegate()
			{
				return this.settings;
			};
			this.Settings.Set = delegate(ParticleSettings value)
			{
				this.settings = value;
				if (this.main != null)
				{
					if (this.vertexBuffer != null)
						this.vertexBuffer.Dispose();
					if (this.indexBuffer != null)
						this.indexBuffer.Dispose();
					this.initialize(false);
				}
			};
		}

		protected void initialize(bool reload)
		{
			if (this.particles == null || this.particles.Length != this.settings.MaxParticles * 4)
			{
				// Allocate the particle array, and fill in the corner fields (which never change).
				this.particles = new ParticleVertex[this.settings.MaxParticles * 4];

				for (int i = 0; i < this.settings.MaxParticles; i++)
				{
					this.particles[i * 4 + 0].Corner = new Short2(-1, -1);
					this.particles[i * 4 + 1].Corner = new Short2(1, -1);
					this.particles[i * 4 + 2].Corner = new Short2(1, 1);
					this.particles[i * 4 + 3].Corner = new Short2(-1, 1);
				}
			}

			// Create a dynamic vertex buffer.
			this.vertexBuffer = new DynamicVertexBuffer(this.main.GraphicsDevice, ParticleVertex.VertexDeclaration,
												   this.settings.MaxParticles * 4, BufferUsage.WriteOnly);

			// Create and populate the index buffer.
			uint[] indices = new uint[this.settings.MaxParticles * 6];

			for (int i = 0; i < this.settings.MaxParticles; i++)
			{
				indices[i * 6 + 0] = (uint)(i * 4 + 0);
				indices[i * 6 + 1] = (uint)(i * 4 + 1);
				indices[i * 6 + 2] = (uint)(i * 4 + 2);

				indices[i * 6 + 3] = (uint)(i * 4 + 0);
				indices[i * 6 + 4] = (uint)(i * 4 + 2);
				indices[i * 6 + 5] = (uint)(i * 4 + 3);
			}

			this.indexBuffer = new IndexBuffer(this.main.GraphicsDevice, typeof(uint), indices.Length, BufferUsage.WriteOnly);

			this.indexBuffer.SetData(indices);

			if (this.particleEffect == null || reload)
			{
				Effect effect = this.main.Content.Load<Effect>(this.settings.EffectFile != null ? this.settings.EffectFile : "Effects\\Particle");

				// If we have several particle systems, the content manager will return
				// a single shared effect instance to them all. But we want to preconfigure
				// the effect with parameters that are specific to this particular
				// particle system. By cloning the effect, we prevent one particle system
				// from stomping over the parameter settings of another.

				this.particleEffect = effect.Clone();
			}

			EffectParameterCollection parameters = this.particleEffect.Parameters;

			// Look up shortcuts for parameters that change every frame.
			this.effectViewParameter = parameters["View"];
			this.effectMaterialIdParameter = parameters["MaterialID"];
			this.effectInverseViewParameter = parameters["InverseView"];
			this.effectCameraPositionParameter = parameters["CameraPosition"];
			this.effectProjectionParameter = parameters["Projection"];
			this.effectViewportScaleParameter = parameters["ViewportScale"];
			this.effectTimeParameter = parameters["CurrentTime"];
			this.effectDepthBufferParameter = parameters["Depth" + Model.SamplerPostfix];
			this.effectFrameBufferParameter = parameters["Frame" + Model.SamplerPostfix];

			// Set the values of parameters that do not change.
			parameters["DurationRandomness"].SetValue(this.settings.DurationRandomness);
			parameters["Gravity"].SetValue(this.settings.Gravity);
			parameters["EndVelocity"].SetValue(this.settings.EndVelocity);
			parameters["MinColor"].SetValue(this.settings.MinColor);
			parameters["MaxColor"].SetValue(this.settings.MaxColor);

			EffectParameter param = parameters["RotateSpeed"];
			if (param != null)
				param.SetValue(new Vector2(this.settings.MinRotateSpeed, this.settings.MaxRotateSpeed));

			param = parameters["StartSize"];
			if (param != null)
				param.SetValue(new Vector2(this.settings.MinStartSize, this.settings.MaxStartSize));

			param = parameters["EndSize"];
			if (param != null)
				param.SetValue(new Vector2(this.settings.MinEndSize, this.settings.MaxEndSize));

			// Load the particle texture, and set it onto the effect.
			Texture2D texture = this.main.Content.Load<Texture2D>(this.settings.TextureName);

			param = parameters[Model.SamplerPostfix];
			if (param != null)
				param.SetValue(texture);

			this.DrawOrder.Value = this.settings.DrawOrder;
		}

		public void LoadContent(bool reload)
		{
			if (this.settings != null)
				this.initialize(reload);
		}

		/// <summary>
		/// Updates the particle system.
		/// </summary>
		public void Update(float elapsedTime)
		{
			if (this.main.Paused)
				return;

			this.currentTime += elapsedTime;

			this.retireActiveParticles();
			this.freeRetiredParticles();

			if (this.firstActiveParticle == this.firstFreeParticle)
				this.currentTime = 0;

			if (this.firstRetiredParticle == this.firstActiveParticle)
				this.drawCounter = 0;
		}


		/// <summary>
		/// Helper for checking when active particles have reached the end of
		/// their life. It moves old particles from the active area of the queue
		/// to the retired section.
		/// </summary>
		protected void retireActiveParticles()
		{
			float particleDuration = (float)settings.Duration.TotalSeconds;

			while (this.firstActiveParticle != this.firstNewParticle)
			{
				// Is this particle old enough to retire?
				// We multiply the active particle index by four, because each
				// particle consists of a quad that is made up of four vertices.
				float particleAge = this.currentTime - this.particles[this.firstActiveParticle * 4].Time;

				if (particleAge < particleDuration)
					break;

				// Remember the time at which we retired this particle.
				this.particles[firstActiveParticle * 4].Time = this.drawCounter;

				// Move the particle from the active to the retired queue.
				this.firstActiveParticle++;

				if (this.firstActiveParticle >= this.settings.MaxParticles)
					this.firstActiveParticle = 0;
			}
		}

		/// <summary>
		/// Helper for checking when retired particles have been kept around long
		/// enough that we can be sure the GPU is no longer using them. It moves
		/// old particles from the retired area of the queue to the free section.
		/// </summary>
		protected void freeRetiredParticles()
		{
			while (this.firstRetiredParticle != this.firstActiveParticle)
			{
				// Has this particle been unused long enough that
				// the GPU is sure to be finished with it?
				// We multiply the retired particle index by four, because each
				// particle consists of a quad that is made up of four vertices.
				int age = this.drawCounter - (int)this.particles[this.firstRetiredParticle * 4].Time;

				// The GPU is never supposed to get more than 2 frames behind the CPU.
				// We add 1 to that, just to be safe in case of buggy drivers that
				// might bend the rules and let the GPU get further behind.
				if (age < 3)
					break;

				// Move the particle from the retired to the free queue.
				this.firstRetiredParticle++;

				if (this.firstRetiredParticle >= settings.MaxParticles)
					this.firstRetiredParticle = 0;
			}
		}

		/// <summary>
		/// Draws the particle system if it is an opaque particle system.
		/// </summary>
		void IDrawableComponent.Draw(GameTime gameTime, RenderParameters parameters)
		{
			if (this.settings.BlendState == BlendState.Opaque)
				this.draw(parameters);
		}
		
		/// <summary>
		/// Draws the particle system, if it is in fact an alpha-enabled particle system.
		/// </summary>
		void IDrawablePostAlphaComponent.DrawPostAlpha(GameTime gameTime, RenderParameters parameters)
		{
			if (this.settings.BlendState != BlendState.Opaque && this.settings.PostAlpha)
				this.draw(parameters);
		}

		/// <summary>
		/// Draws the particle system, if it is in fact an alpha-enabled particle system.
		/// </summary>
		void IDrawableAlphaComponent.DrawAlpha(GameTime gameTime, RenderParameters parameters)
		{
			if (this.settings.BlendState != BlendState.Opaque && !this.settings.PostAlpha)
				this.draw(parameters);
		}

		protected void draw(RenderParameters parameters)
		{
			if (this.settings.UnsupportedTechniques.Contains(parameters.Technique))
				return;

			GraphicsDevice device = this.main.GraphicsDevice;

			// Restore the vertex buffer contents if the graphics device was lost.
			if (this.vertexBuffer.IsContentLost)
				this.vertexBuffer.SetData(particles);

			// If there are any particles waiting in the newly added queue,
			// we'd better upload them to the GPU ready for drawing.
			if (this.firstNewParticle != this.firstFreeParticle)
				this.addNewParticlesToVertexBuffer();

			// If there are any active particles, draw them now!
			if (this.firstActiveParticle != this.firstFreeParticle)
			{
				string techniqueName;
				if (settings.BlendState == BlendState.Additive)
					techniqueName = "AdditiveParticles";
				else if (settings.BlendState == BlendState.Opaque)
				{
					techniqueName = "OpaqueParticles";
					this.main.LightingManager.SetRenderParameters(this.particleEffect, parameters);
				}
				else
					techniqueName = "AlphaParticles";

				if (parameters.Technique == Technique.Clip)
					this.particleEffect.Parameters["ClipPlanes"].SetValue(parameters.ClipPlaneData);

				techniqueName = parameters.Technique.ToString() + techniqueName;

				EffectTechnique techniqueInstance = this.particleEffect.Techniques[techniqueName];
				if (techniqueInstance == null)
				{
					this.settings.UnsupportedTechniques.Add(parameters.Technique);
					return;
				}
				else
					this.particleEffect.CurrentTechnique = techniqueInstance;

				device.DepthStencilState = DepthStencilState.DepthRead;

				// Set an effect parameter describing the viewport size. This is
				// needed to convert particle sizes into screen space point sizes.
				if (this.effectViewportScaleParameter != null)
					this.effectViewportScaleParameter.SetValue(new Vector2(0.5f / device.Viewport.AspectRatio, -0.5f));

				// Set an effect parameter describing the current time. All the vertex
				// shader particle animation is keyed off this value.
				this.effectTimeParameter.SetValue(this.currentTime);

				// Update view/projection matrix
				if (this.effectViewParameter != null)
					this.effectViewParameter.SetValue(parameters.Camera.View);
				if (this.effectMaterialIdParameter != null)
					this.effectMaterialIdParameter.SetValue(this.main.LightingManager.GetMaterialIndex(this.settings.Material));
				if (this.effectInverseViewParameter != null)
					this.effectInverseViewParameter.SetValue(parameters.Camera.InverseView);
				if (this.effectCameraPositionParameter != null)
					this.effectCameraPositionParameter.SetValue(parameters.Camera.Position);
				if (this.effectProjectionParameter != null)
					this.effectProjectionParameter.SetValue(parameters.Camera.Projection);
				if (this.effectDepthBufferParameter != null)
					this.effectDepthBufferParameter.SetValue(parameters.DepthBuffer);
				if (this.effectFrameBufferParameter != null)
					this.effectFrameBufferParameter.SetValue(parameters.FrameBuffer);

				// Set the particle vertex and index buffer.
				device.SetVertexBuffer(this.vertexBuffer);
				device.Indices = this.indexBuffer;

				RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
				this.main.GraphicsDevice.RasterizerState = this.noCullRasterizerState;

				// Activate the particle effect.
				this.particleEffect.CurrentTechnique.Passes[0].Apply();

				if (this.firstActiveParticle < this.firstFreeParticle)
				{
					// If the active particles are all in one consecutive range,
					// we can draw them all in a single call.
					device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
												 this.firstActiveParticle * 4, (this.firstFreeParticle - this.firstActiveParticle) * 4,
												 this.firstActiveParticle * 6, (this.firstFreeParticle - this.firstActiveParticle) * 2);
					Model.DrawCallCounter++;
					Model.TriangleCounter += (this.firstFreeParticle - this.firstActiveParticle) * 2;
				}
				else
				{
					// If the active particle range wraps past the end of the queue
					// back to the start, we must split them over two draw calls.
					device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
												 this.firstActiveParticle * 4, (this.settings.MaxParticles - this.firstActiveParticle) * 4,
												 this.firstActiveParticle * 6, (this.settings.MaxParticles - this.firstActiveParticle) * 2);
					Model.DrawCallCounter++;
					Model.TriangleCounter += (this.settings.MaxParticles - this.firstActiveParticle) * 2;

					if (this.firstFreeParticle > 0)
					{
						device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
													 0, this.firstFreeParticle * 4,
													 0, this.firstFreeParticle * 2);
						Model.DrawCallCounter++;
						Model.TriangleCounter += this.firstFreeParticle * 2;
					}
				}

				this.main.GraphicsDevice.RasterizerState = originalState;

				// Reset some of the renderstates that we changed,
				// so as not to mess up any other subsequent drawing.
				device.DepthStencilState = DepthStencilState.Default;
			}

			this.drawCounter++;
		}

		/// <summary>
		/// Helper for uploading new particles from our managed
		/// array to the GPU vertex buffer.
		/// </summary>
		protected void addNewParticlesToVertexBuffer()
		{
			int stride = ParticleVertex.SizeInBytes;

			if (this.firstNewParticle < this.firstFreeParticle)
			{
				// If the new particles are all in one consecutive range,
				// we can upload them all in a single call.
				this.vertexBuffer.SetData(this.firstNewParticle * stride * 4, this.particles,
									 this.firstNewParticle * 4,
									 (this.firstFreeParticle - this.firstNewParticle) * 4,
									 stride, SetDataOptions.NoOverwrite);
			}
			else
			{
				// If the new particle range wraps past the end of the queue
				// back to the start, we must split them over two upload calls.
				this.vertexBuffer.SetData(this.firstNewParticle * stride * 4, this.particles,
									 this.firstNewParticle * 4,
									 (this.settings.MaxParticles - this.firstNewParticle) * 4,
									 stride, SetDataOptions.NoOverwrite);

				if (this.firstFreeParticle > 0)
				{
					this.vertexBuffer.SetData(0, this.particles,
										 0, this.firstFreeParticle * 4,
										 stride, SetDataOptions.NoOverwrite);
				}
			}

			// Move the particles we just uploaded from the new to the active queue.
			this.firstNewParticle = this.firstFreeParticle;
		}

		/// <summary>
		/// Adds a new particle to the system.
		/// </summary>
		public void AddParticle(Vector3 position, Vector3 velocity, float lifetime = -1.0f, float size = -1.0f, float prePrime = 0.0f)
		{
			if (lifetime == -1.0f)
				lifetime = (float)this.settings.Duration.TotalSeconds;
			
			if (size == -1.0f)
				size = this.settings.MinStartSize + (float)ParticleSystem.random.NextDouble() * (this.settings.MaxStartSize - this.settings.MinStartSize);

			// Figure out where in the circular queue to allocate the new particle.
			int nextFreeParticle = this.firstFreeParticle + 1;

			if (nextFreeParticle >= this.settings.MaxParticles)
				nextFreeParticle = 0;

			// If there are no free particles, we just have to give up.
			if (nextFreeParticle == this.firstRetiredParticle)
				return;

			// Adjust the input velocity based on how much
			// this particle system wants to be affected by it.
			velocity *= this.settings.EmitterVelocitySensitivity;

			// Add in some random amount of horizontal velocity.
			float horizontalVelocity = MathHelper.Lerp(this.settings.MinHorizontalVelocity,
													   this.settings.MaxHorizontalVelocity,
													   (float)ParticleSystem.random.NextDouble());

			double horizontalAngle = ParticleSystem.random.NextDouble() * MathHelper.TwoPi;

			velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
			velocity.Z += horizontalVelocity * (float)Math.Sin(horizontalAngle);

			// Add in some random amount of vertical velocity.
			velocity.Y += MathHelper.Lerp(this.settings.MinVerticalVelocity,
										  this.settings.MaxVerticalVelocity,
										  (float)ParticleSystem.random.NextDouble());

			// Choose four random control values. These will be used by the vertex
			// shader to give each particle a different size, rotation, and color.
			Color randomValues = new Color((byte)ParticleSystem.random.Next(255),
										   (byte)ParticleSystem.random.Next(255),
										   (byte)ParticleSystem.random.Next(255),
										   (byte)ParticleSystem.random.Next(255));

			// Fill in the particle vertex structure.
			for (int i = 0; i < 4; i++)
			{
				ParticleVertex v = this.particles[this.firstFreeParticle * 4 + i];
				v.Position = position;
				v.Velocity = velocity;
				v.Random = randomValues;
				v.Time = currentTime - prePrime;
				v.Lifetime = lifetime;
				v.StartSize = size;
				this.particles[this.firstFreeParticle * 4 + i] = v;
			}

			this.firstFreeParticle = nextFreeParticle;
		}
	}
}