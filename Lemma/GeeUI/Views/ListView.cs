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
			this.Add(new NotifyBinding(this.RecomputeOffset, this.ChildrenBoundBox, this.Width, this.Height));
		}


		private void RecomputeOffset()
		{
			this.ContentOffset.Value = new Vector2(0, this.ContentOffset.Value.Y);
			if (ChildrenBoundBox.Value.Height <= this.AbsoluteBoundBox.Height)
			{
				this.ContentOffset.Value = new Vector2(this.ContentOffset.Value.X, 0);
				return;
			}
			if (ChildrenBoundBox.Value.Bottom < AbsoluteBoundBox.Bottom)
			{
				this.ContentOffset.Value += new Vector2(0, ChildrenBoundBox.Value.Bottom - AbsoluteBoundBox.Bottom);
			}
			if (ChildrenBoundBox.Value.Top > AbsoluteBoundBox.Top)
				this.ContentOffset.Value = new Vector2(this.ContentOffset.Value.X, 0);
		}

		public override void OnMScroll(Vector2 position, int scrollDelta, bool fromChild = false)
		{
			this.ContentOffset.Value -= new Vector2(0, scrollDelta * ScrollMultiplier);
			RecomputeOffset();
			base.OnMScroll(position, scrollDelta, fromChild);
		}

		public override void OnMClick(Vector2 position, bool fromChild = false)
		{
			base.OnMClick(position);
		}

		public override void OnMClickAway(bool fromChild = false)
		{
			base.OnMClickAway();
		}

		public override void OnMOver(bool fromChild = false)
		{
			base.OnMOver();
		}
		public override void OnMOff(bool fromChild = false)
		{
			base.OnMOff();
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			if (ContainerNinePatch != null)
				ContainerNinePatch.Draw(spriteBatch, AbsolutePosition, Width, Height, 0f, EffectiveOpacity);
			base.Draw(spriteBatch);
		}
	}
}
