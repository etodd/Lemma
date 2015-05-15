using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using GeeUI.Views;
using Lemma.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Lemma.Util;
using Lemma.GInterfaces;
using Lemma.Factories;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.GZip;
using Steamworks;

namespace Lemma.Components
{
	public class Menu : Component<Main>, IUpdateableComponent
	{
		private static Dictionary<string, string> maps = new Dictionary<string, string>
		{
			{ "rain", "\\map rain" },
			{ "dawn", "\\map dawn" },
			{ "forest", "\\map forest" },
			{ "monolith", "\\map monolith" },
			{ "fracture1", "\\map fracture" },
			{ "fortress", "\\map fortress" },
			{ "end", "\\map mark" },
		};

#if DEMO
		public const int MaxLevelIndex = 1;
#else
		public const int MaxLevelIndex = int.MaxValue;
#endif

		private const float messageFadeTime = 0.75f;
		private const float messageBackgroundOpacity = UIFactory.Opacity;

		private const float menuButtonWidth = 310.0f;
		private const float menuButtonLeftPadding = 30.0f;
		private const float animationSpeed = 2.5f;
		private const float hideAnimationSpeed = 5.0f;

		private List<Property<PCInput.PCInputBinding>> inputBindings = new List<Property<PCInput.PCInputBinding>>();

		private ListContainer messages;
		private ListContainer collectibleCounters;

		private PCInput input;

		public string Credits;

		private int displayModeIndex;

		public DisplayModeCollection SupportedDisplayModes;

		Property<UIComponent> currentMenu = new Property<UIComponent> { Value = null };

		public Property<bool> Showing = new Property<bool>();

		public Property<bool> CanPause = new Property<bool> { Value = true };

		// Settings to be restored when unpausing
		private float originalBlurAmount = 0.0f;
		private bool originalMouseVisible;
		private bool originalUIMouseVisible;
		private Point originalMousePosition = new Point();
		private float lastGamepadMove;
		private float lastGamepadScroll;

		public void ClearMessages()
		{
			this.messages.Children.Clear();
			this.collectibleCounters.Children.Clear();
		}

		public Container BuildMessage(string text = null, float width = 250.0f)
		{
			Container msgBackground = new Container();

			msgBackground.Tint.Value = Color.Black;
			msgBackground.Opacity.Value = messageBackgroundOpacity;
			TextElement msg = new TextElement();
			if (!string.IsNullOrEmpty(text))
				msg.Text.Value = text;
			msg.FontFile.Value = this.main.Font;
			msg.WrapWidth.Value = width;
			msgBackground.Children.Add(msg);
			return msgBackground;
		}

		public Container ShowMessage(Entity entity, Func<string> text, params IProperty[] properties)
		{
			Container container = this.BuildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Add(new Binding<string>(textElement.Text, text, properties));

			this.messages.Children.Add(container);

			this.animateMessage(entity, container);

			return container;
		}

		private void animateMessage(Entity entity, Container container, bool enabledWhenPaused = false)
		{
			container.CheckLayout();
			Vector2 originalSize = container.Size;
			container.ResizeVertical.Value = false;
			container.EnableScissor.Value = true;
			container.Size.Value = new Vector2(originalSize.X, 0);

			Animation anim = new Animation
			(
				new Animation.Ease(new Animation.Vector2MoveTo(container.Size, originalSize, messageFadeTime), Animation.Ease.EaseType.OutExponential),
				new Animation.Set<bool>(container.ResizeVertical, true)
			);

			container.UserData.Value = anim;

			anim.EnabledWhenPaused = enabledWhenPaused;
			if (entity == null)
				this.main.AddComponent(anim);
			else
				entity.Add(anim);
		}

		public Container ShowMessage(Entity entity, string text)
		{
			Container container = this.BuildMessage(text);

			this.messages.Children.Add(container);

			this.animateMessage(entity, container);

			return container;
		}

		public Container ShowMessageFormat(Entity entity, string text, params object[] properties)
		{
			if (text[0] == '\\')
			{
				return this.ShowMessage
				(
					entity,
					delegate()
					{
						return string.Format(main.Strings.Get(text.Substring(1)) ?? text.Substring(1), properties);
					},
					this.main.Strings.Language
				);
			}
			else
				return this.ShowMessage(entity, string.Format(text, properties));
		}

		public void HideMessage(Entity entity, Container container, float delay = 0.0f)
		{
			if (container != null && container.Active)
			{
				container.CheckLayout();
				Animation anim = null;
				anim = new Animation
				(
					new Animation.Delay(delay),
					new Animation.Execute(delegate()
					{
						Animation existingAnimation = container.UserData.Value as Animation;
						if (existingAnimation != null && existingAnimation.Active)
							existingAnimation.Delete.Execute();
						container.UserData.Value = anim;
					}),
					new Animation.Set<bool>(container.ResizeVertical, false),
					new Animation.Ease(new Animation.Vector2MoveTo(container.Size, new Vector2(container.Size.Value.X, 0), messageFadeTime), Animation.Ease.EaseType.OutExponential),
					new Animation.Execute(container.Delete)
				);

				if (entity == null)
				{
					anim.EnabledWhenPaused = false;
					this.main.AddComponent(anim);
				}
				else
					entity.Add(anim);
			}
		}

		public void RemoveSaveGame(string filename)
		{
			UIComponent container = this.loadSaveList.Children.FirstOrDefault(x => ((string)x.UserData.Value) == filename);
			if (container != null)
				container.Delete.Execute();
		}

		public void AddSaveGame(string timestamp)
		{
			Main.SaveInfo info = null;
			string thumbnailPath = null;
			try
			{
				using (Stream fs = new FileStream(Path.Combine(this.main.SaveDirectory, timestamp, "save.dat"), FileMode.Open, FileAccess.Read, FileShare.None))
				using (Stream stream = new GZipInputStream(fs))
				using (StreamReader reader = new StreamReader(stream))
					info = JsonConvert.DeserializeObject<Main.SaveInfo>(reader.ReadToEnd());

				if (info.Version != Main.MapVersion)
					throw new Exception();
				thumbnailPath = Path.Combine(this.main.SaveDirectory, timestamp, "thumbnail.png");
			}
			catch (Exception) // Incompatible version. Ignore.
			{
				return;
			}

			UIComponent container = this.main.UIFactory.CreateButton();
			container.UserData.Value = timestamp;

			ListContainer layout = new ListContainer();
			layout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			container.Children.Add(layout);

			if (File.Exists(thumbnailPath))
			{
				Sprite sprite = new Sprite();
				sprite.IsStandardImage.Value = true;
				sprite.Image.Value = thumbnailPath;
				layout.Children.Add(sprite);
			}

			TextElement label = new TextElement();
			label.FontFile.Value = this.main.Font;
			label.Text.Value = timestamp;
			layout.Children.Add(label);

			container.Add(new CommandBinding(container.MouseLeftUp, delegate()
			{
				if (this.saveMode)
				{
					this.ShowDialog("\\overwrite prompt", "\\overwrite", delegate()
					{
						container.Delete.Execute();
						this.main.SaveOverwrite(timestamp);
						this.hideLoadSave();
						this.main.Paused.Value = false;
						this.restorePausedSettings();
					});
				}
				else
				{
					this.hideLoadSave();
					this.main.Paused.Value = false;
					this.restorePausedSettings();
					this.main.CurrentSave.Value = timestamp;
					IO.MapLoader.Load(this.main, info.MapFile);
				}
				container.SwallowCurrentMouseEvent();
			}));

			this.loadSaveList.Children.Insert(1, container);
			this.loadSaveScroll.ScrollToTop();
		}

		private ListContainer challengeMenu;
		private ListContainer leaderboardMenu;
		private ListContainer leaderboardView;

		private ListContainer pauseMenu;
		private ListContainer notifications;

		private ListContainer loadSaveMenu;
		private ListContainer loadSaveList;
		private Scroller loadSaveScroll;
		private bool loadSaveShown;
		private Animation loadSaveAnimation;
		private Property<bool> saveMode = new Property<bool> { Value = false };

		private Animation pauseAnimation;
		private Container dialog;

		private void hidePauseMenu()
		{
			AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UI_SWOOSH);
			if (this.pauseAnimation != null)
				this.pauseAnimation.Delete.Execute();
			this.pauseAnimation = new Animation
			(
				new Animation.Vector2MoveToSpeed(this.pauseMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
				new Animation.Set<bool>(this.pauseMenu.Visible, false)
			);
			this.main.AddComponent(this.pauseAnimation);
			this.currentMenu.Value = null;
		}

		private void showPauseMenu()
		{
			AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UI_SWOOSH);
			if (this.dialog != null)
			{
				this.dialog.Delete.Execute();
				this.dialog = null;
			}
			this.pauseMenu.Visible.Value = true;
			if (this.pauseAnimation != null)
				this.pauseAnimation.Delete.Execute();
			this.pauseAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(this.pauseMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
			this.main.AddComponent(this.pauseAnimation);
			this.currentMenu.Value = this.pauseMenu;
		}

		// Pause
		private void savePausedSettings(bool initial = false)
		{
			if (!initial)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UI_SWOOSH);
			Session.Recorder.Event(main, "Pause");

			// Take screenshot
			Point size;
#if VR
			if (this.main.VR)
				size = this.main.VRActualScreenSize;
			else
#endif
				size = this.main.ScreenSize;
			this.main.Screenshot.Take(size);

			this.originalMouseVisible = this.main.IsMouseVisible;
			this.originalUIMouseVisible = this.main.UI.IsMouseVisible;
			this.main.UI.IsMouseVisible.Value = true;
			this.originalBlurAmount = this.main.Renderer.BlurAmount;

			// Save mouse position
			MouseState mouseState = this.main.MouseState;
			this.originalMousePosition = new Point(mouseState.X, mouseState.Y);

			this.pauseMenu.Visible.Value = true;
			this.pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);

			// Blur the screen and show the pause menu
			if (this.pauseAnimation != null && this.pauseAnimation.Active)
				this.pauseAnimation.Delete.Execute();

			float blurAmount;
#if VR
			if (this.main.VR)
				blurAmount = 0.0f;
			else
#endif
			{
				if (this.main.MapFile.Value == Main.MenuMap)
					blurAmount = 0.0f;
				else
					blurAmount = 1.0f;
			}

			this.pauseAnimation = new Animation
			(
				new Animation.Parallel
				(
					new Animation.FloatMoveToSpeed(this.main.Renderer.BlurAmount, blurAmount, 1.0f),
					new Animation.Ease
					(
						new Animation.Parallel
						(
							new Animation.Vector2MoveToSpeed(this.pauseMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
							new Animation.FloatMoveToSpeed(this.main.PauseAudioEffect, this.main.MapFile == Main.MenuMap ? 0.0f : 1.0f, Menu.animationSpeed)
						),
						Animation.Ease.EaseType.OutExponential
					)
				)
			);
			this.main.AddComponent(this.pauseAnimation);

			if (!this.main.EditorEnabled)
			{
				int orbs_collected = Collectible.Collectibles.Count(x => x.PickedUp);
				int orbs_total = Collectible.Collectibles.Count;

				int notes_collected = Note.Notes.Where(x => x.IsCollected).Count();
				int notes_total = Note.Notes.Count;

				if (orbs_collected < orbs_total)
				{
					Container container = this.BuildMessage();
					TextElement textElement = (TextElement)container.Children[0];

					textElement.Add(new Binding<string>
					(
						textElement.Text,
						delegate()
						{
							return string.Format(main.Strings.Get("orbs collected") ?? "{0} / {1} orbs collected", orbs_collected, orbs_total);
						},
						this.main.Strings.Language
					));
						
					this.collectibleCounters.Children.Add(container);
					this.animateMessage(null, container, enabledWhenPaused: true);
				}

				if (notes_collected < notes_total)
				{
					Container container = this.BuildMessage();
					TextElement textElement = (TextElement)container.Children[0];

					textElement.Add(new Binding<string>
					(
						textElement.Text,
						delegate()
						{
							return string.Format(main.Strings.Get("notes read") ?? "{0} / {1} notes read", notes_collected, notes_total);
						},
						this.main.Strings.Language
					));
						
					this.collectibleCounters.Children.Add(container);
					this.animateMessage(null, container, enabledWhenPaused: true);
				}
			}

			this.currentMenu.Value = this.pauseMenu;
		}

		// Unpause
		private void restorePausedSettings()
		{
			Session.Recorder.Event(main, "Unpause");

			if (this.pauseAnimation != null && this.pauseAnimation.Active)
				this.pauseAnimation.Delete.Execute();

			// Restore mouse
			if (!this.originalMouseVisible)
			{
				// Only restore mouse position if the cursor was not visible
				// i.e., we're in first-person camera mode
				Microsoft.Xna.Framework.Input.Mouse.SetPosition(this.originalMousePosition.X, this.originalMousePosition.Y);
				MouseState m = new MouseState(this.originalMousePosition.X, this.originalMousePosition.Y, this.main.MouseState.Value.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
				this.main.LastMouseState.Value = m;
				this.main.MouseState.Value = m;
			}
			this.main.UI.IsMouseVisible.Value = this.originalUIMouseVisible;
			this.main.IsMouseVisible = this.originalMouseVisible;

			this.main.SaveSettings();

			// Unlur the screen and show the pause menu
			if (this.pauseAnimation != null && this.pauseAnimation.Active)
				this.pauseAnimation.Delete.Execute();

			this.main.Renderer.BlurAmount.Value = originalBlurAmount;
			this.pauseAnimation = new Animation
			(
				new Animation.Parallel
				(
					new Animation.Vector2MoveToSpeed(this.pauseMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.FloatMoveToSpeed(this.main.PauseAudioEffect, 0.0f, Menu.hideAnimationSpeed)
				),
				new Animation.Set<bool>(this.pauseMenu.Visible, false)
			);
			this.main.AddComponent(this.pauseAnimation);

			this.main.Screenshot.Clear();

			this.currentMenu.Value = null;

			this.collectibleCounters.Children.Clear();
		}

		private float width
		{
			get
			{
				return Menu.menuButtonWidth * this.main.FontMultiplier + Menu.menuButtonLeftPadding + 4.0f;
			}
		}

		private float spacing
		{
			get
			{
				return 8.0f * this.main.FontMultiplier;
			}
		}

		private void resizeToMenu(Container c)
		{
			c.ResizeHorizontal.Value = false;
			c.Size.Value = new Vector2(this.width, 0.0f);
			c.PaddingLeft.Value = Menu.menuButtonLeftPadding;
		}

		public void EnableInput(bool enable)
		{
			if (this.currentMenu.Value != null && this.dialog == null)
				this.currentMenu.Value.EnableInput.Value = enable;
		}

		public void ShowDialog(string question, string action, Action callback, string alternate = "\\cancel", Action alternateCallback = null)
		{
			if (this.dialog != null)
				this.dialog.Delete.Execute();
			this.EnableInput(false);
			this.dialog = new Container();
			this.dialog.Tint.Value = Color.Black;
			this.dialog.Opacity.Value = UIFactory.Opacity;
			this.dialog.AnchorPoint.Value = new Vector2(0.5f);
			this.dialog.Add(new Binding<Vector2, Point>(this.dialog.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.main.ScreenSize));
			this.dialog.Add(new CommandBinding(this.dialog.Delete, delegate()
			{
				this.dialog = null;
				this.EnableInput(true);
			}));
			this.main.UI.Root.Children.Add(this.dialog);

			ListContainer dialogLayout = new ListContainer();
			dialogLayout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			this.dialog.Children.Add(dialogLayout);

			TextElement prompt = new TextElement();
			prompt.FontFile.Value = this.main.Font;
			prompt.Text.Value = question;
			dialogLayout.Children.Add(prompt);

			ListContainer dialogButtons = new ListContainer();
			dialogButtons.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			dialogLayout.Children.Add(dialogButtons);

			UIComponent okay = this.main.UIFactory.CreateButton("", delegate()
			{
				this.dialog.Delete.Execute();
				callback();
			});
			TextElement okayText = (TextElement)okay.GetChildByName("Text");
			string actionGamepad = string.Format("{0} gamepad", action);
			okayText.Add(new Binding<string, bool>(okayText.Text, x => x ? actionGamepad : action, this.main.GamePadConnected));
			okay.Name.Value = "Okay";
			dialogButtons.Children.Add(okay);

			UIComponent cancel = this.main.UIFactory.CreateButton(alternate, delegate()
			{
				this.dialog.Delete.Execute();
				if (alternateCallback != null)
					alternateCallback();
			});
			dialogButtons.Children.Add(cancel);

			TextElement cancelText = (TextElement)cancel.GetChildByName("Text");
			string alternateGamepad = string.Format("{0} gamepad", alternate);
			cancelText.Add(new Binding<string, bool>(cancelText.Text, x => x ? alternateGamepad : alternate, this.main.GamePadConnected));
		}

		private void hideLoadSave()
		{
			this.showPauseMenu();

			this.loadSaveShown = false;

			if (this.loadSaveAnimation != null)
				this.loadSaveAnimation.Delete.Execute();
			this.loadSaveAnimation = new Animation
			(
				new Animation.Vector2MoveToSpeed(this.loadSaveMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
				new Animation.Set<bool>(this.loadSaveMenu.Visible, false)
			);
			this.main.AddComponent(this.loadSaveAnimation);
		}

		public void SetupDisplayModes(DisplayModeCollection supportedDisplayModes, int displayModeIndex)
		{
			this.SupportedDisplayModes = supportedDisplayModes;
			this.displayModeIndex = displayModeIndex;
		}

		private Animation challengeAnimation = null;
		private Animation leaderboardAnimation = null;
		private Animation leaderboardViewAnimation = null;
		private Animation officialAnimation = null;
		private bool challengeMenuShown = false;
		private Action<bool> hideChallenge = null;
		private Action<bool> hideChallengeMenu = null;
		private Property<bool> leaderboardActive = new Property<bool>();
		private LeaderboardProxy leaderboardProxy = new LeaderboardProxy();
		private bool leaderboardOfficialMaps;
		public void ConstructChallengeMenu()
		{
			#region Root Challenge Menu
			this.challengeMenu = new ListContainer();
			this.challengeMenu.Visible.Value = false;
			this.challengeMenu.Add(new Binding<Vector2, Point>(this.challengeMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			this.challengeMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(this.challengeMenu);
			this.challengeMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container labelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(labelPadding);
			this.challengeMenu.Children.Add(labelPadding);

			ListContainer challengeLabelContainer = new ListContainer();
			challengeLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			labelPadding.Children.Add(challengeLabelContainer);

			TextElement challengeLabel = new TextElement();
			challengeLabel.FontFile.Value = this.main.Font;
			challengeLabel.Text.Value = "\\challenge title";
			challengeLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			challengeLabelContainer.Children.Add(challengeLabel);

			TextElement challengeWarning = new TextElement();
			challengeWarning.FontFile.Value = this.main.Font;
			challengeWarning.Text.Value = "\\challenge warning";
			challengeWarning.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			challengeLabelContainer.Children.Add(challengeWarning);

			this.hideChallengeMenu = delegate(bool showPrev)
			{
				if (showPrev)
					this.showPauseMenu();

				if (challengeAnimation != null)
					challengeAnimation.Delete.Execute();
				challengeAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(this.challengeMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(this.challengeMenu.Visible, false)
				);
				this.main.AddComponent(challengeAnimation);
				this.challengeMenuShown = false;
				this.hideChallenge = null;
			};

			this.hideChallenge = hideChallengeMenu;

			Container challengeButton = this.main.UIFactory.CreateButton("\\challenge levels", ShowChallengeMenu);
			this.resizeToMenu(challengeButton);
			challengeButton.Add(new Binding<bool, string>(challengeButton.Visible, x => x == Main.MenuMap || this.main.IsChallengeMap(x), this.main.MapFile));
			this.pauseMenu.Children.Add(challengeButton);

			Container challengeBack = this.main.UIFactory.CreateButton("\\back", () => hideChallengeMenu(true));
			this.resizeToMenu(challengeBack);
			this.challengeMenu.Children.Add(challengeBack);
			#endregion

			#region Leaderboard menu
			this.leaderboardMenu = new ListContainer();
			this.leaderboardMenu.Visible.Value = false;
			this.leaderboardMenu.Add(new Binding<Vector2, Point>(this.leaderboardMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			this.leaderboardMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(this.leaderboardMenu);
			this.leaderboardMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container leaderboardLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(leaderboardLabelPadding);
			this.leaderboardMenu.Children.Add(leaderboardLabelPadding);

			ListContainer leaderboardLabelContainer = new ListContainer();
			leaderboardLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			leaderboardLabelPadding.Children.Add(leaderboardLabelContainer);

			TextElement leaderboardLabel = new TextElement();
			leaderboardLabel.FontFile.Value = this.main.Font;
			leaderboardLabel.Text.Value = "\\leaderboard title";
			leaderboardLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			leaderboardLabelContainer.Children.Add(leaderboardLabel);

			Action<bool> hideLeaderboardMenu = delegate(bool showPrev)
			{
				if (this.leaderboardAnimation != null)
					this.leaderboardAnimation.Delete.Execute();
				this.leaderboardAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(this.leaderboardMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(this.leaderboardMenu.Visible, false)
				);
				this.main.AddComponent(this.leaderboardAnimation);

				this.challengeMenuShown = showPrev;
				if (showPrev)
					ShowChallengeMenu();
			};

			Action showLeaderboardMenu = delegate()
			{
				hideChallengeMenu(false);
				this.hidePauseMenu();

				this.challengeMenuShown = true;

				this.leaderboardMenu.Visible.Value = true;
				if (this.leaderboardAnimation != null)
					this.leaderboardAnimation.Delete.Execute();
				this.leaderboardAnimation = new Animation
				(
					new Animation.Ease
					(
						new Animation.Vector2MoveToSpeed(this.leaderboardMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
						Animation.Ease.EaseType.OutExponential
					)
				);
				this.main.AddComponent(this.leaderboardAnimation);
				this.currentMenu.Value = this.leaderboardMenu;
				this.hideChallenge = hideLeaderboardMenu;
			};

			Container leaderboardBack = this.main.UIFactory.CreateButton("\\back", () => hideLeaderboardMenu(true));
			this.resizeToMenu(leaderboardBack);
			this.leaderboardMenu.Children.Add(leaderboardBack);
			#endregion

			#region Leaderboard display
			this.leaderboardView = new ListContainer();
			this.leaderboardView.Visible.Value = false;
			this.leaderboardView.Add(new Binding<Vector2, Point>(this.leaderboardView.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			this.leaderboardView.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(this.leaderboardView);
			this.leaderboardView.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container leaderboardViewLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(leaderboardViewLabelPadding);
			this.leaderboardView.Children.Add(leaderboardViewLabelPadding);

			ListContainer leaderboardViewLabelContainer = new ListContainer();
			leaderboardViewLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			leaderboardViewLabelPadding.Children.Add(leaderboardViewLabelContainer);

			TextElement leaderboardViewLabel = new TextElement();
			leaderboardViewLabel.FontFile.Value = this.main.Font;
			leaderboardViewLabel.Text.Value = "\\leaderboard title";
			leaderboardViewLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			leaderboardViewLabelContainer.Children.Add(leaderboardViewLabel);

			TextElement leaderboardViewMapTitle = new TextElement();
			leaderboardViewMapTitle.FontFile.Value = this.main.Font;
			leaderboardViewMapTitle.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			leaderboardViewLabelContainer.Children.Add(leaderboardViewMapTitle);

			TextElement leaderboardViewScrollLabel = new TextElement();
			leaderboardViewScrollLabel.FontFile.Value = this.main.Font;
			leaderboardViewScrollLabel.Text.Value = "\\scroll for more";
			leaderboardViewScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			leaderboardViewLabelContainer.Children.Add(leaderboardViewScrollLabel);

			Action<bool> hideLeaderboardView = null;
			Action showOfficialMenu = null, showWorkshopMenu = null;

			Container leaderboardViewBack = this.main.UIFactory.CreateButton("\\back", () => hideLeaderboardView(true));
			this.resizeToMenu(leaderboardViewBack);
			this.leaderboardView.Children.Add(leaderboardViewBack);

			ListContainer leaderboardViewList = new ListContainer();
			leaderboardViewList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Scroller leaderboardViewScroller = new Scroller();
			leaderboardViewScroller.Children.Add(leaderboardViewList);
			leaderboardViewScroller.Add(new Binding<Vector2>(leaderboardViewScroller.Size, () => new Vector2(leaderboardViewList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), leaderboardViewList.Size, this.main.ScreenSize));
			this.leaderboardView.Children.Add(leaderboardViewScroller);

			hideLeaderboardView = delegate(bool showPrev)
			{
				this.leaderboardProxy.CancelCallbacks();
				if (this.leaderboardViewAnimation != null)
					this.leaderboardViewAnimation.Delete.Execute();
				this.leaderboardViewAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(this.leaderboardView.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(this.leaderboardView.Visible, false)
				);
				this.main.AddComponent(this.leaderboardViewAnimation);

				this.challengeMenuShown = showPrev;
				if (showPrev)
				{
					if (this.leaderboardOfficialMaps)
						showOfficialMenu();
					else
						showWorkshopMenu();
				}
			};

			this.Add(new CommandBinding(this.leaderboardProxy.OnLeaderboardError, delegate()
			{
				leaderboardViewList.Children.Clear();
				TextElement error = this.main.UIFactory.CreateLabel();
				error.Text.Value = "\\leaderboard error";
				leaderboardViewList.Children.Add(error);;
			}));

			this.Add(new CommandBinding<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t>(this.leaderboardProxy.OnLeaderboardSync, delegate(LeaderboardScoresDownloaded_t globalScores, LeaderboardScoresDownloaded_t friendScores)
			{
				leaderboardViewList.Children.Clear();

				int[] details = new int[] {};
				for (int i = 0; i < globalScores.m_cEntryCount; i++)
				{
					LeaderboardEntry_t entry;
					SteamUserStats.GetDownloadedLeaderboardEntry(globalScores.m_hSteamLeaderboardEntries, i, out entry, details, 0);
					leaderboardViewList.Children.Add(this.leaderboardEntry(entry));
				}

				if (friendScores.m_cEntryCount > 1)
				{
					{
						TextElement friendsLabel = this.main.UIFactory.CreateLabel("\\friends");
						Container labelContainer = this.main.UIFactory.CreateContainer();
						this.resizeToMenu(labelContainer);
						friendsLabel.Position.Value = new Vector2(labelContainer.Size.Value.X * 0.5f, 0.0f);
						friendsLabel.AnchorPoint.Value = new Vector2(0.5f, 0.0f);
						leaderboardViewList.Children.Add(labelContainer);
						labelContainer.Children.Add(friendsLabel);
					}

					for (int i = 0; i < friendScores.m_cEntryCount; i++)
					{
						LeaderboardEntry_t entry;
						SteamUserStats.GetDownloadedLeaderboardEntry(friendScores.m_hSteamLeaderboardEntries, i, out entry, details, 0);
						leaderboardViewList.Children.Add(this.leaderboardEntry(entry));
					}
				}
			}));

			Action<string, string> showLeaderboardView = delegate(string name, string uuid)
			{
				hideLeaderboardMenu(false);
				leaderboardViewMapTitle.Text.Value = name;

				leaderboardViewList.Children.Clear();

				TextElement loading = this.main.UIFactory.CreateLabel();
				loading.Text.Value = "\\loading";

				Container loadingContainer = this.main.UIFactory.CreateContainer();
				this.resizeToMenu(loadingContainer);
				loadingContainer.Children.Add(loading);
				loading.Position.Value = new Vector2(menuButtonLeftPadding, 0);
				loading.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
				leaderboardViewList.Children.Add(loadingContainer);

				this.leaderboardProxy.Sync(uuid);

				this.challengeMenuShown = true;

				this.leaderboardView.Visible.Value = true;
				if (this.leaderboardViewAnimation != null)
					this.leaderboardViewAnimation.Delete.Execute();
				this.leaderboardViewAnimation = new Animation
				(
					new Animation.Ease
					(
						new Animation.Vector2MoveToSpeed(this.leaderboardView.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
						Animation.Ease.EaseType.OutExponential
					)
				);
				this.main.AddComponent(this.leaderboardViewAnimation);
				this.currentMenu.Value = leaderboardViewList;
				this.hideChallenge = hideLeaderboardView;
			};
			#endregion

			#region Official Maps
			ListContainer officialMapsMenu = new ListContainer();
			officialMapsMenu.Visible.Value = false;
			officialMapsMenu.Add(new Binding<Vector2, Point>(officialMapsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			officialMapsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(officialMapsMenu);
			officialMapsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container officialLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(officialLabelPadding);
			officialMapsMenu.Children.Add(officialLabelPadding);

			ListContainer officialLabelContainer = new ListContainer();
			officialLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			officialLabelPadding.Children.Add(officialLabelContainer);

			TextElement officialLabel = new TextElement();
			officialLabel.FontFile.Value = this.main.Font;
			officialLabel.Add(new Binding<string, bool>(officialLabel.Text, x => x ? "\\leaderboard title" : "\\challenge title", this.leaderboardActive));
			officialLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			officialLabelContainer.Children.Add(officialLabel);

			TextElement officialLabel2 = new TextElement();
			officialLabel2.FontFile.Value = this.main.Font;
			officialLabel2.Text.Value = "\\official levels";
			officialLabel2.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			officialLabelContainer.Children.Add(officialLabel2);

			TextElement officialScrollLabel = new TextElement();
			officialScrollLabel.FontFile.Value = this.main.Font;
			officialScrollLabel.Text.Value = "\\scroll for more";
			officialScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			officialLabelContainer.Children.Add(officialScrollLabel);

			Action<bool> hideOfficialMenu = delegate(bool showPrev)
			{
				if (officialAnimation != null)
					officialAnimation.Delete.Execute();
				officialAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(officialMapsMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(officialMapsMenu.Visible, false)
				);
				this.main.AddComponent(officialAnimation);

				this.challengeMenuShown = showPrev;
				if (showPrev)
				{
					if (this.leaderboardActive)
						showLeaderboardMenu();
					else
						ShowChallengeMenu();
				}
			};

			ListContainer officialMapsList = new ListContainer();
			officialMapsList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			showOfficialMenu = delegate()
			{
				this.hidePauseMenu();

				challengeMenuShown = true;
				this.hideChallenge = hideOfficialMenu;

				officialMapsMenu.Visible.Value = true;
				if (officialAnimation != null)
					officialAnimation.Delete.Execute();
				officialAnimation = new Animation
				(
					new Animation.Ease
					(
						new Animation.Vector2MoveToSpeed(officialMapsMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
						Animation.Ease.EaseType.OutExponential
					)
				);
				this.main.AddComponent(officialAnimation);
				this.currentMenu.Value = officialMapsList;
			};

#if STEAMWORKS
			Container officialLeaderboard = this.main.UIFactory.CreateButton("\\official levels", delegate()
			{
				hideLeaderboardMenu(false);
				this.leaderboardActive.Value = true;
				this.leaderboardOfficialMaps = true;
				showOfficialMenu();
			});
			this.resizeToMenu(officialLeaderboard);
			this.leaderboardMenu.Children.Add(officialLeaderboard);
#endif

			Container officialBack = this.main.UIFactory.CreateButton("\\back", () => hideOfficialMenu(true));
			this.resizeToMenu(officialBack);
			officialMapsMenu.Children.Add(officialBack);

			Scroller officialMapScroller = new Scroller();
			officialMapScroller.Children.Add(officialMapsList);
			officialMapScroller.Add(new Binding<Vector2>(officialMapScroller.Size, () => new Vector2(officialMapsList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), officialMapsList.Size, this.main.ScreenSize));
			officialMapsMenu.Children.Add(officialMapScroller);

			DirectoryInfo dir = new DirectoryInfo(Path.Combine(this.main.MapDirectory, "Challenge"));
			if (dir.Exists)
			{
				foreach (var file in dir.GetFiles("*.map"))
				{
					string display = Path.GetFileNameWithoutExtension(file.Name);
					Container button = this.main.UIFactory.CreateButton(display, delegate()
					{
						hideOfficialMenu(false);
						if (this.leaderboardActive)
						{
							string uuid;
							using (Stream fs = File.OpenRead(file.FullName))
							{
								using (Stream stream = new GZipInputStream(fs))
								{
									uuid = ((List<Entity>)IO.MapLoader.Serializer.Deserialize(stream))[0].Get<World>().UUID;
								}
							}
							showLeaderboardView(display, uuid);
						}
						else
						{
							hidePauseMenu();
							this.main.Paused.Value = false;
							this.restorePausedSettings();
							this.main.CurrentSave.Value = null;
							this.main.AddComponent(new Animation
							(
								new Animation.Delay(0.2f),
								new Animation.Execute(delegate()
								{
									IO.MapLoader.Load(this.main, file.FullName);
								})
							));
						}
					});
					this.resizeToMenu(button);
					officialMapsList.Children.Add(button);
				}
			}

			Container officialMaps = this.main.UIFactory.CreateButton("\\official levels", delegate()
			{
				hideChallengeMenu(false);
				this.leaderboardActive.Value = false;
				showOfficialMenu();
			});
			this.resizeToMenu(officialMaps);
			this.challengeMenu.Children.Add(officialMaps);

			#endregion

			#region Workshop Maps
			ListContainer workshopMapsMenu = new ListContainer();
			workshopMapsMenu.Visible.Value = false;
			workshopMapsMenu.Add(new Binding<Vector2, Point>(workshopMapsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			workshopMapsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(workshopMapsMenu);
			workshopMapsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container workshopLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(workshopLabelPadding);
			workshopMapsMenu.Children.Add(workshopLabelPadding);

			ListContainer workshopLabelContainer = new ListContainer();
			workshopLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			workshopLabelPadding.Children.Add(workshopLabelContainer);

			TextElement workshopLabel = new TextElement();
			workshopLabel.FontFile.Value = this.main.Font;
			workshopLabel.Add(new Binding<string, bool>(workshopLabel.Text, x => x ? "\\leaderboard title" : "\\challenge title", this.leaderboardActive));
			workshopLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			workshopLabelContainer.Children.Add(workshopLabel);

			TextElement workshopLabel2 = new TextElement();
			workshopLabel2.FontFile.Value = this.main.Font;
			workshopLabel2.Text.Value = "\\workshop levels";
			workshopLabel2.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			workshopLabelContainer.Children.Add(workshopLabel2);

			TextElement workshopScrollLabel = new TextElement();
			workshopScrollLabel.FontFile.Value = this.main.Font;
			workshopScrollLabel.Text.Value = "\\scroll for more";
			workshopScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			workshopLabelContainer.Children.Add(workshopScrollLabel);

			Action<bool> hideWorkshopMenu = delegate(bool showPrev)
			{
				if (officialAnimation != null)
					officialAnimation.Delete.Execute();
				officialAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(workshopMapsMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(workshopMapsMenu.Visible, false)
				);
				this.main.AddComponent(officialAnimation);
				this.challengeMenuShown = showPrev;
				if (showPrev)
				{
					if (this.leaderboardActive)
						showLeaderboardMenu();
					else
						ShowChallengeMenu();
				}
			};

			ListContainer workshopMapsList = new ListContainer();
			workshopMapsList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			showWorkshopMenu = delegate()
			{
				this.hidePauseMenu();

				challengeMenuShown = true;
				this.hideChallenge = hideWorkshopMenu;

				workshopMapsMenu.Visible.Value = true;
				if (officialAnimation != null)
					officialAnimation.Delete.Execute();
				officialAnimation = new Animation
				(
					new Animation.Ease
					(
						new Animation.Vector2MoveToSpeed(workshopMapsMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
						Animation.Ease.EaseType.OutExponential
					)
				);
				this.main.AddComponent(officialAnimation);
				this.currentMenu.Value = workshopMapsList;
			};
			Container workshopGetMore = this.main.UIFactory.CreateButton("\\get more", delegate()
			{
				UIFactory.OpenURL(string.Format("http://steamcommunity.com/workshop/browse?appid={0}", Main.SteamAppID));
			});
			workshopGetMore.Add(new Binding<bool>(workshopGetMore.Visible, x => !x, this.leaderboardActive));
			this.resizeToMenu(workshopGetMore);
			workshopMapsList.Children.Add(workshopGetMore);

			Container workshopBack = this.main.UIFactory.CreateButton("\\back", () => hideWorkshopMenu(true));
			this.resizeToMenu(workshopBack);
			workshopMapsMenu.Children.Add(workshopBack);

			Scroller workshopMapsScroller = new Scroller();
			workshopMapsScroller.Children.Add(workshopMapsList);
			workshopMapsScroller.Add(new Binding<Vector2>(workshopMapsScroller.Size, () => new Vector2(workshopMapsList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), workshopMapsList.Size, this.main.ScreenSize));
			workshopMapsMenu.Children.Add(workshopMapsScroller);

			Action reloadMaps = delegate()
			{
				while (workshopMapsList.Children.Count > 1)
					workshopMapsList.Children.RemoveAt(workshopMapsList.Children.Count - 1);
				DirectoryInfo workshopDir = SteamWorker.DownloadedMaps;
				if (workshopDir.Exists)
				{
					foreach (var subDir in workshopDir.GetDirectories())
					{
						SteamWorker.WorkshopMapMetadata	metadata = null;
						try
						{
							metadata = JsonConvert.DeserializeObject<SteamWorker.WorkshopMapMetadata>(File.ReadAllText(Path.Combine(subDir.FullName, "meta.json")));
						}
						catch (Exception)
						{

						}

						if (metadata != null)
						{
							string mapPath = subDir.GetFiles(subDir.Name + IO.MapLoader.MapExtension)[0].FullName;
							Container button = this.main.UIFactory.CreateButton(metadata.Title, delegate()
							{
								hideWorkshopMenu(false);
								if (this.leaderboardActive)
								{
									string uuid;
									using (Stream fs = File.OpenRead(mapPath))
									{
										using (Stream stream = new GZipInputStream(fs))
										{
											uuid = ((List<Entity>)IO.MapLoader.Serializer.Deserialize(stream))[0].Get<World>().UUID;
										}
									}
									showLeaderboardView(metadata.Title, uuid);
								}
								else
								{
									this.hidePauseMenu();
									this.main.Paused.Value = false;
									this.restorePausedSettings();
									this.main.CurrentSave.Value = null;
									this.main.AddComponent(new Animation
									(
										new Animation.Delay(0.2f),
										new Animation.Execute(delegate()
										{
											IO.MapLoader.Load(this.main, mapPath);
										})
									));
								}
							});
							this.resizeToMenu(button);
							workshopMapsList.Children.Add(button);
						}
					}
				}
			};

			this.Add(new CommandBinding(SteamWorker.OnLevelDownloaded, delegate()
			{
				if (SteamWorker.Downloading == 0)
					this.HideMessage(null, this.ShowMessage(null, "\\workshop downloads complete"), 3.0f);
				reloadMaps();
			}));

#if STEAMWORKS
			Container workshopLeaderboard = this.main.UIFactory.CreateButton("\\workshop levels", delegate()
			{
				reloadMaps();
				hideLeaderboardMenu(false);
				this.leaderboardActive.Value = true;
				this.leaderboardOfficialMaps = false;
				showWorkshopMenu();
			});
			this.resizeToMenu(workshopLeaderboard);
			this.leaderboardMenu.Children.Add(workshopLeaderboard);

			Container workshopMaps = this.main.UIFactory.CreateButton("\\workshop levels", delegate()
			{
				reloadMaps();
				hideChallengeMenu(false);
				this.leaderboardActive.Value = false;
				showWorkshopMenu();
			});
			this.resizeToMenu(workshopMaps);
			this.challengeMenu.Children.Add(workshopMaps);

			Container leaderboardButton = this.main.UIFactory.CreateButton("\\leaderboard", showLeaderboardMenu);
			this.resizeToMenu(leaderboardButton);
			this.challengeMenu.Children.Add(leaderboardButton);

#endif

			Container levelEditor = this.main.UIFactory.CreateButton("\\level editor", delegate()
			{
				hideChallengeMenu(false);
				this.editMode();
			});
			this.resizeToMenu(levelEditor);
			this.challengeMenu.Children.Add(levelEditor);

			#endregion
		}

		public void ShowChallengeMenu()
		{
			this.hidePauseMenu();

			this.challengeMenuShown = true;

			this.challengeMenu.Visible.Value = true;
			if (challengeAnimation != null)
				challengeAnimation.Delete.Execute();
			challengeAnimation = new Animation
			(
				new Animation.Ease
				(
					new Animation.Vector2MoveToSpeed(this.challengeMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed),
					Animation.Ease.EaseType.OutExponential
				)
			);
			this.main.AddComponent(challengeAnimation);
			this.currentMenu.Value = this.challengeMenu;
			this.hideChallenge = this.hideChallengeMenu;
		}

		private Container leaderboardEntry(LeaderboardEntry_t entry)
		{
			Container container = this.main.UIFactory.CreateButton(delegate()
			{
				if (SteamWorker.SteamInitialized)
					SteamFriends.ActivateGameOverlayToUser("steamid", entry.m_steamIDUser);
			});
			this.resizeToMenu(container);

			TextElement rank = this.main.UIFactory.CreateLabel(entry.m_nGlobalRank.ToString());
			rank.AnchorPoint.Value = new Vector2(1, 0);
			rank.Position.Value = new Vector2(this.width * 0.15f, 0);
			container.Children.Add(rank);

			TextElement name = this.main.UIFactory.CreateLabel();
			name.FilterUnicode.Value = true;
			if (SteamFriends.RequestUserInformation(entry.m_steamIDUser, true))
			{
				// Need to wait for a callback before we know their username
				name.Add(new Binding<string>(name.Text, () => SteamFriends.GetFriendPersonaName(entry.m_steamIDUser), this.leaderboardProxy.PersonaNotification));
			}
			else
			{
				// We already know the username
				name.Text.Value = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
			}
			name.Position.Value = new Vector2(this.width * 0.15f + this.spacing, 0);
			container.Children.Add(name);

			TextElement score = this.main.UIFactory.CreateLabel(TimeTrialUI.SecondsToTimeString((float)entry.m_nScore / 1000.0f));
			score.AnchorPoint.Value = new Vector2(1, 0);
			score.Position.Value = new Vector2(this.width - 4.0f - this.spacing, 0);
			container.Children.Add(score);
			return container;
		}

		public override void Awake()
		{
			base.Awake();

			this.input = new PCInput();
			this.main.AddComponent(this.input);

			this.Add(new Binding<bool, UIComponent>(this.Showing, x => x != null, this.currentMenu));

			Log.Handler = delegate(string log)
			{
#if DEBUG
				this.HideMessage(null, this.ShowMessage(null, log), 2.0f);
				this.main.ConsoleUI.LogText(log);
#endif
				Session.Recorder.Event(main, "Log", log);
			};

			// Message list
			{
				bool vrMessagePlacement = false;
#if VR
				if (this.main.VR)
					vrMessagePlacement = true;
#endif
				this.messages = new ListContainer();
				this.messages.Alignment.Value = ListContainer.ListAlignment.Middle;
				this.messages.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
				Vector2 messagePlacement = new Vector2(0.5f, vrMessagePlacement ? 0.6f : 0.85f);
				this.messages.Add(new Binding<Vector2, Point>(this.messages.Position, x => new Vector2(x.X * messagePlacement.X, x.Y * messagePlacement.Y), this.main.ScreenSize));
				this.main.UI.Root.Children.Add(this.messages);
			}

			// Collectible counters
			{
				bool vrCounterPlacement = false;
#if VR
				if (this.main.VR)
					vrCounterPlacement = true;
#endif
				this.collectibleCounters = new ListContainer();
				this.collectibleCounters.Reversed.Value = true;
				this.collectibleCounters.Alignment.Value = ListContainer.ListAlignment.Middle;
				this.collectibleCounters.AnchorPoint.Value = new Vector2(0.5f, 0.0f);
				this.collectibleCounters.Add(new Binding<bool>(this.collectibleCounters.Visible, x => !x, ConsoleUI.Showing));
				Vector2 counterPlacement = new Vector2(0.5f, vrCounterPlacement ? 0.4f : 0.15f);
				this.collectibleCounters.Add(new Binding<Vector2, Point>(this.collectibleCounters.Position, x => new Vector2(x.X * counterPlacement.X, x.Y * counterPlacement.Y), this.main.ScreenSize));
				this.main.UI.Root.Children.Add(this.collectibleCounters);
			}

			{
				Container downloading = this.BuildMessage();
				downloading.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
				downloading.Add(new Binding<Vector2, Point>(downloading.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.1f), this.main.ScreenSize));
				TextElement downloadingLabel = (TextElement)downloading.Children[0];
				downloading.Add(new Binding<bool>(downloading.Visible, () => this.main.MapFile == Main.MenuMap && SteamWorker.Downloading > 0, this.main.MapFile, SteamWorker.Downloading));
				downloadingLabel.Add(new Binding<string>
				(
					downloadingLabel.Text,
					delegate()
					{
						return string.Format(main.Strings.Get("downloading workshop maps") ?? "downloading {0} workshop maps", SteamWorker.Downloading.Value);
					},
					this.main.Strings.Language, SteamWorker.Downloading
				));
				this.main.UI.Root.Children.Add(downloading);
			}

			this.notifications = new ListContainer();
			this.notifications.Alignment.Value = ListContainer.ListAlignment.Max;
			this.notifications.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
			this.notifications.Name.Value = "Notifications";
			this.notifications.Add(new Binding<Vector2, Point>(this.notifications.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.1f), this.main.ScreenSize));
			this.main.UI.Root.Children.Add(this.notifications);

			// Pause menu
			this.pauseMenu = new ListContainer();
			this.pauseMenu.Visible.Value = false;
			this.pauseMenu.Add(new Binding<Vector2, Point>(this.pauseMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			this.pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(this.pauseMenu);
			this.pauseMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			// Load / save menu
			this.loadSaveMenu = new ListContainer();
			this.loadSaveMenu.Visible.Value = false;
			this.loadSaveMenu.Add(new Binding<Vector2, Point>(this.loadSaveMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			this.loadSaveMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.loadSaveMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;
			this.main.UI.Root.Children.Add(this.loadSaveMenu);

			Container loadSavePadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(loadSavePadding);
			this.loadSaveMenu.Children.Add(loadSavePadding);

			ListContainer loadSaveLabelContainer = new ListContainer();
			loadSaveLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			loadSavePadding.Children.Add(loadSaveLabelContainer);

			TextElement loadSaveLabel = new TextElement();
			loadSaveLabel.FontFile.Value = this.main.Font;
			loadSaveLabel.Add(new Binding<string, bool>(loadSaveLabel.Text, x => x ? "\\save title" : "\\load title", this.saveMode));
			loadSaveLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			loadSaveLabelContainer.Children.Add(loadSaveLabel);

			TextElement loadSaveScrollLabel = new TextElement();
			loadSaveScrollLabel.FontFile.Value = this.main.Font;
			loadSaveScrollLabel.Text.Value = "\\scroll for more";
			loadSaveScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			loadSaveLabelContainer.Children.Add(loadSaveScrollLabel);

			TextElement quickSaveLabel = new TextElement();
			quickSaveLabel.FontFile.Value = this.main.Font;
			quickSaveLabel.Add(new Binding<bool>(quickSaveLabel.Visible, this.saveMode));
			quickSaveLabel.Text.Value = "\\quicksave instructions";
			quickSaveLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			loadSaveLabelContainer.Children.Add(quickSaveLabel);

			Container loadSaveBack = this.main.UIFactory.CreateButton("\\back", this.hideLoadSave);
			this.resizeToMenu(loadSaveBack);
			this.loadSaveMenu.Children.Add(loadSaveBack);

			this.loadSaveScroll = new Scroller();
			this.loadSaveScroll.Add(new Binding<Vector2, Point>(this.loadSaveScroll.Size, x => new Vector2(Menu.menuButtonWidth * this.main.FontMultiplier + Menu.menuButtonLeftPadding + 4.0f, x.Y * 0.5f), this.main.ScreenSize));
			this.loadSaveMenu.Children.Add(this.loadSaveScroll);

			this.loadSaveList = new ListContainer();
			this.loadSaveList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			this.loadSaveScroll.Children.Add(this.loadSaveList);

			Container saveNewButton = this.main.UIFactory.CreateButton("\\save new", delegate()
			{
				this.main.SaveNew();
				this.hideLoadSave();
				this.main.Paused.Value = false;
				this.restorePausedSettings();
			});
			this.resizeToMenu(saveNewButton);
			saveNewButton.Add(new Binding<bool>(saveNewButton.Visible, this.saveMode));
			this.loadSaveList.Children.Add(saveNewButton);

			foreach (string saveFile in Directory.GetDirectories(this.main.SaveDirectory, "*", SearchOption.TopDirectoryOnly).Select(x => Path.GetFileName(x)).OrderBy(x => x))
				this.AddSaveGame(saveFile);

			// Settings menu
			bool settingsShown = false;
			Animation settingsAnimation = null;

			Func<bool, string> boolDisplay = x => x ? "\\on" : "\\off";

			ListContainer settingsMenu = new ListContainer();
			settingsMenu.Visible.Value = false;
			settingsMenu.Add(new Binding<Vector2, Point>(settingsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			settingsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(settingsMenu);
			settingsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container settingsLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(settingsLabelPadding);
			settingsMenu.Children.Add(settingsLabelPadding);

			ListContainer settingsLabelContainer = new ListContainer();
			settingsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			settingsLabelPadding.Children.Add(settingsLabelContainer);

			TextElement settingsLabel = new TextElement();
			settingsLabel.FontFile.Value = this.main.Font;
			settingsLabel.Text.Value = "\\options title";
			settingsLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			settingsLabelContainer.Children.Add(settingsLabel);

			TextElement settingsScrollLabel = new TextElement();
			settingsScrollLabel.FontFile.Value = this.main.Font;
			settingsScrollLabel.Add(new Binding<string>(settingsScrollLabel.Text, delegate()
			{
				if (this.main.GamePadConnected)
					return "\\modify setting gamepad";
				else
					return "\\modify setting";
			}, this.main.GamePadConnected));
			settingsScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			settingsLabelContainer.Children.Add(settingsScrollLabel);

			TextElement settingsScrollLabel2 = new TextElement();
			settingsScrollLabel2.FontFile.Value = this.main.Font;
			settingsScrollLabel2.Text.Value = "\\scroll for more";
			settingsScrollLabel2.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			settingsLabelContainer.Children.Add(settingsScrollLabel2);

			Action hideSettings = delegate()
			{
				this.main.SaveSettings();
				this.showPauseMenu();

				settingsShown = false;

				if (settingsAnimation != null)
					settingsAnimation.Delete.Execute();
				settingsAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(settingsMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(settingsMenu.Visible, false)
				);
				this.main.AddComponent(settingsAnimation);
			};

			Container settingsBack = this.main.UIFactory.CreateButton("\\back", hideSettings);
			this.resizeToMenu(settingsBack);
			settingsMenu.Children.Add(settingsBack);

			ListContainer settingsList = new ListContainer();
			settingsList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Scroller settingsScroller = new Scroller();
			settingsScroller.Add(new Binding<Vector2>(settingsScroller.Size, () => new Vector2(settingsList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), settingsList.Size, this.main.ScreenSize));
			settingsScroller.Children.Add(settingsList);
			settingsMenu.Children.Add(settingsScroller);

			Container soundEffectVolume = this.main.UIFactory.CreateScrollButton<float>("\\sound effect volume", this.main.Settings.SoundEffectVolume, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Settings.SoundEffectVolume.Value = MathHelper.Clamp(this.main.Settings.SoundEffectVolume.Value + (delta * 0.1f), 0, 1);
			});
			this.resizeToMenu(soundEffectVolume);
			settingsList.Children.Add(soundEffectVolume);

			Container musicVolume = this.main.UIFactory.CreateScrollButton<float>("\\music volume", this.main.Settings.MusicVolume, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Settings.MusicVolume.Value = MathHelper.Clamp(this.main.Settings.MusicVolume.Value + (delta * 0.1f), 0, 1);
			});
			this.resizeToMenu(musicVolume);
			settingsList.Children.Add(musicVolume);

#if VR
			if (this.main.VR)
			{
				Container reticleEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\reticle", this.main.Settings.EnableReticleVR, boolDisplay, delegate(int delta)
				{
					this.main.Settings.EnableReticleVR.Value = !this.main.Settings.EnableReticleVR;
				});
				this.resizeToMenu(reticleEnabled);
				settingsList.Children.Add(reticleEnabled);
			}
			else
#endif
			{
				Container reticleEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\reticle", this.main.Settings.EnableReticle, boolDisplay, delegate(int delta)
				{
					this.main.Settings.EnableReticle.Value = !this.main.Settings.EnableReticle;
				});
				this.resizeToMenu(reticleEnabled);
				settingsList.Children.Add(reticleEnabled);
			}

			{
				Container minimizeHeadBob = this.main.UIFactory.CreateScrollButton<bool>("\\minimize head bob", this.main.MinimizeCameraMovement, boolDisplay, delegate(int delta)
				{
					this.main.MinimizeCameraMovement.Value = !this.main.MinimizeCameraMovement;
				});
				this.resizeToMenu(minimizeHeadBob);
				settingsList.Children.Add(minimizeHeadBob);
			}

			{
				Container waypointsEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\waypoints", this.main.Settings.EnableWaypoints, boolDisplay, delegate(int delta)
				{
					this.main.Settings.EnableWaypoints.Value = !this.main.Settings.EnableWaypoints;
				});
				this.resizeToMenu(waypointsEnabled);
				settingsList.Children.Add(waypointsEnabled);
			}

			Container fullscreenResolution = this.main.UIFactory.CreateScrollButton<Point>("\\fullscreen resolution", this.main.Settings.FullscreenResolution, x => x.X.ToString() + "x" + x.Y.ToString(), delegate(int delta)
			{
				displayModeIndex = (displayModeIndex + delta) % this.SupportedDisplayModes.Count();
				while (displayModeIndex < 0)
					displayModeIndex += this.SupportedDisplayModes.Count();
				DisplayMode mode = this.SupportedDisplayModes.ElementAt(displayModeIndex);
				this.main.Settings.FullscreenResolution.Value = new Point(mode.Width, mode.Height);
			});
			this.resizeToMenu(fullscreenResolution);
			settingsList.Children.Add(fullscreenResolution);

#if VR
			if (!this.main.VR)
#endif
			{
				Container borderless = this.main.UIFactory.CreateScrollButton<bool>("\\borderless", this.main.Settings.Borderless, boolDisplay, delegate(int delta)
				{
					Point res = this.main.ScreenSize;
					this.main.ResizeViewport(res.X, res.Y, this.main.Settings.Fullscreen, !this.main.Settings.Borderless);
				});
				this.resizeToMenu(borderless);
				settingsList.Children.Add(borderless);
			}

			Container vsyncEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\vsync", this.main.Settings.Vsync, boolDisplay, delegate(int delta)
			{
				this.main.Settings.Vsync.Value = !this.main.Settings.Vsync;
			});
			this.resizeToMenu(vsyncEnabled);
			settingsList.Children.Add(vsyncEnabled);

			Container applyResolution = this.main.UIFactory.CreateButton(null, delegate()
			{
				if (this.main.Settings.Fullscreen)
				{
					Point res = this.main.Settings.FullscreenResolution;
					this.main.ResizeViewport(res.X, res.Y, true, this.main.Settings.Borderless);
				}
				else
					this.main.EnterFullscreen();
			});
			applyResolution.Add(new Binding<string, bool>(((TextElement)applyResolution.GetChildByName("Text")).Text, x => x ? "\\apply resolution" : "\\enter fullscreen", this.main.Settings.Fullscreen));
			this.resizeToMenu(applyResolution);
			settingsList.Children.Add(applyResolution);

			Container fpsLimit = this.main.UIFactory.CreateScrollButton<int>("\\fps limit", this.main.Settings.FPSLimit, delegate(int delta)
			{
				this.main.Settings.FPSLimit.Value = Math.Max(20, this.main.Settings.FPSLimit + delta * 5);
			});
			this.resizeToMenu(fpsLimit);
			settingsList.Children.Add(fpsLimit);

			Container gamma = this.main.UIFactory.CreateScrollButton<float>("\\gamma", this.main.Renderer.Gamma, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Renderer.Gamma.Value = Math.Max(0, Math.Min(2, this.main.Renderer.Gamma + (delta * 0.1f)));
			});
			this.resizeToMenu(gamma);
			settingsList.Children.Add(gamma);

#if VR
			if (!this.main.VR)
#endif
			{
				Container fieldOfView = this.main.UIFactory.CreateScrollButton<float>("\\field of view", this.main.Settings.FieldOfView, x => ((int)Math.Round(MathHelper.ToDegrees(this.main.Settings.FieldOfView))).ToString() + "°", delegate(int delta)
				{
					this.main.Settings.FieldOfView.Value = Math.Max(MathHelper.ToRadians(60.0f), Math.Min(MathHelper.ToRadians(120.0f), this.main.Settings.FieldOfView + MathHelper.ToRadians(delta)));
				});
				this.resizeToMenu(fieldOfView);
				settingsList.Children.Add(fieldOfView);
			}

#if VR
			if (!this.main.VR)
#endif
			{
				Container motionBlurAmount = this.main.UIFactory.CreateScrollButton<float>("\\motion blur amount", this.main.Renderer.MotionBlurAmount, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
				{
					this.main.Renderer.MotionBlurAmount.Value = Math.Max(0, Math.Min(1, this.main.Renderer.MotionBlurAmount + (delta * 0.1f)));
				});
				this.resizeToMenu(motionBlurAmount);
				settingsList.Children.Add(motionBlurAmount);
			}

			Container reflectionsEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\reflections", this.main.Settings.Reflections, boolDisplay, delegate(int delta)
			{
				this.main.Settings.Reflections.Value = !this.main.Settings.Reflections;
			});
			this.resizeToMenu(reflectionsEnabled);
			settingsList.Children.Add(reflectionsEnabled);

			Container ssaoEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\ambient occlusion", this.main.Settings.SSAO, boolDisplay, delegate(int delta)
			{
				this.main.Settings.SSAO.Value = !this.main.Settings.SSAO;
			});
			this.resizeToMenu(ssaoEnabled);
			settingsList.Children.Add(ssaoEnabled);

			Container volumetricLightingEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\volumetric lighting", this.main.Settings.VolumetricLighting, boolDisplay, delegate(int delta)
			{
				this.main.Settings.VolumetricLighting.Value = !this.main.Settings.VolumetricLighting;
			});
			this.resizeToMenu(volumetricLightingEnabled);
			settingsList.Children.Add(volumetricLightingEnabled);

			Container bloomEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\bloom", this.main.Renderer.EnableBloom, boolDisplay, delegate(int delta)
			{
				this.main.Renderer.EnableBloom.Value = !this.main.Renderer.EnableBloom;
			});
			this.resizeToMenu(bloomEnabled);
			settingsList.Children.Add(bloomEnabled);

			int numDynamicShadowSettings = typeof(LightingManager.DynamicShadowSetting).GetFields(BindingFlags.Static | BindingFlags.Public).Length;
			Container dynamicShadows = this.main.UIFactory.CreateScrollButton<LightingManager.DynamicShadowSetting>("\\dynamic shadows", this.main.LightingManager.DynamicShadows, x => "\\" + x.ToString().ToLower(), delegate(int delta)
			{
				int newValue = ((int)this.main.LightingManager.DynamicShadows.Value) + delta;
				while (newValue < 0)
					newValue += numDynamicShadowSettings;
				this.main.LightingManager.DynamicShadows.Value = (LightingManager.DynamicShadowSetting)Enum.ToObject(typeof(LightingManager.DynamicShadowSetting), newValue % numDynamicShadowSettings);
			});
			this.resizeToMenu(dynamicShadows);
			settingsList.Children.Add(dynamicShadows);

#if ANALYTICS
			Container analyticsEnabled = this.main.UIFactory.CreateScrollButton<Main.Config.RecordAnalytics>("\\analytics", this.main.Settings.Analytics, delegate(int delta)
			{
				Main.Config.RecordAnalytics current = this.main.Settings.Analytics;
				this.main.Settings.Analytics.Value = current == Main.Config.RecordAnalytics.On ? Main.Config.RecordAnalytics.Off : Main.Config.RecordAnalytics.On;
			});
			this.resizeToMenu(analyticsEnabled);
			settingsList.Children.Add(analyticsEnabled);
#endif

			Container settingsReset = this.main.UIFactory.CreateButton("\\reset options", delegate()
			{
				this.ShowDialog("\\reset options?", "\\reset", delegate()
				{
					this.main.Settings.DefaultOptions();
					this.main.SaveSettings();
				});
			});
			this.resizeToMenu(settingsReset);
			settingsList.Children.Add(settingsReset);

			// Controls menu
			Animation controlsAnimation = null;

			ListContainer controlsMenu = new ListContainer();
			controlsMenu.Visible.Value = false;
			controlsMenu.Add(new Binding<Vector2, Point>(controlsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			controlsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(controlsMenu);
			controlsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container controlsLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(controlsLabelPadding);
			controlsMenu.Children.Add(controlsLabelPadding);

			ListContainer controlsLabelContainer = new ListContainer();
			controlsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			controlsLabelPadding.Children.Add(controlsLabelContainer);

			TextElement controlsLabel = new TextElement();
			controlsLabel.FontFile.Value = this.main.Font;
			controlsLabel.Text.Value = "\\controls title";
			controlsLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			controlsLabelContainer.Children.Add(controlsLabel);

			TextElement controlsScrollLabel = new TextElement();
			controlsScrollLabel.FontFile.Value = this.main.Font;
			controlsScrollLabel.Text.Value = "\\scroll for more";
			controlsScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			controlsLabelContainer.Children.Add(controlsScrollLabel);

			bool controlsShown = false;

			Action hideControls = delegate()
			{
				controlsShown = false;
				this.main.SaveSettings();

				this.showPauseMenu();

				if (controlsAnimation != null)
					controlsAnimation.Delete.Execute();
				controlsAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(controlsMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(controlsMenu.Visible, false)
				);
				this.main.AddComponent(controlsAnimation);
			};

			Container controlsBack = this.main.UIFactory.CreateButton("\\back", hideControls);
			this.resizeToMenu(controlsBack);
			controlsMenu.Children.Add(controlsBack);

			ListContainer controlsList = new ListContainer();
			controlsList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Scroller controlsScroller = new Scroller();
			controlsScroller.Add(new Binding<Vector2>(controlsScroller.Size, () => new Vector2(controlsList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), controlsList.Size, this.main.ScreenSize));
			controlsScroller.Children.Add(controlsList);
			controlsMenu.Children.Add(controlsScroller);

			Container invertMouseX = this.main.UIFactory.CreateScrollButton<bool>("\\invert look x", this.main.Settings.InvertMouseX, boolDisplay, delegate(int delta)
			{
				this.main.Settings.InvertMouseX.Value = !this.main.Settings.InvertMouseX;
			});
			this.resizeToMenu(invertMouseX);
			controlsList.Children.Add(invertMouseX);

			Container invertMouseY = this.main.UIFactory.CreateScrollButton<bool>("\\invert look y", this.main.Settings.InvertMouseY, boolDisplay, delegate(int delta)
			{
				this.main.Settings.InvertMouseY.Value = !this.main.Settings.InvertMouseY;
			});
			this.resizeToMenu(invertMouseY);
			controlsList.Children.Add(invertMouseY);

			Container mouseSensitivity = this.main.UIFactory.CreateScrollButton<float>("\\look sensitivity", this.main.Settings.MouseSensitivity, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Settings.MouseSensitivity.Value = Math.Max(0, Math.Min(5, this.main.Settings.MouseSensitivity + (delta * 0.1f)));
			});
			this.resizeToMenu(mouseSensitivity);
			controlsList.Children.Add(mouseSensitivity);

			Container controllerVibration = this.main.UIFactory.CreateScrollButton<bool>("\\controller vibration", this.main.Settings.ControllerVibration, boolDisplay, delegate(int delta)
			{
				this.main.Settings.ControllerVibration.Value = !this.main.Settings.ControllerVibration;
			});
			this.resizeToMenu(controllerVibration);
			controlsList.Children.Add(controllerVibration);
			controllerVibration.Add(new Binding<bool>(controllerVibration.Visible, this.main.GamePadConnected));

			Func<Property<PCInput.PCInputBinding>, string, bool, bool, Container> addInputSetting = delegate(Property<PCInput.PCInputBinding> setting, string display, bool allowGamepad, bool allowMouse)
			{
				this.inputBindings.Add(setting);
				Container button = this.main.UIFactory.CreatePropertyButton<PCInput.PCInputBinding>(display, setting);
				this.resizeToMenu(button);
				button.Add(new CommandBinding(button.MouseLeftUp, delegate()
				{
					PCInput.PCInputBinding originalValue = setting;
					setting.Value = new PCInput.PCInputBinding();
					this.main.UI.EnableMouse.Value = false;
					input.GetNextInput(delegate(PCInput.PCInputBinding binding)
					{
						if (binding.Key == Keys.Escape)
							setting.Value = originalValue;
						else
						{
							PCInput.PCInputBinding newValue = new PCInput.PCInputBinding();
							newValue.Key = originalValue.Key;
							newValue.MouseButton = originalValue.MouseButton;
							newValue.GamePadButton = originalValue.GamePadButton;

							if (binding.Key != Keys.None)
							{
								newValue.Key = binding.Key;
								newValue.MouseButton = PCInput.MouseButton.None;
							}
							else if (allowMouse && binding.MouseButton != PCInput.MouseButton.None)
							{
								newValue.Key = Keys.None;
								newValue.MouseButton = binding.MouseButton;
							}

							if (allowGamepad)
							{
								if (binding.GamePadButton != 0)
									newValue.GamePadButton = binding.GamePadButton;
							}
							else
								newValue.GamePadButton = 0;

							setting.Value = newValue;
						}
						this.main.UI.EnableMouse.Value = true;
					});
				}));
				controlsList.Children.Add(button);
				return button;
			};

			Action<Container> hideForGamepad = delegate(Container c)
			{
				c.Add(new Binding<bool>(c.Visible, x => !x, this.main.GamePadConnected));
			};

			hideForGamepad(addInputSetting(this.main.Settings.Forward, "\\move forward", false, true));
			hideForGamepad(addInputSetting(this.main.Settings.Left, "\\move left", false, true));
			hideForGamepad(addInputSetting(this.main.Settings.Backward, "\\move backward", false, true));
			hideForGamepad(addInputSetting(this.main.Settings.Right, "\\move right", false, true));
			addInputSetting(this.main.Settings.Jump, "\\jump", true, true);
			addInputSetting(this.main.Settings.Parkour, "\\parkour", true, true);
			addInputSetting(this.main.Settings.RollKick, "\\roll / kick", true, true);
			addInputSetting(this.main.Settings.TogglePhone, "\\toggle phone", true, true);
			addInputSetting(this.main.Settings.QuickSave, "\\quicksave", true, true);
#if VR
			if (this.main.VR)
				addInputSetting(this.main.Settings.RecenterVRPose, "\\recenter pose", true, true);
#endif
			Container consoleSetting = addInputSetting(this.main.Settings.ToggleConsole, "\\toggle console", true, true);
			consoleSetting.Add(new Binding<bool>(consoleSetting.Visible, this.main.Settings.GodModeProperty));

#if VR
			if (!this.main.VR)
#endif
			{
				// Mapping LMB to toggle fullscreen makes it impossible to change any other settings.
				// So don't allow it.
				addInputSetting(this.main.Settings.ToggleFullscreen, "\\toggle fullscreen", true, false);
			}

			Container controlsReset = this.main.UIFactory.CreateButton("\\reset options", delegate()
			{
				this.ShowDialog("\\reset options?", "\\reset", delegate()
				{
					this.main.Settings.DefaultControls();
					this.main.SaveSettings();
				});
			});
			this.resizeToMenu(controlsReset);
			controlsList.Children.Add(controlsReset);

			// Start menu
			Animation startAnimation = null;
			bool startShown = false;

			ListContainer startMenu = new ListContainer();
			startMenu.Visible.Value = false;
			startMenu.Add(new Binding<Vector2, Point>(startMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			startMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(startMenu);
			startMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container startLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(startLabelPadding);
			startMenu.Children.Add(startLabelPadding);

			ListContainer startLabelContainer = new ListContainer();
			startLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			startLabelPadding.Children.Add(startLabelContainer);

			TextElement startLabel = new TextElement();
			startLabel.FontFile.Value = this.main.Font;
			startLabel.Text.Value = "\\start title";
			startLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			startLabelContainer.Children.Add(startLabel);

			TextElement startScrollLabel = new TextElement();
			startScrollLabel.FontFile.Value = this.main.Font;
			startScrollLabel.Text.Value = "\\scroll for more";
			startScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			startLabelContainer.Children.Add(startScrollLabel);

			Action hideStart = delegate()
			{
				startShown = false;

				this.showPauseMenu();

				if (startAnimation != null)
					startAnimation.Delete.Execute();
				startAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(startMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(startMenu.Visible, false)
				);
				this.main.AddComponent(startAnimation);
			};

			Container startBack = this.main.UIFactory.CreateButton("\\back", hideStart);
			this.resizeToMenu(startBack);
			startMenu.Children.Add(startBack);

			ListContainer startList = new ListContainer();
			startList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			{
				int i = 0;
				foreach (KeyValuePair<string, string> item in Menu.maps)
				{
					int index = i;
					string m = item.Key;
					Container button = this.main.UIFactory.CreateButton(item.Value, delegate()
					{
						hideStart();
						this.restorePausedSettings();
						this.main.CurrentSave.Value = null;
						this.main.AddComponent(new Animation
						(
							new Animation.Delay(0.2f),
							new Animation.Execute(delegate()
							{
								IO.MapLoader.Load(this.main, m);
							})
						));
					});
					button.Add(new Binding<bool>(button.Visible, () => index <= MaxLevelIndex && (this.main.Settings.GodModeProperty || this.main.Settings.LevelIndexProperty >= index), this.main.Settings.GodModeProperty, this.main.Settings.LevelIndexProperty));
					this.resizeToMenu(button);
					startList.Children.Add(button);
					i++;
				}
			}

			Scroller startScroller = new Scroller();
			startScroller.Children.Add(startList);
			startScroller.Add(new Binding<Vector2>(startScroller.Size, () => new Vector2(startList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), startList.Size, this.main.ScreenSize));
			startMenu.Children.Add(startScroller);

#if DEMO
			{
				Container labelPadding = this.main.UIFactory.CreateContainer();
				labelPadding.Opacity.Value = 0.0f;
				this.resizeToMenu(labelPadding);
				this.pauseMenu.Children.Add(labelPadding);

				TextElement label = new TextElement();
				label.FontFile.Value = this.main.Font;
				label.Text.Value = "\\demo";
				label.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
				labelPadding.Children.Add(label);
			}
#endif

			// Resume button
			Container resume = this.main.UIFactory.CreateButton("\\resume", delegate()
			{
				this.main.Paused.Value = false;
				this.restorePausedSettings();
			});
			this.resizeToMenu(resume);
			resume.Visible.Value = false;
			this.pauseMenu.Children.Add(resume);
			resume.Add(new Binding<bool, string>(resume.Visible, x => x != Main.MenuMap, this.main.MapFile));

			// Start button
			Container start = this.main.UIFactory.CreateButton("\\new game", delegate()
			{
				this.hidePauseMenu();

				startMenu.Visible.Value = true;
				if (startAnimation != null)
					startAnimation.Delete.Execute();
				startAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(startMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(startAnimation);

				startShown = true;
				this.currentMenu.Value = startList;
			});
			this.resizeToMenu(start);
			start.Add(new Binding<bool, string>(start.Visible, x => x == Main.MenuMap, this.main.MapFile));
			this.pauseMenu.Children.Add(start);

			// Save button
			Container saveButton = this.main.UIFactory.CreateButton("\\save", delegate()
			{
				if (PlayerFactory.Instance != null)
				{
					this.hidePauseMenu();

					this.saveMode.Value = true;

					this.loadSaveMenu.Visible.Value = true;
					if (this.loadSaveAnimation != null)
						this.loadSaveAnimation.Delete.Execute();
					this.loadSaveAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(this.loadSaveMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
					this.main.AddComponent(this.loadSaveAnimation);

					this.loadSaveShown = true;
					this.currentMenu.Value = this.loadSaveList;
				}
			});
			this.resizeToMenu(saveButton);
			saveButton.Add(new Binding<bool>(saveButton.Visible, () => this.main.MapFile != Main.MenuMap && !this.main.IsChallengeMap(this.main.MapFile) && !this.main.EditorEnabled, this.main.MapFile, this.main.EditorEnabled));
			this.pauseMenu.Children.Add(saveButton);

			Action showLoad = delegate()
			{
				this.hidePauseMenu();

				this.saveMode.Value = false;

				this.loadSaveMenu.Visible.Value = true;
				if (this.loadSaveAnimation != null)
					this.loadSaveAnimation.Delete.Execute();
				this.loadSaveAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(this.loadSaveMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(this.loadSaveAnimation);

				this.loadSaveShown = true;
				this.currentMenu.Value = this.loadSaveList;
			};

			// Load button
			Container load = this.main.UIFactory.CreateButton("\\load", showLoad);
			load.Add(new Binding<bool>(load.Visible, () => !this.main.IsChallengeMap(this.main.MapFile) && !this.main.EditorEnabled, this.main.MapFile, this.main.EditorEnabled));
			this.resizeToMenu(load);
			this.pauseMenu.Children.Add(load);

			// Retry button
			Container retry = this.main.UIFactory.CreateButton("\\retry", delegate()
			{
				this.pauseMenu.Visible.Value = false;
				this.currentMenu.Value = null;
				this.main.Paused.Value = false;
				if (this.pauseAnimation != null)
				{
					this.pauseAnimation.Delete.Execute();
					this.pauseAnimation = null;
				}

				IO.MapLoader.Load(this.main, this.main.MapFile);
			});
			retry.Add(new Binding<bool>(retry.Visible, () => !this.main.EditorEnabled && this.main.IsChallengeMap(this.main.MapFile), this.main.EditorEnabled, this.main.MapFile));
			this.resizeToMenu(retry);
			this.pauseMenu.Children.Add(retry);

			ConstructChallengeMenu();

			// Controls button
			Container controlsButton = this.main.UIFactory.CreateButton("\\controls", delegate()
			{
				this.hidePauseMenu();

				controlsMenu.Visible.Value = true;
				if (controlsAnimation != null)
					controlsAnimation.Delete.Execute();
				controlsAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(controlsMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(controlsAnimation);

				controlsShown = true;
				this.currentMenu.Value = controlsList;
			});
			this.resizeToMenu(controlsButton);
			this.pauseMenu.Children.Add(controlsButton);

			// Settings button
			Container settingsButton = this.main.UIFactory.CreateButton("\\options", delegate()
			{
				this.hidePauseMenu();

				settingsMenu.Visible.Value = true;
				if (settingsAnimation != null)
					settingsAnimation.Delete.Execute();
				settingsAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(settingsMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(settingsAnimation);

				settingsShown = true;

				this.currentMenu.Value = settingsList;
			});
			this.resizeToMenu(settingsButton);
			this.pauseMenu.Children.Add(settingsButton);

#if VR
			// Recenter VR pose button
			if (this.main.VR)
			{
				Container recenterVrPose = this.main.UIFactory.CreateButton("\\recenter pose button", this.main.VRHmd.RecenterPose);
				this.resizeToMenu(recenterVrPose);
				pauseMenu.Children.Add(recenterVrPose);
			}
#endif

			// Edit mode toggle button
			Container switchToEditMode = this.main.UIFactory.CreateButton("\\edit mode", this.editMode);
			switchToEditMode.Add(new Binding<bool>(switchToEditMode.Visible, () => !this.main.EditorEnabled && (this.main.Settings.GodModeProperty || Path.GetDirectoryName(this.main.MapFile) == this.main.CustomMapDirectory), this.main.EditorEnabled, this.main.MapFile, this.main.Settings.GodModeProperty));
			this.resizeToMenu(switchToEditMode);
			this.pauseMenu.Children.Add(switchToEditMode);

			// Credits window
			Animation creditsAnimation = null;
			bool creditsShown = false;

			ListContainer creditsMenu = new ListContainer();
			creditsMenu.Visible.Value = false;
			creditsMenu.Add(new Binding<Vector2, Point>(creditsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			creditsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(creditsMenu);
			creditsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container creditsLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(creditsLabelPadding);
			creditsMenu.Children.Add(creditsLabelPadding);

			ListContainer creditsLabelContainer = new ListContainer();
			creditsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			creditsLabelPadding.Children.Add(creditsLabelContainer);

			TextElement creditsLabel = new TextElement();
			creditsLabel.FontFile.Value = this.main.Font;
			creditsLabel.Text.Value = "\\credits title";
			creditsLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			creditsLabelContainer.Children.Add(creditsLabel);

			TextElement creditsScrollLabel = new TextElement();
			creditsScrollLabel.FontFile.Value = this.main.Font;
			creditsScrollLabel.Text.Value = "\\scroll for more";
			creditsScrollLabel.WrapWidth.Value = menuButtonWidth - menuButtonLeftPadding;
			creditsLabelContainer.Children.Add(creditsScrollLabel);

			Action hideCredits = delegate()
			{
				creditsShown = false;

				this.showPauseMenu();

				if (creditsAnimation != null)
					creditsAnimation.Delete.Execute();
				creditsAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(creditsMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(creditsMenu.Visible, false)
				);
				this.main.AddComponent(creditsAnimation);
			};

			Container creditsBack = this.main.UIFactory.CreateButton("\\back", delegate()
			{
				hideCredits();
			});
			this.resizeToMenu(creditsBack);
			creditsMenu.Children.Add(creditsBack);

			Container creditsDisplay = new Container();
			creditsDisplay.PaddingLeft.Value = Menu.menuButtonLeftPadding;
			creditsDisplay.Opacity.Value = 0;

			TextElement creditsText = new TextElement();
			creditsText.FontFile.Value = this.main.Font;
			creditsText.Text.Value = this.Credits = File.ReadAllText("attribution.txt");
			creditsDisplay.Children.Add(creditsText);

			Scroller creditsScroller = new Scroller();
			creditsScroller.Add(new Binding<Vector2>(creditsScroller.Size, () => new Vector2(creditsDisplay.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), creditsDisplay.Size, this.main.ScreenSize));
			creditsScroller.Children.Add(creditsDisplay);
			creditsMenu.Children.Add(creditsScroller);

			// Credits button
			Container credits = this.main.UIFactory.CreateButton("\\credits", delegate()
			{
				this.hidePauseMenu();

				creditsMenu.Visible.Value = true;
				if (creditsAnimation != null)
					creditsAnimation.Delete.Execute();
				creditsAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(creditsMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(creditsAnimation);

				creditsShown = true;
				this.currentMenu.Value = creditsDisplay;
			});
			this.resizeToMenu(credits);
			credits.Add(new Binding<bool, string>(credits.Visible, x => x == Main.MenuMap, this.main.MapFile));
			this.pauseMenu.Children.Add(credits);

			// Main menu button
			Container mainMenu = this.main.UIFactory.CreateButton("\\main menu", delegate()
			{
				this.ShowDialog
				(
					"\\quit prompt", "\\quit",
					delegate()
					{
						this.main.CurrentSave.Value = null;
						this.main.EditorEnabled.Value = false;
						IO.MapLoader.Load(this.main, Main.MenuMap);
						this.main.Paused.Value = false;
					}
				);
			});
			this.resizeToMenu(mainMenu);
			this.pauseMenu.Children.Add(mainMenu);
			mainMenu.Add(new Binding<bool, string>(mainMenu.Visible, x => x != Main.MenuMap, this.main.MapFile));

			// Exit button
			Container exit = this.main.UIFactory.CreateButton("\\exit", delegate()
			{
				if (this.main.MapFile.Value != Main.MenuMap)
				{
					this.ShowDialog
					(
						"\\exit prompt", "\\exit",
						delegate()
						{
							this.main.SaveSettings();
							throw new Main.ExitException();
						}
					);
				}
				else
					throw new Main.ExitException();
			});
			this.resizeToMenu(exit);
			this.pauseMenu.Children.Add(exit);

			// Escape key
			// Make sure we can only pause when there is a player currently spawned
			// Otherwise we could save the current map without the player. And that would be awkward.
			Func<bool> canPause = delegate()
			{
				if (!this.CanPause)
					return false;

				if (this.main.Paused)
					return this.main.MapFile.Value != Main.MenuMap; // Only allow pausing on the menu map, don't allow unpausing

				if (SteamWorker.OverlayActive || !SteamWorker.OverlaySafelyGone)
					return false;

				return true;
			};

			Action togglePause = delegate()
			{
				if (this.dialog != null)
				{
					this.dialog.Delete.Execute();
					this.dialog = null;
					return;
				}
				else if (settingsShown)
				{
					hideSettings();
					return;
				}
				else if (controlsShown)
				{
					hideControls();
					return;
				}
				else if (creditsShown)
				{
					hideCredits();
					return;
				}
				else if (this.loadSaveShown)
				{
					this.hideLoadSave();
					return;
				}
				else if (startShown)
				{
					hideStart();
					return;
				}
				else if (this.challengeMenuShown)
				{
					this.hideChallenge(true);
					return;
				}

				if (this.main.MapFile.Value == Main.MenuMap)
				{
					if (this.currentMenu.Value == null)
						this.savePausedSettings();
				}
				else
				{
					if (this.currentMenu.Value == null)
						this.savePausedSettings();
					else if (ConsoleUI.Showing)
						ConsoleUI.Showing.Value = false;
					else
						this.restorePausedSettings();
					this.main.Paused.Value = this.currentMenu.Value != null;
				}
			};

			this.input.Bind(this.main.Settings.ToggleConsole, PCInput.InputState.Up, delegate()
			{
				if (this.main.Settings.GodModeProperty && (this.main.Paused || this.CanPause))
				{
					if (this.currentMenu.Value == null && !ConsoleUI.Showing)
					{
						if (canPause())
						{
							togglePause();
							ConsoleUI.Showing.Value = true;
						}
					}
					else
						ConsoleUI.Showing.Value = !ConsoleUI.Showing.Value;
				}
			});

			this.input.Add(new CommandBinding(input.GetKeyDown(Keys.Escape), delegate()
			{
				if (this.main.EditorEnabled)
					return canPause() && (this.currentMenu.Value != null || this.dialog != null);
				else
					return canPause() || this.dialog != null;
			}, togglePause));
			this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.Start), canPause, togglePause));
			this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.B), () => this.currentMenu.Value != null || this.dialog != null, togglePause));

			// Gamepad menu code

			int selected = 0;

			Func<UIComponent, int, int, int> nextMenuItem = delegate(UIComponent menu, int current, int delta)
			{
				int end = menu.Children.Count;
				int newValue = current + delta;
				if (newValue < 0)
					return end - 1;
				else if (newValue > end - 1)
					return 0;
				else
					return newValue;
			};

			Func<UIComponent, bool> isButton = delegate(UIComponent item)
			{
				return item.Visible && item.GetType() == typeof(Container) && item.MouseLeftUp.Bindings.Count > 0;
			};

			Func<UIComponent, bool> isScrollButton = delegate(UIComponent item)
			{
				return item.Visible && item.GetType() == typeof(Container) && item.GetChildByName("<") != null;
			};

			this.input.Add(new NotifyBinding(delegate()
			{
				UIComponent menu = this.currentMenu;
				if (menu != null && menu != creditsDisplay)
				{
					bool highlight = this.main.GamePadConnected;
					if (highlight)
					{
						foreach (UIComponent item in menu.Children)
							item.Highlighted.Value = false;
					}

					int i = 0;
					foreach (UIComponent item in menu.Children)
					{
						if (isButton(item) || isScrollButton(item))
						{
							if (highlight)
								item.Highlighted.Value = true;
							selected = i;
							break;
						}
						i++;
					}
				}
			}, this.currentMenu));

			const float gamepadMoveInterval = 0.1f;
			const float gamepadScrollMoveInterval = 0.2f;
			Action<int> moveSelection = delegate(int delta)
			{
				if (this.main.GameTime.TotalGameTime.TotalSeconds - this.lastGamepadMove > gamepadMoveInterval
					&& this.main.GameTime.TotalGameTime.TotalSeconds - this.lastGamepadScroll > gamepadScrollMoveInterval)
				{
					UIComponent menu = this.currentMenu;
					if (menu != null && menu.EnableInput && this.dialog == null)
					{
						if (menu == creditsDisplay)
						{
							Scroller scroll = (Scroller)menu.Parent;
							scroll.MouseScrolled.Execute(delta * -4);
							return;
						}

						Container button;
						if (selected >= 0 && selected < menu.Children.Length)
						{
							button = (Container)menu.Children[selected];
							button.Highlighted.Value = false;
						}

						if (menu.Children.Count > 0)
						{
							int i = nextMenuItem(menu, selected, delta);
							while (true)
							{
								UIComponent item = menu.Children[i];
								if (isButton(item) || isScrollButton(item))
								{
									selected = i;
									break;
								}

								i = nextMenuItem(menu, i, delta);
							}

							this.lastGamepadMove = (float)this.main.GameTime.TotalGameTime.TotalSeconds;

							if (selected >= 0 && selected < menu.Children.Count)
							{
								button = (Container)menu.Children[selected];
								button.Highlighted.Value = true;

								if (menu.Parent.Value.GetType() == typeof(Scroller))
								{
									Scroller scroll = (Scroller)menu.Parent;
									scroll.ScrollTo(button);
								}
							}
						}
					}
				}
			};

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickUp), delegate()
			{
				moveSelection(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadUp), delegate()
			{
				moveSelection(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickDown), delegate()
			{
				moveSelection(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadDown), delegate()
			{
				moveSelection(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.A), delegate()
			{
				if (this.dialog != null)
					this.dialog.GetChildByName("Okay").MouseLeftUp.Execute();
				else
				{
					UIComponent menu = this.currentMenu;
					if (menu != null && menu != creditsDisplay)
					{
						if (selected >= 0 && selected < menu.Children.Count)
						{
							UIComponent selectedItem = menu.Children[selected];
							if (isScrollButton(selectedItem) && selectedItem.Highlighted)
								selectedItem.GetChildByName(">").MouseLeftUp.Execute();
							else if (isButton(selectedItem) && selectedItem.Highlighted)
								selectedItem.MouseLeftUp.Execute();
						}
					}
				}
			}));

			Action<int> scrollButton = delegate(int delta)
			{
				if (this.main.GameTime.TotalGameTime.TotalSeconds - this.lastGamepadMove > gamepadScrollMoveInterval
					&& this.main.GameTime.TotalGameTime.TotalSeconds - this.lastGamepadScroll > gamepadMoveInterval)
				{
					UIComponent menu = this.currentMenu;
					if (menu != null && menu != creditsDisplay && this.dialog == null)
					{
						if (selected >= 0 && selected < menu.Children.Count)
						{
							UIComponent selectedItem = menu.Children[selected];
							if (isScrollButton(selectedItem) && selectedItem.Highlighted)
							{
								selectedItem.GetChildByName(delta > 0 ? ">" : "<").MouseLeftUp.Execute();
								this.lastGamepadScroll = (float)this.main.GameTime.TotalGameTime.TotalSeconds;
							}
						}
					}
				}
			};

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickLeft), delegate()
			{
				scrollButton(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadLeft), delegate()
			{
				scrollButton(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickRight), delegate()
			{
				scrollButton(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadRight), delegate()
			{
				scrollButton(1);
			}));
		}

		public void Update(float dt)
		{
			if (this.main.GamePadState.Value.IsConnected != this.main.LastGamePadState.Value.IsConnected)
			{
				// Re-bind inputs so their string representations are properly displayed
				// We need to show both PC and gamepad bindings

				foreach (Property<PCInput.PCInputBinding> binding in this.inputBindings)
					binding.Reset();
			}
		}

		private void editMode()
		{
			this.pauseMenu.Visible.Value = false;
			this.currentMenu.Value = null;
			this.main.Paused.Value = false;
			if (this.pauseAnimation != null)
			{
				this.pauseAnimation.Delete.Execute();
				this.pauseAnimation = null;
			}
			this.main.EditorEnabled.Value = true;
			this.main.CurrentSave.Value = null;

			if (this.main.Settings.GodModeProperty || Path.GetDirectoryName(this.main.MapFile) == this.main.CustomMapDirectory)
				IO.MapLoader.Load(this.main, this.main.MapFile);
			else
				IO.MapLoader.Load(this.main, null);
		}

		public void Show(bool initial = false)
		{
			if (!this.Showing)
				this.savePausedSettings(initial);
		}

		public void Toggle()
		{
			if (this.Showing)
			{
				if (this.currentMenu.Value == this.pauseMenu)
					this.restorePausedSettings();
			}
			else
				this.savePausedSettings();
		}
	}
}
