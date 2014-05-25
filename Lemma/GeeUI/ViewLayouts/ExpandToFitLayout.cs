using GeeUI.Views;
using System.Collections.Generic;
namespace GeeUI.ViewLayouts
{
    public class ExpandToFitLayout : ViewLayout
    {
		public ExpandToFitLayout()
        {

        }

        public override void OrderChildren(View parentView)
        {
	        int leftX = 0, rightX = 0, topY = 0, bottomY = 0;
	        foreach (var child in parentView.Children)
	        {
		        if (child.AbsoluteBoundBox.Left < leftX || leftX == 0) leftX = child.AbsoluteBoundBox.Left;
				if (child.AbsoluteBoundBox.Top < topY || topY == 0) topY = child.AbsoluteBoundBox.Top;
				if (child.AbsoluteBoundBox.Bottom > bottomY || bottomY == 0) bottomY = child.AbsoluteBoundBox.Bottom;
				if (child.AbsoluteBoundBox.Right > rightX || rightX == 0) rightX = child.AbsoluteBoundBox.Right;
	        }
	        parentView.Width.Value = (rightX - leftX);
			parentView.Height.Value = (bottomY - topY);
        }
    }
}
