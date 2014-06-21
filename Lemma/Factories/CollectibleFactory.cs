using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CollectibleFactory : Factory<Main>
	{
		private Random random = new Random();

		public CollectibleFactory()
		{
			this.Color = new Vector3(0.5f, 0.5f, 0.5f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Collectible");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");
			trigger.Serialize = false;

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			
			trigger.Radius.Value = 3;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			Collectible collectible = entity.GetOrCreate<Collectible>("Collectible");

			collectible.Add(new CommandBinding(trigger.PlayerEntered, collectible.PlayerTouched));

			AkGameObjectTracker.Attach(entity, trigger.Position);

			PointLight light = entity.Create<PointLight>();
			light.Serialize = false;
			light.Attenuation.Value = 10.0f;
			light.Color.Value = new Vector3(1.25f, 1.75f, 2.0f);
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			ParticleEmitter distortionEmitter = entity.GetOrCreate<ParticleEmitter>("DistortionEmitter");
			distortionEmitter.Serialize = false;
			distortionEmitter.Add(new Binding<Vector3>(distortionEmitter.Position, trigger.Position));
			distortionEmitter.ParticleType.Value = "Distortion";
			distortionEmitter.ParticlesPerSecond.Value = 4;
			distortionEmitter.Jitter.Value = new Vector3(0.5f);

			Model model = entity.GetOrCreate<Model>("Model");
			model.MapContent.Value = true;
			model.Filename.Value = "Models\\sphere";
			model.Serialize = false;
			model.Scale.Value = new Vector3(0.5f);
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			this.SetMain(entity, main);

			attachable.EditorProperties();
			entity.Add("Collected", collectible.PlayerTouched);
		}


		public override void AttachEditorComponents(Entity entity, Main main)
		{
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
			PlayerTrigger.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
