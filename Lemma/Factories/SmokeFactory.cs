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
		private static Random random = new Random();

		public SmokeFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
			this.EditorCanSpawn = false;
		}

		const float speed = 20.0f;

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Smoke");
			Smoke smoke = new Smoke();
			smoke.Velocity.Value = speed * Vector3.Normalize(new Vector3(((float)random.NextDouble() - 0.5f) * 2.0f, (float)random.NextDouble(), ((float)random.NextDouble() - 0.5f) * 2.0f));
			entity.Add("Smoke", smoke);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.Serialize = false;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Smoke smoke = entity.GetOrCreate<Smoke>("Smoke");
			smoke.Add(new TwoWayBinding<Vector3>(transform.Position, smoke.Position));
			smoke.Add(new CommandBinding(smoke.Delete, entity.Delete));

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
			emitter.ParticlesPerSecond.Value = 35;
			emitter.ParticleType.Value = "Smoke";

			this.SetMain(entity, main);
		}
	}
}
