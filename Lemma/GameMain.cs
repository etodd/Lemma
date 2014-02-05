#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;

using Lemma.Components;
using Lemma.Factories;
using Lemma.Util;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
#endregion

namespace Lemma
{
	public class GameMain : Main
	{
		public class ExitException : Exception
		{

		}

		public const int ConfigVersion = 4;
		public const int MapVersion = 4;
		public const int Build = 4;

		public class Config
		{
			public Property<bool> Fullscreen = new Property<bool> { Value = false };
			public Property<bool> Maximized = new Property<bool> { Value = false };
			public Property<Point> Origin = new Property<Point> { Value = new Point(50, 50) };
			public Property<Point> Size = new Property<Point> { Value = new Point(1280, 720) };
			public Property<Point> FullscreenResolution = new Property<Point> { Value = Point.Zero };
			public Property<float> MotionBlurAmount = new Property<float> { Value = 0.5f };
			public Property<float> Gamma = new Property<float> { Value = 1.0f };
			public Property<bool> EnableReflections = new Property<bool> { Value = true };
			public Property<bool> EnableBloom = new Property<bool> { Value = true };
			public Property<LightingManager.DynamicShadowSetting> DynamicShadows = new Property<LightingManager.DynamicShadowSetting> { Value = LightingManager.DynamicShadowSetting.High };
			public Property<bool> InvertMouseX = new Property<bool> { Value = false };
			public Property<bool> InvertMouseY = new Property<bool> { Value = false };
			public Property<float> MouseSensitivity = new Property<float> { Value = 1.0f };
			public Property<float> FieldOfView = new Property<float> { Value = MathHelper.ToRadians(80.0f) };
			public int Version;
			public string UUID;
			public Property<PCInput.PCInputBinding> Forward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.W } };
			public Property<PCInput.PCInputBinding> Left = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.A } };
			public Property<PCInput.PCInputBinding> Right = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.D } };
			public Property<PCInput.PCInputBinding> Backward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.S } };
			public Property<PCInput.PCInputBinding> Jump = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Space, GamePadButton = Buttons.RightTrigger } };
			public Property<PCInput.PCInputBinding> Parkour = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.LeftShift, GamePadButton = Buttons.LeftTrigger } };
			public Property<PCInput.PCInputBinding> Roll = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.LeftControl, GamePadButton = Buttons.LeftStick } };
			public Property<PCInput.PCInputBinding> Kick = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.LeftMouseButton, GamePadButton = Buttons.RightStick } };
			public Property<PCInput.PCInputBinding> TogglePhone = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Tab, GamePadButton = Buttons.Y } };
			public Property<PCInput.PCInputBinding> QuickSave = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F5, GamePadButton = Buttons.Back } };
			public Property<PCInput.PCInputBinding> ToggleFullscreen = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F11 } };
		}

		public class SaveInfo
		{
			public string MapFile;
			public int Version;
		}

		public bool CanSpawn = true;

		public Config Settings;
		private string settingsDirectory;
		private string saveDirectory;
		private string analyticsDirectory;
		private string settingsFile;

		private Entity player;
		private Entity editor;
		private PCInput input;

		private string initialMapFile;
		private bool allowEditing;

		private bool loadingSavedGame;

		public Property<string> StartSpawnPoint = new Property<string>();

		public Command<Entity> PlayerSpawned = new Command<Entity>();

		const float respawnInterval = 0.5f;

		private float respawnTimer = -1.0f;

		private bool saveAfterTakingScreenshot = false;

		private DisplayModeCollection supportedDisplayModes;

		private const float startGamma = 10.0f;
		private static Vector3 startTint = new Vector3(2.0f);

		public const int DefaultRespawnRewindLength = 3;
		public int RespawnRewindLength = DefaultRespawnRewindLength;

		private int displayModeIndex;

		private List<Property<PCInput.PCInputBinding>> bindings = new List<Property<PCInput.PCInputBinding>>();

		public GameMain(bool allowEditing, string mapFile)
			: base()
		{
			this.graphics.PreparingDeviceSettings += delegate(object sender, PreparingDeviceSettingsEventArgs args)
			{
				this.supportedDisplayModes = args.GraphicsDeviceInformation.Adapter.SupportedDisplayModes;
				int i = 0;
				foreach (DisplayMode mode in this.supportedDisplayModes)
				{
					if (mode.Format == SurfaceFormat.Color && mode.Width == this.Settings.FullscreenResolution.Value.X && mode.Height == this.Settings.FullscreenResolution.Value.Y)
					{
						this.displayModeIndex = i;
						break;
					}
					i++;
				}
			};
			this.EditorEnabled.Value = allowEditing;
			this.initialMapFile = mapFile;
			this.allowEditing = allowEditing;
			this.settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lemma");
			if (!Directory.Exists(this.settingsDirectory))
				Directory.CreateDirectory(this.settingsDirectory);
			this.settingsFile = Path.Combine(this.settingsDirectory, "settings.xml");
			this.saveDirectory = Path.Combine(this.settingsDirectory, "saves");
			if (!Directory.Exists(this.saveDirectory))
				Directory.CreateDirectory(this.saveDirectory);
			this.analyticsDirectory = Path.Combine(this.settingsDirectory, "analytics");
			if (!Directory.Exists(this.analyticsDirectory))
				Directory.CreateDirectory(this.analyticsDirectory);

			try
			{
				// Attempt to load previous window state
				using (Stream stream = new FileStream(this.settingsFile, FileMode.Open, FileAccess.Read, FileShare.None))
					this.Settings = (Config)new XmlSerializer(typeof(Config)).Deserialize(stream);
				if (this.Settings.Version != GameMain.ConfigVersion)
					throw new Exception();
			}
			catch (Exception) // File doesn't exist, there was a deserialization error, or we are on a new version. Use default window settings
			{
				this.Settings = new Config { Version = GameMain.ConfigVersion, };
			}

			if (this.Settings.UUID == null)
				Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32);

			// Restore window state
			if (this.Settings.Fullscreen)
				this.ResizeViewport(this.Settings.FullscreenResolution.Value.X, this.Settings.FullscreenResolution.Value.Y, true);
			else
				this.ResizeViewport(this.Settings.Size.Value.X, this.Settings.Size.Value.Y, false, false);
		}

		public override void ClearEntities(bool deleteEditor)
		{
			base.ClearEntities(deleteEditor);
			this.UI.Root.GetChildByName("Messages").Children.Clear();
		}

		private void copySave(string src, string dst)
		{
			if (!Directory.Exists(dst))
				Directory.CreateDirectory(dst);

			string[] ignoredExtensions = new[] { ".cs", ".dll", ".xnb", };

			foreach (string path in Directory.GetFiles(src))
			{
				string filename = Path.GetFileName(path);
				if (filename == "thumbnail.jpg" || filename == "save.xml" || ignoredExtensions.Contains(Path.GetExtension(filename)))
					continue;
				File.Copy(path, Path.Combine(dst, filename));
			}

			foreach (string path in Directory.GetDirectories(src))
				this.copySave(path, Path.Combine(dst, Path.GetFileName(path)));
		}

		private UIComponent createMenuButton<Type>(string label, Property<Type> property)
		{
			return this.createMenuButton<Type>(label, property, (x) => x.ToString());
		}

		private UIComponent createMenuButton<Type>(string label, Property<Type> property, Func<Type, string> conversion)
		{
			UIComponent result = this.createMenuButton(label);
			TextElement text = (TextElement)result.GetChildByName("Text");
			text.Add(new Binding<string, Type>(text.Text, x => label + ": " + conversion(x), property));
			return result;
		}

		private UIComponent createMenuButton(string label)
		{
			Container result = new Container();
			result.Tint.Value = Color.Black;
			result.Add(new Binding<float, bool>(result.Opacity, x => x ? 1.0f : 0.5f, result.Highlighted));
			TextElement text = new TextElement();
			text.Name.Value = "Text";
			text.FontFile.Value = "Font";
			text.Text.Value = label;
			result.Children.Add(text);
			return result;
		}

#if ANALYTICS
		public Session.Recorder SessionRecorder;

		public void SaveAnalytics()
		{
			this.SessionRecorder.Save(Path.Combine(this.analyticsDirectory, this.MapFile.Value + "-" + Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32) + ".xml"));
		}

		public string[] AnalyticsSessionFiles
		{
			get
			{
				return Directory.GetFiles(this.analyticsDirectory, "*", SearchOption.TopDirectoryOnly);
			}
		}
#endif

		public List<Session> LoadAnalytics(string map)
		{
			List<Session> result = new List<Session>();
			foreach (string file in Directory.GetFiles(this.analyticsDirectory, "*", SearchOption.TopDirectoryOnly))
			{
				string name = Path.GetFileNameWithoutExtension(file);
				name = name.Substring(0, name.LastIndexOf('-'));
				if (name == map)
					result.Add(Session.Load(file));
			}
			return result;
		}

		// Takes a screenshot and saves a directory with a copy of all the map files
		public Command Save = new Command();

		// Just saves the current map file
		public Command SaveCurrentMap = new Command();

		protected string currentSave;

		protected override void LoadContent()
		{
			bool firstInitialization = this.firstLoadContentCall;
			base.LoadContent();

			if (firstInitialization)
			{
				this.IsMouseVisible.Value = true;

#if ANALYTICS
				this.SessionRecorder = new Session.Recorder();
				this.AddComponent(this.SessionRecorder);

				this.SessionRecorder.Add("Position", delegate()
				{
					if (this.player != null && this.player.Active)
						return this.player.Get<Transform>().Position;
					else
						return Vector3.Zero;
				});

				this.SessionRecorder.Add("Health", delegate()
				{
					if (this.player != null && this.player.Active)
						return this.player.Get<Player>().Health;
					else
						return 0.0f;
				});

				this.SessionRecorder.Add("Stamina", delegate()
				{
					if (this.player != null && this.player.Active)
						return this.player.Get<Player>().Stamina;
					else
						return 0;
				});
#endif

				this.MapFile.Set = delegate(string value)
				{
					this.ClearEntities(false);

					if (value == null || value.Length == 0)
					{
						this.MapFile.InternalValue = null;
						return;
					}

					try
					{
						IO.MapLoader.Load(this, this.currentSave == null ? null : Path.Combine(this.saveDirectory, this.currentSave), value, false);
						this.MapFile.InternalValue = value;
					}
					catch (FileNotFoundException)
					{
						this.MapFile.InternalValue = value;
						// Create a new map
						Entity world = Factory.CreateAndBind(this, "World");
						world.Get<Transform>().Position.Value = new Vector3(0, 3, 0);
						this.Add(world);

						Entity ambientLight = Factory.CreateAndBind(this, "AmbientLight");
						ambientLight.Get<Transform>().Position.Value = new Vector3(0, 5.0f, 0);
						ambientLight.Get<AmbientLight>().Color.Value = new Vector3(0.25f, 0.25f, 0.25f);
						this.Add(ambientLight);

						Entity map = Factory.CreateAndBind(this, "Map");
						map.Get<Transform>().Position.Value = new Vector3(0, 1, 0);
						this.Add(map);

						this.MapLoaded.Execute();
					}
				};

				this.Renderer.LightRampTexture.Value = "Images\\default-ramp";
				this.Renderer.EnvironmentMap.Value = "Maps\\env0";

				this.input = new PCInput();
				this.AddComponent(this.input);

				new TwoWayBinding<LightingManager.DynamicShadowSetting>(this.Settings.DynamicShadows, this.LightingManager.DynamicShadows);
				new TwoWayBinding<float>(this.Settings.MotionBlurAmount, this.Renderer.MotionBlurAmount);
				new TwoWayBinding<float>(this.Settings.Gamma, this.Renderer.Gamma);
				new TwoWayBinding<bool>(this.Settings.EnableBloom, this.Renderer.EnableBloom);
				new TwoWayBinding<float>(this.Settings.FieldOfView, this.Camera.FieldOfView);
				if (this.Settings.FullscreenResolution.Value.X == 0)
				{
					Microsoft.Xna.Framework.Graphics.DisplayMode display = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
					this.Settings.FullscreenResolution.Value = new Point(display.Width, display.Height);
				}

				// Message list
				ListContainer messages = new ListContainer();
				messages.Alignment.Value = ListContainer.ListAlignment.Max;
				messages.AnchorPoint.Value = new Vector2(1.0f, 1.0f);
				messages.Reversed.Value = true;
				messages.Name.Value = "Messages";
				messages.Add(new Binding<Vector2, Point>(messages.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.9f), this.ScreenSize));
				this.UI.Root.Children.Add(messages);

				ListContainer notifications = new ListContainer();
				notifications.Alignment.Value = ListContainer.ListAlignment.Max;
				notifications.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
				notifications.Name.Value = "Notifications";
				notifications.Add(new Binding<Vector2, Point>(notifications.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.1f), this.ScreenSize));
				this.UI.Root.Children.Add(notifications);

				bool controlsShown = false;

				// Toggle fullscreen
				this.input.Bind(this.Settings.ToggleFullscreen, PCInput.InputState.Down, delegate()
				{
					if (this.graphics.IsFullScreen) // Already fullscreen. Go to windowed mode.
						this.ExitFullscreen();
					else // In windowed mode. Go to fullscreen.
						this.EnterFullscreen();
				});

				// Fullscreen message
				Container msgBackground = new Container();
				this.UI.Root.Children.Add(msgBackground);
				msgBackground.Tint.Value = Color.Black;
				msgBackground.Opacity.Value = 0.2f;
				msgBackground.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
				msgBackground.Add(new Binding<Vector2, Point>(msgBackground.Position, x => new Vector2(x.X * 0.5f, x.Y - 30.0f), this.ScreenSize));
				TextElement msg = new TextElement();
				msg.FontFile.Value = "Font";
				msg.Add(new Binding<string, PCInput.PCInputBinding>(msg.Text, x => x.ToString() + " - Toggle Fullscreen", this.Settings.ToggleFullscreen));
				msgBackground.Children.Add(msg);
				this.AddComponent(new Animation
				(
					new Animation.Delay(4.0f),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(msgBackground.Opacity, 0.0f, 2.0f),
						new Animation.FloatMoveTo(msg.Opacity, 0.0f, 2.0f)
					),
					new Animation.Execute(delegate() { this.UI.Root.Children.Remove(msgBackground); })
				));

				// Logo
				Sprite logo = new Sprite();
				logo.Image.Value = "Images\\logo";
				logo.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				logo.Add(new Binding<Vector2, Point>(logo.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.ScreenSize));
				logo.Add(new Binding<Vector2>(logo.Scale, () => new Vector2((this.ScreenSize.Value.X * 0.75f) / logo.Size.Value.X), this.ScreenSize, logo.Size));
				this.UI.Root.Children.Add(logo);

				Property<UIComponent> currentMenu = new Property<UIComponent> { Value = null };

				// Pause menu
				ListContainer pauseMenu = new ListContainer();
				pauseMenu.Visible.Value = false;
				pauseMenu.Add(new Binding<Vector2, Point>(pauseMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(pauseMenu);
				pauseMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Container pauseLabelContainer = new Container();
				pauseLabelContainer.Opacity.Value = 0.0f;

				TextElement pauseLabel = new TextElement();
				pauseLabel.FontFile.Value = "Font";
				pauseLabel.Text.Value = "L E M M A";
				pauseLabelContainer.Children.Add(pauseLabel);

				pauseMenu.Children.Add(pauseLabelContainer);

				Animation pauseAnimation = null;

				Action hidePauseMenu = delegate()
				{
					if (pauseAnimation != null)
						pauseAnimation.Delete.Execute();
					pauseAnimation = new Animation
					(
						new Animation.Vector2MoveToSpeed(pauseMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
						new Animation.Set<bool>(pauseMenu.Visible, false)
					);
					this.AddComponent(pauseAnimation);
					currentMenu.Value = null;
				};

				Action showPauseMenu = delegate()
				{
					pauseMenu.Visible.Value = true;
					if (pauseAnimation != null)
						pauseAnimation.Delete.Execute();
					pauseAnimation = new Animation(new Animation.Vector2MoveToSpeed(pauseMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(pauseAnimation);
					currentMenu.Value = pauseMenu;
				};

				// Settings to be restored when unpausing
				float originalBlurAmount = 0.0f;
				bool originalMouseVisible = false;
				Point originalMousePosition = new Point();

				RenderTarget2D screenshot = null;
				Point screenshotSize = Point.Zero;

				Action<bool> setupScreenshot = delegate(bool s)
				{
					this.saveAfterTakingScreenshot = s;
					screenshotSize = this.ScreenSize;
					screenshot = new RenderTarget2D(this.GraphicsDevice, screenshotSize.X, screenshotSize.Y, false, SurfaceFormat.Color, DepthFormat.Depth16);
					this.renderTarget = screenshot;
				};

				// Pause
				Action savePausedSettings = delegate()
				{
					// Take screenshot
					setupScreenshot(false);

					originalMouseVisible = this.IsMouseVisible;
					this.IsMouseVisible.Value = true;
					originalBlurAmount = this.Renderer.BlurAmount;

					// Save mouse position
					MouseState mouseState = this.MouseState;
					originalMousePosition = new Point(mouseState.X, mouseState.Y);

					pauseMenu.Visible.Value = true;
					pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);

					// Blur the screen and show the pause menu
					if (pauseAnimation != null && pauseAnimation.Active)
						pauseAnimation.Delete.Execute();

					pauseAnimation = new Animation
					(
						new Animation.Parallel
						(
							new Animation.FloatMoveToSpeed(this.Renderer.BlurAmount, 1.0f, 1.0f),
							new Animation.Vector2MoveToSpeed(pauseMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f)
						)
					);
					this.AddComponent(pauseAnimation);

					currentMenu.Value = pauseMenu;
				};

				// Unpause
				Action restorePausedSettings = delegate()
				{
					if (pauseAnimation != null && pauseAnimation.Active)
						pauseAnimation.Delete.Execute();

					// Restore mouse
					if (!originalMouseVisible)
					{
						// Only restore mouse position if the cursor was not visible
						// i.e., we're in first-person camera mode
						Microsoft.Xna.Framework.Input.Mouse.SetPosition(originalMousePosition.X, originalMousePosition.Y);
						MouseState m = new MouseState(originalMousePosition.X, originalMousePosition.Y, this.MouseState.Value.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
						this.LastMouseState.Value = m;
						this.MouseState.Value = m;
					}
					this.IsMouseVisible.Value = originalMouseVisible;

					this.saveSettings();

					// Unlur the screen and show the pause menu
					if (pauseAnimation != null && pauseAnimation.Active)
						pauseAnimation.Delete.Execute();

					this.Renderer.BlurAmount.Value = originalBlurAmount;
					pauseAnimation = new Animation
					(
						new Animation.Parallel
						(
							new Animation.Sequence
							(
								new Animation.Vector2MoveToSpeed(pauseMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
								new Animation.Set<bool>(pauseMenu.Visible, false)
							)
						)
					);
					this.AddComponent(pauseAnimation);

					if (screenshot != null)
					{
						screenshot.Dispose();
						screenshot = null;
						screenshotSize = Point.Zero;
					}

					currentMenu.Value = null;
				};

				// Load / save menu
				ListContainer loadSaveMenu = new ListContainer();
				loadSaveMenu.Visible.Value = false;
				loadSaveMenu.Add(new Binding<Vector2, Point>(loadSaveMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				loadSaveMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				loadSaveMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;
				this.UI.Root.Children.Add(loadSaveMenu);

				bool loadSaveShown = false;
				Animation loadSaveAnimation = null;

				Property<bool> saveMode = new Property<bool> { Value = false };

				Container dialog = null;

				Action<string, string, Action> showDialog = delegate(string question, string action, Action callback)
				{
					if (dialog != null)
						dialog.Delete.Execute();
					dialog = new Container();
					dialog.Tint.Value = Color.Black;
					dialog.Opacity.Value = 0.2f;
					dialog.AnchorPoint.Value = new Vector2(0.5f);
					dialog.Add(new Binding<Vector2, Point>(dialog.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.ScreenSize));
					dialog.Add(new CommandBinding(dialog.Delete, delegate()
					{
						loadSaveMenu.EnableInput.Value = true;
					}));
					this.UI.Root.Children.Add(dialog);

					ListContainer dialogLayout = new ListContainer();
					dialogLayout.Orientation.Value = ListContainer.ListOrientation.Vertical;
					dialog.Children.Add(dialogLayout);

					TextElement prompt = new TextElement();
					prompt.FontFile.Value = "Font";
					prompt.Text.Value = question;
					dialogLayout.Children.Add(prompt);

					ListContainer dialogButtons = new ListContainer();
					dialogButtons.Orientation.Value = ListContainer.ListOrientation.Horizontal;
					dialogLayout.Children.Add(dialogButtons);

					UIComponent overwrite = this.createMenuButton(action);
					overwrite.Name.Value = "Okay";
					dialogButtons.Children.Add(overwrite);
					overwrite.Add(new CommandBinding<Point>(overwrite.MouseLeftUp, delegate(Point p2)
					{
						dialog.Delete.Execute();
						dialog = null;
						callback();
					}));

					UIComponent cancel = this.createMenuButton("Cancel");
					dialogButtons.Children.Add(cancel);
					cancel.Add(new CommandBinding<Point>(cancel.MouseLeftUp, delegate(Point p2)
					{
						dialog.Delete.Execute();
						dialog = null;
					}));
				};

				Action hideLoadSave = delegate()
				{
					showPauseMenu();

					if (dialog != null)
					{
						dialog.Delete.Execute();
						dialog = null;
					}

					loadSaveShown = false;

					if (loadSaveAnimation != null)
						loadSaveAnimation.Delete.Execute();
					loadSaveAnimation = new Animation
					(
						new Animation.Vector2MoveToSpeed(loadSaveMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
						new Animation.Set<bool>(loadSaveMenu.Visible, false)
					);
					this.AddComponent(loadSaveAnimation);
				};

				Container loadSavePadding = new Container();
				loadSavePadding.Opacity.Value = 0.0f;
				loadSavePadding.PaddingLeft.Value = 8.0f;
				loadSaveMenu.Children.Add(loadSavePadding);

				ListContainer loadSaveLabelContainer = new ListContainer();
				loadSaveLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
				loadSavePadding.Children.Add(loadSaveLabelContainer);

				TextElement loadSaveLabel = new TextElement();
				loadSaveLabel.FontFile.Value = "Font";
				loadSaveLabel.Add(new Binding<string, bool>(loadSaveLabel.Text, x => x ? "S A V E" : "L O A D", saveMode));
				loadSaveLabelContainer.Children.Add(loadSaveLabel);

				TextElement loadSaveScrollLabel = new TextElement();
				loadSaveScrollLabel.FontFile.Value = "Font";
				loadSaveScrollLabel.Text.Value = "Scroll for more";
				loadSaveLabelContainer.Children.Add(loadSaveScrollLabel);

				TextElement quickSaveLabel = new TextElement();
				quickSaveLabel.FontFile.Value = "Font";
				quickSaveLabel.Add(new Binding<bool>(quickSaveLabel.Visible, saveMode));
				quickSaveLabel.Add(new Binding<string>(quickSaveLabel.Text, () => this.Settings.QuickSave.Value.ToString() + " to quicksave", this.Settings.QuickSave));
				loadSaveLabelContainer.Children.Add(quickSaveLabel);

				UIComponent loadSaveBack = this.createMenuButton("Back");
				loadSaveBack.Add(new CommandBinding<Point>(loadSaveBack.MouseLeftUp, delegate(Point p)
				{
					hideLoadSave();
				}));
				loadSaveMenu.Children.Add(loadSaveBack);

				UIComponent saveNew = this.createMenuButton("Save new");
				saveNew.Add(new Binding<bool>(saveNew.Visible, saveMode));
				loadSaveMenu.Children.Add(saveNew);

				Scroller loadSaveScroll = new Scroller();
				loadSaveScroll.Add(new Binding<Vector2, Point>(loadSaveScroll.Size, x => new Vector2(276.0f, x.Y * 0.5f), this.ScreenSize));
				loadSaveMenu.Children.Add(loadSaveScroll);

				ListContainer loadSaveList = new ListContainer();
				loadSaveList.Orientation.Value = ListContainer.ListOrientation.Vertical;
				loadSaveList.Reversed.Value = true;
				loadSaveScroll.Children.Add(loadSaveList);

				Action save = null;

				Action<string> addSaveGame = delegate(string timestamp)
				{
					SaveInfo info = null;
					try
					{
						using (Stream stream = new FileStream(Path.Combine(this.saveDirectory, timestamp, "save.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
							info = (SaveInfo)new XmlSerializer(typeof(SaveInfo)).Deserialize(stream);
						if (info.Version != GameMain.MapVersion)
							throw new Exception();
					}
					catch (Exception)
					{
						string savePath = Path.Combine(this.saveDirectory, timestamp);
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

					Container container = new Container();
					container.Tint.Value = Color.Black;
					container.Add(new Binding<float, bool>(container.Opacity, x => x ? 1.0f : 0.2f, container.Highlighted));
					container.UserData.Value = timestamp;

					ListContainer layout = new ListContainer();
					layout.Orientation.Value = ListContainer.ListOrientation.Vertical;
					container.Children.Add(layout);

					Sprite sprite = new Sprite();
					sprite.IsStandardImage.Value = true;
					sprite.Image.Value = Path.Combine(this.saveDirectory, timestamp, "thumbnail.jpg");
					layout.Children.Add(sprite);

					TextElement label = new TextElement();
					label.FontFile.Value = "Font";
					label.Text.Value = timestamp;
					layout.Children.Add(label);

					container.Add(new CommandBinding<Point>(container.MouseLeftUp, delegate(Point p)
					{
						if (saveMode)
						{
							loadSaveMenu.EnableInput.Value = false;
							showDialog("Overwrite this save?", "Overwrite", delegate()
							{
								container.Delete.Execute();
								save();
								Directory.Delete(Path.Combine(this.saveDirectory, timestamp), true);
								hideLoadSave();
								this.Paused.Value = false;
								restorePausedSettings();
							});
						}
						else
						{
							this.loadingSavedGame = true;
							hideLoadSave();
							this.Paused.Value = false;
							restorePausedSettings();
							this.currentSave = timestamp;
							this.MapFile.Value = info.MapFile;
						}
					}));

					loadSaveList.Children.Add(container);
					loadSaveScroll.ScrollToTop();
				};

				Action createNewSave = delegate()
				{
					string newSave = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
					if (newSave != this.currentSave)
					{
						this.copySave(this.currentSave == null ? "Content\\Maps" : Path.Combine(this.saveDirectory, this.currentSave), Path.Combine(this.saveDirectory, newSave));
						this.currentSave = newSave;
					}
				};

				this.SaveCurrentMap.Action = delegate()
				{
					if (this.currentSave == null)
						createNewSave();
					IO.MapLoader.Save(this, Path.Combine(this.saveDirectory, this.currentSave), this.MapFile);
				};

				save = delegate()
				{
					createNewSave();

					using (Stream stream = File.OpenWrite(Path.Combine(this.saveDirectory, this.currentSave, "thumbnail.jpg")))
						screenshot.SaveAsJpeg(stream, 256, (int)(screenshotSize.Y * (256.0f / screenshotSize.X)));

					this.SaveCurrentMap.Execute();

					try
					{
						using (Stream stream = new FileStream(Path.Combine(this.saveDirectory, this.currentSave, "save.xml"), FileMode.Create, FileAccess.Write, FileShare.None))
							new XmlSerializer(typeof(SaveInfo)).Serialize(stream, new SaveInfo { MapFile = this.MapFile, Version = GameMain.MapVersion });
					}
					catch (InvalidOperationException e)
					{
						throw new Exception("Failed to save game.", e);
					}

					addSaveGame(this.currentSave);
				};

				this.Save.Action = delegate()
				{
					if (screenshot == null)
						setupScreenshot(true);
					else
					{
						// Delete the old save thumbnail.
						string oldSave = this.currentSave;
						if (oldSave != null)
						{
							UIComponent container = loadSaveList.Children.FirstOrDefault(x => ((string)x.UserData.Value) == this.currentSave);
							if (container != null)
								container.Delete.Execute();
						}

						// Create the new save.
						save();

						// Delete the old save files.
						// We have to do this AFTER creating the new save
						// because it copies the old save to create the new one
						if (oldSave != null)
							Directory.Delete(Path.Combine(this.saveDirectory, oldSave), true);

						this.saveAfterTakingScreenshot = false;
						screenshot.Dispose();
						screenshot = null;
						screenshotSize = Point.Zero;
					}
				};

				foreach (string saveFile in Directory.GetDirectories(this.saveDirectory, "*", SearchOption.TopDirectoryOnly).Select(x => Path.GetFileName(x)).OrderBy(x => x))
					addSaveGame(saveFile);

				saveNew.Add(new CommandBinding<Point>(saveNew.MouseLeftUp, delegate(Point p)
				{
					save();
					hideLoadSave();
					this.Paused.Value = false;
					restorePausedSettings();
				}));

				// Settings menu
				bool settingsShown = false;
				Animation settingsAnimation = null;

				ListContainer settingsMenu = new ListContainer();
				settingsMenu.Visible.Value = false;
				settingsMenu.Add(new Binding<Vector2, Point>(settingsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				settingsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(settingsMenu);
				settingsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Container settingsLabelPadding = new Container();
				settingsLabelPadding.PaddingLeft.Value = 8.0f;
				settingsLabelPadding.Opacity.Value = 0.0f;
				settingsMenu.Children.Add(settingsLabelPadding);

				ListContainer settingsLabelContainer = new ListContainer();
				settingsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
				settingsLabelPadding.Children.Add(settingsLabelContainer);

				TextElement settingsLabel = new TextElement();
				settingsLabel.FontFile.Value = "Font";
				settingsLabel.Text.Value = "O P T I O N S";
				settingsLabelContainer.Children.Add(settingsLabel);

				TextElement settingsScrollLabel = new TextElement();
				settingsScrollLabel.FontFile.Value = "Font";
				settingsScrollLabel.Text.Value = "Scroll or click to modify";
				settingsLabelContainer.Children.Add(settingsScrollLabel);

				Action hideSettings = delegate()
				{
					showPauseMenu();

					settingsShown = false;

					if (settingsAnimation != null)
						settingsAnimation.Delete.Execute();
					settingsAnimation = new Animation
					(
						new Animation.Vector2MoveToSpeed(settingsMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
						new Animation.Set<bool>(settingsMenu.Visible, false)
					);
					this.AddComponent(settingsAnimation);
				};

				UIComponent settingsBack = this.createMenuButton("Back");
				settingsBack.Add(new CommandBinding<Point>(settingsBack.MouseLeftUp, delegate(Point p)
				{
					hideSettings();
				}));
				settingsMenu.Children.Add(settingsBack);

				UIComponent fullscreenResolution = this.createMenuButton<Point>("Fullscreen Resolution", this.Settings.FullscreenResolution, x => x.X.ToString() + "x" + x.Y.ToString());
				
				Action<int> changeFullscreenResolution = delegate(int scroll)
				{
					displayModeIndex = (displayModeIndex + scroll) % this.supportedDisplayModes.Count();
					while (displayModeIndex < 0)
						displayModeIndex += this.supportedDisplayModes.Count();
					DisplayMode mode = this.supportedDisplayModes.ElementAt(displayModeIndex);
					this.Settings.FullscreenResolution.Value = new Point(mode.Width, mode.Height);
				};

				fullscreenResolution.Add(new CommandBinding<Point>(fullscreenResolution.MouseLeftUp, delegate(Point mouse)
				{
					changeFullscreenResolution(1);
				}));
				fullscreenResolution.Add(new CommandBinding<Point, int>(fullscreenResolution.MouseScrolled, delegate(Point mouse, int scroll)
				{
					changeFullscreenResolution(scroll);
				}));
				settingsMenu.Children.Add(fullscreenResolution);

				UIComponent gamma = this.createMenuButton<float>("Gamma", this.Renderer.Gamma, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				gamma.Add(new CommandBinding<Point, int>(gamma.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Renderer.Gamma.Value = Math.Max(0, Math.Min(2, this.Renderer.Gamma + (scroll * 0.1f)));
				}));
				settingsMenu.Children.Add(gamma);

				UIComponent fieldOfView = this.createMenuButton<float>("Field of View", this.Camera.FieldOfView, x => ((int)Math.Round(MathHelper.ToDegrees(this.Camera.FieldOfView))).ToString() + " deg");
				fieldOfView.Add(new CommandBinding<Point, int>(fieldOfView.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Camera.FieldOfView.Value = Math.Max(MathHelper.ToRadians(60.0f), Math.Min(MathHelper.ToRadians(120.0f), this.Camera.FieldOfView + MathHelper.ToRadians(scroll)));
				}));
				settingsMenu.Children.Add(fieldOfView);

				UIComponent motionBlurAmount = this.createMenuButton<float>("Motion Blur Amount", this.Renderer.MotionBlurAmount, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				motionBlurAmount.Add(new CommandBinding<Point, int>(motionBlurAmount.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Renderer.MotionBlurAmount.Value = Math.Max(0, Math.Min(1, this.Renderer.MotionBlurAmount + (scroll * 0.1f)));
				}));
				settingsMenu.Children.Add(motionBlurAmount);

				UIComponent reflectionsEnabled = this.createMenuButton<bool>("Reflections Enabled", this.Settings.EnableReflections);
				reflectionsEnabled.Add(new CommandBinding<Point, int>(reflectionsEnabled.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Settings.EnableReflections.Value = !this.Settings.EnableReflections;
				}));
				reflectionsEnabled.Add(new CommandBinding<Point>(reflectionsEnabled.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.EnableReflections.Value = !this.Settings.EnableReflections;
				}));
				settingsMenu.Children.Add(reflectionsEnabled);

				UIComponent bloomEnabled = this.createMenuButton<bool>("Bloom Enabled", this.Renderer.EnableBloom);
				bloomEnabled.Add(new CommandBinding<Point, int>(bloomEnabled.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Renderer.EnableBloom.Value = !this.Renderer.EnableBloom;
				}));
				bloomEnabled.Add(new CommandBinding<Point>(bloomEnabled.MouseLeftUp, delegate(Point mouse)
				{
					this.Renderer.EnableBloom.Value = !this.Renderer.EnableBloom;
				}));
				settingsMenu.Children.Add(bloomEnabled);

				UIComponent dynamicShadows = this.createMenuButton<LightingManager.DynamicShadowSetting>("Dynamic Shadows", this.LightingManager.DynamicShadows);
				int numDynamicShadowSettings = typeof(LightingManager.DynamicShadowSetting).GetFields(BindingFlags.Static | BindingFlags.Public).Length;
				dynamicShadows.Add(new CommandBinding<Point, int>(dynamicShadows.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.LightingManager.DynamicShadows.Value = (LightingManager.DynamicShadowSetting)Enum.ToObject(typeof(LightingManager.DynamicShadowSetting), (((int)this.LightingManager.DynamicShadows.Value) + scroll) % numDynamicShadowSettings);
				}));
				dynamicShadows.Add(new CommandBinding<Point>(dynamicShadows.MouseLeftUp, delegate(Point mouse)
				{
					this.LightingManager.DynamicShadows.Value = (LightingManager.DynamicShadowSetting)Enum.ToObject(typeof(LightingManager.DynamicShadowSetting), (((int)this.LightingManager.DynamicShadows.Value) + 1) % numDynamicShadowSettings);
				}));
				settingsMenu.Children.Add(dynamicShadows);

				// Controls menu
				Animation controlsAnimation = null;

				ListContainer controlsMenu = new ListContainer();
				controlsMenu.Visible.Value = false;
				controlsMenu.Add(new Binding<Vector2, Point>(controlsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				controlsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(controlsMenu);
				controlsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Container controlsLabelPadding = new Container();
				controlsLabelPadding.PaddingLeft.Value = 8.0f;
				controlsLabelPadding.Opacity.Value = 0.0f;
				controlsMenu.Children.Add(controlsLabelPadding);

				ListContainer controlsLabelContainer = new ListContainer();
				controlsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
				controlsLabelPadding.Children.Add(controlsLabelContainer);

				TextElement controlsLabel = new TextElement();
				controlsLabel.FontFile.Value = "Font";
				controlsLabel.Text.Value = "C O N T R O L S";
				controlsLabelContainer.Children.Add(controlsLabel);

				TextElement controlsScrollLabel = new TextElement();
				controlsScrollLabel.FontFile.Value = "Font";
				controlsScrollLabel.Text.Value = "Scroll for more";
				controlsLabelContainer.Children.Add(controlsScrollLabel);

				Action hideControls = delegate()
				{
					controlsShown = false;

					showPauseMenu();

					if (controlsAnimation != null)
						controlsAnimation.Delete.Execute();
					controlsAnimation = new Animation
					(
						new Animation.Vector2MoveToSpeed(controlsMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
						new Animation.Set<bool>(controlsMenu.Visible, false)
					);
					this.AddComponent(controlsAnimation);
				};

				UIComponent controlsBack = this.createMenuButton("Back");
				controlsBack.Add(new CommandBinding<Point>(controlsBack.MouseLeftUp, delegate(Point p)
				{
					hideControls();
				}));
				controlsMenu.Children.Add(controlsBack);

				ListContainer controlsList = new ListContainer();
				controlsList.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Scroller controlsScroller = new Scroller();
				controlsScroller.Add(new Binding<Vector2>(controlsScroller.Size, () => new Vector2(controlsList.Size.Value.X, this.ScreenSize.Value.Y * 0.5f), controlsList.Size, this.ScreenSize));
				controlsScroller.Children.Add(controlsList);
				controlsMenu.Children.Add(controlsScroller);

				UIComponent invertMouseX = this.createMenuButton<bool>("Invert Look X", this.Settings.InvertMouseX);
				invertMouseX.Add(new CommandBinding<Point, int>(invertMouseX.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Settings.InvertMouseX.Value = !this.Settings.InvertMouseX;
				}));
				invertMouseX.Add(new CommandBinding<Point>(invertMouseX.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.InvertMouseX.Value = !this.Settings.InvertMouseX;
				}));
				controlsList.Children.Add(invertMouseX);

				UIComponent invertMouseY = this.createMenuButton<bool>("Invert Look Y", this.Settings.InvertMouseY);
				invertMouseY.Add(new CommandBinding<Point, int>(invertMouseY.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Settings.InvertMouseY.Value = !this.Settings.InvertMouseY;
				}));
				invertMouseY.Add(new CommandBinding<Point>(invertMouseY.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.InvertMouseY.Value = !this.Settings.InvertMouseY;
				}));
				controlsList.Children.Add(invertMouseY);

				UIComponent mouseSensitivity = this.createMenuButton<float>("Look Sensitivity", this.Settings.MouseSensitivity, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				mouseSensitivity.SwallowMouseEvents.Value = true;
				mouseSensitivity.Add(new CommandBinding<Point, int>(mouseSensitivity.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Settings.MouseSensitivity.Value = Math.Max(0, Math.Min(5, this.Settings.MouseSensitivity + (scroll * 0.1f)));
				}));
				controlsList.Children.Add(mouseSensitivity);

				Action<Property<PCInput.PCInputBinding>, string, bool> addInputSetting = delegate(Property<PCInput.PCInputBinding> setting, string display, bool allowGamepad)
				{
					this.bindings.Add(setting);
					UIComponent button = this.createMenuButton<PCInput.PCInputBinding>(display, setting);
					button.Add(new CommandBinding<Point>(button.MouseLeftUp, delegate(Point mouse)
					{
						PCInput.PCInputBinding originalValue = setting;
						setting.Value = new PCInput.PCInputBinding();
						this.UI.EnableMouse.Value = false;
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
								else if (binding.MouseButton != PCInput.MouseButton.None)
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
							this.UI.EnableMouse.Value = true;
						});
					}));
					controlsList.Children.Add(button);
				};

				addInputSetting(this.Settings.Forward, "Move Forward", false);
				addInputSetting(this.Settings.Left, "Move Left", false);
				addInputSetting(this.Settings.Backward, "Move Backward", false);
				addInputSetting(this.Settings.Right, "Move Right", false);
				addInputSetting(this.Settings.Jump, "Jump", true);
				addInputSetting(this.Settings.Parkour, "Parkour", true);
				addInputSetting(this.Settings.Roll, "Roll / Crouch", true);
				addInputSetting(this.Settings.Kick, "Kick", true);
				addInputSetting(this.Settings.TogglePhone, "Toggle Phone", true);
				addInputSetting(this.Settings.QuickSave, "Quicksave", true);
				addInputSetting(this.Settings.ToggleFullscreen, "Toggle Fullscreen", true);

				// Resume button
				UIComponent resume = this.createMenuButton("Resume");
				resume.Visible.Value = false;
				resume.Add(new CommandBinding<Point>(resume.MouseLeftUp, delegate(Point p)
				{
					this.Paused.Value = false;
					restorePausedSettings();
				}));
				pauseMenu.Children.Add(resume);

				// Save button
				UIComponent saveButton = this.createMenuButton("Save");
				saveButton.Add(new CommandBinding<Point>(saveButton.MouseLeftUp, delegate(Point p)
				{
					hidePauseMenu();

					saveMode.Value = true;

					loadSaveMenu.Visible.Value = true;
					if (loadSaveAnimation != null)
						loadSaveAnimation.Delete.Execute();
					loadSaveAnimation = new Animation(new Animation.Vector2MoveToSpeed(loadSaveMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(loadSaveAnimation);

					loadSaveShown = true;
					currentMenu.Value = loadSaveMenu;
				}));
				saveButton.Visible.Value = false;
				pauseMenu.Children.Add(saveButton);

				Action showLoad = delegate()
				{
					hidePauseMenu();

					saveMode.Value = false;

					loadSaveMenu.Visible.Value = true;
					if (loadSaveAnimation != null)
						loadSaveAnimation.Delete.Execute();
					loadSaveAnimation = new Animation(new Animation.Vector2MoveToSpeed(loadSaveMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(loadSaveAnimation);

					loadSaveShown = true;
					currentMenu.Value = loadSaveList;
				};

				// Load button
				UIComponent load = this.createMenuButton("Load");
				load.Add(new CommandBinding<Point>(load.MouseLeftUp, delegate(Point p)
				{
					showLoad();
				}));
				pauseMenu.Children.Add(load);

				// Controls button
				UIComponent controlsButton = this.createMenuButton("Controls");
				controlsButton.Add(new CommandBinding<Point>(controlsButton.MouseLeftUp, delegate(Point mouse)
				{
					hidePauseMenu();

					controlsMenu.Visible.Value = true;
					if (controlsAnimation != null)
						controlsAnimation.Delete.Execute();
					controlsAnimation = new Animation(new Animation.Vector2MoveToSpeed(controlsMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(controlsAnimation);

					controlsShown = true;
					currentMenu.Value = controlsList;
				}));
				pauseMenu.Children.Add(controlsButton);

				// Settings button
				UIComponent settingsButton = this.createMenuButton("Options");
				settingsButton.Add(new CommandBinding<Point>(settingsButton.MouseLeftUp, delegate(Point mouse)
				{
					hidePauseMenu();

					settingsMenu.Visible.Value = true;
					if (settingsAnimation != null)
						settingsAnimation.Delete.Execute();
					settingsAnimation = new Animation(new Animation.Vector2MoveToSpeed(settingsMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(settingsAnimation);

					settingsShown = true;

					currentMenu.Value = settingsMenu;
				}));
				pauseMenu.Children.Add(settingsButton);

				// Edit mode toggle button
				if (this.allowEditing)
				{
					UIComponent switchToEditMode = this.createMenuButton("Switch to edit mode");
					switchToEditMode.Add(new CommandBinding<Point>(switchToEditMode.MouseLeftUp, delegate(Point mouse)
					{
						pauseMenu.Visible.Value = false;
						this.EditorEnabled.Value = true;
						this.Paused.Value = false;
						if (pauseAnimation != null)
						{
							pauseAnimation.Delete.Execute();
							pauseAnimation = null;
						}
						IO.MapLoader.Load(this, null, this.MapFile, true);
						this.currentSave = null;
					}));
					pauseMenu.Children.Add(switchToEditMode);
				}

				// Credits window
				Animation creditsAnimation = null;
				bool creditsShown = false;

				ListContainer creditsMenu = new ListContainer();
				creditsMenu.Visible.Value = false;
				creditsMenu.Add(new Binding<Vector2, Point>(creditsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				creditsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(creditsMenu);
				creditsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Container creditsLabelPadding = new Container();
				creditsLabelPadding.PaddingLeft.Value = 8.0f;
				creditsLabelPadding.Opacity.Value = 0.0f;
				creditsMenu.Children.Add(creditsLabelPadding);

				ListContainer creditsLabelContainer = new ListContainer();
				creditsLabelContainer.Orientation.Value = ListContainer.ListOrientation.Vertical;
				creditsLabelPadding.Children.Add(creditsLabelContainer);

				TextElement creditsLabel = new TextElement();
				creditsLabel.FontFile.Value = "Font";
				creditsLabel.Text.Value = "C R E D I T S";
				creditsLabelContainer.Children.Add(creditsLabel);

				TextElement creditsScrollLabel = new TextElement();
				creditsScrollLabel.FontFile.Value = "Font";
				creditsScrollLabel.Text.Value = "Scroll for more";
				creditsLabelContainer.Children.Add(creditsScrollLabel);

				Action hideCredits = delegate()
				{
					creditsShown = false;

					showPauseMenu();

					if (creditsAnimation != null)
						creditsAnimation.Delete.Execute();
					creditsAnimation = new Animation
					(
						new Animation.Vector2MoveToSpeed(creditsMenu.AnchorPoint, new Vector2(1, 0.5f), 5.0f),
						new Animation.Set<bool>(creditsMenu.Visible, false)
					);
					this.AddComponent(creditsAnimation);
				};

				UIComponent creditsBack = this.createMenuButton("Back");
				creditsBack.Add(new CommandBinding<Point>(creditsBack.MouseLeftUp, delegate(Point p)
				{
					hideCredits();
				}));
				creditsMenu.Children.Add(creditsBack);

				TextElement creditsDisplay = new TextElement();
				creditsDisplay.FontFile.Value = "Font";
				creditsDisplay.Text.Value = File.ReadAllText("attribution.txt");

				Scroller creditsScroller = new Scroller();
				creditsScroller.Add(new Binding<Vector2>(creditsScroller.Size, () => new Vector2(creditsDisplay.Size.Value.X, this.ScreenSize.Value.Y * 0.5f), creditsDisplay.Size, this.ScreenSize));
				creditsScroller.Children.Add(creditsDisplay);
				creditsMenu.Children.Add(creditsScroller);

				// Credits button
				UIComponent credits = this.createMenuButton("Credits");
				credits.Add(new CommandBinding<Point>(credits.MouseLeftUp, delegate(Point mouse)
				{
					hidePauseMenu();

					creditsMenu.Visible.Value = true;
					if (creditsAnimation != null)
						creditsAnimation.Delete.Execute();
					creditsAnimation = new Animation(new Animation.Vector2MoveToSpeed(creditsMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(creditsAnimation);

					creditsShown = true;
					currentMenu.Value = creditsDisplay;
				}));
				pauseMenu.Children.Add(credits);

				// Exit button
				UIComponent exit = this.createMenuButton("Exit");
				exit.Add(new CommandBinding<Point>(exit.MouseLeftUp, delegate(Point mouse)
				{
					if (this.MapFile.Value != null)
					{
						showDialog
						(
							"Exiting will erase any unsaved progress. Are you sure?", "Exit",
							delegate()
							{
								throw new ExitException();
							}
						);
					}
					else
						throw new ExitException();
				}));
				pauseMenu.Children.Add(exit);

				bool saving = false;
				this.input.Bind(this.Settings.QuickSave, PCInput.InputState.Down, delegate()
				{
					if (!saving && !this.Paused)
					{
						saving = true;
						Container notification = new Container();
						notification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
						notification.Opacity.Value = 0.5f;
						TextElement notificationText = new TextElement();
						notificationText.Name.Value = "Text";
						notificationText.FontFile.Value = "Font";
						notificationText.Text.Value = "Saving...";
						notification.Children.Add(notificationText);
						this.UI.Root.GetChildByName("Notifications").Children.Add(notification);
						this.AddComponent(new Animation
						(
							new Animation.Delay(0.01f),
							new Animation.Execute(this.Save),
							new Animation.Set<string>(notificationText.Text, "Saved"),
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
				Func<bool> canPause = () => !this.EditorEnabled && ((this.player != null && this.player.Active) || this.MapFile.Value == null);

				Action togglePause = delegate()
				{
					if (dialog != null)
					{
						dialog.Delete.Execute();
						dialog = null;
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
					else if (loadSaveShown)
					{
						hideLoadSave();
						return;
					}

					if (this.MapFile.Value != null || !this.Paused)
					{
						this.Paused.Value = !this.Paused;

						if (this.Paused)
							savePausedSettings();
						else
							restorePausedSettings();
					}
				};

				this.input.Add(new CommandBinding(input.GetKeyDown(Keys.Escape), canPause, togglePause));
				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.Start), canPause, togglePause));
				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.B), () => this.Paused, togglePause));

				// Gamepad menu code

				int selected = 0;

				Func<UIComponent, int, int, int> nextMenuItem = delegate(UIComponent menu, int current, int delta)
				{
					int end = menu.Children.Count;
					if (current <= 0 && delta < 0)
						return end - 1;
					else if (current >= end - 1 && delta > 0)
						return 0;
					else
						return current + delta;
				};

				Func<UIComponent, bool> isButton = delegate(UIComponent item)
				{
					return item.Visible && item.GetType() == typeof(Container) && (item.MouseLeftUp.HasBindings || item.MouseScrolled.HasBindings);
				};

				Func<UIComponent, bool> isScrollButton = delegate(UIComponent item)
				{
					return item.Visible && item.GetType() == typeof(Container) && item.MouseScrolled.HasBindings;
				};

				this.input.Add(new NotifyBinding(delegate()
				{
					UIComponent menu = currentMenu;
					if (menu != null && menu != creditsDisplay && this.GamePadState.Value.IsConnected)
					{
						foreach (UIComponent item in menu.Children)
							item.Highlighted.Value = false;

						int i = 0;
						foreach (UIComponent item in menu.Children)
						{
							if (isButton(item))
							{
								item.Highlighted.Value = true;
								selected = i;
								break;
							}
							i++;
						}
					}
				}, currentMenu));

				Action<int> moveSelection = delegate(int delta)
				{
					UIComponent menu = currentMenu;
					if (menu != null && dialog == null)
					{
						if (menu == loadSaveList)
							delta = -delta;
						else if (menu == creditsDisplay)
						{
							Scroller scroll = (Scroller)menu.Parent;
							scroll.MouseScrolled.Execute(new Point(), delta * -4);
							return;
						}

						Container button = (Container)menu.Children[selected];
						button.Highlighted.Value = false;

						int i = nextMenuItem(menu, selected, delta);
						while (true)
						{
							UIComponent item = menu.Children[i];
							if (isButton(item))
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

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickUp), () => this.Paused, delegate()
				{
					moveSelection(-1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadUp), () => this.Paused, delegate()
				{
					moveSelection(-1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickDown), () => this.Paused, delegate()
				{
					moveSelection(1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadDown), () => this.Paused, delegate()
				{
					moveSelection(1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.A), () => this.Paused, delegate()
				{
					if (dialog != null)
						dialog.GetChildByName("Okay").MouseLeftUp.Execute(new Point());
					else
					{
						UIComponent menu = currentMenu;
						if (menu != null && menu != creditsDisplay )
						{
							UIComponent selectedItem = menu.Children[selected];
							if (isButton(selectedItem) && selectedItem.Highlighted)
								selectedItem.MouseLeftUp.Execute(new Point());
						}
					}
				}));

				Action<int> scrollButton = delegate(int delta)
				{
					UIComponent menu = currentMenu;
					if (menu != null && menu != creditsDisplay && dialog == null)
					{
						UIComponent selectedItem = menu.Children[selected];
						if (isScrollButton(selectedItem) && selectedItem.Highlighted)
							selectedItem.MouseScrolled.Execute(new Point(), delta);
					}
				};

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickLeft), () => this.Paused, delegate()
				{
					scrollButton(-1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadLeft), () => this.Paused, delegate()
				{
					scrollButton(-1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickRight), () => this.Paused, delegate()
				{
					scrollButton(1);
				}));

				this.input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadRight), () => this.Paused, delegate()
				{
					scrollButton(1);
				}));

				if (allowEditing)
				{
					// Editor instructions
					Container editorMsgBackground = new Container();
					this.UI.Root.Children.Add(editorMsgBackground);
					editorMsgBackground.Tint.Value = Color.Black;
					editorMsgBackground.Opacity.Value = 0.2f;
					editorMsgBackground.AnchorPoint.Value = new Vector2(0.5f, 0.0f);
					editorMsgBackground.Add(new Binding<Vector2, Point>(editorMsgBackground.Position, x => new Vector2(x.X * 0.5f, 30.0f), this.ScreenSize));
					TextElement editorMsg = new TextElement();
					editorMsg.FontFile.Value = "Font";
					editorMsg.Text.Value = "Space - Show menu";
					editorMsgBackground.Children.Add(editorMsg);
					this.AddComponent(new Animation
					(
						new Animation.Delay(4.0f),
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(editorMsgBackground.Opacity, 0.0f, 2.0f),
							new Animation.FloatMoveTo(editorMsg.Opacity, 0.0f, 2.0f)
						),
						new Animation.Execute(delegate() { this.UI.Root.Children.Remove(editorMsgBackground); })
					));
				}
				else
				{
					// "Press space to start" screen

					this.Paused.Value = true;
					savePausedSettings();

					TextElement header = new TextElement();
					header.FontFile.Value = "Font";
					header.Text.Value = "Alpha " + GameMain.Build.ToString();
					header.AnchorPoint.Value = new Vector2(0.5f, 0);
					header.Add(new Binding<Vector2>(header.Position, () => logo.Position + new Vector2(0, 30 + (logo.InverseAnchorPoint.Value.Y * logo.ScaledSize.Value.Y)), logo.Position, logo.InverseAnchorPoint, logo.ScaledSize));
					this.UI.Root.Children.Add(header);

					UIComponent startNew = this.createMenuButton("Start New");
					startNew.Add(new CommandBinding<Point>(startNew.MouseLeftUp, delegate(Point p)
					{
						this.Paused.Value = false;
						restorePausedSettings();
						logo.Opacity.Value = 1.0f;
						header.Opacity.Value = 1.0f;
						header.Text.Value = "Loading...";
						this.AddComponent(new Animation
						(
							new Animation.Delay(0.2f),
							new Animation.Set<string>(this.MapFile, this.initialMapFile)
						));
					}));
					pauseMenu.Children.Insert(1, startNew);

					logo.Opacity.Value = 0.0f;
					header.Opacity.Value = 0.0f;

					Animation fadeAnimation = new Animation
					(
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(logo.Opacity, 1.0f, 1.0f),
							new Animation.FloatMoveTo(header.Opacity, 1.0f, 1.0f)
						)
					);

					this.AddComponent(fadeAnimation);

					new CommandBinding(this.MapLoaded, header.Delete);

					startNew.Add(new CommandBinding(this.MapLoaded, startNew.Delete));
				}

#if ANALYTICS
				bool editorLastEnabled = this.EditorEnabled;
				new CommandBinding<string>(this.LoadingMap, delegate(string newMap)
				{
					if (this.MapFile.Value != null && !editorLastEnabled)
					{
						this.SessionRecorder.RecordEvent("ChangedMap", newMap);
						this.SaveAnalytics();
					}
					this.SessionRecorder.Reset();
					editorLastEnabled = this.EditorEnabled;
				});
#endif

				new CommandBinding(this.MapLoaded, delegate()
				{
					this.respawnTimer = -1.0f;
					this.mapJustLoaded = true;
					this.Renderer.InternalGamma.Value = GameMain.startGamma;
					this.Renderer.Brightness.Value = 1.0f;
				});

				logo.Add(new CommandBinding(this.MapLoaded, delegate() { resume.Visible.Value = saveButton.Visible.Value = true; }));
				logo.Add(new CommandBinding(this.MapLoaded, logo.Delete));
			}
		}

		public void EndGame()
		{
			this.MapFile.Value = null; // Clears all the entities too
			this.Renderer.InternalGamma.Value = 0.0f;
			this.Renderer.Brightness.Value = 0.0f;
			this.Renderer.BackgroundColor.Value = Color.Black;
			this.IsMouseVisible.Value = true;

			ListContainer list = new ListContainer();
			list.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			list.Spacing.Value = 40.0f;
			list.Alignment.Value = ListContainer.ListAlignment.Middle;
			list.Add(new Binding<Vector2, Point>(list.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.ScreenSize));
			this.UI.Root.Children.Add(list);

			Sprite logo = new Sprite();
			logo.Image.Value = "Images\\logo";
			logo.Add(new Binding<Vector2>(logo.Scale, () => new Vector2((this.ScreenSize.Value.X * 0.75f) / logo.Size.Value.X), this.ScreenSize, logo.Size));
			list.Children.Add(logo);

			ListContainer texts = new ListContainer();
			texts.Spacing.Value = 40.0f;
			list.Children.Add(texts);

			Action<string> addText = delegate(string text)
			{
				TextElement element = new TextElement();
				element.FontFile.Value = "Font";
				element.Text.Value = text;
				element.Add(new Binding<float, Vector2>(element.WrapWidth, x => x.X * 0.5f, logo.ScaledSize));
				texts.Children.Add(element);
			};

			addText("Thanks for playing! That's it for now. If you like what you see, please spread the word! Click the links below to join the discussion.");

			ListContainer links = new ListContainer();
			links.Alignment.Value = ListContainer.ListAlignment.Middle;
			links.ResizePerpendicular.Value = false;
			links.Spacing.Value = 20.0f;
			links.Add(new Binding<Vector2>(links.Size, x => new Vector2(x.X * 0.5f, links.Size.Value.Y), logo.ScaledSize));
			texts.Children.Add(links);

			System.Windows.Forms.Form winForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);

			Action<string, string> addLink = delegate(string text, string url)
			{
				TextElement element = new TextElement();
				element.FontFile.Value = "Font";
				element.Text.Value = text;
				element.Add(new Binding<float, Vector2>(element.WrapWidth, x => x.X * 0.5f, logo.ScaledSize));
				element.Add(new Binding<Color, bool>(element.Tint, x => x ? new Color(1.0f, 0.0f, 0.0f) : new Color(1.0f, 1.0f, 1.0f), element.Highlighted));
				element.Add(new CommandBinding<Point>(element.MouseLeftUp, delegate(Point mouse)
				{
					Process.Start(new ProcessStartInfo(url));
					if (this.graphics.IsFullScreen)
						this.ExitFullscreen();
				}));
				element.Add(new CommandBinding<Point>(element.MouseOver, delegate(Point mouse)
				{
					winForm.Cursor = System.Windows.Forms.Cursors.Hand;
				}));
				element.Add(new CommandBinding<Point>(element.MouseOut, delegate(Point mouse)
				{
					winForm.Cursor = System.Windows.Forms.Cursors.Default;
				}));
				links.Children.Add(element);
			};

			addLink("lemmagame.com", "http://lemmagame.com");
			addLink("indiedb.com/games/lemma", "http://indiedb.com/games/lemma");
			addLink("Greenlight", "http://steamcommunity.com/sharedfiles/filedetails/?id=105075009");
			addLink("et1337.com", "http://et1337.com");
			addLink("twitter.com/et1337", "http://twitter.com/et1337");

			addText("Writing, programming, and artwork by Evan Todd. Music and some sounds by Jack Menhorn. Full attribution list available on the website.");
		}

		protected void saveSettings()
		{
			// Save settings
			using (Stream stream = new FileStream(this.settingsFile, FileMode.Create, FileAccess.Write, FileShare.None))
				new XmlSerializer(typeof(Config)).Serialize(stream, this.Settings);
		}

		private bool mapJustLoaded = false;

		private Vector3 lastEditorPosition;
		private Vector2 lastEditorMouse;
		private string lastEditorSpawnPoint;

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (this.GamePadState.Value.IsConnected != this.LastGamePadState.Value.IsConnected)
			{
				// Re-bind inputs so their string representations are properly displayed
				// We need to show both PC and gamepad bindings

				foreach (Property<PCInput.PCInputBinding> binding in this.bindings)
					binding.Reset();
			}

			if (this.mapJustLoaded)
			{
				// If we JUST loaded a map, wait one frame for any scripts to execute before we spawn a player
				this.mapJustLoaded = false;
				return;
			}

			// Spawn an editor or a player if needed
			if (this.EditorEnabled)
			{
				this.player = null;
				this.Renderer.InternalGamma.Value = 0.0f;
				this.Renderer.Brightness.Value = 0.0f;
				if (this.editor == null)
				{
					this.editor = Factory.Get("Editor").CreateAndBind(this);
					FPSInput.RecenterMouse();
					this.editor.Get<Editor>().Position.Value = this.lastEditorPosition;
					this.editor.Get<FPSInput>().Mouse.Value = this.lastEditorMouse;
					this.StartSpawnPoint.Value = this.lastEditorSpawnPoint;
					this.Add(this.editor);
				}
				else
				{
					this.lastEditorPosition = this.editor.Get<Editor>().Position;
					this.lastEditorMouse = this.editor.Get<FPSInput>().Mouse;
				}
			}
			else
			{
				if (this.MapFile.Value == null || !this.CanSpawn)
					return;

				this.editor = null;

				bool setupSpawn = this.player == null || !this.player.Active;

				if (setupSpawn)
					this.player = PlayerFactory.Instance;

				bool createPlayer = this.player == null || !this.player.Active;

				if (createPlayer || setupSpawn)
				{
					if (this.loadingSavedGame)
					{
						this.Renderer.InternalGamma.Value = 0.0f;
						this.Renderer.Brightness.Value = 0.0f;
						this.PlayerSpawned.Execute(this.player);
						this.loadingSavedGame = false;
						this.respawnTimer = 0;
					}
					else
					{
						if (this.respawnTimer <= 0)
						{
							this.AddComponent(new Animation
							(
								new Animation.Parallel
								(
									new Animation.Vector3MoveTo(this.Renderer.Tint, GameMain.startTint, 0.5f),
									new Animation.FloatMoveTo(this.Renderer.InternalGamma, GameMain.startGamma, 0.5f),
									new Animation.FloatMoveTo(this.Renderer.Brightness, 1.0f, 0.5f)
								)
							));
						}

						if (this.respawnTimer > GameMain.respawnInterval || this.respawnTimer < 0)
						{
							if (createPlayer)
							{
								this.player = Factory.CreateAndBind(this, "Player");
								this.Add(this.player);
							}

							bool spawnFound = false;
							PlayerFactory.RespawnLocation foundSpawnLocation = default(PlayerFactory.RespawnLocation);

							ListProperty<PlayerFactory.RespawnLocation> respawnLocations = Factory.Get<PlayerDataFactory>().Instance(this).GetOrMakeListProperty<PlayerFactory.RespawnLocation>("RespawnLocations");
							int supportedLocations = 0;
							while (respawnLocations.Count > 0)
							{
								PlayerFactory.RespawnLocation respawnLocation = respawnLocations[respawnLocations.Count - 1];
								Entity respawnMapEntity = respawnLocation.Map.Target;
								if (respawnMapEntity != null && respawnMapEntity.Active)
								{
									Map respawnMap = respawnMapEntity.Get<Map>();
									if (respawnMap.Active && respawnMap[respawnLocation.Coordinate].ID != 0 && respawnMap.GetAbsoluteVector(respawnMap.GetRelativeDirection(Direction.PositiveY).GetVector()).Y > 0.5f)
									{
										supportedLocations++;
										Vector3 absolutePos = respawnMap.GetAbsolutePosition(respawnLocation.Coordinate);
										DynamicMap dynamicMap = respawnMap as DynamicMap;
										if (dynamicMap == null || dynamicMap.IsAffectedByGravity || absolutePos.Y > respawnLocation.OriginalPosition.Y - 1.0f)
										{
											Map.GlobalRaycastResult hit = Map.GlobalRaycast(absolutePos + new Vector3(0, 1, 0), Vector3.Up, 2);
											if (hit.Map == null)
											{
												// We can spawn here
												spawnFound = true;
												foundSpawnLocation = respawnLocation;
											}
										}
									}
								}
								respawnLocations.RemoveAt(respawnLocations.Count - 1);
								if (supportedLocations >= this.RespawnRewindLength)
									break;
							}

							this.RespawnRewindLength = DefaultRespawnRewindLength;

							if (spawnFound)
							{
								Vector3 absolutePos = foundSpawnLocation.Map.Target.Get<Map>().GetAbsolutePosition(foundSpawnLocation.Coordinate);
								this.player.Get<Transform>().Position.Value = this.Camera.Position.Value = absolutePos + new Vector3(0, 3, 0);

								FPSInput.RecenterMouse();
								Property<Vector2> mouse = this.player.Get<FPSInput>().Mouse;
								mouse.Value = new Vector2(foundSpawnLocation.Rotation, 0.0f);
							}
							else
							{
								PlayerSpawn spawn = null;
								Entity spawnEntity = null;
								if (!string.IsNullOrEmpty(this.StartSpawnPoint.Value))
								{
									spawnEntity = this.GetByID(this.StartSpawnPoint);
									if (spawnEntity != null)
										spawn = spawnEntity.Get<PlayerSpawn>();
									this.lastEditorSpawnPoint = this.StartSpawnPoint;
									this.StartSpawnPoint.Value = null;
								}

								if (spawnEntity == null)
								{
									spawn = PlayerSpawn.FirstActive();
									spawnEntity = spawn == null ? null : spawn.Entity;
								}

								if (spawnEntity != null)
									this.player.Get<Transform>().Position.Value = this.Camera.Position.Value = spawnEntity.Get<Transform>().Position;

								if (spawn != null)
								{
									spawn.IsActivated.Value = true;
									FPSInput.RecenterMouse();
									Property<Vector2> mouse = this.player.Get<FPSInput>().Mouse;
									mouse.Value = new Vector2(spawn.Rotation, 0.0f);
								}
							}

							this.AddComponent(new Animation
							(
								new Animation.Parallel
								(
									new Animation.Vector3MoveTo(this.Renderer.Tint, Vector3.One, 0.5f),
									new Animation.FloatMoveTo(this.Renderer.InternalGamma, 0.0f, 0.5f),
									new Animation.FloatMoveTo(this.Renderer.Brightness, 0.0f, 0.5f)
								)
							));
							this.respawnTimer = 0;

							this.PlayerSpawned.Execute(this.player);
						}
						else
							this.respawnTimer += this.ElapsedTime;
					}
				}
			}
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (this.renderTarget != null)
			{
				// We just took a screenshot (i.e. we rendered to a target other than the screen).
				// So make it so we're rendering to the screen again, then copy the render target to the screen.

				this.GraphicsDevice.SetRenderTarget(null);

				SpriteBatch spriteBatch = new SpriteBatch(this.GraphicsDevice);
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
				spriteBatch.Draw(this.renderTarget, Vector2.Zero, Color.White);
				spriteBatch.End();

				this.renderTarget = null;

				if (this.saveAfterTakingScreenshot)
					this.Save.Execute();
			}
		}

		public override void ResizeViewport(int width, int height, bool fullscreen, bool applyChanges = true)
		{
			base.ResizeViewport(width, height, fullscreen, applyChanges);
			this.Settings.Fullscreen.Value = fullscreen;
			this.saveSettings();
		}

		public void EnterFullscreen()
		{
			Point res = this.Settings.FullscreenResolution;
			this.ResizeViewport(res.X, res.Y, true);
		}

		public void ExitFullscreen()
		{
			Point res = this.Settings.Size;
			this.ResizeViewport(res.X, res.Y, false);
		}
	}
}