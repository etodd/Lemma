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
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace Lemma.GInterfaces
{
	public class TimeTrialUI : Component<Main>, IUpdateableComponent, IGraphicsComponent
	{
		public PanelView RootTimePanelView;
		private TextView TimeTrialCurTimeView;
		private TextView TimeTrialBestTimeView;

		public PanelView RootTimeEndView;
		private TextView EndTimeTextView;
		private TextView EndTimeBestView;
		private TextView EndTimeTitleView;
		private ButtonView RetryMapButton;
		private ButtonView NextMapButton;
		private ButtonView MainMenuButton;

		public SpriteFont MainFont;
		public SpriteFont BiggerFont;
		public SpriteFont YourTimeFont;
		public SpriteFont BestTimeFont;

		public Action<bool> EndPanelClosed;

		public Property<float> ElapsedTime = new Property<float>();
		public Property<bool> TimeTrialTicking = new Property<bool>() { Value = true };

		private TimeTrial theTimeTrial;

		public TimeTrialUI(TimeTrial tT)
		{
			this.theTimeTrial = tT;
		}

		public override void delete()
		{
			this.AnimateOut(true);
			base.delete();
		}

		public override void Awake()
		{
			this.Serialize = false;
			this.EnabledWhenPaused = false;

			this.EndPanelClosed = shouldRetry =>
			{
				if (shouldRetry)
					IO.MapLoader.Load(this.main, this.main.MapFile);
			};

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

			EndTimeTitleView = new TextView(main.GeeUI, RootTimeEndView, "Map Title", Vector2.Zero, BiggerFont);
			EndTimeTitleView.TextJustification = TextJustification.Center;
			EndTimeTitleView.AutoSize.Value = false;
			EndTimeTitleView.Width.AddBinding(new Binding<int>(EndTimeTitleView.Width, RootTimeEndView.Width));

			EndTimeTextView = new TextView(main.GeeUI, RootTimeEndView, "Your time: ", new Vector2(0, 70), YourTimeFont);
			EndTimeTextView.TextJustification = TextJustification.Center;
			EndTimeTextView.AutoSize.Value = false;
			EndTimeTextView.Width.AddBinding(new Binding<int>(EndTimeTextView.Width, RootTimeEndView.Width));

			EndTimeBestView = new TextView(main.GeeUI, RootTimeEndView, "Best time: ", new Vector2(0, 100), BestTimeFont);
			EndTimeBestView.TextJustification = TextJustification.Center;
			EndTimeBestView.AutoSize.Value = false;
			EndTimeBestView.Width.AddBinding(new Binding<int>(EndTimeBestView.Width, RootTimeEndView.Width));

			RetryMapButton = new ButtonView(main.GeeUI, RootTimeEndView, "Retry", new Vector2(30, 250), MainFont);
			RetryMapButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.retry();
			};
			NextMapButton = new ButtonView(main.GeeUI, RootTimeEndView, "Next Map", new Vector2(80, 250), MainFont);
			NextMapButton.Active.Value = !string.IsNullOrEmpty(this.theTimeTrial.NextMap.Value);
			NextMapButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.main.CurrentSave.Value = null;
				this.main.EditorEnabled.Value = false;
				IO.MapLoader.Load(this.main, this.theTimeTrial.NextMap);
			};
			MainMenuButton = new ButtonView(main.GeeUI, RootTimeEndView, "Back", new Vector2(160, 250), MainFont);
			MainMenuButton.OnMouseClick += delegate(object sender, EventArgs e)
			{
				this.main.CurrentSave.Value = null;
				this.main.EditorEnabled.Value = false;
				IO.MapLoader.Load(this.main, Main.MenuMap);
				this.main.Menu.Show();
			};

			RootTimePanelView.UnselectedNinepatch = RootTimePanelView.SelectedNinepatch = GeeUIMain.NinePatchBtnDefault;

			TimeTrialCurTimeView = new TextView(main.GeeUI, RootTimePanelView, "Time: 00:00.00", Vector2.Zero, BiggerFont);
			TimeTrialBestTimeView = new TextView(main.GeeUI, RootTimePanelView, "Best: 00:00.00", Vector2.Zero, MainFont);

			RootTimePanelView.Active.Value = false;
			RootTimeEndView.Active.Value = false;

			this.TimeTrialCurTimeView.Add(new Binding<string, float>(TimeTrialCurTimeView.Text, x => "Time: " + SecondsToTimeString(x), this.ElapsedTime));

			this.AnimateOut();

			MapManifest manifest = MapManifest.FromMapPath(main, main.MapFile);
			float bestTime;
			if (manifest == null)
				bestTime = 0;
			else
				bestTime = manifest.BestPersonalTimeTrialTime;
			TimeTrialBestTimeView.Text.Value = "Best: " + SecondsToTimeString(bestTime);

			this.TimeTrialBestTimeView.Add(new Binding<string, float>(TimeTrialBestTimeView.Text, x =>
			{
				if (bestTime != 0) x = bestTime - x;
				return "Best: " + SecondsToTimeString(x);
			}, this.ElapsedTime));

			EndTimeTitleView.Text.Value = Path.GetFileNameWithoutExtension(main.MapFile);

			this.Add(new CommandBinding(this.main.Spawner.PlayerSpawned, delegate()
			{
				PlayerFactory.Instance.Add(new CommandBinding(PlayerFactory.Instance.Get<Player>().Die, (Action)this.retry));
			}));

			base.Awake();
		}

		private void retry()
		{
			this.main.CurrentSave.Value = null;
			this.main.EditorEnabled.Value = false;
			IO.MapLoader.Load(this.main, this.main.MapFile);
		}

		public void LoadContent(bool reload)
		{
			MainFont = main.Content.Load<SpriteFont>("Font");
			BiggerFont = main.Content.Load<SpriteFont>("TimeFont");
			YourTimeFont = main.Content.Load<SpriteFont>("TimeYourTimeFont");
			BestTimeFont = main.Content.Load<SpriteFont>("TimeBestTimeFont");
		}

		public void AnimateIn()
		{
			this.main.AddComponent(
				new Animation(
					new Animation.Set<bool>(RootTimePanelView.Active, true),
					new Animation.Vector2MoveTo(RootTimePanelView.Position, new Vector2(main.ScreenSize.Value.X - 30, 30), 0.2f)
				)
			);
			this.ElapsedTime.Value = 0f;
		}

		public void AnimateOut(bool remove = false)
		{
			this.main.AddComponent(
				new Animation(
					new Animation.Vector2MoveTo(RootTimePanelView.Position, new Vector2(main.ScreenSize.Value.X + RootTimePanelView.Width, 30), 0.2f),
					new Animation.Set<bool>(RootTimePanelView.Active, false),
					new Animation.Execute(() =>
					{
						if (!remove) return;
						if (RootTimePanelView != null)
							RootTimePanelView.RemoveFromParent();

						if (RootTimeEndView != null)
							RootTimeEndView.RemoveFromParent();
					})
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

		public void ShowEndPanel(bool success)
		{
			PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = false;
			main.IsMouseVisible.Value = true;
			RootTimeEndView.Active.Value = true;
			AnimateOut();
			StopTicking();

			if (success)
			{
				MapManifest manifest = MapManifest.FromMapPath(main, main.MapFile);
				float bestTime = manifest.BestPersonalTimeTrialTime;
				manifest.LastPersonalTimeTrialTime = ElapsedTime;
				if (this.ElapsedTime < bestTime || bestTime <= 0)
				{
					manifest.BestPersonalTimeTrialTime = ElapsedTime;
				}
				manifest.Save();

				EndTimeTextView.Text.Value = "Time: " + SecondsToTimeString(ElapsedTime);
				EndTimeBestView.Text.Value = "Best: " + SecondsToTimeString(bestTime);
				if (bestTime >= ElapsedTime)
				{
					EndTimeBestView.Text.Value += " Record!";
				}
			}
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

		public void Update(float dt)
		{
			if (TimeTrialTicking.Value)
				this.ElapsedTime.Value += dt;
		}
	}
}
