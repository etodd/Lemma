using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using GeeUI;
using GeeUI.Views;
using Microsoft.Xna.Framework;

namespace Lemma.GeeUI.Composites
{
	internal class DropDownPanelView : PanelView
	{
		private DropDownView DropView;

		public DropDownPanelView(GeeUIMain GeeUI, DropDownView dropView)
			: base(GeeUI, GeeUI.RootView, Vector2.Zero)
		{
			DropView = dropView;
			this.Add(new NotifyBinding(() =>
			{
				if (!DropView.Attached && this.ParentView.Value != null)
					this.ParentView.Value.RemoveChild(this);
			}, DropView.Attached));
		}

	}
}
