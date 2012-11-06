using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class PistolFactory : Factory
	{
		private const int maxAmmo = 10;

		public PistolFactory()
		{
			
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Pistol");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PhysicsBlock physics = new PhysicsBlock();
			physics.Size.Value = new Vector3(0.5f, 0.4f, 0.15f);
			physics.Mass.Value = 0.05f;
			result.Add("Physics", physics);

			AnimatedModel model = new AnimatedModel();
			result.Add("Model", model);
			model.Filename.Value = "Models\\P220";
			model.Editable = false;

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
			light.Attenuation.Value = 6.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			result.Add("Attached", new Property<bool> { Value = false, Editable = false });
			result.Add("Active", new Property<bool> { Value = false, Editable = false });
			result.Add("Ammo", new Property<int> { Value = 0, Editable = true });
			result.Add("Magazines", new Property<int> { Value = 0, Editable = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			AnimatedModel model = result.Get<AnimatedModel>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			PointLight highlighter = result.Get<PointLight>();
			Property<bool> attached = result.GetProperty<bool>("Attached");
			Property<bool> active = result.GetProperty<bool>("Active");
			Property<int> ammo = result.GetProperty<int>("Ammo");
			Property<int> mags = result.GetProperty<int>("Magazines");

			Sound fire = new Sound();
			fire.Cue.Value = "Pistol";
			fire.Is3D.Value = true;
			fire.Add(new Binding<Vector3>(fire.Position, transform.Position));
			fire.Serialize = false;
			result.Add("FireSound", fire);
			Sound reload = new Sound();
			reload.Cue.Value = "Reload";
			reload.Is3D.Value = true;
			reload.Add(new Binding<Vector3>(reload.Position, transform.Position));
			reload.Serialize = false;
			result.Add("ReloadSound", reload);
			Sound reloadWithChamberedRound = new Sound();
			reloadWithChamberedRound.Cue.Value = "Reload With Chambered Round";
			reloadWithChamberedRound.Is3D.Value = true;
			reloadWithChamberedRound.Add(new Binding<Vector3>(reloadWithChamberedRound.Position, transform.Position));
			reloadWithChamberedRound.Serialize = false;
			result.Add("ReloadWithChamberedRoundSound", reloadWithChamberedRound);
			Sound drawSound = new Sound();
			drawSound.Cue.Value = "Pistol Draw";
			drawSound.Is3D.Value = true;
			drawSound.Add(new Binding<Vector3>(drawSound.Position, transform.Position));
			drawSound.Serialize = false;
			result.Add("DrawSound", drawSound);

			UIRenderer ui = new UIRenderer();
			result.Add("UI", ui);

			ui.Add(new Binding<bool>(ui.Enabled, active));

			ListContainer layout = new ListContainer();
			layout.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			layout.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
			layout.Add(new Binding<Vector2, Point>(layout.Position, x => new Vector2(x.X - 30.0f, 30.0f), main.ScreenSize));
			ui.Root.Children.Add(layout);

			ListContainer ammoList = new ListContainer();
			ammoList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			ammoList.Spacing.Value = 1.0f;
			layout.Children.Add(ammoList);

			Sprite magIcon = new Sprite();
			magIcon.Image.Value = "Images\\ui-mag";
			layout.Children.Add(magIcon);

			TextElement magLabel = new TextElement();
			magLabel.FontFile.Value = "Font";
			layout.Children.Add(magLabel);
			magLabel.Add(new Binding<string, int>(magLabel.Text, x => "x" + x.ToString(), mags));

			result.Add(new NotifyBinding(delegate()
			{
				if (active)
					drawSound.Play.Execute();
			}, active));

			Action refreshAmmoIcons = delegate()
			{
				while (ammoList.Children.Count > ammo)
					ammoList.Children[0].Delete.Execute();
				while (ammoList.Children.Count < ammo)
				{
					Sprite bulletIcon = new Sprite();
					bulletIcon.Image.Value = "Images\\ui-bullet";
					ammoList.Children.Add(bulletIcon);
				}
			};
			result.Add(new NotifyBinding(refreshAmmoIcons, ammo));
			refreshAmmoIcons();

			highlighter.Add(new Binding<Vector3, Matrix>(highlighter.Position, x => Vector3.Transform(new Vector3(-0.25f, 0.125f, 0.0f), transform.Matrix), transform.Matrix));
			highlighter.Add(new Binding<bool>(highlighter.Enabled, x => !x, attached));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Add(new Binding<bool>(model.Enabled, () => (!attached) || active, attached, active));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new Binding<bool>(trigger.Enabled, x => !x, attached));
			physics.Add(new Binding<bool>(physics.Enabled, x => !x, attached));

			TwoWayBinding<Matrix> physicsBinding = new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform);
			physics.Add(physicsBinding);

			Binding<Matrix> attachBinding = null;

			Command<Property<Matrix>> attach = new Command<Property<Matrix>>
			{
				Action = delegate(Property<Matrix> parent)
				{
					attached.Value = true;

					Matrix rotation = Matrix.CreateRotationX((float)Math.PI * 0.5f);

					attachBinding = new Binding<Matrix>(transform.Matrix, x => rotation * parent, parent);
					result.Add(attachBinding);

					physicsBinding.Enabled = false;
				}
			};
			result.Add("Attach", attach);

			Vector3 muzzleOffset = new Vector3(-0.25f, 0.125f, 0.0f);

			result.Add("Fire", new Command
			{
				Action = delegate()
				{
					if (!active || ammo < 1 || model.IsPlaying("Reload") || model.IsPlaying("ReloadWithChamberedRound"))
						return;

					ammo.Value--;

					if (ammo == 0)
					{
						model.StartClip("DryFire", 1, false, 0.0f);
						model.StartClip("Dry", 0, true, 0.0f);
					}
					else
						model.StartClip("Fire", 0, false, 0.0f);

					PointLight light = new PointLight();
					light.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
					light.Attenuation.Value = 6.0f;
					light.Shadowed.Value = false;
					light.Add(new Binding<Vector3, Matrix>(light.Position, x => Vector3.Transform(muzzleOffset, transform.Matrix), transform.Matrix));
					result.Add(light);
					result.Add(new Animation
					(
						new Animation.FloatMoveTo(light.Attenuation, 0.0f, 0.1f),
						new Animation.Execute(delegate() { light.Delete.Execute(); })
					));

					fire.Play.Execute();

					Map.GlobalRaycastResult hit = Map.GlobalRaycast(Vector3.Transform(new Vector3(-0.15f, 0.17f, 0.0f), transform.Matrix), Vector3.TransformNormal(Vector3.Left, transform.Matrix), main.Camera.FarPlaneDistance);
					if (hit.Map != null)
					{
						PointLight hitLight = new PointLight();
						hitLight.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
						hitLight.Attenuation.Value = 6.0f;
						hitLight.Shadowed.Value = false;
						hitLight.Position.Value = hit.Position + hit.Map.GetAbsoluteVector(hit.Normal.GetVector());
						result.Add(hitLight);
						result.Add(new Animation
						(
							new Animation.FloatMoveTo(hitLight.Attenuation, 0.0f, 0.1f),
							new Animation.Execute(delegate() { hitLight.Delete.Execute(); })
						));

						if (hit.Map.Empty(hit.Coordinate.Value))
							hit.Map.Regenerate();
					}
				}
			});

			result.Add("Reload", new Command
			{
				Action = delegate()
				{
					if (!active || mags < 1)
						return;

					model.Stop("Dry");
					model.StartClip(ammo == 0 ? "Reload" : "ReloadWithChamberedRound", 0, false, 0.0f);

					if (ammo == 0)
						reload.Play.Execute();
					else
						reloadWithChamberedRound.Play.Execute();

					mags.Value--;

					ammo.Value = PistolFactory.maxAmmo;
				}
			});

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				Property<Entity.Handle> pistolProperty = player.GetProperty<Entity.Handle>("Pistol");
				if (pistolProperty.Value.Target != null)
				{
					// Player already has a pistol.
					pistolProperty.Value.Target.GetProperty<int>("Magazines").Value += mags;
					Sound.PlayCue(main, "Pick Up Mag", transform.Position);
					result.Delete.Execute();
				}
				else
				{
					pistolProperty.Value = result;
					active.Value = true;
				}
			}));

			result.Add("Detach", new Command
			{
				Action = delegate()
				{
					active.Value = false;
					attached.Value = false;

					physicsBinding.Enabled = true;

					result.Remove(attachBinding);
					attachBinding = null;
				}
			});

			this.SetMain(result, main);

			if (ammo == 0)
				model.StartClip("Dry", 0, true, 0.0f);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
