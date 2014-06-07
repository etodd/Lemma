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
			//Shamefully ripped STRAIGHT from SignalTower
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");
			this.SetMain(entity, main);

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				VoxelAttachable.MakeAttachable(entity, main);
			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			
			trigger.Editable = true;
			trigger.Radius.Value = 3;
			trigger.Add(new Binding<Vector3>(trigger.Position, () => Vector3.Transform(new Vector3(0.0f, 0.0f, attachOffset), transform.Matrix), attachOffset, transform.Matrix));

			Collectible collectible = entity.GetOrCreate<Collectible>("Collectible");

			collectible.Add(new CommandBinding(trigger.PlayerEntered, collectible.PlayerTouched));

			AkGameObjectTracker.Attach(entity, trigger.Position);

			ParticleEmitter distortionEmitter = entity.GetOrCreate<ParticleEmitter>("DistortionEmitter");
			distortionEmitter.Serialize = false;
			distortionEmitter.Add(new Binding<Vector3>(distortionEmitter.Position, trigger.Position));
			distortionEmitter.ParticleType.Value = "Distortion";
			distortionEmitter.ParticlesPerSecond.Value = 4;
			distortionEmitter.Jitter.Value = new Vector3(0.5f);

			ParticleEmitter purpleEmitter = entity.GetOrCreate<ParticleEmitter>("PurpleEmitter");
			purpleEmitter.Serialize = false;
			purpleEmitter.Add(new Binding<Vector3>(purpleEmitter.Position, trigger.Position));
			purpleEmitter.ParticleType.Value = "Purple";
			purpleEmitter.ParticlesPerSecond.Value = 30;
			purpleEmitter.Jitter.Value = new Vector3(0.5f);

			Model model = entity.GetOrCreate<Model>("Model");
			model.MapContent.Value = true;
			model.Filename.Value = "Models\\sphere";
			model.Editable = false;
			model.Serialize = false;
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
			PlayerTrigger.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
