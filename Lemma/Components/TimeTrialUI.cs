using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using ComponentBind;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeeUI;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma.Console;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class TimeTrialUI : Component<Main>
	{
		public PanelView RootTimePanelView;
		private TextView TimeTrialCurTimeView;
		private TextView TimeTrialBestTimeView;

		public PanelView RootTimeEndView;
		private TextView EndTimeTextView;
		private TextView EndTimeBestView;
		private TextView EndTimeTitleView;

		// Input properties
		public Property<float> ElapsedTime = new Property<float>();
		public Property<string> NextMap = new Property<string>();

		[XmlIgnore]
		public Command Retry = new Command();

		[XmlIgnore]
		public Command MainMenu = new Command();

		[XmlIgnore]
		public Command Edit = new Command();

		[XmlIgnore]
		public Command LoadNextMap = new Command();

		public override void delete()
		{
			if (this.RootTimePanelView != null)
				this.RootTimePanelView.RemoveFromParent();
			if (this.RootTimeEndView != null)
				this.RootTimeEndView.RemoveFromParent();
			base.delete();
		}

		private Property<float> bestTime = new Property<float>();

		public override void Awake()
		{
			this.Serialize = false;
			this.EnabledWhenPaused = false;

			RootTimePanelView = new PanelView(main.GeeUI, main.GeeUI.RootView, Vector2.Zero);
			RootTimePanelView.AnchorPoint.Value = new Vector2(1.0f, 0f);
			RootTimePanelView.Add(new Binding<Vector2, Point>(RootTimePanelView.Position, point => this.RootTimePanelView.Active ? new Vector2(point.X - 30, 30) :
				new Vector2(main.ScreenSize.Value.X + RootTimePanelView.Width.Value, 30), main.ScreenSize));
			RootTimePanelView.ChildrenLayouts.Add(new VerticalViewLayout(2, false));
			RootTimePanelView.Width.Value = 200;
			RootTimePanelView.Height.Value = 70;

			RootTimeEndView = new PanelView(main.GeeUI, main.GeeUI.RootView, Vector2.Zero);
			RootTimeEndView.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			RootTimeEndView.Add(new Binding<Vector2, Point>(RootTimeEndView.Position, point => new Vector2(point.X / 2f, point.Y / 2f), main.ScreenSize));
			RootTimeEndView.Width.Value = 400;
			RootTimeEndView.Height.Value = 300;

			EndTimeTitleView = new TextView(main.GeeUI, RootTimeEndView, "Map Title", Vector2.Zero);
			EndTimeTitleView.TextJustification = TextJustification.Center;
			EndTimeTitleView.AutoSize.Value = false;
			EndTimeTitleView.Width.AddBinding(new Binding<int>(EndTimeTitleView.Width, RootTimeEndView.Width));

			EndTimeTextView = new TextView(main.GeeUI, RootTimeEndView, "Your time: ", new Vector2(0, 70));
			EndTimeTextView.TextJustification = TextJustification.Center;
			EndTimeTextView.AutoSize.Value = false;
			EndTimeTextView.Width.AddBinding(new Binding<int>(EndTimeTextView.Width, RootTimeEndView.Width));

			EndTimeBestView = new TextView(main.GeeUI, RootTimeEndView, "Best time: ", new Vector2(0, 100));
			EndTimeBestView.TextJustification = TextJustification.Center;
			EndTimeBestView.AutoSize.Value = false;
			EndTimeBestView.Width.AddBinding(new Binding<int>(EndTimeBestView.Width, RootTimeEndView.Width));

			ButtonView MainMenuButton = new ButtonView(main.GeeUI, RootTimeEndView, "Back", new Vector2(20, 250));
			MainMenuButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.MainMenu.Execute();
			};

			ButtonView RetryMapButton = new ButtonView(main.GeeUI, RootTimeEndView, "Retry", new Vector2(70, 250));
			RetryMapButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.Retry.Execute();
			};

			ButtonView EditButton = new ButtonView(main.GeeUI, RootTimeEndView, "Edit", new Vector2(130, 250));
			EditButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.Edit.Execute();
			};
			EditButton.Active.Value = this.main.Settings.GodMode || Path.GetDirectoryName(this.main.MapFile) == this.main.CustomMapDirectory;

			ButtonView NextMapButton = new ButtonView(main.GeeUI, RootTimeEndView, "Next map", new Vector2(180, 250));
			this.Add(new Binding<bool, string>(NextMapButton.Active, x => !string.IsNullOrEmpty(x), this.NextMap));
			NextMapButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.LoadNextMap.Execute();
			};

			RootTimePanelView.UnselectedNinepatch = RootTimePanelView.SelectedNinepatch = GeeUIMain.NinePatchBtnDefault;

			TimeTrialCurTimeView = new TextView(main.GeeUI, RootTimePanelView, "Time: 00:00.00", Vector2.Zero);
			TimeTrialBestTimeView = new TextView(main.GeeUI, RootTimePanelView, "Best: 00:00.00", Vector2.Zero);

			RootTimePanelView.Active.Value = false;
			RootTimeEndView.Active.Value = false;

			this.TimeTrialCurTimeView.Add(new Binding<string, float>(TimeTrialCurTimeView.Text, this.SecondsToTimeString, this.ElapsedTime));

			this.AnimateOut();

			this.bestTime.Value = this.main.GetMapTime(WorldFactory.Instance.Get<World>().UUID);

			this.TimeTrialBestTimeView.Add(new Binding<string>(TimeTrialBestTimeView.Text, () =>
			{
				if (this.bestTime == 0) 
					return "Best: n/a";
				else
					return string.Format("Best: {0}", this.SecondsToTimeString(this.bestTime - this.ElapsedTime));
			}, this.ElapsedTime, this.bestTime));

			EndTimeTitleView.Text.Value = Path.GetFileNameWithoutExtension(main.MapFile);

			base.Awake();
		}

		public void AnimateIn()
		{
			this.Entity.Add
			(
				new Animation
				(
					new Animation.Set<bool>(RootTimePanelView.Active, true),
					new Animation.Vector2MoveTo(RootTimePanelView.Position, new Vector2(main.ScreenSize.Value.X - 30, 30), 0.2f)
				)
			);
			this.ElapsedTime.Value = 0f;
		}

		public void AnimateOut()
		{
			this.Entity.Add
			(
				new Animation
				(
					new Animation.Vector2MoveTo(RootTimePanelView.Position, new Vector2(main.ScreenSize.Value.X + RootTimePanelView.Width, 30), 0.2f),
					new Animation.Set<bool>(RootTimePanelView.Active, false)
				)
			);
		}

		public void ShowEndPanel()
		{
			PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = false;
			this.main.UI.IsMouseVisible.Value = true;
			this.RootTimeEndView.Active.Value = true;
			this.AnimateOut();

			string uuid = WorldFactory.Instance.Get<World>().UUID;
			this.bestTime.Value = this.main.SaveMapTime(uuid, this.ElapsedTime);

			EndTimeTextView.Text.Value = "Time: " + SecondsToTimeString(ElapsedTime);
			EndTimeBestView.Text.Value = "Best: " + SecondsToTimeString(this.bestTime);
			if (this.bestTime.Value == ElapsedTime.Value)
				EndTimeBestView.Text.Value += " Record!";
		}

		public string SecondsToTimeString(float seconds)
		{
			bool negative = seconds < 0;
			if (negative) seconds *= -1;
			int decimalValue = (int)Math.Round((seconds - Math.Floor(seconds)) * 100, 2);
			int minutes = (int)(seconds / 60f);
			int leftOverSeconds = (int)Math.Floor(seconds - (minutes * 60));

			string sMinutes = minutes.ToString();
			string sSeconds = leftOverSeconds.ToString();
			string sMs = decimalValue.ToString();
			if (sMinutes.Length == 1) sMinutes = "0" + sMinutes;
			if (sSeconds.Length == 1) sSeconds = "0" + sSeconds;
			if (sMs.Length == 1) sMs = "0" + sMs;
			return (negative ? "-" : "") + sMinutes + ":" + sSeconds + "." + sMs;
		}
	}
}