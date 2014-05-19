using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
    public class HorizontalViewLayout : ViewLayout
    {
        private int _paddingBetweenHorizontal;
        private int _paddingBetweenVertical;
        private bool _wrapAround;

        /// <summary>
        /// Creates a new HorizontalViewLayout with the specified padding
        /// </summary>
        /// <param name="paddingBetweenHorizontal">The padding in between each View that is ordered</param>
        /// <param name="wrapAround">Whether or not to wrap views if they extend past the parent's boundbox.</param>
        /// <param name="paddingBetweenVertical">If wrapping around, this is the padding in between each layer of views.</param>
        public HorizontalViewLayout(int paddingBetweenHorizontal = 2, bool wrapAround = false, int paddingBetweenVertical = 2)
        {
            _paddingBetweenHorizontal = paddingBetweenHorizontal;
            _paddingBetweenVertical = paddingBetweenVertical;
            _wrapAround = wrapAround;
        }

        private void NoWrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xDone = container.Left - parentView.X;
            foreach (View v in parentView.Children)
            {
                v.Position = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;
                v.Position = new Vector2(xDone, container.Top);
                xDone += v.BoundBox.Width + _paddingBetweenHorizontal;
            }
        }

        private void Wrap(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xDone = container.Left - parentView.X;
            int yDone = container.Top - parentView.Y;
            View tallestChild = null;
            foreach (View v in parentView.Children)
            {
                v.Position = Vector2.Zero;
                if (ExcludedChildren.Contains(v)) continue;

                if (tallestChild == null || v.BoundBox.Height > tallestChild.BoundBox.Height)
                    tallestChild = v;

                //Wrapping around has never felt so good
                if (v.BoundBox.Right + xDone > container.Right - parentView.X)
                {
                    xDone = container.Left - parentView.X;
                    yDone += tallestChild.BoundBox.Height + _paddingBetweenVertical;
                }

                v.Position = new Vector2(xDone, yDone);
                xDone += v.BoundBox.Width + _paddingBetweenHorizontal;
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
