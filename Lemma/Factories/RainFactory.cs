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
	public class RainFactory : Factory<Main>
	{
		public RainFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Rain");

			ParticleEmitter emitter = new ParticleEmitter();
			emitter.ParticleType.Value = "Rain";
			emitter.ParticlesPerSecond.Value = 12000;
			entity.Add("Emitter", emitter);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;
			entity.GetOrCreate<Transform>("Transform");

			Rain rain = entity.GetOrCreate<Rain>("Rain");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Jitter.Editable = false;
			emitter.ParticleType.Editable = false;
			emitter.Add(new Binding<Vector3>(emitter.Jitter, rain.Jitter));

			Components.DirectionalLight lightning = entity.GetOrCreate<Components.DirectionalLight>("Lightning");
			lightning.Enabled.Value = false;

			lightning.Add(new TwoWayBinding<Vector3>(lightning.Color, rain.LightningColor));

			if (!main.EditorEnabled)
				lightning.Add(new Binding<bool>(lightning.Enabled, rain.LightningEnabled));

			emitter.AddParticle = delegate(Vector3 position, Vector3 velocity) 
			{
				Vector3 kernelCoord = (position - rain.KernelOffset) / Rain.KernelSpacing;
				float height = rain.RaycastHeights[Math.Max(0, Math.Min(Rain.KernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(Rain.KernelSize - 1, (int)kernelCoord.Z))];
				if (height < position.Y)
					emitter.ParticleSystem.AddParticle(position, Vector3.Zero, Math.Min((position.Y - height) / Rain.VerticalSpeed, Rain.MaxLifetime));
			};

			emitter.Add(new Binding<Vector3>(emitter.Position, x => x + new Vector3(0.0f, Rain.StartHeight, 0.0f), main.Camera.Position));

			this.SetMain(entity, main);
			emitter.ParticleType.Value = "Rain";
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Components.DirectionalLight lightning = entity.Get<Components.DirectionalLight>();
			lightning.Add(new Binding<bool>(lightning.Enabled, entity.EditorSelected));
		}
	}
}