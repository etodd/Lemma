using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Factories
{
	public class SnowFactory : Factory<Main>
	{
		public SnowFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Snow");

			ParticleEmitter emitter = new ParticleEmitter();
			emitter.ParticlesPerSecond.Value = 8000;
			entity.Add("Emitter", emitter);

			ParticleEmitter windEmitter = new ParticleEmitter();
			windEmitter.ParticlesPerSecond.Value = 200;
			entity.Add("WindEmitter", windEmitter);

			return entity;
		}

		public const float MaxLifetime = 5.0f;
		public const float MaxWindLifetime = 8.0f;

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			if (ParticleSystem.Get(main, "Snow") == null)
			{
				ParticleSystem.Add(main, "Snow",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\default",
					EffectFile = "Effects\\ParticleSnow",
					MaxParticles = 50000,
					Duration = TimeSpan.FromSeconds(SnowFactory.MaxLifetime),
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
					Duration = TimeSpan.FromSeconds(SnowFactory.MaxWindLifetime),
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

			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			ParticleWind snow = entity.GetOrCreate<ParticleWind>("Wind");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Add(new Binding<Vector3>(emitter.Jitter, snow.Jitter));

			ParticleEmitter windEmitter = entity.GetOrCreate<ParticleEmitter>("WindEmitter");
			windEmitter.Add(new Binding<Vector3>(windEmitter.Jitter, snow.Jitter));

			Property<Vector3> dir = new Property<Vector3>();
			transform.Add(new Binding<Vector3, Quaternion>(dir, x => Vector3.Transform(Vector3.Down, x), transform.Quaternion));
			snow.Add(new Binding<Quaternion>(snow.Orientation, transform.Quaternion));

			emitter.Position.Value = new Vector3(0, ParticleWind.StartHeight, 0);
			windEmitter.Position.Value = new Vector3(0, ParticleWind.StartHeight * 2, 0);

			emitter.AddParticle = delegate(Vector3 position, Vector3 velocity)
			{
				Vector3 kernelCoord = (position + snow.Jitter) / ParticleWind.KernelSpacing;
				float distance = snow.RaycastDistances[Math.Max(0, Math.Min(ParticleWind.KernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(ParticleWind.KernelSize - 1, (int)kernelCoord.Z))];
				if (distance > 0)
					emitter.ParticleSystem.AddParticle(main.Camera.Position + Vector3.Transform(position, transform.Quaternion), dir.Value * snow.Speed.Value, Math.Min(distance / snow.Speed, SnowFactory.MaxLifetime));
			};
			
			windEmitter.AddParticle = delegate(Vector3 position, Vector3 velocity)
			{
				Vector3 kernelCoord = (position + snow.Jitter) / ParticleWind.KernelSpacing;
				float distance = snow.RaycastDistances[Math.Max(0, Math.Min(ParticleWind.KernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(ParticleWind.KernelSize - 1, (int)kernelCoord.Z))];
				if (distance > 0)
					windEmitter.ParticleSystem.AddParticle(main.Camera.Position + Vector3.Transform(position, transform.Quaternion), dir.Value * snow.Speed.Value, Math.Min((distance + ParticleWind.StartHeight) / snow.Speed, SnowFactory.MaxWindLifetime));
			};

			this.SetMain(entity, main);
			emitter.ParticleType.Value = "Snow";
			windEmitter.ParticleType.Value = "Wind";

			entity.Add("ParticlesPerSecond", emitter.ParticlesPerSecond);
			entity.Add("WindParticlesPerSecond", windEmitter.ParticlesPerSecond);
			entity.Add("Wind", snow.Speed);
		}
	}
}