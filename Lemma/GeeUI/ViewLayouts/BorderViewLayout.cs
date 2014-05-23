using System.Collections.Generic;
using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace GeeUI.ViewLayouts
{
    public class BorderViewLayout : ViewLayout
    {
        List<View> childrenLayout = new List<View>();
        private int topBottomHeight, rightLeftWidth;

        /// <summary>
        /// Creates a new BorderViewLayout
        /// </summary>
        public BorderViewLayout(View top, View bottom, View left, View right, View middle, int topBottomHeight, int rightLeftWidth)
        {
            childrenLayout.Add(top);
            childrenLayout.Add(bottom);
            childrenLayout.Add(left);
            childrenLayout.Add(right);
            childrenLayout.Add(middle);
            this.topBottomHeight = topBottomHeight;
            this.rightLeftWidth = rightLeftWidth;
        }

        public override void OrderChildren(View parentView)
        {
            Rectangle container = parentView.ContentBoundBox;
            int xStart = container.Left - parentView.X;
            int yStart = container.Top - parentView.Y;
            int xEnd = container.Right - parentView.X;
            int yEnd = container.Bottom - parentView.Y;
            int height = yEnd - yStart;
            int width = xEnd - xStart;

            var top = childrenLayout[0];
            var bottom = childrenLayout[1];
            var left = childrenLayout[2];
            var right = childrenLayout[3];
            var middle = childrenLayout[4];

            top.X = xStart;
            top.Y = yStart;
            top.Width = width;
            top.Height = topBottomHeight;

            left.X = xStart;
            left.Y = yStart + topBottomHeight;
            left.Width = rightLeftWidth;
            left.Height = height - (topBottomHeight*2);

            bottom.X = xStart;
            bottom.Y = height - topBottomHeight;
            bottom.Width = width;
            bottom.Height = topBottomHeight;

            right.X = xEnd - rightLeftWidth;
            right.Y = yStart + topBottomHeight;
            right.Width = rightLeftWidth;
            right.Height = height - (topBottomHeight*2);

            middle.X = xStart + rightLeftWidth;
            middle.Y = yStart + topBottomHeight;
            middle.Width = width - (rightLeftWidth*2);
            middle.Height = height - (topBottomHeight*2);

        }
    }
}
