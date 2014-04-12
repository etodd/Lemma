using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class Container : UIComponent
	{
		public Property<float> PaddingLeft = new Property<float> { Value = 4.0f, Editable = true };
		public Property<float> PaddingRight = new Property<float> { Value = 4.0f, Editable = true };
		public Property<float> PaddingBottom = new Property<float> { Value = 4.0f, Editable = true };
		public Property<float> PaddingTop = new Property<float> { Value = 4.0f, Editable = true };
		public Property<Color> Tint = new Property<Color> { Value = new Color(255, 255, 255, 255), Editable = true };
		public Property<float> Opacity = new Property<float> { Value = 1.0f, Editable = true };
		public Property<bool> ResizeHorizontal = new Property<bool> { Value = true, Editable = true };
		public Property<bool> ResizeVertical = new Property<bool> { Value = true, Editable = true };

		private static Texture2D texture;
		private NotifyBinding binding;

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			this.Add(new ListNotifyBinding<UIComponent>(this.childrenChanged, this.Children));
			this.Add(new NotifyBinding(this.childrenChanged, this.ResizeHorizontal, this.ResizeVertical, this.PaddingLeft, this.PaddingRight, this.PaddingBottom, this.PaddingTop));
			this.childrenChanged();
		}

		private void childrenChanged()
		{
			if (this.binding != null)
			{
				this.Remove(this.binding);
				this.binding = null;
			}
			this.layoutDirty = true;
		}

		protected override void updateLayout()
		{
			if (!this.ResizeHorizontal && !this.ResizeVertical)
				return;

			if (this.binding == null)
			{
				this.binding = new NotifyBinding(delegate() { this.layoutDirty = true; }, this.Children.SelectMany(x => new IProperty[] { x.ScaledSize, x.Visible }).ToArray());
				this.Add(this.binding);
			}

			Vector2 size = new Vector2(this.PaddingLeft, this.PaddingTop);
			foreach (UIComponent child in this.Children)
			{
				Vector2 childPos = child.Position;
				Vector2 min = childPos - (child.AnchorPoint.Value * child.Size.Value);
				childPos.X += Math.Max(0, this.PaddingLeft - min.X);
				childPos.Y += Math.Max(0, this.PaddingTop - min.Y);
				child.Position.Value = childPos;
				if (child.Visible)
				{
					Vector2 max = childPos + (child.InverseAnchorPoint.Value * child.Size.Value);
					size.X = Math.Max(size.X, max.X);
					size.Y = Math.Max(size.Y, max.Y);
				}
			}
			Vector2 originalSize = this.Size;
			size.X = this.ResizeHorizontal ? size.X + this.PaddingRight : originalSize.X;
			size.Y = this.ResizeVertical ? size.Y + this.PaddingBottom : originalSize.Y;
			this.Size.Value = size;
		}

		public override void LoadContent(bool reload)
		{
			base.LoadContent(reload);
			if (Container.texture == null || Container.texture.IsDisposed || Container.texture.GraphicsDevice != this.main.GraphicsDevice)
			{
				Container.texture = new Texture2D(this.main.GraphicsDevice, 1, 1);
				Container.texture.SetData(new[] { Color.White });
			}
		}

		protected override void draw(GameTime time, Matrix parent, Matrix transform)
		{
			if (this.Opacity > 0.0f)
			{
				Vector2 position = Vector2.Transform(this.Position, parent);
				float rotation = this.Rotation + (float)Math.Atan2(parent.M12, parent.M11);
				Vector2 scale = this.Scale;
				scale.X *= (float)Math.Sqrt((parent.M11 * parent.M11) + (parent.M12 * parent.M12));
				scale.Y *= (float)Math.Sqrt((parent.M21 * parent.M21) + (parent.M22 * parent.M22));

				Rectangle rect = this.Rectangle;
				rect.Width = (int)Math.Round((float)rect.Width * scale.X);
				rect.Height = (int)Math.Round((float)rect.Height * scale.Y);
				rect.Offset((int)Math.Round(position.X), (int)Math.Round(position.Y));

				this.renderer.Batch.Draw(Container.texture, rect, null, this.Tint.Value * this.Opacity.Value, rotation, this.AnchorPoint, SpriteEffects.None, this.DrawOrder);
			}
		}
	}
}
