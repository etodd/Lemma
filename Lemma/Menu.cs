using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
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

namespace Lemma.Components
{
	public class Menu : Component<Main>, IUpdateableComponent
	{
		private static Dictionary<string, string> maps = new Dictionary<string, string>
		{
#if DEBUG
			{ "test", "Test Level" },
#endif
			{ "sandbox", "Sandbox" },
			{ "start", "\\map apartment" },
			{ "rain", "\\map rain" },
			{ "dawn", "\\map dawn" },
			{ "forest", "\\map forest" },
			{ "monolith", "\\map monolith" },
			//{ "valley", "\\map valley" }, // Temporarily disabled
		};

		private const float messageFadeTime = 0.75f;
		private const float messageBackgroundOpacity = 0.75f;

		private const float menuButtonWidth = 256.0f;
		private const float menuButtonSettingOffset = 180.0f; // Horizontal offset for the value label on a settings menu item
		private const float menuButtonLeftPadding = 40.0f;
		private const float animationSpeed = 2.5f;
		private const float hideAnimationSpeed = 5.0f;

		private List<Property<PCInput.PCInputBinding>> inputBindings = new List<Property<PCInput.PCInputBinding>>();

		private ListContainer messages;

		private PCInput input;

		public string Credits;

		private int displayModeIndex;

		public DisplayModeCollection SupportedDisplayModes;

		Property<UIComponent> currentMenu = new Property<UIComponent> { Value = null };

		// Settings to be restored when unpausing
		private float originalBlurAmount = 0.0f;
		private bool originalMouseVisible = false;
		private Point originalMousePosition = new Point();

		public void ClearMessages()
		{
			this.messages.Children.Clear();
		}

		private Container buildMessage()
		{
			Container msgBackground = new Container();

			this.messages.Children.Add(msgBackground);

			msgBackground.Tint.Value = Color.Black;
			msgBackground.Opacity.Value = messageBackgroundOpacity;
			TextElement msg = new TextElement();
			msg.FontFile.Value = "Font";
			msg.WrapWidth.Value = 250.0f;
			msgBackground.Children.Add(msg);
			return msgBackground;
		}

		public Container ShowMessage(Entity entity, Func<string> text, params IProperty[] properties)
		{
			Container container = this.buildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Add(new Binding<string>(textElement.Text, text, properties));

			this.animateMessage(entity, container);

			return container;
		}

		private void animateMessage(Entity entity, Container container)
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

			if (entity == null)
			{
				anim.EnabledWhenPaused = false;
				this.main.AddComponent(anim);
			}
			else
				entity.Add(anim);
		}

		public Container ShowMessage(Entity entity, string text)
		{
			Container container = this.buildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Text.Value = text;

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
						return string.Format(main.Strings.Get(text.Substring(1)), properties);
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
				Animation anim = new Animation
				(
					new Animation.Delay(delay),
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
				using (Stream stream = new FileStream(Path.Combine(this.main.SaveDirectory, timestamp, "save.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
					info = (Main.SaveInfo)new XmlSerializer(typeof(Main.SaveInfo)).Deserialize(stream);
				if (info.Version != Main.MapVersion)
					throw new Exception();
				thumbnailPath = Path.Combine(this.main.SaveDirectory, timestamp, "thumbnail.jpg");
				if (!File.Exists(thumbnailPath))
					throw new Exception();
			}
			catch (Exception) // Old version. Delete it.
			{
				string savePath = Path.Combine(this.main.SaveDirectory, timestamp);
				if (Directory.Exists(savePath))
				{
					try
					{
						Directory.Delete(savePath, true);
					}
					catch (Exception)
					{
						// Whatever. We can't delete it, tough beans.
					}
				}
				return;
			}

			UIComponent container = this.main.UIFactory.CreateButton();
			container.UserData.Value = timestamp;

			ListContainer layout = new ListContainer();
			layout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			container.Children.Add(layout);

			Sprite sprite = new Sprite();
			sprite.IsStandardImage.Value = true;
			sprite.Image.Value = thumbnailPath;
			layout.Children.Add(sprite);

			TextElement label = new TextElement();
			label.FontFile.Value = "Font";
			label.Text.Value = timestamp;
			layout.Children.Add(label);

			container.Add(new CommandBinding(container.MouseLeftUp, delegate()
			{
				if (this.saveMode)
				{
					this.loadSaveMenu.EnableInput.Value = false;
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
			}));

			this.loadSaveList.Children.Add(container);
			this.loadSaveScroll.ScrollToTop();
		}

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
			this.pauseMenu.Visible.Value = true;
			if (this.pauseAnimation != null)
				this.pauseAnimation.Delete.Execute();
			this.pauseAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(this.pauseMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
			this.main.AddComponent(this.pauseAnimation);
			this.currentMenu.Value = this.pauseMenu;
		}

		// Pause
		private void savePausedSettings()
		{
			Session.Recorder.Event(main, "Pause");

			// Take screenshot
			this.main.Screenshot.Take(this.main.ScreenSize);

			this.originalMouseVisible = this.main.IsMouseVisible;
			this.main.IsMouseVisible.Value = true;
			this.originalBlurAmount = this.main.Renderer.BlurAmount;

			// Save mouse position
			MouseState mouseState = this.main.MouseState;
			this.originalMousePosition = new Point(mouseState.X, mouseState.Y);

			this.pauseMenu.Visible.Value = true;
			this.pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);

			// Blur the screen and show the pause menu
			if (this.pauseAnimation != null && this.pauseAnimation.Active)
				this.pauseAnimation.Delete.Execute();

			this.pauseAnimation = new Animation
			(
				new Animation.Parallel
				(
					new Animation.FloatMoveToSpeed(this.main.Renderer.BlurAmount, 1.0f, 1.0f),
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

			this.currentMenu.Value = this.pauseMenu;
		}

		// Unpause
		private void restorePausedSettings()
		{
			Session.Recorder.Event(main, "Unpause");
			if (this.pauseAnimation != null && this.pauseAnimation.Active)
				this.pauseAnimation.Delete.Execute();

			// Restore mouse
			if (!originalMouseVisible)
			{
				// Only restore mouse position if the cursor was not visible
				// i.e., we're in first-person camera mode
				Microsoft.Xna.Framework.Input.Mouse.SetPosition(originalMousePosition.X, originalMousePosition.Y);
				MouseState m = new MouseState(originalMousePosition.X, originalMousePosition.Y, this.main.MouseState.Value.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
				this.main.LastMouseState.Value = m;
				this.main.MouseState.Value = m;
			}
			this.main.IsMouseVisible.Value = originalMouseVisible;

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
		}

		private void resizeToMenu(Container c)
		{
			c.ResizeHorizontal.Value = false;
			c.Size.Value = new Vector2(Menu.menuButtonWidth + Menu.menuButtonLeftPadding + 4.0f, 0.0f);
			c.PaddingLeft.Value = Menu.menuButtonLeftPadding;
		}

		public void ShowDialog(string question, string action, Action callback)
		{
			if (this.dialog != null)
				this.dialog.Delete.Execute();
			this.dialog = new Container();
			this.dialog.Tint.Value = Color.Black;
			this.dialog.Opacity.Value = 0.5f;
			this.dialog.AnchorPoint.Value = new Vector2(0.5f);
			this.dialog.Add(new Binding<Vector2, Point>(this.dialog.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.main.ScreenSize));
			this.dialog.Add(new CommandBinding(this.dialog.Delete, delegate()
			{
				this.loadSaveMenu.EnableInput.Value = true;
			}));
			this.main.UI.Root.Children.Add(this.dialog);

			ListContainer dialogLayout = new ListContainer();
			dialogLayout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			this.dialog.Children.Add(dialogLayout);

			TextElement prompt = new TextElement();
			prompt.FontFile.Value = "Font";
			prompt.Text.Value = question;
			dialogLayout.Children.Add(prompt);

			ListContainer dialogButtons = new ListContainer();
			dialogButtons.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			dialogLayout.Children.Add(dialogButtons);

			UIComponent okay = this.main.UIFactory.CreateButton("", delegate()
			{
				this.dialog.Delete.Execute();
				this.dialog = null;
				callback();
			});
			TextElement okayText = (TextElement)okay.GetChildByName("Text");
			okayText.Add(new Binding<string, bool>(okayText.Text, x => action + (x ? " gamepad" : ""), this.main.GamePadConnected));
			okay.Name.Value = "Okay";
			dialogButtons.Children.Add(okay);

			UIComponent cancel = this.main.UIFactory.CreateButton("\\cancel", delegate()
			{
				this.dialog.Delete.Execute();
				this.dialog = null;
			});
			dialogButtons.Children.Add(cancel);

			TextElement cancelText = (TextElement)cancel.GetChildByName("Text");
			cancelText.Add(new Binding<string, bool>(cancelText.Text, x => x ? "\\cancel gamepad" : "\\cancel", this.main.GamePadConnected));
		}

		private void hideLoadSave()
		{
			this.showPauseMenu();

			if (this.dialog != null)
			{
				this.dialog.Delete.Execute();
				this.dialog = null;
			}

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

		public override void Awake()
		{
			base.Awake();

			this.input = new PCInput();
			this.main.AddComponent(this.input);

			// Toggle fullscreen
			this.input.Bind(this.main.Settings.ToggleFullscreen, PCInput.InputState.Down, delegate()
			{
				if (this.main.Graphics.IsFullScreen) // Already fullscreen. Go to windowed mode.
					this.main.ExitFullscreen();
				else // In windowed mode. Go to fullscreen.
					this.main.EnterFullscreen();
			});

#if DEBUG
			Log.Handler = delegate(string log)
			{
				this.HideMessage(null, this.ShowMessage(null, log), 2.0f);
				this.main.ConsoleUI.LogText(log);
			};
#endif

			// Message list
			this.messages = new ListContainer();
			this.messages.Alignment.Value = ListContainer.ListAlignment.Max;
			this.messages.AnchorPoint.Value = new Vector2(1.0f, 1.0f);
			this.messages.Reversed.Value = true;
			this.messages.Add(new Binding<Vector2, Point>(this.messages.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.9f), this.main.ScreenSize));
			this.main.UI.Root.Children.Add(this.messages);

			this.notifications = new ListContainer();
			this.notifications.Alignment.Value = ListContainer.ListAlignment.Max;
			this.notifications.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
			this.notifications.Name.Value = "Notifications";
			this.notifications.Add(new Binding<Vector2, Point>(this.notifications.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.1f), this.main.ScreenSize));
			this.main.UI.Root.Children.Add(this.notifications);

			// Fullscreen message
			Container msgBackground = new Container();
			this.main.UI.Root.Children.Add(msgBackground);
			msgBackground.Tint.Value = Color.Black;
			msgBackground.Opacity.Value = 0.2f;
			msgBackground.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
			msgBackground.Add(new Binding<Vector2, Point>(msgBackground.Position, x => new Vector2(x.X * 0.5f, x.Y - 30.0f), this.main.ScreenSize));
			TextElement msg = new TextElement();
			msg.FontFile.Value = "Font";
			msg.Text.Value = "\\toggle fullscreen tooltip";
			msgBackground.Children.Add(msg);
			this.main.AddComponent(new Animation
			(
				new Animation.Delay(4.0f),
				new Animation.Parallel
				(
					new Animation.FloatMoveTo(msgBackground.Opacity, 0.0f, 2.0f),
					new Animation.FloatMoveTo(msg.Opacity, 0.0f, 2.0f)
				),
				new Animation.Execute(delegate() { msgBackground.Delete.Execute(); })
			));

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
			loadSaveLabel.FontFile.Value = "Font";
			loadSaveLabel.Add(new Binding<string, bool>(loadSaveLabel.Text, x => x ? "S A V E" : "L O A D", this.saveMode));
			loadSaveLabelContainer.Children.Add(loadSaveLabel);

			TextElement loadSaveScrollLabel = new TextElement();
			loadSaveScrollLabel.FontFile.Value = "Font";
			loadSaveScrollLabel.Text.Value = "\\scroll for more";
			loadSaveLabelContainer.Children.Add(loadSaveScrollLabel);

			TextElement quickSaveLabel = new TextElement();
			quickSaveLabel.FontFile.Value = "Font";
			quickSaveLabel.Add(new Binding<bool>(quickSaveLabel.Visible, this.saveMode));
			quickSaveLabel.Text.Value = "\\quicksave instructions";
			loadSaveLabelContainer.Children.Add(quickSaveLabel);

			Container loadSaveBack = this.main.UIFactory.CreateButton("\\back", this.hideLoadSave);
			this.resizeToMenu(loadSaveBack);
			this.loadSaveMenu.Children.Add(loadSaveBack);

			Container saveNewButton = this.main.UIFactory.CreateButton("\\save new", delegate()
			{
				this.main.SaveOverwrite();
				this.hideLoadSave();
				this.main.Paused.Value = false;
				this.restorePausedSettings();
			});
			this.resizeToMenu(saveNewButton);
			saveNewButton.Add(new Binding<bool>(saveNewButton.Visible, this.saveMode));
			this.loadSaveMenu.Children.Add(saveNewButton);

			this.loadSaveScroll = new Scroller();
			this.loadSaveScroll.Add(new Binding<Vector2, Point>(this.loadSaveScroll.Size, x => new Vector2(Menu.menuButtonWidth + Menu.menuButtonLeftPadding + 4.0f, x.Y * 0.5f), this.main.ScreenSize));
			this.loadSaveMenu.Children.Add(this.loadSaveScroll);

			this.loadSaveList = new ListContainer();
			this.loadSaveList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			this.loadSaveList.Reversed.Value = true;
			this.loadSaveScroll.Children.Add(this.loadSaveList);

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
			settingsLabel.FontFile.Value = "Font";
			settingsLabel.Text.Value = "\\options title";
			settingsLabelContainer.Children.Add(settingsLabel);

			TextElement settingsScrollLabel = new TextElement();
			settingsScrollLabel.FontFile.Value = "Font";
			settingsScrollLabel.Add(new Binding<string>(settingsScrollLabel.Text, delegate()
			{
				if (this.main.GamePadConnected)
					return "\\modify setting gamepad";
				else
					return "\\modify setting";
			}, this.main.GamePadConnected));
			settingsLabelContainer.Children.Add(settingsScrollLabel);

			Action hideSettings = delegate()
			{
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

			Container vsyncEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\vsync", this.main.Settings.EnableVsync, boolDisplay, delegate(int delta)
			{
				this.main.Settings.EnableVsync.Value = !this.main.Settings.EnableVsync;
			});
			this.resizeToMenu(vsyncEnabled);
			settingsList.Children.Add(vsyncEnabled);

			Container gamma = this.main.UIFactory.CreateScrollButton<float>("\\gamma", this.main.Renderer.Gamma, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Renderer.Gamma.Value = Math.Max(0, Math.Min(2, this.main.Renderer.Gamma + (delta * 0.1f)));
			});
			this.resizeToMenu(gamma);
			settingsList.Children.Add(gamma);

			Container fieldOfView = this.main.UIFactory.CreateScrollButton<float>("\\field of view", this.main.Camera.FieldOfView, x => ((int)Math.Round(MathHelper.ToDegrees(this.main.Camera.FieldOfView))).ToString() + "°", delegate(int delta)
			{
				this.main.Camera.FieldOfView.Value = Math.Max(MathHelper.ToRadians(60.0f), Math.Min(MathHelper.ToRadians(120.0f), this.main.Camera.FieldOfView + MathHelper.ToRadians(delta)));
			});
			this.resizeToMenu(fieldOfView);
			settingsList.Children.Add(fieldOfView);

			Container motionBlurAmount = this.main.UIFactory.CreateScrollButton<float>("\\motion blur amount", this.main.Renderer.MotionBlurAmount, x => ((int)Math.Round(x * 100.0f)).ToString() + "%", delegate(int delta)
			{
				this.main.Renderer.MotionBlurAmount.Value = Math.Max(0, Math.Min(1, this.main.Renderer.MotionBlurAmount + (delta * 0.1f)));
			});
			this.resizeToMenu(motionBlurAmount);
			settingsList.Children.Add(motionBlurAmount);

			Container reflectionsEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\reflections", this.main.Settings.EnableReflections, boolDisplay, delegate(int delta)
			{
				this.main.Settings.EnableReflections.Value = !this.main.Settings.EnableReflections;
			});
			this.resizeToMenu(reflectionsEnabled);
			settingsList.Children.Add(reflectionsEnabled);

			Container ssaoEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\ambient occlusion", this.main.Settings.EnableSSAO, boolDisplay, delegate(int delta)
			{
				this.main.Settings.EnableSSAO.Value = !this.main.Settings.EnableSSAO;
			});
			this.resizeToMenu(ssaoEnabled);
			settingsList.Children.Add(ssaoEnabled);

			Container godRaysEnabled = this.main.UIFactory.CreateScrollButton<bool>("\\god rays", this.main.Settings.EnableGodRays, boolDisplay, delegate(int delta)
			{
				this.main.Settings.EnableGodRays.Value = !this.main.Settings.EnableGodRays;
			});
			this.resizeToMenu(godRaysEnabled);
			settingsList.Children.Add(godRaysEnabled);

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

			Container settingsReset = this.main.UIFactory.CreateButton("\\reset options", delegate()
			{
				this.ShowDialog("\\reset options?", "\\reset", this.main.ResetToFactoryDefaults);
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
			controlsLabel.FontFile.Value = "Font";
			controlsLabel.Text.Value = "\\controls title";
			controlsLabelContainer.Children.Add(controlsLabel);

			TextElement controlsScrollLabel = new TextElement();
			controlsScrollLabel.FontFile.Value = "Font";
			controlsScrollLabel.Text.Value = "\\scroll for more";
			controlsLabelContainer.Children.Add(controlsScrollLabel);

			bool controlsShown = false;

			Action hideControls = delegate()
			{
				controlsShown = false;

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

			Action<Property<PCInput.PCInputBinding>, string, bool, bool> addInputSetting = delegate(Property<PCInput.PCInputBinding> setting, string display, bool allowGamepad, bool allowMouse)
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
								if (binding.GamePadButton != Buttons.BigButton)
									newValue.GamePadButton = binding.GamePadButton;
							}
							else
								newValue.GamePadButton = Buttons.BigButton;

							setting.Value = newValue;
						}
						this.main.UI.EnableMouse.Value = true;
					});
				}));
				controlsList.Children.Add(button);
			};

			addInputSetting(this.main.Settings.Forward, "\\move forward", false, true);
			addInputSetting(this.main.Settings.Left, "\\move left", false, true);
			addInputSetting(this.main.Settings.Backward, "\\move backward", false, true);
			addInputSetting(this.main.Settings.Right, "\\move right", false, true);
			addInputSetting(this.main.Settings.Jump, "\\jump", true, true);
			addInputSetting(this.main.Settings.Parkour, "\\parkour", true, true);
			addInputSetting(this.main.Settings.RollKick, "\\roll / kick", true, true);
			addInputSetting(this.main.Settings.SpecialAbility, "\\special ability", true, true);
			addInputSetting(this.main.Settings.TogglePhone, "\\toggle phone", true, true);
			addInputSetting(this.main.Settings.QuickSave, "\\quicksave", true, true);
			addInputSetting(this.main.Settings.ToggleConsole, "\\toggle console", true, true);

			// Mapping LMB to toggle fullscreen makes it impossible to change any other settings.
			// So don't allow it.
			addInputSetting(this.main.Settings.ToggleFullscreen, "\\toggle fullscreen", true, false);

			// Start new button
			Container startNew = this.main.UIFactory.CreateButton("\\new game", delegate()
			{
				this.ShowDialog("\\alpha disclaimer", "\\play", delegate()
				{
					this.restorePausedSettings();
					this.main.CurrentSave.Value = null;
					this.main.AddComponent(new Animation
					(
						new Animation.Delay(0.2f),
						new Animation.Execute(delegate()
						{
							IO.MapLoader.Load(this.main, Main.InitialMap);
						})
					));
				});
			});
			this.resizeToMenu(startNew);
			this.pauseMenu.Children.Add(startNew);
			startNew.Add(new Binding<bool, string>(startNew.Visible, x => x == Main.MenuMap, this.main.MapFile));

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
			saveButton.Add(new Binding<bool>(saveButton.Visible, () => this.main.MapFile != Main.MenuMap && !this.main.EditorEnabled, this.main.MapFile, this.main.EditorEnabled));

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
			this.resizeToMenu(load);
			this.pauseMenu.Children.Add(load);

			// Sandbox button
			Container sandbox = this.main.UIFactory.CreateButton("\\sandbox", delegate()
			{
				this.ShowDialog("\\sandbox disclaimer", "\\play anyway", delegate()
				{
					this.restorePausedSettings();
					this.main.CurrentSave.Value = null;
					this.main.AddComponent(new Animation
					(
						new Animation.Delay(0.2f),
						new Animation.Execute(delegate()
						{
							IO.MapLoader.Load(this.main, "sandbox");
						})
					));
				});
			});
			this.resizeToMenu(sandbox);
			this.pauseMenu.Children.Add(sandbox);
			sandbox.Add(new Binding<bool, string>(sandbox.Visible, x => x == Main.MenuMap, this.main.MapFile));

			// Cheat menu
#if CHEAT
			Animation cheatAnimation = null;
			bool cheatShown = false;

			ListContainer cheatMenu = new ListContainer();
			cheatMenu.Visible.Value = false;
			cheatMenu.Add(new Binding<Vector2, Point>(cheatMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.main.ScreenSize));
			cheatMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
			this.main.UI.Root.Children.Add(cheatMenu);
			cheatMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

			Container cheatLabelPadding = this.main.UIFactory.CreateContainer();
			this.resizeToMenu(cheatLabelPadding);
			cheatMenu.Children.Add(cheatLabelPadding);

			ListContainer cheatLabelContainer = new ListContainer();
			cheatLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
			cheatLabelPadding.Children.Add(cheatLabelContainer);

			TextElement cheatLabel = new TextElement();
			cheatLabel.FontFile.Value = "Font";
			cheatLabel.Text.Value = "\\cheat title";
			cheatLabelContainer.Children.Add(cheatLabel);

			TextElement cheatScrollLabel = new TextElement();
			cheatScrollLabel.FontFile.Value = "Font";
			cheatScrollLabel.Text.Value = "\\scroll for more";
			cheatLabelContainer.Children.Add(cheatScrollLabel);

			Action hideCheat = delegate()
			{
				cheatShown = false;

				this.showPauseMenu();

				if (cheatAnimation != null)
					cheatAnimation.Delete.Execute();
				cheatAnimation = new Animation
				(
					new Animation.Vector2MoveToSpeed(cheatMenu.AnchorPoint, new Vector2(1, 0.5f), Menu.hideAnimationSpeed),
					new Animation.Set<bool>(cheatMenu.Visible, false)
				);
				this.main.AddComponent(cheatAnimation);
			};

			Container cheatBack = this.main.UIFactory.CreateButton("\\back", hideCheat);
			this.resizeToMenu(cheatBack);
			cheatMenu.Children.Add(cheatBack);

			ListContainer cheatList = new ListContainer();
			cheatList.Orientation.Value = ListContainer.ListOrientation.Vertical;

			foreach (KeyValuePair<string, string> item in Menu.maps)
			{
				string m = item.Key;
				Container button = this.main.UIFactory.CreateButton(item.Value, delegate()
				{
					hideCheat();
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
				this.resizeToMenu(button);
				cheatList.Children.Add(button);
			}
#if STEAMWORKS
			Container cheatIncrementTimePlayed = this.main.UIFactory.CreateButton("+60s", delegate()
			{
				SteamWorker.IncrementStat("stat_time_played", 60);
				SteamWorker.UploadStats();
			});
			this.resizeToMenu(cheatIncrementTimePlayed);
			cheatMenu.Children.Add(cheatIncrementTimePlayed);

			Container cheatUploadStats = this.main.UIFactory.CreateButton("Upload Stats", delegate()
			{
				SteamWorker.UploadStats();
			});
			this.resizeToMenu(cheatUploadStats);
			cheatMenu.Children.Add(cheatUploadStats);

			Container cheatResetStats = this.main.UIFactory.CreateButton("Reset Stats", delegate()
			{
				SteamWorker.ResetAllStats(false);
			});
			this.resizeToMenu(cheatResetStats);
			cheatMenu.Children.Add(cheatResetStats);

			Container cheatResetCheevos = this.main.UIFactory.CreateButton("Reset Stats and Achievements", delegate()
			{
				SteamWorker.ResetAllStats(true);
			});
			this.resizeToMenu(cheatResetCheevos);
			cheatMenu.Children.Add(cheatResetCheevos);
#endif

			Scroller cheatScroller = new Scroller();
			cheatScroller.Children.Add(cheatList);
			cheatScroller.Add(new Binding<Vector2>(cheatScroller.Size, () => new Vector2(cheatList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), cheatList.Size, this.main.ScreenSize));
			cheatMenu.Children.Add(cheatScroller);

			// Cheat button
			Container cheat = this.main.UIFactory.CreateButton("\\cheat", delegate()
			{
				this.hidePauseMenu();

				cheatMenu.Visible.Value = true;
				if (cheatAnimation != null)
					cheatAnimation.Delete.Execute();
				cheatAnimation = new Animation(new Animation.Ease(new Animation.Vector2MoveToSpeed(cheatMenu.AnchorPoint, new Vector2(0, 0.5f), Menu.animationSpeed), Animation.Ease.EaseType.OutExponential));
				this.main.AddComponent(cheatAnimation);

				cheatShown = true;
				this.currentMenu.Value = cheatList;
			});
			this.resizeToMenu(cheat);
			cheat.Add(new Binding<bool, string>(cheat.Visible, x => x == Main.MenuMap, this.main.MapFile));
			this.pauseMenu.Children.Add(cheat);
#endif

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

				this.currentMenu.Value = settingsMenu;
			});
			this.resizeToMenu(settingsButton);
			this.pauseMenu.Children.Add(settingsButton);

			// Edit mode toggle button
			Container switchToEditMode = this.main.UIFactory.CreateButton("\\edit mode", delegate()
			{
				this.pauseMenu.Visible.Value = false;

				this.main.Paused.Value = false;
				if (this.pauseAnimation != null)
				{
					this.pauseAnimation.Delete.Execute();
					this.pauseAnimation = null;
				}
				this.main.EditorEnabled.Value = true;
				this.main.CurrentSave.Value = null;
				if (this.main.MapFile == Main.MenuMap)
					IO.MapLoader.Load(this.main, Main.TemplateMap);
				else
					IO.MapLoader.Load(this.main, this.main.MapFile);
			});
			switchToEditMode.Add(new Binding<bool>(switchToEditMode.Visible, x => !x, this.main.EditorEnabled));
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
			creditsLabel.FontFile.Value = "Font";
			creditsLabel.Text.Value = "\\credits title";
			creditsLabelContainer.Children.Add(creditsLabel);

			TextElement creditsScrollLabel = new TextElement();
			creditsScrollLabel.FontFile.Value = "Font";
			creditsScrollLabel.Text.Value = "\\scroll for more";
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

			TextElement creditsDisplay = new TextElement();
			creditsDisplay.FontFile.Value = "Font";
			creditsDisplay.Text.Value = this.Credits = File.ReadAllText("attribution.txt");

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

			bool saving = false;
			this.input.Bind(this.main.Settings.QuickSave, PCInput.InputState.Down, delegate()
			{
				if (!saving && !this.main.Paused && this.main.MapFile != Main.MenuMap && PlayerFactory.Instance != null)
				{
					saving = true;
					Container notification = new Container();
					notification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
					notification.Opacity.Value = 0.5f;
					TextElement notificationText = new TextElement();
					notificationText.Name.Value = "Text";
					notificationText.FontFile.Value = "Font";
					notificationText.Text.Value = "\\saving";
					notification.Children.Add(notificationText);
					this.main.UI.Root.GetChildByName("Notifications").Children.Add(notification);
					this.main.AddComponent(new Animation
					(
						new Animation.Delay(0.01f),
						new Animation.Execute(delegate()
						{
							this.main.SaveOverwrite();
						}),
						new Animation.Set<string>(notificationText.Text, "\\saved"),
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(notification.Opacity, 0.0f, 1.0f),
							new Animation.FloatMoveTo(notificationText.Opacity, 0.0f, 1.0f)
						),
						new Animation.Execute(notification.Delete),
						new Animation.Execute(delegate()
						{
							saving = false;
						})
					));
				}
			});

			// Escape key
			// Make sure we can only pause when there is a player currently spawned
			// Otherwise we could save the current map without the player. And that would be awkward.
			Func<bool> canPause = delegate()
			{
				if (this.main.Paused)
					return this.main.MapFile.Value != Main.MenuMap; // Only allow pausing on the menu map, don't allow unpausing

				if (SteamWorker.OverlayActive || !SteamWorker.OverlaySafelyGone)
					return false;

				if (this.main.EditorEnabled)
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
#if CHEAT
				else if (cheatShown)
				{
					hideCheat();
					return;
				}
#endif

				if (this.main.MapFile.Value == Main.MenuMap)
				{
					if (this.currentMenu.Value == null)
						this.savePausedSettings();
				}
				else
				{
					this.main.Paused.Value = !this.main.Paused;

					if (this.main.Paused)
						this.savePausedSettings();
					else
						this.restorePausedSettings();
				}
			};

			this.input.Bind(this.main.Settings.ToggleConsole, PCInput.InputState.Down, delegate()
			{
				if (canPause() && ConsoleUI.Showing.Value == main.Paused.Value)
					togglePause();
				ConsoleUI.Showing.Value = !ConsoleUI.Showing.Value;
			});

			this.input.Add(new CommandBinding(input.GetKeyDown(Keys.Escape), () => canPause() || this.dialog != null, togglePause));
			this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.Start), canPause, togglePause));
			this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.B), () => canPause() || this.dialog != null, togglePause));

#if !DEVELOPMENT
			// Pause on window lost focus
			this.main.Deactivated += delegate(object sender, EventArgs e)
			{
				if (!this.main.Paused && this.main.MapFile.Value != Main.MenuMap && !this.main.EditorEnabled)
				{
					this.main.Paused.Value = true;
					this.savePausedSettings();
				}
			};
#endif
			// Gamepad menu code

			int selected = 0;

			Func<UIComponent, int, int, int> nextMenuItem = delegate(UIComponent menu, int current, int delta)
			{
				int end = menu.Children.Length;
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
				return item.Visible && item.GetType() == typeof(Container) && item.MouseLeftUp.HasBindings;
			};

			Func<UIComponent, bool> isScrollButton = delegate(UIComponent item)
			{
				return item.Visible && item.GetType() == typeof(Container) && item.MouseScrolled.HasBindings;
			};

			this.input.Add(new NotifyBinding(delegate()
			{
				UIComponent menu = this.currentMenu;
				if (menu != null && menu != creditsDisplay && this.main.GamePadConnected)
				{
					foreach (UIComponent item in menu.Children)
						item.Highlighted.Value = false;

					int i = 0;
					foreach (UIComponent item in menu.Children)
					{
						if (isButton(item) || isScrollButton(item))
						{
							item.Highlighted.Value = true;
							selected = i;
							break;
						}
						i++;
					}
				}
			}, this.currentMenu));

			Action<int> moveSelection = delegate(int delta)
			{
				UIComponent menu = this.currentMenu;
				if (menu != null && this.dialog == null)
				{
					if (menu == this.loadSaveList)
						delta = -delta;
					else if (menu == creditsDisplay)
					{
						Scroller scroll = (Scroller)menu.Parent;
						scroll.MouseScrolled.Execute(delta * -4);
						return;
					}

					Container button;
					if (selected < menu.Children.Length)
					{
						button = (Container)menu.Children[selected];
						button.Highlighted.Value = false;
					}

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

					button = (Container)menu.Children[selected];
					button.Highlighted.Value = true;

					if (menu.Parent.Value.GetType() == typeof(Scroller))
					{
						Scroller scroll = (Scroller)menu.Parent;
						scroll.ScrollTo(button);
					}
				}
			};

			Func<bool> enableGamepad = delegate()
			{
				return this.main.Paused || this.main.MapFile.Value == Main.MenuMap;
			};

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickUp), enableGamepad, delegate()
			{
				moveSelection(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadUp), enableGamepad, delegate()
			{
				moveSelection(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickDown), enableGamepad, delegate()
			{
				moveSelection(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadDown), enableGamepad, delegate()
			{
				moveSelection(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.A), enableGamepad, delegate()
			{
				if (this.dialog != null)
					this.dialog.GetChildByName("Okay").MouseLeftUp.Execute();
				else
				{
					UIComponent menu = this.currentMenu;
					if (menu != null && menu != creditsDisplay)
					{
						UIComponent selectedItem = menu.Children[selected];
						if (isButton(selectedItem) && selectedItem.Highlighted)
							selectedItem.MouseLeftUp.Execute();
					}
				}
			}));

			Action<int> scrollButton = delegate(int delta)
			{
				UIComponent menu = this.currentMenu;
				if (menu != null && menu != creditsDisplay && this.dialog == null)
				{
					UIComponent selectedItem = menu.Children[selected];
					if (isScrollButton(selectedItem) && selectedItem.Highlighted)
						selectedItem.MouseScrolled.Execute(delta);
				}
			};

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickLeft), enableGamepad, delegate()
			{
				scrollButton(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadLeft), enableGamepad, delegate()
			{
				scrollButton(-1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.LeftThumbstickRight), enableGamepad, delegate()
			{
				scrollButton(1);
			}));

			this.input.Add(new CommandBinding(this.input.GetButtonDown(Buttons.DPadRight), enableGamepad, delegate()
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

		public void Show()
		{
			if (!this.main.Paused)
			{
				this.main.Paused.Value = true;
				this.savePausedSettings();
			}
		}
	}
}
