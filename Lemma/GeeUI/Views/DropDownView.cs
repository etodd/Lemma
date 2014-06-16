using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using GeeUI;
using GeeUI.Managers;
using GeeUI.ViewLayouts;
using GeeUI.Views;
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
			public SpriteFont Font;
			public Action OnClicked;
			public object Related;
		}

		private PanelView DropDownPanelView;
		private ListView DropDownListView;
		public List<DropDownOption> DropDownOptions = new List<DropDownOption>();
		private List<DropDownOption> DisplayingOptions = new List<DropDownOption>();
		private SpriteFont mainFont;
		private TextFieldView FilterView;

		public Property<string> Label = new Property<string>();
		public Property<int> LastItemSelected = new Property<int>() { Value = -1 };
		public Property<bool> AllowRightClickExecute = new Property<bool>() { Value = true };
		public Property<bool> AllowFilterText = new Property<bool>() { Value = true };
		public Property<int> FilterThreshhold = new Property<int>() { Value = 15 };

		private int _arrowKeysIndex = 0;

		public bool DropDownShowing
		{
			get { return !(DropDownPanelView == null || !DropDownPanelView.Active); }
		}

		public DropDownView(GeeUIMain theGeeUI, View parentView, Vector2 position, SpriteFont font)
			: base(theGeeUI, parentView)
		{
			this.NumChildrenAllowed.Value = 1;
			this.mainFont = font;
			ParentGeeUI.OnKeyPressedHandler += this.keyPressedHandler;
			var button = new ButtonView(theGeeUI, this, "", Vector2.Zero, font);
			button.Add(new Binding<int>(this.Width, button.Width));
			button.Add(new Binding<int>(this.Height, button.Height));
			button.OnMouseClick += (sender, args) => ToggleDropDown();
			button.OnMouseClickAway += (sender, args) => { HideDropDown(); };
			button.OnMouseRightClick += (sender, args) =>
			{
				if (AllowRightClickExecute)
					ExecuteLast();
			};

			button.Name = "button";

			this.DropDownPanelView = new PanelView(theGeeUI, theGeeUI.RootView, Vector2.Zero);
			DropDownPanelView.ChildrenLayouts.Add(new VerticalViewLayout(2, false));
			this.DropDownPanelView.Draggable = false;
			this.DropDownPanelView.SelectedNinepatch = this.DropDownPanelView.UnselectedNinepatch;

			FilterView = new TextFieldView(theGeeUI, DropDownPanelView, Vector2.Zero, mainFont);
			FilterView.Height.Value = 20;
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
			DropDownPanelView.Add(new Binding<int>(DropDownPanelView.Width, x => Math.Max(200, x), DropDownListView.Width));

			DropDownListView.Name = "DropList";
			DropDownPanelView.Add(new Binding<int>(DropDownPanelView.Height, (i1) => i1 + 2 + ((AllowFilterText && FilterView.Active) ? FilterView.BoundBox.Height : 0), DropDownListView.Height));

			DropDownPanelView.PostUpdate = f =>
			{
				if (!this.AttachedToRoot(this.ParentView))
					DropDownPanelView.ParentView.RemoveChild(DropDownPanelView);
			};

			DropDownPanelView.Active.Value = false;

			this.Label.Set = delegate(string value)
			{
				this.Label.InternalValue = value;
				((ButtonView)FindFirstChildByName("button")).Text = value;
			};
		}

		private void keyPressedHandler(string keyPressed, Keys key)
		{
			if (key == Keys.Escape && this.DropDownShowing)
				this.HideDropDown();
			if (key == Keys.Down && this.DropDownShowing)
			{
				this._arrowKeysIndex++;
		}
			if (key == Keys.Up && this.DropDownShowing)
			{
				this._arrowKeysIndex--;
			}
			if (key == Keys.Enter && this.DropDownShowing)
			{
				int index = _arrowKeysIndex;
				if (index < 0) index = 0;
				if (index >= DisplayingOptions.Count) index = 0;
				if (DisplayingOptions.Count == 0) return;

				var option = DisplayingOptions[index];
				this.OnOptionSelected(option);
				HideDropDown();
			}

			ArrowKeysHandle();
		}

		private void ArrowKeysHandle()
		{
			if (DropDownListView.Children.Length == 0)
			{
				_arrowKeysIndex = 0;
				return;
			}

			foreach (var child in DropDownListView.Children)
				child.Selected.Value = false;
			if (_arrowKeysIndex >= DropDownListView.Children.Length)
				_arrowKeysIndex = 0;
			DropDownListView.Children[_arrowKeysIndex].Selected.Value = true;
		}

		public void Refilter()
		{
			this._arrowKeysIndex = 0;
			string text = FilterView.Text;
			DropDownOption[] goodOptions = (from op in DropDownOptions
											where op.Text.ToLower().Contains(text.ToLower()) || text == ""
											select op).ToArray();
			DisplayingOptions = goodOptions.ToList();

			DropDownListView.RemoveAllChildren();
			FilterView.SubmitOnClickAway = false;
			if (goodOptions.Length > 0)
				FilterView.OnTextSubmitted = () =>
				{
					if (text != "")
					{
						HideDropDown();
					}

				};

			foreach (var option in goodOptions)
			{
				var dropButton = new ButtonView(this.ParentGeeUI, DropDownListView, option.Text, Vector2.Zero, option.Font);
				dropButton.OnMouseClick += (sender, args) =>
				{
					OnOptionSelected(option);
				};
			}

			ArrowKeysHandle();

		}

		public void ExecuteLast()
		{
			if (LastItemSelected.Value < 0)
			{
				LastItemSelected.Value = -1;
				return;
			}
			if (LastItemSelected.Value >= DropDownOptions.Count)
			{
				LastItemSelected.Value = -1;
				return;
			}
			var oc = DropDownOptions[LastItemSelected.Value].OnClicked;
			if (oc == null) return;
			oc();
		}

		public int GetOptionIndex(string optionName)
		{
			int i = -1;
			foreach (var dropdown in DropDownOptions)
			{
				i++;
				if (dropdown.Text.Equals(optionName)) return i;
			}
			return -1;
		}

		public DropDownOption GetSelectedOption()
		{
			if (LastItemSelected.Value == -1) return null;
			return DropDownOptions[LastItemSelected.Value];
		}

		public void SetSelectedOption(string optionName, bool callOnSelected = true)
		{
			int optionIndex = GetOptionIndex(optionName);
			if (optionIndex == -1) return;
			OnOptionSelected(DropDownOptions[optionIndex], callOnSelected);
		}

		public void OnOptionSelected(DropDownOption option, bool call = true)
		{
			if (string.IsNullOrEmpty(this.Label.Value))
				((ButtonView)FindFirstChildByName("button")).Text = option.Text;
			if (option.OnClicked != null && call) option.OnClicked();
			this.LastItemSelected.Value = GetOptionIndex(option.Text);
		}

		public void AddOption(string name, Action action, SpriteFont fontString = null, object related = null)
		{
			if (fontString == null) fontString = mainFont;
			var dropDownOption = new DropDownOption()
			{
				Font = fontString,
				Text = name,
				OnClicked = action,
				Related = related
			};
			DropDownOptions.Add(dropDownOption);

			var dropButton = new ButtonView(this.ParentGeeUI, DropDownListView, name, Vector2.Zero, fontString);
			dropButton.OnMouseClick += (sender, args) =>
			{
				OnOptionSelected(dropDownOption);
			};

			if (DropDownOptions.Count == 1 && string.IsNullOrEmpty(this.Label))
			{
				this.LastItemSelected.Value = 0;
				((ButtonView)FindFirstChildByName("button")).Text = name;
			}
		}

		public void RemoveAllOptions()
		{
			if (string.IsNullOrEmpty(this.Label))
				((ButtonView)FindFirstChildByName("button")).Text = "";
			DropDownOptions.Clear();
			DropDownListView.RemoveAllChildren();
			this.LastItemSelected.Value = -1;
		}

		public override void Update(float dt)
		{
			ComputeMouse();
			DropDownListView.Height.Value = DropDownListView.ChildrenBoundBox.Height;
			DropDownListView.Width.Value = DropDownListView.ChildrenBoundBox.Width;
			DropDownPanelView.Position.Value = new Vector2(AbsoluteX, this.AbsoluteBoundBox.Bottom);
			if (DropDownListView.AbsoluteBoundBox.Bottom > ParentGeeUI.RootView.Height)
			{
				DropDownListView.Height.Value -= (DropDownListView.AbsoluteBoundBox.Bottom - ParentGeeUI.RootView.Height);
			}
			if (DropDownShowing)
				DropDownPanelView.ParentView.BringChildToFront(DropDownPanelView);
			base.Update(dt);
		}

		private Point mouse;
		public void ComputeMouse()
		{
			if (!DropDownShowing || this.mouse == InputManager.GetMousePos()) return;
			if (DropDownPanelView.AbsoluteBoundBox.Contains(InputManager.GetMousePos()) ||
				Children[0].AbsoluteBoundBox.Contains(InputManager.GetMousePos())) return;
			HideDropDown();
		}

		public void ToggleDropDown()
		{
			if (DropDownPanelView == null) return;
			if (DropDownPanelView.Active)
				HideDropDown();
			else
			{
				ShowDropDown();
			}
		}

		public void HideDropDown()
		{
			if (!DropDownShowing) return;
			DropDownPanelView.Active.Value = false;
			FilterView.Selected.Value = FilterView.Active.Value = false;
			FilterView.ClearText();
			Refilter();
		}

		public void ShowDropDown()
		{
			if (DropDownPanelView == null) return;
			this.mouse = InputManager.GetMousePos();
			FilterView.ClearText();
			FilterView.Active.Value = FilterView.Selected.Value = AllowFilterText && FilterThreshhold.Value <= DropDownOptions.Count;
			DropDownPanelView.Active.Value = true;
			DropDownPanelView.ParentView.BringChildToFront(DropDownPanelView);
			DropDownPanelView.FindFirstChildByName("DropList").SetContentOffset(Vector2.Zero);
		}

		public override void OnDelete()
		{
			ParentGeeUI.OnKeyPressedHandler -= this.keyPressedHandler;
			DropDownPanelView.ParentView.RemoveChild(DropDownPanelView);
			base.OnDelete();
		}
	}
}
