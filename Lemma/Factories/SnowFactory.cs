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

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			Snow snow = entity.GetOrCreate<Snow>("Snow");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Add(new Binding<Vector3>(emitter.Jitter, snow.Jitter));

			Property<Vector3> dir = new Property<Vector3>();
			transform.Add(new Binding<Vector3, Quaternion>(dir, x => Vector3.Transform(Vector3.Down, x), transform.Quaternion));
			snow.Add(new Binding<Quaternion>(snow.Orientation, transform.Quaternion));

			emitter.Position.Value = new Vector3(0, Snow.StartHeight, 0);

			emitter.AddParticle = delegate(Vector3 position, Vector3 velocity)
			{
				Vector3 kernelCoord = (position + snow.Jitter) / Snow.KernelSpacing;
				float distance = snow.RaycastDistances[Math.Max(0, Math.Min(Snow.KernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(Snow.KernelSize - 1, (int)kernelCoord.Z))];
				if (distance > 0)
					emitter.ParticleSystem.AddParticle(main.Camera.Position + Vector3.Transform(position, transform.Quaternion), dir.Value * snow.WindSpeed.Value, Math.Min(distance / snow.WindSpeed, Snow.MaxLifetime));
			};

			this.SetMain(entity, main);
			emitter.ParticleType.Value = "Snow";

			entity.Add("ParticlesPerSecond", emitter.ParticlesPerSecond);
			entity.Add("Wind", snow.WindSpeed);
		}
	}
}