using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class Rain : Component<Main>, IUpdateableComponent
	{
		public const float KernelSpacing = 8.0f;
		public const int KernelSize = 10;
		public const float RaycastHeight = 30.0f;
		public const float RaycastInterval = 0.25f;
		public const float VerticalSpeed = 90.0f;
		public const float MaxLifetime = 1.0f;
		public const float StartHeight = 25.0f;

		private Vector3 originalLightningColor;

		private float[,] audioKernel = new float[KernelSize, KernelSize];

		[XmlIgnore]
		public float[,] RaycastHeights = new float[KernelSize, KernelSize];

		private float raycastTimer = RaycastInterval;

		private float thunderTimer;

		private ModelAlpha skybox = null;
		private Vector3 originalSkyboxColor = Vector3.Zero;

		private static Random random = new Random();

		// Input properties
		public Property<float> ThunderIntervalMin = new Property<float> { Value = 12.0f };
		public Property<float> ThunderIntervalMax = new Property<float> { Value = 36.0f };
		public Property<float> ThunderMaxDelay = new Property<float> { Value = 5.0f };

		// Output properties
		public Property<Vector3> Jitter = new Property<Vector3>();
		public Property<Vector3> KernelOffset = new Property<Vector3>();
		public Property<Vector3> LightningColor = new Property<Vector3>();
		public Property<bool> LightningEnabled = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;

			float sum = 0.0f;
			for (int x = 0; x < KernelSize; x++)
			{
				for (int y = 0; y < KernelSize; y++)
				{
					float cell = (KernelSize / 2) - new Vector2(x - (KernelSize / 2), y - (KernelSize / 2)).Length();
					audioKernel[x, y] = cell;
					sum += cell;
				}
			}

			for (int x = 0; x < KernelSize; x++)
			{
				for (int y = 0; y < KernelSize; y++)
					audioKernel[x, y] /= sum;
			}

			this.Jitter.Value = new Vector3(KernelSpacing * KernelSize * 0.5f, 0.0f, KernelSpacing * KernelSize * 0.5f);

			if (ParticleSystem.Get(main, "Rain") == null)
			{
				ParticleSystem.Add(main, "Rain",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\default",
					EffectFile = "Effects\\ParticleRain",
					MaxParticles = 25000,
					Duration = TimeSpan.FromSeconds(MaxLifetime),
					MinHorizontalVelocity = 0.0f,
					MaxHorizontalVelocity = 0.0f,
					MinVerticalVelocity = -VerticalSpeed,
					MaxVerticalVelocity = -VerticalSpeed,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					MinRotateSpeed = 0.0f,
					MaxRotateSpeed = 0.0f,
					MinStartSize = 0.3f,
					MaxStartSize = 0.3f,
					MinEndSize = 0.3f,
					MaxEndSize = 0.3f,
					BlendState = BlendState.Opaque,
					MinColor = new Vector4(0.5f, 0.6f, 0.7f, 1.0f),
					MaxColor = new Vector4(0.5f, 0.6f, 0.7f, 1.0f),
					Material = new Components.Model.Material { SpecularIntensity = 0.0f, SpecularPower = 1.0f },
				});
			}

			this.thunderTimer = this.ThunderIntervalMin + ((float)random.NextDouble() * (this.ThunderIntervalMax - this.ThunderIntervalMin));
		}

		public override void Start()
		{
			if (!main.EditorEnabled)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_RAIN, this.Entity);

			Entity skyboxEntity = main.Get("Skybox").FirstOrDefault();
			if (skyboxEntity != null)
			{
				skybox = skyboxEntity.Get<ModelAlpha>();
				originalSkyboxColor = skybox.Color;
			}

			this.originalLightningColor = this.LightningColor;
		}

		public void Update()
		{
			this.raycastTimer = 0.0f;
			Vector3 cameraPos = main.Camera.Position;
			float averageHeight = 0.0f;
			this.KernelOffset.Value = main.Camera.Position + new Vector3(KernelSize * KernelSpacing * -0.5f, RaycastHeight + StartHeight, KernelSize * KernelSpacing * -0.5f);
			for (int x = 0; x < KernelSize; x++)
			{
				for (int y = 0; y < KernelSize; y++)
				{
					Vector3 pos = KernelOffset + new Vector3(x * KernelSpacing, 0, y * KernelSpacing);
					Voxel.GlobalRaycastResult raycast = Voxel.GlobalRaycast(pos, Vector3.Down, StartHeight + RaycastHeight + (VerticalSpeed * MaxLifetime), (index, type) => type != Voxel.t.Invisible);
					float height = raycast.Voxel == null ? float.MinValue : raycast.Position.Y;
					this.RaycastHeights[x, y] = height;
					averageHeight += Math.Max(cameraPos.Y, Math.Min(height, cameraPos.Y + StartHeight)) * audioKernel[x, y];
				}
			}
			AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.RAIN_VOLUME, 1.0f - ((averageHeight - cameraPos.Y) / StartHeight));
		}

		public void Update(float dt)
		{
			this.raycastTimer += dt;
			if (this.raycastTimer > RaycastInterval)
				this.Update();

			if (!this.main.EditorEnabled)
			{
				this.thunderTimer -= dt;
				if (this.thunderTimer < 0)
				{
					float volume = 0.6f + ((float)random.NextDouble() * 0.4f);
					this.LightningColor.Value = Vector3.Zero;
					Property<Vector3> skyboxColor;
					if (skybox == null)
						skyboxColor = new Property<Vector3>(); // Dummy property
					else
						skyboxColor = skybox.Color;

					this.Entity.Add(new Animation
					(
						new Animation.Set<bool>(this.LightningEnabled, true),
						new Animation.Parallel
						(
							new Animation.Vector3MoveTo(this.LightningColor, this.originalLightningColor * volume, 0.2f),
							new Animation.Vector3MoveTo(skyboxColor, this.originalSkyboxColor * (1.0f + volume), 0.2f)
						),
						new Animation.Parallel
						(
							new Animation.Vector3MoveTo(this.LightningColor, Vector3.Zero, 0.5f),
							new Animation.Vector3MoveTo(skyboxColor, this.originalSkyboxColor, 0.5f)
						),
						new Animation.Set<bool>(this.LightningEnabled, false),
						new Animation.Delay((1.0f - volume) * this.ThunderMaxDelay),
						new Animation.Execute(delegate()
						{
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_THUNDER, main.Camera.Position + Vector3.Normalize(new Vector3(2.0f * ((float)random.NextDouble() - 0.5f), 1.0f, 2.0f * ((float)random.NextDouble() - 0.5f))) * 1000.0f);
						})
					));
					this.thunderTimer = this.ThunderIntervalMin + ((float)random.NextDouble() * (this.ThunderIntervalMax - this.ThunderIntervalMin));
				}
			}
		}

		public override void delete()
		{
			base.delete();
			AkSoundEngine.PostEvent(AK.EVENTS.STOP_RAIN, this.Entity);
		}
	}
}
