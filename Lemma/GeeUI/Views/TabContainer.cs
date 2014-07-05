using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeeUI.Views
{
	internal class TabContainer : View
	{
		public SpriteFont TabFont;

		public TabView ActiveTabView
		{
			get
			{
				return Children.Where(child => child is TabView).Cast<TabView>().FirstOrDefault(v => v.Selected);
			}
		}

		public int AllTabsWidth
		{
			get {
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
			host.TabClicked(Children.InternalList.IndexOf(child));
		}

		public TabView AddTab(string tabText, View tabChild)
		{
			var ret = new TabView(ParentGeeUI, this, new Vector2(AllTabsWidth, 0), TabFont) { TabText = tabText };
			ParentView.Value.Children.Add(tabChild);
			if (ActiveTabView == null)
				TabClicked(ret);
			else 
				TabClicked(ActiveTabView);
			return ret;
		}

		public TabContainer(GeeUIMain GeeUI, View rootView, SpriteFont font)
			: base(GeeUI, rootView)
		{
			Position.Value = Vector2.Zero;
			TabFont = font;
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
