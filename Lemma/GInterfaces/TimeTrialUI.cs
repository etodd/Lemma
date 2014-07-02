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
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.GInterfaces
{
	public class TimeTrialUI : Component<Main>, IUpdateableComponent, IGraphicsComponent
	{
		public PanelView RootTimePanelView;
		private TextView TimeTrialCurTimeView;
		private TextView TimeTrialBestTimeView;

		public PanelView RootTimeEndView;
		private TextView EndTimeTextView;
		private TextView EndTimeCollectiblesTextView;
		private ButtonView RetryMapButton;
		private ButtonView MainMenuButton;

		public SpriteFont MainFont;
		public SpriteFont BiggerFont;

		public Action<bool> EndPanelClosed;

		public Property<float> ElapsedTime = new Property<float>();
		public Property<bool> TimeTrialTicking = new Property<bool>() { Value = true };

		//Stupid little "bruteforce"
		public int Seconds_FirstPlace = 0;
		public int Seconds_SecondPlace = 0;
		public int Seconds_ThirdPlace = 0;

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
			RootTimePanelView.Draggable = RootTimeEndView.Draggable = false;

			RootTimePanelView.UnselectedNinepatch = RootTimePanelView.SelectedNinepatch = GeeUIMain.NinePatchBtnDefault;

			TimeTrialCurTimeView = new TextView(main.GeeUI, RootTimePanelView, "Time: 00:00.00", Vector2.Zero, BiggerFont);
			TimeTrialBestTimeView = new TextView(main.GeeUI, RootTimePanelView, "Best: 00:00.00", Vector2.Zero, MainFont);

			RootTimePanelView.Active.Value = false;
			RootTimeEndView.Active.Value = false;

			this.TimeTrialCurTimeView.Add(new Binding<string, float>(TimeTrialCurTimeView.Text, x => "Time: " + SecondsToTimeString(x), this.ElapsedTime));

			this.AnimateOut();

			MapManifest manifest = MapManifest.FromMapPath(main.MapFile);
			float bestTime = manifest.BestPersonalTimeTrialTime;
			TimeTrialBestTimeView.Text.Value = "Best: " + SecondsToTimeString(bestTime);

			base.Awake();
		}

		public void LoadContent(bool reload)
		{
			MainFont = main.Content.Load<SpriteFont>("Font");
			BiggerFont = main.Content.Load<SpriteFont>("TimeFont");
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
						if (RootTimePanelView != null && RootTimePanelView.ParentView != null)
							RootTimePanelView.ParentView.RemoveChild(RootTimePanelView);

						if (RootTimeEndView != null && RootTimeEndView.ParentView != null)
							RootTimeEndView.ParentView.RemoveChild(RootTimeEndView);
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

		public void ShowEndPanel()
		{
			RootTimeEndView.Active.Value = true;
			AnimateOut();
			StopTicking();
			MapManifest manifest = MapManifest.FromMapPath(main.MapFile);
			float bestTime = manifest.BestPersonalTimeTrialTime;
			manifest.LastPersonalTimeTrialTime = ElapsedTime;
			if (this.ElapsedTime < bestTime || bestTime <= 0)
			{
				manifest.BestPersonalTimeTrialTime = ElapsedTime;
			}
			manifest.Save();
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

		private int NumCollectiblesPickedUp()
		{
			List<Entity> notes = main.Get("Note").ToList();
			return notes.Count(x => x.Get<Note>().IsCollected);
		}

		private int NumCollectiblesTotal()
		{
			List<Entity> notes = main.Get("Note").ToList();
			return notes != null ? notes.Count : 0;
		}
	}
}
