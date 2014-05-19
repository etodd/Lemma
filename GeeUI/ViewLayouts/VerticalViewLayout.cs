using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
    public class VerticalViewLayout : ViewLayout
    {
        private int _paddingBetweenHorizontal;
        private int _paddingBetweenVertical;
        private bool _wrapAround;

        /// <summary>
        /// Creates a new HorizontalViewLayout with the specified padding
        /// </summary>
        /// <param name="paddingBetweenHorizontal">If wrapping around, this is the padding in between each layer of views.</param>
        /// <param name="wrapAround">Whether or not to wrap views if they extend past the parent's boundbox.</param>
        /// <param name="paddingBetweenVertical">The padding in between each View that is ordered</param>
        public VerticalViewLayout(int paddingBetweenVertical = 2, bool wrapAround = false, int paddingBetweenHorizontal = 2)
        {
            _paddingBetweenHorizontal = paddingBetweenHorizontal;
            _paddingBetweenVertical = paddingBetweenVertical;
            _wrapAround = wrapAround;
        }

        private void NoWrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int yDone = container.Top - parentView.Y;
            foreach (View v in parentView.Children)
            {
                v.Position = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;
                v.Position = new Vector2(container.Left, yDone);
                yDone += v.BoundBox.Height + _paddingBetweenVertical;
            }
        }

        private void Wrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xDone = container.Left - parentView.X;
            int yDone = container.Top - parentView.Y;
            View widestChild = null;
            foreach (View v in parentView.Children)
            {
                v.Position = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;

                if (widestChild == null || v.BoundBox.Width > widestChild.BoundBox.Width)
                    widestChild = v;

                //Wrapping around has never felt so good
                if (v.BoundBox.Bottom + yDone > container.Bottom - parentView.Y)
                {
                    yDone = container.Top - parentView.Y;
                    xDone += widestChild.BoundBox.Width + _paddingBetweenHorizontal;
                }

                v.Position = new Vector2(xDone, yDone);
                yDone += v.BoundBox.Height + _paddingBetweenVertical;
            }
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
