using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SignalTowerFactory : Factory
	{
		private Random random = new Random();

		public SignalTowerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "SignalTower");

			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.GetOrCreate<Transform>("Transform");
			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);
			
			PointLight light = result.GetOrCreate<PointLight>();
			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Color.Value = new Vector3(1.0f, 0.5f, 1.7f);

			Property<float> lightBaseRadius = new Property<float> { Value = 10.0f };

			Updater updater = new Updater
			{
				delegate(float dt)
				{
					light.Attenuation.Value = lightBaseRadius.Value * (1.0f + (((float)this.random.NextDouble() - 0.5f) * 0.1f));
				}
			};
			updater.EnabledInEditMode.Value = true;
			result.Add(updater);
			
			PlayerTrigger trigger = result.GetOrCreate<PlayerTrigger>();
			trigger.Radius.Value = 15.0f;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			Sound loop = result.GetOrCreate<Sound>("LoopSound");
			loop.Serialize = false;
			loop.Cue.Value = "Signal Tower Loop";
			loop.Is3D.Value = true;
			loop.Add(new Binding<Vector3>(loop.Position, transform.Position));
			loop.IsPlaying.Value = true;

			Sound activate = result.GetOrCreate<Sound>("ActivateSound");
			activate.Serialize = false;
			activate.Cue.Value = "Signal Tower Activate";
			activate.Is3D.Value = true;
			activate.Add(new Binding<Vector3>(activate.Position, transform.Position));

			ParticleEmitter distortionEmitter = result.GetOrCreate<ParticleEmitter>("DistortionEmitter");
			distortionEmitter.Serialize = false;
			distortionEmitter.Add(new Binding<Vector3>(distortionEmitter.Position, transform.Position));
			distortionEmitter.ParticleType.Value = "Distortion";
			distortionEmitter.ParticlesPerSecond.Value = 4;
			distortionEmitter.Jitter.Value = new Vector3(0.5f);

			ParticleEmitter purpleEmitter = result.GetOrCreate<ParticleEmitter>("PurpleEmitter");
			purpleEmitter.Serialize = false;
			purpleEmitter.Add(new Binding<Vector3>(purpleEmitter.Position, transform.Position));
			purpleEmitter.ParticleType.Value = "Purple";
			purpleEmitter.ParticlesPerSecond.Value = 30;
			purpleEmitter.Jitter.Value = new Vector3(0.5f);

			Animation enterAnimation = null;
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				if (enterAnimation == null || !enterAnimation.Active)
				{
					activate.Play.Execute();
					enterAnimation = new Animation
					(
						new Animation.FloatMoveTo(lightBaseRadius, 20.0f, 0.25f),
						new Animation.FloatMoveTo(lightBaseRadius, 10.0f, 0.5f)
					);
					result.Add(enterAnimation);
				}
			}));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);
		}
	}
}
