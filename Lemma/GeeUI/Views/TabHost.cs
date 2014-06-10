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


		public TabHost(GeeUIMain GeeUI, View rootView, Vector2 position, SpriteFont font)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
			TabContainerView = new TabContainer(GeeUI, this, font);
			TabContainerView.ChildrenLayouts.Add(new HorizontalViewLayout(1, true));
		}

		internal View TabViewToView(TabView v)
		{
			int index = TabContainerView.Children.ToList().IndexOf(v) + 1;
			return index >= Children.Length ? null : Children[index];
		}


		public void RemoveAllTabs()
		{
			foreach (var child in TabContainerView.Children)
				TabContainerView.RemoveChild(child);
			foreach (var child in Children)
			{
				if (child == TabContainerView) continue;
				RemoveChild(child);
			}
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
			for (int i = 1; i < Children.Length; i++)
			{
				Children[i].Active.Value = false;
				Children[i].Selected.Value = false;
			}
			Children[index + 1].Active.Value = true;
			Children[index + 1].Selected.Value = true;
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

		public override void BringChildToFront(View view)
		{
			return;
		}

		public override void Update(float dt)
		{
			for (int i = 1; i < Children.Length; i++)
			{
				Children[i].Position.Value = new Vector2(0, TabContainerView.BoundBox.Height);
				Children[i].Width = Width;
				Children[i].Height.Value = Height.Value - TabContainerView.BoundBox.Height - 10;
			}
			base.Update(dt);
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
