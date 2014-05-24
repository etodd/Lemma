using System.Security;
using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
	public class VerticalViewLayout : ViewLayout
	{
		private int _paddingBetweenHorizontal;
		private int _paddingBetweenVertical;
		private bool _wrapAround;
		private bool _resizeParentToFit;

		private int _thinnestSeenParent = -1;

		/// <summary>
		/// Creates a new HorizontalViewLayout with the specified padding
		/// </summary>
		/// <param name="paddingBetweenHorizontal">If wrapping around, this is the padding in between each layer of views.</param>
		/// <param name="wrapAround">Whether or not to wrap views if they extend past the parent's boundbox.</param>
		/// <param name="paddingBetweenVertical">The padding in between each View that is ordered</param>
		/// /// <param name="resizeParentToFit">Expand horizontally to fit new rows if need be</param>
		public VerticalViewLayout(int paddingBetweenVertical = 2, bool wrapAround = true, int paddingBetweenHorizontal = 2, bool resizeParentToFit = false)
		{
			_paddingBetweenHorizontal = paddingBetweenHorizontal;
			_paddingBetweenVertical = paddingBetweenVertical;
			_wrapAround = wrapAround;
			_resizeParentToFit = resizeParentToFit;
		}

		private void NoWrap(View parentView)
		{
			Rectangle container = parentView.ContentBoundBox;
			int yDone = container.Top - parentView.RealY;
			foreach (View v in parentView.Children)
			{
				v.Position.Value = Vector2.Zero;
				if (ExcludedChildren.Contains(v)) continue;
				v.Position.Value = new Vector2(container.Left - parentView.RealX, yDone);
				yDone += v.BoundBox.Height + _paddingBetweenVertical;
			}
		}

		private void Wrap(View parentView)
		{
			Rectangle container = parentView.ContentBoundBox;
			int xDone = container.Left - parentView.RealX;
			int yDone = container.Top - parentView.RealY;
			View widestChild = null;
			bool nullify = false;
			int furthestRight = 0;
			foreach (View v in parentView.Children)
			{
				if (nullify)
				{
					widestChild = null; //this is per-column
					nullify = false;
				}
				v.Position.Value = Vector2.Zero;
				if (ExcludedChildren.Contains(v)) continue;

				if (widestChild == null || v.BoundBox.Width > widestChild.BoundBox.Width)
				{
					widestChild = v;
				}
				//Wrapping around has never felt so good
				if (v.BoundBox.Bottom + yDone > container.Bottom - parentView.Y)
				{
					yDone = container.Top - parentView.RealY;
					int addWidth = widestChild.BoundBox.Width + _paddingBetweenHorizontal;
					if (_resizeParentToFit)
					{
						int neededWidth = addWidth + xDone + v.BoundBox.Width;
						if (neededWidth > parentView.ContentBoundBox.Width)
						{
							int theWidth = (neededWidth - parentView.ContentBoundBox.Width);
							parentView.Width.Value += theWidth;
							widestChild = v;
						}
					}
					xDone += addWidth;
					nullify = true;
				}

				v.Position.Value = new Vector2(xDone, yDone);
				yDone += v.BoundBox.Height + _paddingBetweenVertical;
				if(widestChild == v)
					furthestRight = v.AbsoluteBoundBox.Right - parentView.AbsoluteContentBoundBox.Left;
			}

			if (_resizeParentToFit)
				parentView.Width.Value = furthestRight + 1;
		}

		public override void OrderChildren(View parentView)
		{
			if (_thinnestSeenParent == -1 || parentView.ContentBoundBox.Width < _thinnestSeenParent)
				_thinnestSeenParent = parentView.ContentBoundBox.Width;
			if (!_wrapAround)
				NoWrap(parentView);
			else
				Wrap(parentView);
		}
	}
}
