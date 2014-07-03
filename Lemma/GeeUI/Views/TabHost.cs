using System.Linq;
using GeeUI.Managers;
using GeeUI.ViewLayouts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GeeUI.Views
{
	public class TabHost : View
	{

		private TabContainer TabContainerView
		{
			get
			{
				return Children.Count == 0 ? null : (TabContainer)Children[0];
			}
			set
			{
				if (Children.Count == 0)
				{
					AddChild(value);
					return;
				}
				this.Children[0] = value;
			}
		}

		private View ActiveView
		{
			get
			{
				if (this.Children.Count == 1) return null;
				foreach (var child in Children)
				{
					if (child == TabContainerView) continue;
					if (child.Active) return child;
				}
				return null;
			}
		}

		private int CtrlTabIndex = 0;

		public TabHost(GeeUIMain GeeUI, View rootView, Vector2 position, SpriteFont font)
			: base(GeeUI, rootView)
		{
			Position.Value = position;
			TabContainerView = new TabContainer(GeeUI, this, font);
			TabContainerView.ChildrenLayouts.Add(new HorizontalViewLayout(1, true));

			InputManager.BindKey(TabTab, Keys.Tab);
		}

		private void TabTab()
		{
			if (this.Children.Count == 1 || !this.ActiveView.Selected || !InputManager.IsKeyPressed(Keys.LeftControl)) return;
			CtrlTabIndex++;
			if (CtrlTabIndex >= this.Children.Count - 1)
			{
				CtrlTabIndex = 0;
			}
			this.SetActiveTab(CtrlTabIndex);
		}

		internal View TabViewToView(TabView v)
		{
			int index = TabContainerView.Children.ToList().IndexOf(v) + 1;
			return index >= Children.Count ? null : Children[index];
		}

		public int TabIndex(string name)
		{
			for (int i = 0; i < TabContainerView.Children.Count; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab == null) continue;
				if (tab.TabText == name)
				{
					return i;
				}
			}
			return -1;
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

		public void RemoveTab(string text)
		{
			for (int i = 0; i < TabContainerView.Children.Count; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab == null) continue;
				if (tab.TabText == text)
				{
					this.RemoveChild(this.Children[i + 1]);
					this.TabContainerView.RemoveChild(tab);
					i--;
					if (this.Children.Count != 1)
						this.SetActiveTab(i);
				}
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

		public string GetActiveTab()
		{
			for (int i = 0; i < TabContainerView.Children.Count; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab != null && tab.Selected)
					return tab.TabText;
			}
			return null;
		}

		internal void TabClicked(int index)
		{
			for (int i = 1; i < Children.Count; i++)
			{
				Children[i].Active.Value = false;
				Children[i].Selected.Value = false;
			}
			Children[index + 1].Active.Value = true;
			Children[index + 1].Selected.Value = true;
			CtrlTabIndex = index;
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
			for (int i = 1; i < Children.Count; i++)
			{
				Children[i].Position.Value = new Vector2(0, TabContainerView.BoundBox.Height);
				Children[i].Width = Width;
				Children[i].Height.Value = Height.Value - TabContainerView.BoundBox.Height - 10;
			}
			base.Update(dt);
		}

		public override void AddChild(View child)
		{
			if (child == null) return;
			if (!(child is TabContainer) && TabContainerView.Children.Count != Children.Count) return;
			base.AddChild(child);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			base.Draw(spriteBatch);
		}
	}
}
