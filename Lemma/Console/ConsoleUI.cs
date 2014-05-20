using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma;
using ComponentBind;
using Lemma.Components;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Console
{
	public class ConsoleUI : Component<Main>, IUpdateableComponent
	{
		public View RootConsoleView;
		public TextFieldView ConsoleLogView;
		public TextFieldView ConsoleInputView;
		public SpriteFont MainFont;
		public SpriteFont ConsoleFont;

		public Property<bool> Showing = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			if (RootConsoleView == null)
			{
				MainFont = main.Content.Load<SpriteFont>("Font");
				ConsoleFont = main.Content.Load<SpriteFont>("ConsoleFont");
				int width = main.ScreenSize.Value.X - 6;
				RootConsoleView = new View(GeeUI.GeeUI.RootView) { Width = width, Height = 190 };
				ConsoleLogView = new TextFieldView(RootConsoleView, Vector2.Zero, ConsoleFont) { Width = width, Height = 170, Editable = false };
				ConsoleInputView = new TextFieldView(RootConsoleView, Vector2.Zero, MainFont) { Width = width, Height = 20, MultiLine = false, OnTextSubmitted = OnTextSubmitted };

				RootConsoleView.ChildrenLayout = new VerticalViewLayout(4, false);

				this.Add(new NotifyBinding(HandleResize, main.ScreenSize)); //Supercool~
				this.Add(new NotifyBinding(HandleToggle, Showing));
			}
			this.Showing.Value = false;
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
			RootConsoleView.Active = ConsoleLogView.Active = ConsoleInputView.Active = ConsoleInputView.Selected = Showing.Value;
		}

		public void HandleResize()
		{
			int width = main.ScreenSize.Value.X - 6;
			RootConsoleView.Width = ConsoleLogView.Width = ConsoleInputView.Width = width;
			int newY = ConsoleLogView.TextLines.Length - 1;
			ConsoleLogView.SetCursorPos(0, newY);
		}

		public void Update(float dt)
		{

		}


		public static void ConsoleInit()
		{
			Console.AddConVar(new ConVar("player_speed", "Player speed.", s =>
			{
				Entity playerData = Factory.Get<PlayerDataFactory>().Instance;
				playerData.GetOrMakeProperty<float>("MaxSpeed").Value = (float)Console.GetConVar("player_speed").GetCastedValue();
			}, "10") { TypeConstraint = typeof(float) });

			Console.AddConCommand(new ConCommand("help", "Recursion~~",
				collection => Console.Instance.PrintConCommandDescription((string)collection.Get("command")),
				new ConCommand.CommandArgument() { Name = "command" }));

			Lemma.Console.Console.AddConCommand(new ConCommand("show_window", "Shows a messagebox with title + description",
				collection =>
				{
					System.Windows.Forms.MessageBox.Show((string)collection.Get("Message"), (string)collection.Get("Title"));
				}, new ConCommand.CommandArgument() { Name = "Message" }, new ConCommand.CommandArgument() { Name = "Title", Optional = true, DefaultVal = "A title" }));

			Lemma.Console.Console.AddConCommand(new ConCommand("set_stat", "Sets the steamwork stat, if it exists",
				collection =>
				{
					string stat = (string)collection.Get("Stat");
					int val = (int)collection.Get("Value");
					SteamWorker.SetStat(stat, val);
				}, new ConCommand.CommandArgument() { Name = "Stat", Validate = (o) => SteamWorker.IsStat((string)o) }, new ConCommand.CommandArgument()
				{
					Name = "Value",
					CommandType = typeof(int),
					Validate =
						o =>
						{
							return (int)o > 0;
						}
				}));
		}
	}
}
