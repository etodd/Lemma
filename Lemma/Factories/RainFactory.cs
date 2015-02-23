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
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			Rain rain = entity.GetOrCreate<Rain>("Rain");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Emitter");
			emitter.Add(new Binding<Vector3>(emitter.Jitter, rain.Jitter));
			rain.Add(new Binding<int>(rain.ParticlesPerSecond, emitter.ParticlesPerSecond));

			Components.DirectionalLight lightning = entity.GetOrCreate<Components.DirectionalLight>("Lightning");
			lightning.Serialize = false;
			lightning.Add(new Binding<Quaternion>(lightning.Quaternion, transform.Quaternion));
			lightning.Add(new Binding<bool>(lightning.Shadowed, rain.LightningShadowed));
			lightning.Enabled.Value = false;

			lightning.Add(new TwoWayBinding<Vector3>(rain.CurrentLightningColor, lightning.Color));

			if (!main.EditorEnabled)
				lightning.Add(new Binding<bool>(lightning.Enabled, rain.LightningEnabled));

			emitter.AddParticle = delegate(Vector3 position, Vector3 velocity, float prime) 
			{
				Vector3 kernelCoord = (position - rain.KernelOffset) / Rain.KernelSpacing;
				float height = rain.RaycastHeights[Math.Max(0, Math.Min(Rain.KernelSize - 1, (int)kernelCoord.X)), Math.Max(0, Math.Min(Rain.KernelSize - 1, (int)kernelCoord.Z))];
				if (height < position.Y)
					emitter.ParticleSystem.AddParticle(position, Vector3.Zero, Math.Min((position.Y - height) / Rain.VerticalSpeed, Rain.MaxLifetime), -1.0f, prime);
			};

			emitter.Add(new Binding<Vector3>(emitter.Position, x => x + new Vector3(0.0f, Rain.StartHeight, 0.0f), main.Camera.Position));

			this.SetMain(entity, main);
			emitter.ParticleType.Value = "Rain";

			entity.Add("ThunderIntervalMin", rain.ThunderIntervalMin);
			entity.Add("ThunderIntervalMax", rain.ThunderIntervalMax);
			entity.Add("ThunderMaxDelay", rain.ThunderMaxDelay);
			entity.Add("ParticlesPerSecond", emitter.ParticlesPerSecond);
			entity.Add("LightningShadowed", rain.LightningShadowed);
			entity.Add("LightningColor", rain.LightningColor);

			entity.Add(new PostInitialization
			{
				delegate()
				{
					rain.Update();
					emitter.Prime(Vector3.Zero);
				}
			});
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\light";
			model.Color.Value = this.Color;
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));

			Components.DirectionalLight lightning = entity.Get<Components.DirectionalLight>();
			lightning.Add(new Binding<bool>(lightning.Enabled, entity.EditorSelected));
		}
	}
}