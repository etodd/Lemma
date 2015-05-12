using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using ComponentBind;

namespace Lemma.Factories
{
	public class PlayerUI
	{
		public static void Attach(Main main, Entity entity, UIRenderer ui, Property<float> health, Property<float> rotation, Property<bool> noteActive, Property<bool> phoneActive)
		{
			Sprite damageOverlay = new Sprite();
			damageOverlay.Image.Value = "Images\\damage";
			damageOverlay.AnchorPoint.Value = new Vector2(0.5f);
			ui.Root.Children.Add(damageOverlay);

			// Center the damage overlay and scale it to fit the screen
			damageOverlay.Add(new Binding<Vector2, Point>(damageOverlay.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			damageOverlay.Add(new Binding<Vector2>(damageOverlay.Scale, () => new Vector2(main.ScreenSize.Value.X / damageOverlay.Size.Value.X, main.ScreenSize.Value.Y / damageOverlay.Size.Value.Y), main.ScreenSize, damageOverlay.Size));
			damageOverlay.Add(new Binding<float, float>(damageOverlay.Opacity, x => 1.0f - x, health));

#if VR
			if (main.VR)
			{
				VirtualReticle reticleController = entity.GetOrCreate<VirtualReticle>();
				reticleController.Add(new Binding<float>(reticleController.Rotation, rotation));

				ModelNonPostProcessed reticle = entity.Create<ModelNonPostProcessed>();
				reticle.Filename.Value = "Models\\plane";
				reticle.EffectFile.Value = "Effects\\VirtualUI";
				reticle.DiffuseTexture.Value = "Images\\reticle";
				reticle.Add(new Binding<Matrix>(reticle.Transform, reticleController.Transform));
				reticle.Add(new Binding<bool>(reticle.Enabled, () => !main.Paused && !phoneActive && !noteActive && main.Settings.EnableReticleVR, main.Paused, phoneActive, noteActive, main.Settings.EnableReticleVR));
			}
			else
#endif
			{
				Sprite reticle = new Sprite();
				reticle.Image.Value = "Images\\reticle";
				reticle.AnchorPoint.Value = new Vector2(0.5f);
				reticle.Opacity.Value = 0.5f;
				ui.Root.Children.Add(reticle);

				reticle.Add(new Binding<bool>(reticle.Visible, main.Settings.EnableReticle));

				// Center the reticle
				reticle.Add(new Binding<Vector2, Point>(reticle.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			}

			UIComponent targets = new UIComponent();
			ui.Root.Children.Add(targets);

			TargetUI targetUi = entity.GetOrCreate<TargetUI>();
			targetUi.Add(new ListBinding<UIComponent>(targetUi.Sprites, targets.Children));

			targets.Add(new ListBinding<UIComponent, Transform>(targets.Children, TargetFactory.Positions, delegate(Transform target)
			{
				Sprite sprite = new Sprite();
				sprite.Image.Value = "Images\\target";
				sprite.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				sprite.UserData.Value = target;
				sprite.Add(new Binding<bool>(sprite.Visible, () => target.Enabled && main.Settings.EnableWaypoints, target.Enabled, main.Settings.EnableWaypoints));
				return sprite;
			}));
		}
	}
}