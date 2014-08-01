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
	public class Snow : Component<Main>, IUpdateableComponent
	{
		public const float KernelSpacing = 8.0f;
		public const int KernelSize = 10;
		public const float RaycastHeight = 30.0f;
		public const float RaycastInterval = 0.25f;
		public const float MaxLifetime = 5.0f;
		public const float MaxWindLifetime = 8.0f;
		public const float StartHeight = 30.0f;

		[XmlIgnore]
		public float[,] RaycastDistances = new float[KernelSize, KernelSize];

		private float raycastTimer = RaycastInterval;

		private static Random random = new Random();

		// Input properties
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<float> WindSpeed = new Property<float>();

		// Output properties
		public Property<Vector3> Jitter = new Property<Vector3>();
		public Property<Vector3> KernelOffset = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;

			this.Jitter.Value = new Vector3(KernelSpacing * KernelSize * 0.5f, KernelSpacing * KernelSize * 0.1f, KernelSpacing * KernelSize * 0.5f);

			if (ParticleSystem.Get(main, "Snow") == null)
			{
				ParticleSystem.Add(main, "Snow",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\default",
					EffectFile = "Effects\\ParticleSnow",
					MaxParticles = 50000,
					Duration = TimeSpan.FromSeconds(Snow.MaxLifetime),
					MinHorizontalVelocity = -1.0f,
					MaxHorizontalVelocity = 1.0f,
					MinVerticalVelocity = -1.0f,
					MaxVerticalVelocity = 1.0f,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					MinRotateSpeed = 0.0f,
					MaxRotateSpeed = 0.0f,
					MinStartSize = 0.05f,
					MaxStartSize = 0.15f,
					MinEndSize = 0.05f,
					MaxEndSize = 0.15f,
					MinColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f),
					MaxColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f),
					EmitterVelocitySensitivity = 1.0f,
					BlendState = BlendState.Opaque,
					Material = new Components.Model.Material { SpecularIntensity = 0.0f, SpecularPower = 1.0f },
				});
				ParticleSystem.Add(main, "Wind",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\wind",
					EffectFile = "Effects\\Particle",
					MaxParticles = 10000,
					Duration = TimeSpan.FromSeconds(Snow.MaxWindLifetime),
					MinHorizontalVelocity = -1.0f,
					MaxHorizontalVelocity = 1.0f,
					MinVerticalVelocity = -1.0f,
					MaxVerticalVelocity = 1.0f,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					MinRotateSpeed = -1.0f,
					MaxRotateSpeed = 1.0f,
					MinStartSize = 15.0f,
					MaxStartSize = 25.0f,
					MinEndSize = 25.0f,
					MaxEndSize = 40.0f,
					MinColor = new Vector4(1.0f, 1.0f, 1.0f, 0.25f),
					MaxColor = new Vector4(1.0f, 1.0f, 1.0f, 0.25f),
					EmitterVelocitySensitivity = 1.0f,
					BlendState = BlendState.AlphaBlend,
				});
			}
		}

		public void Update(float dt)
		{
			this.raycastTimer += dt;
			if (this.raycastTimer > RaycastInterval)
			{
				this.raycastTimer = 0.0f;
				this.KernelOffset.Value = main.Camera.Position + Vector3.Transform(new Vector3(KernelSize * KernelSpacing * -0.5f, RaycastHeight + StartHeight, KernelSize * KernelSpacing * -0.5f), this.Orientation);
				Vector3 dir = Vector3.Transform(Vector3.Down, this.Orientation);
				for (int x = 0; x < KernelSize; x++)
				{
					for (int y = 0; y < KernelSize; y++)
					{
						Vector3 pos = this.KernelOffset + Vector3.Transform(new Vector3(x * KernelSpacing, 0, y * KernelSpacing), this.Orientation);
						Voxel.GlobalRaycastResult raycast = Voxel.GlobalRaycast(pos, dir, (StartHeight * 2.0f) + RaycastHeight, (index, type) => type != Voxel.t.Invisible);
						this.RaycastDistances[x, y] = raycast.Voxel == null ? float.MaxValue : raycast.Distance - RaycastHeight;
					}
				}
			}
		}
	}
}
