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
	public class DustFactory : Factory<Main>
	{
		public DustFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Dust");

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			if (ParticleSystem.Get(main, "Dust") == null)
			{
				ParticleSystem.Add(main, "Dust",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\default",
					EffectFile = "Effects\\ParticleSnow",
					MaxParticles = 20000,
					Duration = TimeSpan.FromSeconds(3.0f),
					MinHorizontalVelocity = -0.25f,
					MaxHorizontalVelocity = 0.25f,
					MinVerticalVelocity = -0.25f,
					MaxVerticalVelocity = 0.25f,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					MinRotateSpeed = 0.0f,
					MaxRotateSpeed = 0.0f,
					MinStartSize = 0.02f,
					MaxStartSize = 0.08f,
					MinEndSize = 0.0f,
					MaxEndSize = 0.0f,
					MinColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
					MaxColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
					EmitterVelocitySensitivity = 1.0f,
					BlendState = BlendState.Opaque,
					Material = new Components.Model.Material { SpecularIntensity = 0.0f, SpecularPower = 1.0f },
				});
			}

			entity.CannotSuspendByDistance = true;
			entity.GetOrCreate<Transform>("Transform");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");

			this.SetMain(entity, main);

			emitter.CalculateVelocity = false;
			emitter.ParticleType.Value = "Dust";
			emitter.Jitter.Value = new Vector3(10, 10, 10);
			emitter.ParticlesPerSecond.Value = 300;
			emitter.Add(new Binding<Vector3>(emitter.Position, main.Camera.Position));

			entity.Add("Velocity", emitter.VelocityOffset);

			entity.Add(new PostInitialization(delegate()
			{
				emitter.Prime(Vector3.Zero);
			}));
		}
	}
}