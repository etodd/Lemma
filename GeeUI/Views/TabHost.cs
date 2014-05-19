using System.Linq;
using GeeUI.ViewLayouts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeeUI.Views
{
    public class TabHost : View
    {

        private TabContainer TabContainerView
        {
            get
            {
                return Children.Length == 0 ? null : (TabContainer)Children[0];
            }
            set
            {
                if (Children.Length == 0)
                {
                    AddChild(value);
                    return;
                }
                this._children[0] = value;
                ReOrderChildrenDepth();
            }
        }

        public override Rectangle BoundBox
        {
            get
            {
                return new Rectangle(X, Y, Width, Height);
            }
        }

        public TabHost(View rootView, Vector2 position, SpriteFont font)
            : base(rootView)
        {
            Position = position;
            TabContainerView = new TabContainer(this, font);
            TabContainerView.ChildrenLayout = new HorizontalViewLayout(1, true);
        }

        internal View TabViewToView(TabView v)
        {
            int index = TabContainerView.Children.ToList().IndexOf(v) + 1;
            return index >= Children.Length ? null : Children[index];
        }

        public void AddTab(string tabText, View newTab)
        {
            TabContainerView.AddTab(tabText, newTab);
        }

        public void SetActiveTab(int index)
        {
            TabContainerView.TabClicked((TabView)TabContainerView.Children[index]);
        }

        internal void TabClicked(int index)
        {
            for(int i = 1; i < Children.Length; i++)
            {
                Children[i].Active = false;
                Children[i].Selected = false;
            }
            Children[index + 1].Active = true;
            Children[index + 1].Selected = true;
        }

        public override void OnMClick(Vector2 position, bool fromChild = false)
        {
            base.OnMClick(position);
        }
        public override void OnMClickAway(bool fromChild = false)
        {
            //base.onMClickAway();
        }

        public override void OnMOver(bool fromChild = false)
        {
            base.OnMOver();
        }
        public override void OnMOff(bool fromChild = false)
        {
            base.OnMOff();
        }

        public override void BringChildToFront(View view)
        {
            return;
        }

        public override void Update(GameTime theTime)
        {
            for (int i = 1; i < Children.Length; i++  )
            {
                Children[i].Position = new Vector2(0, TabContainerView.BoundBox.Height);
                Children[i].Width = Width;
                Children[i].Height = Height - TabContainerView.BoundBox.Height;
            }
            base.Update(theTime);
        }

        public override void AddChild(View child)
        {
            if (!(child is TabContainer) && TabContainerView.Children.Length != Children.Length) return;
            base.AddChild(child);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }
    }
}
