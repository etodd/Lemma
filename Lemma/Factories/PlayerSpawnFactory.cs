using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PlayerSpawnFactory : Factory
	{
		public PlayerSpawnFactory()
		{
			this.Color = new Vector3(0.8f, 0.4f, 1.5f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PlayerSpawn");

			result.Add("PlayerSpawn", new PlayerSpawn());

			Transform transform = new Transform();
			result.Add("Transform", transform);

			result.Add("Trigger", new PlayerTrigger());

			ParticleEmitter emitter1 = new ParticleEmitter();
			emitter1.ParticleType.Value = "Distortion";
			emitter1.ParticlesPerSecond.Value = 5;
			result.Add("ParticleEmitter1", emitter1);

			ParticleEmitter emitter2 = new ParticleEmitter();
			emitter2.ParticleType.Value = "Purple";
			emitter2.ParticlesPerSecond.Value = 5;
			result.Add("ParticleEmitter2", emitter2);

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(0.8f, 0.4f, 1.5f);
			light.Shadowed.Value = false;
			light.Attenuation.Value = 5.0f;
			result.Add("Light", light);

			Sound sound = new Sound();
			sound.Cue.Value = "SpawnLoop";
			sound.Is3D.Value = true;
			sound.IsPlaying.Value = true;
			sound.Add(new Binding<Vector3>(sound.Position, transform.Position));
			result.Add("Sound", sound);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);

			Transform transform = result.Get<Transform>();

			PlayerSpawn spawn = result.Get<PlayerSpawn>();
			spawn.Add(new TwoWayBinding<Vector3>(transform.Position, spawn.Position));
			spawn.Add(new Binding<float, Vector3>(spawn.Rotation, x => ((float)Math.PI * -0.5f) - (float)Math.Atan2(x.Z, x.X), transform.Forward));

			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player) { spawn.Activate.Execute(); }));

			PointLight light = result.Get<PointLight>();
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			spawn.Add(new CommandBinding(spawn.Activate, delegate()
			{
				Sound.PlayCue(main, "SpawnActivate", transform.Position);
				result.Add(new Animation
				(
					new Animation.FloatMoveTo(light.Attenuation, 30.0f, 0.4f),
					new Animation.FloatMoveTo(light.Attenuation, 5.0f, 1.0f)
				));
			}));

			ParticleEmitter emitter1 = result.Get<ParticleEmitter>("ParticleEmitter1");
			emitter1.Add(new Binding<Vector3>(emitter1.Position, transform.Position));

			ParticleEmitter emitter2 = result.Get<ParticleEmitter>("ParticleEmitter2");
			emitter2.Add(new Binding<Vector3>(emitter2.Position, transform.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Color.Value = this.Color;
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));

			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
