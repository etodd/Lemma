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
			Entity result = new Entity(main, "Smoke");
			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.Serialize = false;
			Transform transform = result.GetOrCreate<Transform>();

			const float speed = 20.0f;

			Property<Vector3> velocity = result.GetOrMakeProperty<Vector3>("Velocity");
			velocity.Value = speed * Vector3.Normalize(new Vector3(((float)this.random.NextDouble() - 0.5f) * 2.0f, (float)this.random.NextDouble(), ((float)this.random.NextDouble() - 0.5f) * 2.0f));
			Property<float> lifetime = result.GetOrMakeProperty<float>("Lifetime");

			ParticleEmitter emitter = result.GetOrCreate<ParticleEmitter>();
			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
			emitter.ParticlesPerSecond.Value = 35;
			emitter.ParticleType.Value = "Smoke";
			result.Add(new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;
					if (lifetime > 1.0f)
						result.Delete.Execute();
					else
					{
						velocity.Value += new Vector3(0, dt * -11.0f, 0);
						transform.Position.Value += velocity.Value * dt;
					}
				}
			});

			this.SetMain(result, main);
		}
	}
}
