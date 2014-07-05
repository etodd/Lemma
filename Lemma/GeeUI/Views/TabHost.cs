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
				return Children.Length == 0 ? null : (TabContainer)Children[0];
			}
			set
			{
				if (Children.Length == 0)
					this.Children.Add(value);
				else
					this.Children[0] = value;
			}
		}

		private View ActiveView
		{
			get
			{
				if (this.Children.Length == 1) return null;
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
			if (this.Children.Length > 1 && this.ActiveView.Selected && InputManager.IsKeyPressed(Keys.LeftControl))
				CtrlTabIndex = this.SetActiveTab(CtrlTabIndex + 1);
		}

		internal View TabViewToView(TabView v)
		{
			int index = TabContainerView.Children.ToList().IndexOf(v) + 1;
			return index >= Children.Length ? null : Children[index];
		}

		public int TabIndex(string name)
		{
			for (int i = 0; i < TabContainerView.Children.Length; i++)
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
			TabContainerView.Children.Clear();
			foreach (var child in Children)
			{
				if (child == TabContainerView) continue;
				this.Children.Remove(child);
			}
		}

		public void HideTab(string text)
		{
			for (int i = 0; i < TabContainerView.Children.Length; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab.TabText == text)
				{
					tab.Active.Value = false;
					this.SetActiveTab(i - 1);
					break;
				}
			}
		}

		public void ShowTab(string text)
		{
			for (int i = 0; i < TabContainerView.Children.Length; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab.TabText == text)
				{
					tab.Active.Value = true;
					break;
				}
			}
		}

		public void RemoveTab(string text)
		{
			for (int i = 0; i < TabContainerView.Children.Length; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab.TabText == text)
				{
					this.Children.RemoveAt(i + 1);
					this.TabContainerView.Children.Remove(tab);
					this.SetActiveTab(i - 1);
					break;
				}
			}
		}

		public void AddTab(string tabText, View newTab)
		{
			TabContainerView.AddTab(tabText, newTab);
		}

		public int SetActiveTab(int index)
		{
			for (int i = 0; i < this.TabContainerView.Children.Length; i++)
			{
				if (index > TabContainerView.Children.Length - 1)
					index = 0;
				else if (index < 0)
					index = TabContainerView.Children.Length - 1;

				if (TabContainerView.Children[index].Active)
					break;

				index++;
			}
			TabContainerView.TabClicked((TabView)TabContainerView.Children[index]);
			return index;
		}

		public string GetActiveTab()
		{
			for (int i = 0; i < TabContainerView.Children.Length; i++)
			{
				var tab = TabContainerView.Children[i] as TabView;
				if (tab != null && tab.Selected)
					return tab.TabText;
			}
			return null;
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
			CtrlTabIndex = index;
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
	}
}
