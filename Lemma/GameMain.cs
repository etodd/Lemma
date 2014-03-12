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
#if DEVELOPMENT
		public const string InitialMap = null;
#else
		public const string InitialMap = "intro";
#endif

		public const string MenuMap = "..\\Menu\\menu";

		public class ExitException : Exception
		{
		}

		public const int ConfigVersion = 5;
		public const int Build = 281;

		protected string lastMapFile;
		protected float lastSessionTotalTime;

		public class Config
		{
#if DEVELOPMENT
			public Property<bool> Fullscreen = new Property<bool> { Value = false };
#else
			public Property<bool> Fullscreen = new Property<bool> { Value = true };
#endif
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

		private bool loadingSavedGame;

		public Property<string> StartSpawnPoint = new Property<string>();

		public Command<Entity> PlayerSpawned = new Command<Entity>();

		private float respawnTimer = -1.0f;

		private bool saveAfterTakingScreenshot = false;

		private static Color highlightColor = new Color(0.0f, 0.175f, 0.35f);

		private DisplayModeCollection supportedDisplayModes;

		private const float startGamma = 10.0f;
		private static Vector3 startTint = new Vector3(2.0f);

		public const int RespawnMemoryLength = 100;
		public const int DefaultRespawnRewindLength = 3;
		public const float DefaultRespawnInterval = 0.5f;
		public const int KilledRespawnRewindLength = 40;
		public const float KilledRespawnInterval = 3.0f;

		public int RespawnRewindLength = DefaultRespawnRewindLength;
		public float RespawnInterval = DefaultRespawnInterval;

		private int displayModeIndex;

		private List<Property<PCInput.PCInputBinding>> bindings = new List<Property<PCInput.PCInputBinding>>();

		private ListContainer messages;

		private WaveBank musicWaveBank;
		public SoundBank MusicBank;

		public string Credits { get; private set; }

		public GameMain()
			: base()
		{
#if DEBUG
			Log.Handler = delegate(string log)
			{
				this.HideMessage(null, this.ShowMessage(null, log), 2.0f);
			};
#endif

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
#if DEVELOPMENT
			this.EditorEnabled.Value = true;
#else
			this.EditorEnabled.Value = false;
#endif
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

			if (this.Settings.FullscreenResolution.Value.X == 0)
			{
				Microsoft.Xna.Framework.Graphics.DisplayMode display = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				this.Settings.FullscreenResolution.Value = new Point(display.Width, display.Height);
			}

			// Restore window state
			if (this.Settings.Fullscreen)
				this.ResizeViewport(this.Settings.FullscreenResolution.Value.X, this.Settings.FullscreenResolution.Value.Y, true);
			else
				this.ResizeViewport(this.Settings.Size.Value.X, this.Settings.Size.Value.Y, false, false);
		}

		private const float messageFadeTime = 0.5f;
		private const float messageBackgroundOpacity = 0.75f;

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
				new Animation.Vector2MoveTo(container.Size, originalSize, messageFadeTime),
				new Animation.Set<bool>(container.ResizeVertical, true),
				new Animation.Set<bool>(container.EnableScissor, false)
			);

			if (entity == null)
			{
				anim.EnabledWhenPaused.Value = false;
				this.AddComponent(anim);
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

		public void HideMessage(Entity entity, Container container, float delay = 0.0f)
		{
			if (container != null && container.Active)
			{
				container.CheckLayout();
				Animation anim = new Animation
				(
					new Animation.Delay(delay),
					new Animation.Set<bool>(container.ResizeVertical, false),
					new Animation.Set<bool>(container.EnableScissor, true),
					new Animation.Vector2MoveTo(container.Size, new Vector2(container.Size.Value.X, 0), messageFadeTime),
					new Animation.Execute(container.Delete)
				);

				if (entity == null)
				{
					anim.EnabledWhenPaused.Value = false;
					this.AddComponent(anim);
				}
				else
					entity.Add(anim);
			}
		}

		public override void ClearEntities(bool deleteEditor)
		{
			base.ClearEntities(deleteEditor);
			this.messages.Children.Clear();
			this.AudioEngine.GetCategory("Music").Stop(AudioStopOptions.Immediate);
			this.AudioEngine.GetCategory("Default").Stop(AudioStopOptions.Immediate);
			this.AudioEngine.GetCategory("Ambient").Stop(AudioStopOptions.Immediate);
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

		private UIComponent createMenuButtonContainer()
		{
			Container result = new Container();
			result.Tint.Value = Color.Black;
			result.Add(new Binding<Color, bool>(result.Tint, x => x ? GameMain.highlightColor : new Color(0.0f, 0.0f, 0.0f), result.Highlighted));
			result.Add(new Binding<float, bool>(result.Opacity, x => x ? 1.0f : 0.5f, result.Highlighted));
			result.Add(new NotifyBinding(delegate()
			{
				if (result.Highlighted)
					Sound.PlayCue(this, "Mouse");
			}, result.Highlighted));
			result.Add(new CommandBinding<Point>(result.MouseLeftUp, delegate(Point p)
			{
				Sound.PlayCue(this, "Click");
			}));
			return result;
		}

		private UIComponent createMenuButton(string label)
		{
			UIComponent result = this.createMenuButtonContainer();
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
			string map = string.IsNullOrEmpty(this.lastMapFile) ? this.MapFile : this.lastMapFile;
			string filename = GameMain.Build.ToString() + "-" + (string.IsNullOrEmpty(map) ? "null" : Path.GetFileName(map)) + "-" + Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32) + ".xml";
			this.SessionRecorder.Save(Path.Combine(this.analyticsDirectory, filename), map, this.lastSessionTotalTime);
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
				Session s = Session.Load(file);
				if (s.Build == GameMain.Build)
				{
					string sessionMap = s.Map;
					if (sessionMap == null)
					{
						// Attempt to extract the map name from the filename
						string fileWithoutExtension = Path.GetFileNameWithoutExtension(file);

						int firstDash = fileWithoutExtension.IndexOf('-');
						int lastDash = fileWithoutExtension.LastIndexOf('-');

						if (firstDash == lastDash) // Old filename format "map-hash"
							sessionMap = fileWithoutExtension.Substring(0, firstDash);
						else // New format "build-map-hash"
							sessionMap = fileWithoutExtension.Substring(firstDash + 1, lastDash - (firstDash + 1));
					}
					if (sessionMap == map)
						result.Add(s);
				}
			}
			return result;
		}

		public TextElement CreateLink(string text, string url)
		{
			System.Windows.Forms.Form winForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);

			TextElement element = new TextElement();
			element.FontFile.Value = "Font";
			element.Text.Value = text;
			element.Add(new Binding<Color, bool>(element.Tint, x => x ? new Color(1.0f, 0.0f, 0.0f) : new Color(91.0f / 255.0f, 175.0f / 255.0f, 205.0f / 255.0f), element.Highlighted));
			element.Add(new CommandBinding<Point>(element.MouseLeftUp, delegate(Point mouse)
			{
				this.ExitFullscreen();
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url));
			}));
			element.Add(new CommandBinding<Point>(element.MouseOver, delegate(Point mouse)
			{
				winForm.Cursor = System.Windows.Forms.Cursors.Hand;
			}));
			element.Add(new CommandBinding<Point>(element.MouseOut, delegate(Point mouse)
			{
				winForm.Cursor = System.Windows.Forms.Cursors.Default;
			}));

			return element;
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

				try
				{
					this.musicWaveBank = new WaveBank(this.AudioEngine, "Content\\Game\\Music\\Music.xwb");
					this.MusicBank = new SoundBank(this.AudioEngine, "Content\\Game\\Music\\Music.xsb");
				}
				catch (Exception)
				{
					// Don't HAVE to load music
				}

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
#endif

				this.MapFile.Set = delegate(string value)
				{
					this.lastSessionTotalTime = this.TotalTime;
					this.lastMapFile = this.MapFile;
					this.ClearEntities(false);

					if (value == null || value.Length == 0)
					{
						this.MapFile.InternalValue = null;
						return;
					}

					try
					{
						this.MapFile.InternalValue = value;
						string directory = this.currentSave == null ? null : Path.Combine(this.saveDirectory, this.currentSave);
						if (value == GameMain.MenuMap)
							directory = null; // Don't try to load the menu from a save game
						IO.MapLoader.Load(this, directory, value, false);
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
				this.Renderer.EnvironmentMap.Value = "Images\\env0";

				this.input = new PCInput();
				this.AddComponent(this.input);

				new TwoWayBinding<LightingManager.DynamicShadowSetting>(this.Settings.DynamicShadows, this.LightingManager.DynamicShadows);
				new TwoWayBinding<float>(this.Settings.MotionBlurAmount, this.Renderer.MotionBlurAmount);
				new TwoWayBinding<float>(this.Settings.Gamma, this.Renderer.Gamma);
				new TwoWayBinding<bool>(this.Settings.EnableBloom, this.Renderer.EnableBloom);
				new TwoWayBinding<float>(this.Settings.FieldOfView, this.Camera.FieldOfView);

				// Message list
				this.messages = new ListContainer();
				this.messages.Alignment.Value = ListContainer.ListAlignment.Max;
				this.messages.AnchorPoint.Value = new Vector2(1.0f, 1.0f);
				this.messages.Reversed.Value = true;
				this.messages.Add(new Binding<Vector2, Point>(this.messages.Position, x => new Vector2(x.X * 0.9f, x.Y * 0.9f), this.ScreenSize));
				this.UI.Root.Children.Add(this.messages);

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

				Property<UIComponent> currentMenu = new Property<UIComponent> { Value = null };

				// Pause menu
				ListContainer pauseMenu = new ListContainer();
				pauseMenu.Visible.Value = false;
				pauseMenu.Add(new Binding<Vector2, Point>(pauseMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				pauseMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(pauseMenu);
				pauseMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

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

					if (this.MapFile.Value != GameMain.MenuMap)
					{
						this.AudioEngine.GetCategory("Music").Pause();
						this.AudioEngine.GetCategory("Default").Pause();
					}
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

					this.AudioEngine.GetCategory("Music").Resume();
					this.AudioEngine.GetCategory("Default").Resume();
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
					dialog.Opacity.Value = 0.5f;
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
						if (info.Version != GameMain.Build)
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

					UIComponent container = this.createMenuButtonContainer();
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
						this.copySave(this.currentSave == null ? IO.MapLoader.MapDirectory : Path.Combine(this.saveDirectory, this.currentSave), Path.Combine(this.saveDirectory, newSave));
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
							new XmlSerializer(typeof(SaveInfo)).Serialize(stream, new SaveInfo { MapFile = this.MapFile, Version = GameMain.Build });
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
				settingsScrollLabel.Add(new Binding<string>(settingsScrollLabel.Text, delegate()
				{
					string val;
					if (this.GamePadState.Value.IsConnected)
						val = "[A] or [LeftStick] to modify";
					else
						val = "Scroll or click to modify";
					return val + "\n[" + this.Settings.ToggleFullscreen.Value.ToString() + "] to toggle fullscreen";
				}, this.Settings.ToggleFullscreen));
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

				// Start new button
				UIComponent startNew = this.createMenuButton("Start New");
				startNew.Add(new CommandBinding<Point>(startNew.MouseLeftUp, delegate(Point p)
				{
					restorePausedSettings();
					this.currentSave = null;
					this.AddComponent(new Animation
					(
						new Animation.Delay(0.2f),
						new Animation.Set<string>(this.MapFile, GameMain.InitialMap)
					));
				}));
				pauseMenu.Children.Add(startNew);
				startNew.Add(new Binding<bool, string>(startNew.Visible, x => x == GameMain.MenuMap, this.MapFile));

				// Resume button
				UIComponent resume = this.createMenuButton("Resume");
				resume.Visible.Value = false;
				resume.Add(new CommandBinding<Point>(resume.MouseLeftUp, delegate(Point p)
				{
					this.Paused.Value = false;
					restorePausedSettings();
				}));
				pauseMenu.Children.Add(resume);
				resume.Add(new Binding<bool, string>(resume.Visible, x => x != GameMain.MenuMap, this.MapFile));

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
				saveButton.Add(new Binding<bool, string>(saveButton.Visible, x => x != GameMain.MenuMap, this.MapFile));
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

				// Sandbox button
				UIComponent sandbox = this.createMenuButton("Sandbox");
				sandbox.Add(new CommandBinding<Point>(sandbox.MouseLeftUp, delegate(Point p)
				{
					restorePausedSettings();
					this.currentSave = null;
					this.AddComponent(new Animation
					(
						new Animation.Delay(0.2f),
						new Animation.Set<string>(this.MapFile, "sandbox")
					));
				}));
				pauseMenu.Children.Add(sandbox);
				sandbox.Add(new Binding<bool, string>(sandbox.Visible, x => x == GameMain.MenuMap, this.MapFile));

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

#if DEVELOPMENT
				// Edit mode toggle button
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
#endif

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
				creditsDisplay.Text.Value = this.Credits = File.ReadAllText("attribution.txt");

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
				credits.Add(new Binding<bool, string>(credits.Visible, x => x == GameMain.MenuMap, this.MapFile));
				pauseMenu.Children.Add(credits);

				// Main menu button
				UIComponent mainMenu = this.createMenuButton("Main Menu");
				mainMenu.Add(new CommandBinding<Point>(mainMenu.MouseLeftUp, delegate(Point p)
				{
					showDialog
					(
						"Quitting will erase any unsaved progress. Are you sure?", "Quit to main menu",
						delegate()
						{
							this.currentSave = null;
							this.MapFile.Value = GameMain.MenuMap;
							this.Paused.Value = false;
						}
					);
				}));
				pauseMenu.Children.Add(mainMenu);
				mainMenu.Add(new Binding<bool, string>(mainMenu.Visible, x => x != GameMain.MenuMap, this.MapFile));

				// Exit button
				UIComponent exit = this.createMenuButton("Exit");
				exit.Add(new CommandBinding<Point>(exit.MouseLeftUp, delegate(Point mouse)
				{
					if (this.MapFile.Value != GameMain.MenuMap)
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
				Func<bool> canPause = () => !this.EditorEnabled && ((this.player != null && this.player.Active) || this.MapFile.Value == GameMain.MenuMap);

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

					if (this.MapFile.Value == GameMain.MenuMap)
					{
						if (currentMenu.Value == null)
							savePausedSettings();
					}
					else
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

				// Pause on window lost focus
				this.Deactivated += delegate(object sender, EventArgs e)
				{
					if (!this.Paused && this.MapFile.Value != GameMain.MenuMap && !this.EditorEnabled)
					{
						this.Paused.Value = true;
						savePausedSettings();
					}
				};

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

				Func<bool> enableGamepad = delegate()
				{
					return this.Paused || this.MapFile.Value == GameMain.MenuMap;
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

				new CommandBinding(this.MapLoaded, delegate()
				{
					if (this.MapFile.Value == GameMain.MenuMap)
					{
						this.CanSpawn = false;
						this.Renderer.InternalGamma.Value = 0.0f;
						this.Renderer.Brightness.Value = 0.0f;
					}
					else
					{
						this.CanSpawn = true;
						this.Renderer.InternalGamma.Value = GameMain.startGamma;
						this.Renderer.Brightness.Value = 1.0f;
					}

					this.respawnTimer = -1.0f;
					this.Renderer.BlurAmount.Value = 0.0f;
					this.Renderer.Tint.Value = new Vector3(1.0f);
					this.mapJustLoaded = true;
				});

#if DEVELOPMENT
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
#else
					// "Press space to start" screen

					this.MapFile.Value = GameMain.MenuMap;
					savePausedSettings();
#endif

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
			}
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

						if (this.respawnTimer > this.RespawnInterval || this.respawnTimer < 0)
						{
							this.RespawnInterval = GameMain.DefaultRespawnInterval;
							if (createPlayer)
							{
								this.player = Factory.CreateAndBind(this, "Player");
								this.Add(this.player);
							}

							bool spawnFound = false;

							PlayerFactory.RespawnLocation foundSpawnLocation = default(PlayerFactory.RespawnLocation);

							if (string.IsNullOrEmpty(this.StartSpawnPoint.Value))
							{
								// Look for an autosaved spawn point
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
							}

							if (spawnFound)
							{
								// Spawn at an autosaved location
								Vector3 absolutePos = foundSpawnLocation.Map.Target.Get<Map>().GetAbsolutePosition(foundSpawnLocation.Coordinate);
								this.player.Get<Transform>().Position.Value = this.Camera.Position.Value = absolutePos + new Vector3(0, 3, 0);

								FPSInput.RecenterMouse();
								Property<Vector2> mouse = this.player.Get<FPSInput>().Mouse;
								mouse.Value = new Vector2(foundSpawnLocation.Rotation, 0.0f);
							}
							else
							{
								// Spawn at a spawn point
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
			if (!this.graphics.IsFullScreen)
			{
				Point res = this.Settings.FullscreenResolution;
				this.ResizeViewport(res.X, res.Y, true);
			}
		}

		public void ExitFullscreen()
		{
			if (this.graphics.IsFullScreen)
			{
				Point res = this.Settings.Size;
				this.ResizeViewport(res.X, res.Y, false);
			}
		}
	}
}