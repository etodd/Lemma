﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeeUI.Views
{
	internal class TabContainer : View
	{
		public TabView ActiveTabView
		{
			get
			{
				for (int i = 0; i < this.Children.Count; i++)
				{
					TabView child = (TabView)this.Children[i];
					if (child.Selected)
						return child;
				}
				return null;
			}
		}

		public int AllTabsWidth
		{
			get
			{
				if (Children.Length == 0) return 0;
				View last = Children[Children.Length - 1];
				return (int)last.Position.Value.X + last.BoundBox.Width;
			}
		}

		public override Rectangle BoundBox
		{
			get
			{
				return ActiveTabView == null
						   ? new Rectangle(RealX, RealY, 0, 0)
						   : new Rectangle(RealX, RealY, Width, Height);
			}
		}

		internal void TabClicked(TabView child)
		{
			foreach (TabView tab in Children)
			{
				tab.Selected.Value = false;
			}
			child.Selected.Value = true;
			var host = (TabHost) ParentView;
			host.TabClicked(Children.IndexOf(child));
		}

		public TabView AddTab(string tabText, View tabChild)
		{
			var ret = new TabView(ParentGeeUI, this, new Vector2(AllTabsWidth, 0)) { TabText = tabText };
			ParentView.Value.Children.Add(tabChild);
			if (ActiveTabView == null)
				TabClicked(ret);
			else 
				TabClicked(ActiveTabView);
			return ret;
		}

		public TabContainer(GeeUIMain GeeUI, View rootView)
			: base(GeeUI, rootView)
		{
			Position.Value = Vector2.Zero;
		}

		private void setChildrenWidth()
		{
			var maxWidth = 0;
			foreach (View c in Children)
				if (c.Width > maxWidth) maxWidth = c.Width;
			foreach (var child in Children)
			{
				child.Width.Value = maxWidth;
			}
		}

		public override void Update(float dt)
		{
			//setChildrenWidth();
			Width = ParentView.Value.Width;
			Height.Value = 0;

			foreach(View v in Children)
			{
				if (v.BoundBox.Bottom > Height) Height.Value = v.BoundBox.Bottom;
			}
			base.Update(dt);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);
		}
	}
}
