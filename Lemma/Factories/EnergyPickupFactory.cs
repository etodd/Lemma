using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
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
			Model model = result.GetOrCreate<Model>();
			model.Editable = false;
			model.Serialize = false;
			model.Filename.Value = "Models\\sphere";
			PointLight light = result.GetOrCreate<PointLight>();
			light.Serialize = false;
			light.Editable = false;
			light.Shadowed.Value = false;
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

			model.Add(new Binding<Vector3, int>(model.Scale, delegate(int x)
			{
				return new Vector3(MathHelper.Lerp(0.1f, 1.2f, Math.Max(0, ((float)x - 3.0f) / 80.0f)));
			}, energy));

			light.Add(new Binding<float, int>(light.Attenuation, delegate(int x)
			{
				return MathHelper.Lerp(5.0f, 20.0f, Math.Max(0, ((float)x - 3.0f) / 80.0f));
			}, energy));

			Property<float> gameTime = Factory.Get<PlayerDataFactory>().Instance(main).GetProperty<float>("GameTime");

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				Animation animation;
				if (energy <= 30)
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
							trigger.Enabled.Value = true;
					}
				};

				respawner.Add(new Binding<bool>(respawner.Enabled, () => respawn && !trigger.Enabled, respawn, trigger.Enabled));

				result.Add("Respawner", respawner);

				light.Add(new Binding<bool>(light.Enabled, trigger.Enabled));
				model.Add(new Binding<bool>(model.Enabled, trigger.Enabled));
			}

			result.Add(new NotifyBinding(delegate()
			{
				if (!respawn && !trigger.Enabled)
					result.Delete.Execute();
			}, respawn));			

			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Vector3>(light.Color, model.Color));

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
