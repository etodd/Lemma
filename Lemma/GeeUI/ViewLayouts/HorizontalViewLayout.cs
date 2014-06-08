using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
    public class HorizontalViewLayout : ViewLayout
    {
        private int _paddingBetweenHorizontal;
        private int _paddingBetweenVertical;
        private bool _wrapAround;
		private bool _resizeParentToFit;

		private int _thinnestSeenParent = -1;

        /// <summary>
        /// Creates a new HorizontalViewLayout with the specified padding
        /// </summary>
        /// <param name="paddingBetweenHorizontal">The padding in between each View that is ordered</param>
        /// <param name="wrapAround">Whether or not to wrap views if they extend past the parent's boundbox.</param>
        /// <param name="paddingBetweenVertical">If wrapping around, this is the padding in between each layer of views.</param>
		/// /// <param name="resizeParentToFit">Expand vertically to fit new columns if need be.</param>
        public HorizontalViewLayout(int paddingBetweenHorizontal = 2, bool wrapAround = false, int paddingBetweenVertical = 2, bool resizeParentToFit = false)
        {
            _paddingBetweenHorizontal = paddingBetweenHorizontal;
            _paddingBetweenVertical = paddingBetweenVertical;
            _wrapAround = wrapAround;
	        _resizeParentToFit = resizeParentToFit;
        }

        private void NoWrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xDone = container.Left - parentView.RealX;
            foreach (View v in parentView.Children)
            {
                v.Position.Value = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;
                v.Position.Value = new Vector2(xDone, container.Top - parentView.RealY);
                xDone += v.BoundBox.Width + _paddingBetweenHorizontal;
            }
        }

        private void Wrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xDone = container.Left - parentView.RealX;
            int yDone = container.Top - parentView.RealY;
            View tallestChild = null;
			bool nullify = false;
			int furthestDown = 0;
            foreach (View v in parentView.Children)
            {
				if (nullify)
				{
					tallestChild = null; //this is per-column
					nullify = false;
				}
				v.Position.Value = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;

                if (tallestChild == null || v.BoundBox.Height > tallestChild.BoundBox.Height)
                    tallestChild = v;

                //Wrapping around has never felt so good
                if (v.BoundBox.Right + xDone > container.Right - parentView.X)
                {
                    xDone = container.Left - parentView.X;
	                int addHeight = tallestChild.BoundBox.Height + _paddingBetweenVertical;
	                if (_resizeParentToFit)
	                {
		                int neededHeight = addHeight + yDone + v.BoundBox.Height;
		                if (neededHeight > parentView.ContentBoundBox.Height)
		                {
			                int theHeight = (neededHeight - parentView.ContentBoundBox.Height);
							parentView.Height.Value += theHeight;
			                tallestChild = v;
		                }
	                }
                    yDone += tallestChild.BoundBox.Height + _paddingBetweenVertical;
	                nullify = true;
                }

				v.Position.Value = new Vector2(xDone, yDone);
                xDone += v.BoundBox.Width + _paddingBetweenHorizontal;
	            if (tallestChild == v)
		            furthestDown = v.AbsoluteBoundBox.Bottom - parentView.AbsoluteContentBoundBox.Top;
            }
	        if (_resizeParentToFit)
				parentView.Height.Value = furthestDown + 1;
        }

        public override void OrderChildren(View parentView)
        {
            if (!_wrapAround)
                NoWrap(parentView);
            else
                Wrap(parentView);
        }
    }
}
