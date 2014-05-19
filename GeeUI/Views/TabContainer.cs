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
                return (int)last.Position.X + last.BoundBox.Width;
            }
        }

        public override Rectangle BoundBox
        {
            get
            {
                return ActiveTabView == null
                           ? new Rectangle(X, Y, 0, 0)
                           : new Rectangle(X, Y, Width, Height);
            }
        }

        internal void TabClicked(TabView child)
        {
            foreach (TabView tab in Children)
            {
                tab.Selected = false;
            }
            child.Selected = true;
            var host = (TabHost) ParentView;
            host.TabClicked(_children.IndexOf(child));
        }

        public TabView AddTab(string tabText, View tabChild)
        {
            var ret = new TabView(this, new Vector2(AllTabsWidth, 0), TabFont) { TabText = tabText };
            ParentView.AddChild(tabChild);
            if (ActiveTabView == null)
                TabClicked(ret);
            else 
                TabClicked(ActiveTabView);
            return ret;
        }

        public TabContainer(View rootView,  SpriteFont font)
            : base(rootView)
        {
            Position = Vector2.Zero;
            TabFont = font;
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            //base.OnMClickAway();
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
                child.Width = maxWidth;
            }
        }

        public override void Update(GameTime theTime)
        {
            //setChildrenWidth();
            Height = 10000;
            Width = ParentView.Width;
            OrderChildren(ChildrenLayout);
            Height = 0;

            foreach(View v in Children)
            {
                if (v.BoundBox.Bottom > Height) Height = v.BoundBox.Bottom;
            }
            base.Update(theTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }
    }
}
