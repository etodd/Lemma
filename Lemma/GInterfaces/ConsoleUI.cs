using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma;
using ComponentBind;
using Lemma.Components;
using Lemma.Console;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Lemma.GInterfaces
{
	public class ConsoleUI : Component<Main>
	{
		public View RootConsoleView;
		public TextFieldView ConsoleLogView;
		public TextFieldView ConsoleInputView;

		public static Property<bool> Showing = new Property<bool>();

		private Property<bool> _animating = new Property<bool>() { Value = false };

		public override void Awake()
		{
			base.Awake();
			if (RootConsoleView == null)
			{
				int width = main.ScreenSize.Value.X - 6;
				int textBoxWidth = width - 0;
				RootConsoleView = new View(main.GeeUI, main.GeeUI.RootView).SetWidth(width + 6).SetHeight(210);
				ConsoleLogView =
					(TextFieldView)new TextFieldView(main.GeeUI, RootConsoleView, new Vector2(0, 0)).SetWidth(textBoxWidth)
						.SetHeight(175);
				ConsoleInputView =
					(TextFieldView)new TextFieldView(main.GeeUI, RootConsoleView, new Vector2(0, 0)).SetWidth(textBoxWidth).SetHeight(20);

				ConsoleLogView.Editable = false;
				ConsoleInputView.OnTextSubmitted = OnTextSubmitted;
				ConsoleInputView.SubmitOnClickAway = false;
				ConsoleInputView.MultiLine = false;

				RootConsoleView.ChildrenLayouts.Add(new VerticalViewLayout(0, false));

				this.Add(new NotifyBinding(HandleResize, main.ScreenSize)); //Supercool~
				this.Add(new NotifyBinding(HandleToggle, Showing));

				this.main.GeeUI.OnKeyPressedHandler += this.keyPressedHandler;
			}
			Showing.Value = false;
		}

		public override void delete()
		{
			base.delete();
			this.main.GeeUI.OnKeyPressedHandler -= this.keyPressedHandler;
		}

		private int historyIndex = -1;
		private void keyPressedHandler(string keyPressed, Keys key)
		{
			if (Showing)
			{
				Console.Console console = Console.Console.Instance;
				if (console.History.Count > 0)
				{
					bool doHistory = false;
					if (key == Keys.Down)
					{
						doHistory = true;
						if (this.historyIndex == -1)
							this.historyIndex = 0;
						else
							this.historyIndex++;
					}
					else if (key == Keys.Up)
					{
						doHistory = true;
						if (this.historyIndex == -1)
							this.historyIndex = console.History.Count - 1;
						else
							this.historyIndex--;
					}

					if (doHistory)
					{
						if (this.historyIndex >= console.History.Count)
							this.historyIndex = 0;
						else if (this.historyIndex < 0)
							this.historyIndex = console.History.Count - 1;
						
						ConsoleInputView.Text = console.History[this.historyIndex];
						ConsoleInputView.SetCursorPos(ConsoleInputView.Text.Length, 0);
					}
				}
			}
		}

		public void OnTextSubmitted()
		{
			this.historyIndex = -1;
			main.Console.ConsoleUserInput(ConsoleInputView.Text);
			ConsoleInputView.ClearText();
		}

		public void LogText(string text)
		{
			if (ConsoleLogView.Text.Length != 0)
				ConsoleLogView.AppendText("\n");
			ConsoleLogView.AppendText(text);
			int newY = ConsoleLogView.TextLines.Length - 1;
			ConsoleLogView.SetCursorPos(0, newY);
		}

		public void HandleToggle()
		{
			if (_animating.Value)
				return;

			float scrollTime = 0.15f;
			float fadeTime = 0.1f;
			_animating.Value = true;
			//RootConsoleView.Active.Value = ConsoleLogView.Active.Value = ConsoleInputView.Active.Value = ConsoleInputView.Selected.Value = Showing.Value;
			if (Showing.Value)
			{
				main.AddComponent(new Animation(
					new Animation.Set<bool>(RootConsoleView.Active, true),
					new Animation.Set<bool>(ConsoleLogView.Active, true),
					new Animation.Set<bool>(ConsoleInputView.Active, true),
					new Animation.Set<bool>(ConsoleInputView.Selected, true),
					new Animation.Parallel(
							new Animation.FloatMoveTo(RootConsoleView.MyOpacity, 1.0f, fadeTime),
							new Animation.Vector2MoveTo(RootConsoleView.Position, new Vector2(0, 0), scrollTime)
					),
					new Animation.Set<bool>(_animating, false)
					));
			}
			else
			{
				main.AddComponent(new Animation(
						new Animation.Parallel(
							new Animation.FloatMoveTo(RootConsoleView.MyOpacity, 0.0f, fadeTime),
							new Animation.Vector2MoveTo(RootConsoleView.Position, new Vector2(0, -RootConsoleView.Height), scrollTime)
						),
						new Animation.Set<bool>(RootConsoleView.Active, false),
						new Animation.Set<bool>(ConsoleLogView.Active, false),
						new Animation.Set<bool>(ConsoleInputView.Active, false),
						new Animation.Set<bool>(ConsoleInputView.Selected, false),
						new Animation.Set<bool>(_animating, false)
					));

			}
		}

		public void HandleResize()
		{
			int width = main.ScreenSize.Value.X - 6;
			int textBoxWidth = width - 0;

			RootConsoleView.Width.Value = width;
			ConsoleLogView.Width.Value = ConsoleInputView.Width.Value = textBoxWidth;

			int newY = ConsoleLogView.TextLines.Length - 1;
			ConsoleLogView.SetCursorPos(0, newY);
		}
	}
}
