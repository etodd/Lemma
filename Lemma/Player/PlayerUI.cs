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
		public static void Attach(Main main, UIRenderer ui, Property<float> health)
		{
			Sprite damageOverlay = new Sprite();
			damageOverlay.Image.Value = "Images\\damage";
			damageOverlay.AnchorPoint.Value = new Vector2(0.5f);
			ui.Root.Children.Add(damageOverlay);

			// Center the damage overlay and scale it to fit the screen
			damageOverlay.Add(new Binding<Vector2, Point>(damageOverlay.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			damageOverlay.Add(new Binding<Vector2>(damageOverlay.Scale, () => new Vector2(main.ScreenSize.Value.X / damageOverlay.Size.Value.X, main.ScreenSize.Value.Y / damageOverlay.Size.Value.Y), main.ScreenSize, damageOverlay.Size));
			damageOverlay.Add(new Binding<float, float>(damageOverlay.Opacity, x => 1.0f - x, health));

			Sprite reticle = new Sprite();
			reticle.Image.Value = "Images\\reticle";
			reticle.AnchorPoint.Value = new Vector2(0.5f);
			reticle.Opacity.Value = 0.5f;
			ui.Root.Children.Add(reticle);

			reticle.Add(new Binding<bool>(reticle.Visible, main.Settings.EnableReticle));

			// Center the reticle
			reticle.Add(new Binding<Vector2, Point>(reticle.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));

			UIComponent targets = new UIComponent();
			ui.Root.Children.Add(targets);
			const string targetOnScreen = "Images\\target";
			const string targetOffScreen = "Images\\target-pointer";
			ui.Add(new ListBinding<UIComponent, Transform>(targets.Children, TargetFactory.Positions, delegate(Transform target)
			{
				Sprite sprite = new Sprite();
				sprite.Image.Value = "Images\\target";
				sprite.Opacity.Value = 0.5f;
				sprite.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				sprite.Add(new Binding<bool>(sprite.Visible, target.Enabled));
				sprite.Add(new Binding<Vector2>(sprite.Position, delegate()
				{
					Vector3 pos = target.Position.Value;
					Vector4 projectionSpace = Vector4.Transform(new Vector4(pos.X, pos.Y, pos.Z, 1.0f), main.Camera.ViewProjection);
					float originalDepth = projectionSpace.Z;
					projectionSpace /= projectionSpace.W;

					Point screenSize = main.ScreenSize;
					Vector2 screenCenter = new Vector2(screenSize.X * 0.5f, screenSize.Y * 0.5f);

					Vector2 offset = new Vector2(projectionSpace.X * (float)screenSize.X * 0.5f, -projectionSpace.Y * (float)screenSize.Y * 0.5f);

					float radius = Math.Min(screenSize.X, screenSize.Y) * 0.95f * 0.5f;

					float offsetLength = offset.Length();

					Vector2 normalizedOffset = offset / offsetLength;

					bool offscreen = offsetLength > radius;

					bool behind = originalDepth < main.Camera.NearPlaneDistance;

					string img = offscreen || behind ? targetOffScreen : targetOnScreen;

					if (sprite.Image.Value != img)
						sprite.Image.Value = img;

					if (behind)
						normalizedOffset *= -1.0f;

					if (offscreen || behind)
						sprite.Rotation.Value = -(float)Math.Atan2(normalizedOffset.Y, -normalizedOffset.X) - (float)Math.PI * 0.5f;
					else
						sprite.Rotation.Value = 0.0f;

					if (behind || offscreen)
						offset = normalizedOffset * radius;

					return screenCenter + offset;
				}, target.Position, main.Camera.ViewProjection, main.ScreenSize));
				return sprite;
			}));
		}
	}
}
