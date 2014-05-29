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
using Lemma.Components;
using Lemma.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.GInterfaces
{
	public class TimeTrialUI : Component<Main>, IUpdateableComponent, IGraphicsComponent
	{
		private PanelView RootTimeTrialView;
		private TextView TimeTrialCurTimeView;
		private TextView TimeTrialBestTimeView;

		private SpriteFont MainFont;

		public Property<float> ElapsedTime = new Property<float>();

		public Property<bool> TimeTrialTicking = new Property<bool>() { Value = true };

		public void LoadContent(bool reload)
		{
			MainFont = main.Content.Load<SpriteFont>("Font");
		}

		public override void Awake()
		{
			this.EnabledWhenPaused = false;

			RootTimeTrialView = new PanelView(main.GeeUI, main.GeeUI.RootView, Vector2.Zero);
			RootTimeTrialView.AnchorPoint.Value = new Vector2(1.0f, 0f);
			RootTimeTrialView.Add(new Binding<Vector2, Point>(RootTimeTrialView.Position, point => new Vector2(point.X - 30, 30), main.ScreenSize));
			RootTimeTrialView.ChildrenLayouts.Add(new VerticalViewLayout(2, false));

			RootTimeTrialView.UnselectedNinepatch = RootTimeTrialView.SelectedNinepatch = GeeUIMain.NinePatchBtnDefault;

			TimeTrialCurTimeView = new TextView(main.GeeUI, RootTimeTrialView, "Time: 00:00.00", Vector2.Zero, MainFont);
			TimeTrialBestTimeView = new TextView(main.GeeUI, RootTimeTrialView, "Best: 00:00.00", Vector2.Zero, MainFont);

			RootTimeTrialView.Width.Value = 200;
			RootTimeTrialView.Height.Value = 70;

			TimeTrialCurTimeView.TextScale.Value = 2f;

			RootTimeTrialView.Active.Value = false;

			this.TimeTrialCurTimeView.Add(new Binding<string, float>(TimeTrialCurTimeView.Text, x => "Time: " + SecondsToTimeString(x), this.ElapsedTime));

			this.AnimateIn();

			base.Awake();
		}

		public override void delete()
		{
			this.AnimateOut();
			base.delete();
		}

		private void AnimateIn()
		{
			this.main.AddComponent(
				new Animation(
					new Animation.Set<bool>(RootTimeTrialView.Active, true),
					new Animation.Vector2MoveTo(RootTimeTrialView.Position, new Vector2(main.ScreenSize.Value.X - 30, 30), 0.2f)
				)
			);
			this.ElapsedTime.Value = 0f;
		}

		private void AnimateOut()
		{
			this.main.AddComponent(
				new Animation(
					new Animation.Vector2MoveTo(RootTimeTrialView.Position, new Vector2(main.ScreenSize.Value.X + RootTimeTrialView.Width, 30), 0.2f),
					new Animation.Set<bool>(RootTimeTrialView.Active, false)
				)
			);
		}

		public void StartTicking()
		{
			TimeTrialTicking.Value = true;
		}

		public void StopTicking()
		{
			TimeTrialTicking.Value = false;
		}

		public string SecondsToTimeString(float seconds)
		{
			int decimalValue = (int)Math.Round((seconds - Math.Floor(seconds)) * 100, 2);
			int minutes = (int)(seconds / 60f);
			int leftOverSeconds = (int)Math.Floor(seconds - (minutes * 60));

			string sMinutes = minutes.ToString();
			string sSeconds = leftOverSeconds.ToString();
			string sMs = decimalValue.ToString();
			if (sMinutes.Length == 1) sMinutes = "0" + sMinutes;
			if (sSeconds.Length == 1) sSeconds = "0" + sSeconds;
			if (sMs.Length == 1) sMs = "0" + sMs;
			return sMinutes + ":" + sSeconds + "." + sMs;
		}

		public void Update(float dt)
		{
			if (TimeTrialTicking.Value)
				this.ElapsedTime.Value += dt;
		}
	}
}
