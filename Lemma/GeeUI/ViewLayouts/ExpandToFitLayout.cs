using GeeUI.Views;
using System.Collections.Generic;
namespace GeeUI.ViewLayouts
{
	public class ExpandToFitLayout : ViewLayout
	{
		public bool ExpandVertical = true;
		public bool ExpandHorizontal = true;

		public int ExtraWidth = 0;
		public int ExtraHeight = 0;

		public ExpandToFitLayout()
			: this(true, true)
		{

		}

		public ExpandToFitLayout(bool vertical, bool horizontal)
		{
			this.ExpandHorizontal = horizontal;
			this.ExpandVertical = vertical;
		}

		public override void OrderChildren(View parentView)
		{
			int leftX = 0, rightX = 0, topY = 0, bottomY = 0;
			int parentLeft = parentView.AbsoluteBoundBox.Left;
			int parentTop = parentView.AbsoluteBoundBox.Top;

			foreach (var child in parentView.Children)
			{
				if (child.AbsoluteBoundBox.Left < leftX || leftX == 0) leftX = child.AbsoluteBoundBox.Left;
				if (child.AbsoluteBoundBox.Top < topY || topY == 0) topY = child.AbsoluteBoundBox.Top;
				if (child.AbsoluteBoundBox.Bottom > bottomY || bottomY == 0) bottomY = child.AbsoluteBoundBox.Bottom;
				if (child.AbsoluteBoundBox.Right > rightX || rightX == 0) rightX = child.AbsoluteBoundBox.Right;
			}
			if (ExpandHorizontal)
				parentView.Width.Value = rightX - parentLeft;
			if (ExpandVertical)
				parentView.Height.Value = bottomY - parentTop;
		}
	}
}
