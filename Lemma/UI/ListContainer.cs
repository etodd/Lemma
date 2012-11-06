using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class ListContainer : UIComponent
	{
		public enum ListOrientation
		{
			Vertical, Horizontal
		}

		public enum ListAlignment
		{
			Min, Middle, Max
		}

		public Property<ListOrientation> Orientation = new Property<ListOrientation> { Value = ListOrientation.Vertical };
		public Property<ListAlignment> Alignment = new Property<ListAlignment> { Value = ListAlignment.Min };
		public Property<bool> ResizePerpendicular = new Property<bool> { Value = true };
		public Property<bool> Reversed = new Property<bool> { Value = false };
		public Property<float> Spacing = new Property<float> { Value = 2.0f };
		private NotifyBinding binding;

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			this.Add(new ListNotifyBinding<UIComponent>(this.childrenChanged, this.Children));
			this.Add(new NotifyBinding(delegate() { this.layoutDirty = true; }, this.Orientation, this.Spacing, this.ResizePerpendicular));
			this.childrenChanged();
		}

		private void childrenChanged()
		{
			if (this.binding != null)
				this.Remove(this.binding);
			this.binding = null;
			this.layoutDirty = true;
		}

		protected override void updateLayout()
		{
			if (this.binding == null)
			{
				this.binding = new NotifyBinding(delegate() { this.layoutDirty = true; }, this.Children.SelectMany(x => new IProperty[] { x.ScaledSize, x.Visible }).ToArray());
				this.Add(this.binding);
			}

			float spacing = this.Spacing;

			Vector2 maxSize = Vector2.Zero;
			if (this.ResizePerpendicular)
			{
				foreach (UIComponent child in this.Children)
				{
					if (child.Visible)
					{
						Vector2 size = child.ScaledSize;
						maxSize.X = Math.Max(maxSize.X, size.X);
						maxSize.Y = Math.Max(maxSize.Y, size.Y);
					}
				}
			}
			else
				maxSize = this.Size;

			if (this.Orientation.Value == ListOrientation.Horizontal)
			{
				Vector2 anchorPoint;
				switch (this.Alignment.Value)
				{
					case ListAlignment.Min:
						anchorPoint = Vector2.Zero;
						break;
					case ListAlignment.Middle:
						anchorPoint = new Vector2(0, 0.5f);
						break;
					case ListAlignment.Max:
						anchorPoint = new Vector2(0, 1);
						break;
					default:
						anchorPoint = Vector2.Zero;
						break;
				}
				Vector2 pos = new Vector2(0, maxSize.Y * anchorPoint.Y);
				foreach (UIComponent child in this.Reversed ? this.Children.Reverse() : this.Children)
				{
					child.AnchorPoint.Value = anchorPoint;
					child.Position.Value = pos;
					if (child.Visible)
						pos.X += child.ScaledSize.Value.X + spacing;
				}
				this.Size.Value = new Vector2(pos.X - spacing, maxSize.Y);
			}
			else
			{
				Vector2 anchorPoint;
				switch (this.Alignment.Value)
				{
					case ListAlignment.Min:
						anchorPoint = Vector2.Zero;
						break;
					case ListAlignment.Middle:
						anchorPoint = new Vector2(0.5f, 0);
						break;
					case ListAlignment.Max:
						anchorPoint = new Vector2(1, 0);
						break;
					default:
						anchorPoint = Vector2.Zero;
						break;
				}
				Vector2 pos = new Vector2(maxSize.X * anchorPoint.X, 0);
				foreach (UIComponent child in this.Reversed ? this.Children.Reverse() : this.Children)
				{
					child.AnchorPoint.Value = anchorPoint;
					child.Position.Value = pos;
					if (child.Visible)
						pos.Y += child.ScaledSize.Value.Y + spacing;
				}
				this.Size.Value = new Vector2(maxSize.X, pos.Y - spacing);
			}
		}
	}
}
