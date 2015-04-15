using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class TargetUI : Component<Main>, IUpdateableComponent
	{
		public ListProperty<UIComponent> Sprites = new ListProperty<UIComponent>();
		public void Update(float dt)
		{
			const string targetOnScreen = "Images\\target";
			const string targetOffScreen = "Images\\target-pointer";
			foreach (Sprite sprite in this.Sprites)
			{
				if (sprite.Visible)
				{
					Transform target = (Transform)sprite.UserData.Value;
					Vector3 pos = target.Position.Value;
					Matrix viewProj;
#if VR
					if (this.main.VR)
						viewProj = this.main.VRLastViewProjection;
					else
#endif
						viewProj = this.main.Camera.ViewProjection;

					Vector4 projectionSpace = Vector4.Transform(new Vector4(pos.X, pos.Y, pos.Z, 1.0f), viewProj);
					float originalDepth = projectionSpace.Z;
					projectionSpace /= projectionSpace.W;

					Point screenSize = main.ScreenSize;
					Vector2 screenCenter = new Vector2(screenSize.X * 0.5f, screenSize.Y * 0.5f);

					Vector2 offset = new Vector2(projectionSpace.X * (float)screenSize.X * 0.5f, -projectionSpace.Y * (float)screenSize.Y * 0.5f);

					float radius;
#if VR
					if (this.main.VR)
						radius = Math.Min(screenSize.X, screenSize.Y) * 0.5f * 0.5f;
					else
#endif
						radius = Math.Min(screenSize.X, screenSize.Y) * 0.95f * 0.5f;

					float offsetLength = offset.Length();

					Vector2 normalizedOffset = offset / offsetLength;

					bool offscreen = offsetLength > radius;

					bool behind = originalDepth < main.Camera.NearPlaneDistance;
					{
						string img = offscreen || behind ? targetOffScreen : targetOnScreen;

						if (sprite.Image.Value != img)
							sprite.Image.Value = img;
					}

					if (behind)
						normalizedOffset *= -1.0f;

					if (offscreen || behind)
						sprite.Rotation.Value = -(float)Math.Atan2(normalizedOffset.Y, -normalizedOffset.X) - (float)Math.PI * 0.5f;
					else
						sprite.Rotation.Value = 0.0f;

					if (behind || offscreen)
						offset = normalizedOffset * radius;

					sprite.Position.Value = screenCenter + offset;
				}
			}
		}
	}
}