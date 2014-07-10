using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GeeUI.Structs;
using GeeUI.Managers;
using System;
using ComponentBind;

namespace GeeUI.Views
{
	/// <summary>
	/// An empty class used as a template. I guess.
	/// </summary>
	public class ListView : View
	{

		public NinePatch ContainerNinePatch = null;

		public int ScrollMultiplier = 10;

		public Property<Rectangle> ChildrenBoundBox = new Property<Rectangle>();

		public override void OrderChildren()
		{
			base.OrderChildren();
			if (Children.Length == 0)
				this.ChildrenBoundBox.Value = new Rectangle(RealX, RealY, 0, 0);
			Point max = new Point(int.MinValue, int.MinValue);
			Point min = new Point(int.MaxValue, int.MaxValue);
			foreach (View child in this.Children)
			{
				max.X = Math.Max(max.X, child.X + child.Width);
				max.Y = Math.Max(max.Y, child.Y + child.Height);
				min.X = Math.Min(min.X, child.X);
				min.Y = Math.Min(min.Y, child.Y);
			}
			this.ChildrenBoundBox.Value = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
		}

		public ListView(GeeUIMain GeeUI, View rootView)
			: base(GeeUI, rootView)
		{
			this.Add(new NotifyBinding(delegate()
			{
				this.recomputeOffset(0);
			}, this.ChildrenBoundBox, this.Width, this.Height));
		}

		private void recomputeOffset(int scroll)
		{
			int offsetY = (int)this.ContentOffset.Value.Y + scroll;
			Rectangle childBoundBox = ChildrenBoundBox;
			if (childBoundBox.Height <= this.Height)
				offsetY = 0;
			else
			{
				offsetY = Math.Max(offsetY, 0);
				offsetY = Math.Min(offsetY, childBoundBox.Height - this.Height);
			}

			Vector2 target = new Vector2(0, offsetY);
			if (this.ContentOffset != target)
				this.ContentOffset.Value = target;
		}

		public override void OnMScroll(Vector2 position, int scrollDelta, bool fromChild)
		{
			this.recomputeOffset(scrollDelta * -ScrollMultiplier);
			base.OnMScroll(position, scrollDelta, fromChild);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			if (ContainerNinePatch != null)
				ContainerNinePatch.Draw(spriteBatch, AbsolutePosition, Width, Height, 0f, EffectiveOpacity);
			base.Draw(spriteBatch);
		}
	}
}
