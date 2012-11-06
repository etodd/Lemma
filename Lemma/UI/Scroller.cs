using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Components
{
	public class Scroller : UIComponent
	{
		public Property<float> ScrollAmount = new Property<float> { Editable = false, Value = 30.0f };

		private NotifyBinding binding = null;

		public Scroller()
		{
			this.EnableScissor.Value = true;
		}
		
		public override void InitializeProperties()
		{
			base.InitializeProperties();
			new CommandBinding<Point, int>(this.MouseScrolled, delegate(Point mouse, int delta)
			{
				if (this.Children.Count == 1)
				{
					UIComponent child = this.Children.First();
					bool horizontalScroll = this.main.KeyboardState.Value.IsKeyDown(Keys.LeftShift);
					Vector2 newPosition = child.Position + (horizontalScroll ? new Vector2(delta * this.ScrollAmount, 0) : new Vector2(0, delta * this.ScrollAmount));

					newPosition.X = Math.Max(newPosition.X, this.Size.Value.X - child.ScaledSize.Value.X);
					newPosition.X = Math.Min(newPosition.X, 0);

					newPosition.Y = Math.Max(newPosition.Y, this.Size.Value.Y - child.ScaledSize.Value.Y);
					newPosition.Y = Math.Min(newPosition.Y, 0);
					child.Position.Value = newPosition;
				}
			});
			this.Add(new ListNotifyBinding<UIComponent>(this.childrenChanged, this.Children));
			this.childrenChanged();
		}

		private void childrenChanged()
		{
			this.layoutDirty = true;
			if (this.binding != null)
				this.Remove(this.binding);
			this.binding = null;
		}

		protected override void updateLayout()
		{
			if (this.binding == null)
			{
				if (this.Children.Count == 1)
				{
					UIComponent child = this.Children.First();
					this.binding = new NotifyBinding(delegate() { this.layoutDirty = true; }, this.Size, child.ScaledSize);
					this.Add(this.binding);
				}
			}

			if (this.Children.Count == 1)
			{
				UIComponent child = this.Children.First();
				Vector2 newPosition = child.Position;

				newPosition.X = Math.Max(newPosition.X, this.Size.Value.X - child.ScaledSize.Value.X);
				newPosition.X = Math.Min(newPosition.X, 0);

				newPosition.Y = Math.Max(newPosition.Y, this.Size.Value.Y - child.ScaledSize.Value.Y);
				newPosition.Y = Math.Min(newPosition.Y, 0);
				child.Position.Value = newPosition;
			}
		}

		public void ScrollToBottom()
		{
			if (this.Children.Count == 1)
			{
				UIComponent child = this.Children.First();

				Vector2 newPosition = child.Position;
				newPosition.Y = Math.Min(this.Size.Value.Y - child.ScaledSize.Value.Y, 0);
				child.Position.Value = newPosition;
			}
		}

		public void ScrollToTop()
		{
			if (this.Children.Count == 1)
			{
				UIComponent child = this.Children.First();

				Vector2 newPosition = child.Position;
				newPosition.Y = Math.Max(this.Size.Value.Y - child.ScaledSize.Value.Y, 0);
				child.Position.Value = newPosition;
			}
		}
	}
}
