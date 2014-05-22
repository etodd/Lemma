using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SmokeFactory : Factory<Main>
	{
		private Random random = new Random();
		public SmokeFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Smoke");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.Serialize = false;
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			const float speed = 20.0f;

			Property<Vector3> velocity = entity.GetOrMakeProperty<Vector3>("Velocity");
			velocity.Value = speed * Vector3.Normalize(new Vector3(((float)this.random.NextDouble() - 0.5f) * 2.0f, (float)this.random.NextDouble(), ((float)this.random.NextDouble() - 0.5f) * 2.0f));
			Property<float> lifetime = entity.GetOrMakeProperty<float>("Lifetime");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
			emitter.ParticlesPerSecond.Value = 35;
			emitter.ParticleType.Value = "Smoke";
			entity.Add(new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;
					if (lifetime > 1.0f)
						entity.Delete.Execute();
					else
					{
						velocity.Value += new Vector3(0, dt * -11.0f, 0);
						transform.Position.Value += velocity.Value * dt;
					}
				}
			});

			this.SetMain(entity, main);
		}
	}
}
