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

namespace Lemma.GInterfaces
{
	public class ConsoleUI : Component<GameMain>, IUpdateableComponent
	{
		public View RootConsoleView;
		public TextFieldView ConsoleLogView;
		public TextFieldView ConsoleInputView;
		public SpriteFont MainFont;
		public SpriteFont ConsoleFont;

		[AutoConVar("console_showing", "If true, the console is showing")]
		public static Property<bool> Showing = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			if (RootConsoleView == null)
			{
				MainFont = main.Content.Load<SpriteFont>("Font");
				ConsoleFont = main.Content.Load<SpriteFont>("ConsoleFont");
				int width = main.ScreenSize.Value.X - 6;
				int textBoxWidth = width - 0;
				RootConsoleView = new View(main.GeeUI, main.GeeUI.RootView).SetWidth(width + 6).SetHeight(210);
				ConsoleLogView =
					(TextFieldView)new TextFieldView(main.GeeUI, RootConsoleView, new Vector2(0, 0), ConsoleFont).SetWidth(textBoxWidth)
						.SetHeight(175);
				ConsoleInputView =
					(TextFieldView)new TextFieldView(main.GeeUI, RootConsoleView, new Vector2(0, 0), MainFont).SetWidth(textBoxWidth).SetHeight(20);

				ConsoleLogView.Editable = false;
				ConsoleInputView.OnTextSubmitted = OnTextSubmitted;
				ConsoleInputView.MultiLine = false;

				RootConsoleView.ChildrenLayout = new VerticalViewLayout(0, false);

				this.Add(new NotifyBinding(HandleResize, main.ScreenSize)); //Supercool~
				this.Add(new NotifyBinding(HandleToggle, Showing));
			}
			Showing.Value = false;
		}


		public void OnTextSubmitted()
		{
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
			float scrollTime = 0.15f;
			float fadeTime = 0.1f;
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
					)
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
						new Animation.Set<bool>(ConsoleInputView.Selected, false)
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

		public void Update(float dt)
		{

		}


		public static void ConsoleInit()
		{
			Console.Console.AddConVar(new ConVar("player_speed", "Player speed.", s =>
			{
				Entity playerData = Factory.Get<PlayerDataFactory>().Instance;
				playerData.GetOrMakeProperty<float>("MaxSpeed").Value = (float)Console.Console.GetConVar("player_speed").GetCastedValue();
			}, "10") { TypeConstraint = typeof(float), Validate = o => (float)o > 0 && (float)o < 200 });

			Console.Console.AddConCommand(new ConCommand("help", "Recursion~~",
				collection => Console.Console.Instance.PrintConCommandDescription((string)collection.Get("command")),
				new ConCommand.CommandArgument() { Name = "command" }));

			Console.Console.AddConCommand(new ConCommand("show_window", "Shows a messagebox with title + description",
				collection =>
				{
					System.Windows.Forms.MessageBox.Show((string)collection.Get("Message"), (string)collection.Get("Title"));
				},
				new ConCommand.CommandArgument() { Name = "Message" }, new ConCommand.CommandArgument() { Name = "Title", Optional = true, DefaultVal = "A title" }));
		}
	}
}
