using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ComponentBind;
using GeeUI.Views;
using ICSharpCode.SharpZipLib.Tar;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Point = Microsoft.Xna.Framework.Point;
using View = GeeUI.Views.View;

namespace Lemma.GInterfaces
{
	public class TextPrompt : Component<Main>
	{
		private View EncompassingView;
		private PanelView MainView;
		private TextFieldView Text;

		private ButtonView Okay;
		private ButtonView Cancel;

		private string label;
		private string defaultText;
		private string action;
		private Action<string> callback;

		public TextPrompt(Action<string> callback, string defaultText = "", string label = "", string action = "Okay")
		{
			this.defaultText = defaultText;
			this.label = label;
			this.callback = callback;
			this.action = action;
		}

		public override void Awake()
		{
			//This is to make it so nothing else can be interacted with.
			this.EncompassingView = new View(this.main.GeeUI, this.main.GeeUI.RootView);
			this.MainView = new PanelView(this.main.GeeUI, this.EncompassingView, Vector2.Zero);
			MainView.Resizeable = false;
			MainView.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			MainView.Width.Value = 400;
			MainView.Height.Value = 100;

			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Height, (p) => p.Y, main.ScreenSize));
			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Width, (p) => p.X, main.ScreenSize));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(i / 2f, MainView.Y), EncompassingView.Width));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(MainView.X, i / 2f), EncompassingView.Height));

			new TextView(this.main.GeeUI, this.MainView, this.label, new Vector2(10, 8));
			this.Text = new TextFieldView(this.main.GeeUI, this.MainView, new Vector2(10, 25)) { MultiLine = false, };
			this.Text.Height.Value = 20;
			this.Text.Width.Value = 340;
			this.Text.Text = this.defaultText;
			this.Text.Selected.Value = true;

			this.Okay = new ButtonView(main.GeeUI, MainView, this.action, new Vector2(50, 60));
			this.Cancel = new ButtonView(main.GeeUI, MainView, "Cancel", new Vector2(300, 60));

			this.Okay.OnMouseClick += (sender, args) =>
			{
				this.Go();
			};

			this.Cancel.OnMouseClick += (sender, args) =>
			{
				this.Delete.Execute();
			};

			base.Awake();

			this.Text.ParentGeeUI.OnKeyPressedHandler += this.keyHandler;
		}

		private void keyHandler(string keyPressed, Keys key)
		{
			if (this.Text.Selected && key == Keys.Enter)
				this.Go();
		}

		public void Go()
		{
			if (this.callback != null)
				this.callback(this.Text.Text);
			this.Delete.Execute();
		}

		public override void delete()
		{
			this.Text.ParentGeeUI.OnKeyPressedHandler -= this.keyHandler;
			this.EncompassingView.RemoveFromParent();
			base.delete();
		}
	}
}
