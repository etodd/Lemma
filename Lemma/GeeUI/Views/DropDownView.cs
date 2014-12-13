using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using GeeUI;
using GeeUI.Managers;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma.GeeUI.Composites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using View = GeeUI.Views.View;

namespace GeeUI.Views
{
	public class DropDownView : View
	{
		public class DropDownOption
		{
			public string Text;
			public Action OnClicked;
			public object Related;
		}

		private PanelView DropDownPanelView;
		private ListView DropDownListView;
		public List<DropDownOption> DropDownOptions = new List<DropDownOption>();
		private List<DropDownOption> DisplayingOptions = new List<DropDownOption>();
		private TextFieldView FilterView;

		public Command OnShowing = new Command();

		public Property<string> Label = new Property<string>();
		public Property<int> LastItemSelected = new Property<int>() { Value = -1 };
		public Property<bool> AllowRightClickExecute = new Property<bool>();
		public Property<bool> AllowFilterText = new Property<bool>() { Value = true };
		public Property<int> FilterThreshhold = new Property<int>() { Value = 15 };

		private int _arrowKeysIndex = 0;

		public bool DropDownShowing
		{
			get
			{
				return DropDownPanelView.Active;
			}
		}

		public DropDownView(GeeUIMain theGeeUI, View parentView, Vector2 position)
			: base(theGeeUI, parentView)
		{
			this.numChildrenAllowed = 1;
			ParentGeeUI.OnKeyPressedHandler += this.keyPressedHandler;
			var button = new ButtonView(theGeeUI, this, "", Vector2.Zero);
			button.Add(new Binding<int>(this.Width, button.Width));
			button.Add(new Binding<int>(this.Height, button.Height));
			button.OnMouseClick += delegate(object sender, EventArgs e)
			{
				ToggleDropDown();
				this.FilterView.TemporarilyIgnoreMouseClickAway = true;
			};

			button.OnMouseClickAway += delegate(object sender, EventArgs args)
			{
				HideDropDown();
			};

			button.OnMouseRightClick += (sender, args) =>
			{
				if (AllowRightClickExecute)
					ExecuteLast();
			};

			button.Name = "button";

			this.DropDownPanelView = new PanelView(theGeeUI, theGeeUI.RootView, Vector2.Zero);
			DropDownPanelView.ChildrenLayouts.Add(new VerticalViewLayout(2, false));
			this.DropDownPanelView.SelectedNinepatch = this.DropDownPanelView.UnselectedNinepatch = GeeUIMain.NinePatchDropDown;

			FilterView = new TextFieldView(theGeeUI, DropDownPanelView, Vector2.Zero);

			// HACK
			FilterView.Height.Value = (int)(20 * theGeeUI.Main.MainFontMultiplier);

			FilterView.MultiLine = false;
			FilterView.Add(new Binding<int>(FilterView.Width, x => x - 8, DropDownPanelView.Width));
			FilterView.OnTextChanged = () =>
			{
				if (FilterView.Active && DropDownPanelView.Active && AllowFilterText)
				{
					Refilter();
				}
			};

			DropDownListView = new ListView(theGeeUI, DropDownPanelView);
			DropDownListView.ChildrenLayouts.Add(new VerticalViewLayout(1, false));
			DropDownListView.ScrollMultiplier = 20;
			DropDownListView.Add(new Binding<int, Rectangle>(DropDownListView.Width, x => Math.Max(200, x.Width), DropDownListView.ChildrenBoundBox));
			DropDownListView.Add(new Binding<int, Rectangle>(DropDownListView.Height, x => x.Height, DropDownListView.ChildrenBoundBox));

			DropDownPanelView.Add(new Binding<int>(DropDownPanelView.Width, DropDownListView.Width));

			DropDownListView.Name = "DropList";
			DropDownPanelView.Add(new Binding<int>(DropDownPanelView.Height, (i1) => i1 + 2 + ((AllowFilterText && FilterView.Active) ? FilterView.BoundBox.Height : 0), DropDownListView.Height));

			DropDownPanelView.Active.Value = false;

			this.Add(new SetBinding<string>(this.Label, delegate(string value)
			{
				((ButtonView)FindFirstChildByName("button")).Text = value;
			}));
		}

		private void keyPressedHandler(string keyPressed, Keys key)
		{
			if (this.DropDownShowing)
			{
				if (key == Keys.Escape)
					this.HideDropDown();
				else if (key == Keys.Down)
				{
					this._arrowKeysIndex++;
					this.ArrowKeysHandle();
				}
				else if (key == Keys.Up)
				{
					this._arrowKeysIndex--;
					this.ArrowKeysHandle();
				}
				else if (key == Keys.Enter)
				{
					if (DisplayingOptions.Count != 0)
					{
						int index = _arrowKeysIndex;
						if (index < 0 || index >= DisplayingOptions.Count)
							index = 0;

						var option = DisplayingOptions[index];
						this.OnOptionSelected(option);
						HideDropDown();
					}
				}
			}
		}

		private void ArrowKeysHandle()
		{
			if (DropDownListView.Children.Length == 0)
				_arrowKeysIndex = 0;
			else
			{
				foreach (var child in DropDownListView.Children)
					child.Selected.Value = false;
				if (_arrowKeysIndex >= DropDownListView.Children.Length)
					_arrowKeysIndex = 0;
				if (_arrowKeysIndex < 0)
					_arrowKeysIndex = DropDownListView.Children.Length - 1;
				DropDownListView.Children[_arrowKeysIndex].Selected.Value = true;
			}
		}

		public void Refilter()
		{
			this._arrowKeysIndex = 0;
			string text = FilterView.Text;
			DropDownOption[] goodOptions = (from op in DropDownOptions
											where op.Text.ToLower().Contains(text.ToLower()) || text == ""
											select op).ToArray();
			DisplayingOptions = goodOptions.ToList();

			DropDownListView.Children.Clear();
			FilterView.SubmitOnClickAway = false;
			if (goodOptions.Length > 0)
			{
				FilterView.OnTextSubmitted = () =>
				{
					if (!string.IsNullOrEmpty(text))
						HideDropDown();
				};
			}

			foreach (var option in goodOptions)
			{
				var dropButton = new ButtonView(this.ParentGeeUI, DropDownListView, option.Text, Vector2.Zero);
				dropButton.OnMouseClick += (sender, args) =>
				{
					OnOptionSelected(option);
				};
			}

			ArrowKeysHandle();

		}

		public void ExecuteLast()
		{
			if (LastItemSelected.Value < 0 || LastItemSelected.Value >= DropDownOptions.Count)
				LastItemSelected.Value = -1;
			else
			{
				var oc = DropDownOptions[LastItemSelected.Value].OnClicked;
				if (oc != null)
					oc();
			}
		}

		public int GetOptionIndex(string optionName)
		{
			int i = 0;
			foreach (var dropdown in DropDownOptions)
			{
				if (dropdown.Text.Equals(optionName))
					return i;
				i++;
			}
			return -1;
		}

		public DropDownOption GetSelectedOption()
		{
			if (LastItemSelected.Value == -1 || LastItemSelected.Value > DropDownOptions.Count - 1)
				return null;
			return DropDownOptions[LastItemSelected.Value];
		}

		public void SetSelectedOption(string optionName, bool callOnSelected = true)
		{
			int optionIndex = GetOptionIndex(optionName);
			if (optionIndex != -1)
				OnOptionSelected(DropDownOptions[optionIndex], callOnSelected);
		}

		public void OnOptionSelected(DropDownOption option, bool call = true)
		{
			if (string.IsNullOrEmpty(this.Label.Value))
				((ButtonView)FindFirstChildByName("button")).Text = option.Text;
			if (option.OnClicked != null && call)
				option.OnClicked();
			this.LastItemSelected.Value = GetOptionIndex(option.Text);
			this.HideDropDown();
		}

		public ButtonView AddOption(string name, Action action, object related = null)
		{
			if (this.LastItemSelected == -1)
			{
				if (string.IsNullOrEmpty(this.Label))
					((ButtonView)FindFirstChildByName("button")).Text = name;
				this.LastItemSelected.Value = this.DropDownOptions.Count;
			}

			var dropDownOption = new DropDownOption()
			{
				Text = name,
				OnClicked = action,
				Related = related
			};
			DropDownOptions.Add(dropDownOption);

			var dropButton = new ButtonView(this.ParentGeeUI, DropDownListView, name, Vector2.Zero);
			dropButton.OnMouseClick += (sender, args) =>
			{
				OnOptionSelected(dropDownOption);
			};
			return dropButton;
		}

		public void RemoveAllOptions()
		{
			if (string.IsNullOrEmpty(this.Label))
				((ButtonView)FindFirstChildByName("button")).Text = "";
			DropDownOptions.Clear();
			DropDownListView.Children.Clear();
			this.LastItemSelected.Value = -1;
		}

		private Point mouse;
		public override void Update(float dt)
		{
			if (this.DropDownShowing)
			{
				if (this.mouse != InputManager.GetMousePos()
					&& !DropDownPanelView.AbsoluteBoundBox.Contains(InputManager.GetMousePos())
					&& !Children[0].AbsoluteBoundBox.Contains(InputManager.GetMousePos()))
				{
					this.HideDropDown();
				}
				else
				{
					Vector2 target = new Vector2(AbsoluteX - 1, this.AbsoluteBoundBox.Bottom);
					if (DropDownPanelView.Position != target)
						DropDownPanelView.Position.Value = target;
					if (DropDownListView.AbsoluteBoundBox.Bottom > ParentGeeUI.RootView.Height)
						DropDownListView.Height.Value -= (DropDownListView.AbsoluteBoundBox.Bottom - ParentGeeUI.RootView.Height);
				}
			}
			base.Update(dt);
		}

		public void ToggleDropDown()
		{
			if (this.DropDownShowing)
				HideDropDown();
			else
				ShowDropDown();
		}

		public void HideDropDown()
		{
			if (DropDownShowing)
			{
				DropDownPanelView.Active.Value = false;
				FilterView.Selected.Value = FilterView.Active.Value = false;
				FilterView.ClearText();
				Refilter();
			}
		}

		public void ShowDropDown()
		{
			this.OnShowing.Execute();
			this.mouse = InputManager.GetMousePos();
			DropDownPanelView.Active.Value = true;
			DropDownPanelView.FindFirstChildByName("DropList").SetContentOffset(Vector2.Zero);
			DropDownPanelView.BringToFront();
			FilterView.ClearText();
			FilterView.Active.Value = FilterView.Selected.Value = AllowFilterText && FilterThreshhold.Value <= DropDownOptions.Count;
		}

		public override void delete()
		{
			ParentGeeUI.OnKeyPressedHandler -= this.keyPressedHandler;
			this.DropDownPanelView.RemoveFromParent();
			base.delete();
		}
	}
}
