using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class EnergyPickupFactory : Factory
	{
		public EnergyPickupFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "EnergyPickup");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\sphere";

			PointLight light = new PointLight();
			light.Shadowed.Value = false;
			result.Add("Light", light);

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			result.Add("Energy", new Property<int> { Value = 20 });
			result.Add("Respawn", new Property<bool> { Value = true });
			result.Add("RespawnDelay", new Property<int> { Editable = true, Value = 60 * 3 });
			result.Add("RespawnTime", new Property<float> { Editable = false });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>();
			model.Editable = false;
			PointLight light = result.Get<PointLight>();
			light.Editable = false;
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			trigger.Editable = false;
			Property<int> energy = result.GetOrMakeProperty<int>("Energy", true);
			Property<bool> respawn = result.GetOrMakeProperty<bool>("Respawn", true);
			Property<float> respawnTime = result.GetOrMakeProperty<float>("RespawnTime");
			Property<int> respawnDelay = result.GetOrMakeProperty<int>("RespawnDelay", true);

			energy.Set = delegate(int value)
			{
				energy.InternalValue = Math.Max(0, Math.Min(100, value));
			};

			Property<float> gameTime = Factory.Get<PlayerDataFactory>().Instance(main).GetProperty<float>("GameTime");

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				Animation animation;
				if (energy < 10)
				{
					Sound.PlayCue(main, "GainHealthSmall", transform.Position, 1.0f, 0.05f);
					animation = new Animation
					(
						new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.5f), 0.1f),
						new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.0f), 0.25f)
					);
				}
				else
				{
					Sound.PlayCue(main, "GainHealth", 1.0f, 2.0f);
					animation = new Animation
					(
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(main.Renderer.BlurAmount, 0.5f, 0.25f),
							new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.5f), 0.25f)
						),
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(main.Renderer.BlurAmount, 0.0f, 0.75f),
							new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.0f), 0.75f)
						)
					);
				}
				
				animation.EnabledWhenPaused.Value = false;
				main.AddComponent(animation);
				player.Get<Player>().Stamina.Value += energy;

				respawnTime.Value = gameTime + respawnDelay;
				light.Enabled.Value = false;
				model.Enabled.Value = false;
				trigger.Enabled.Value = false;
			}));

			if (!main.EditorEnabled)
			{
				result.Add(new PostInitialization
				{
					delegate()
					{
						gameTime = Factory.Get<PlayerDataFactory>().Instance(main).GetProperty<float>("GameTime");
					}
				});

				Updater respawner = new Updater
				{
					delegate(float dt)
					{
						if (respawnTime > 0 && gameTime > respawnTime)
						{
							model.Enabled.Value = true;
							light.Enabled.Value = true;
							trigger.Enabled.Value = true;
						}
					}
				};

				respawner.Add(new Binding<bool>(respawner.Enabled, () => respawn && !trigger.Enabled, respawn, trigger.Enabled));

				result.Add("Respawner", respawner);
			}

			result.Add(new NotifyBinding(delegate()
			{
				if (!respawn && !trigger.Enabled)
					result.Delete.Execute();
			}, respawn));			

			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Vector3>(light.Color, model.Color));

			light.Attenuation.Value = 5.0f;

			model.Scale.Value = new Vector3(0.15f);

			model.Add(new Binding<Vector3, int>(model.Color, delegate(int x)
			{
				if (x < 10)
					return new Vector3(1.0f, 1.0f, 1.0f);
				if (x < 30)
					return new Vector3(0.5f, 0.5f, 1.5f);
				if (x < 50)
					return new Vector3(0.5f, 1.5f, 0.5f);
				if (x < 80)
					return new Vector3(1.5f, 0.5f, 0.5f);
				return new Vector3(1.5f, 0.5f, 1.5f);
			}, energy));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
			MapAttachable.AttachEditorComponents(result, main);
		}
	}
}
