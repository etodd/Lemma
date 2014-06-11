using System;
using System.Security.Cryptography.X509Certificates;
using ComponentBind;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma.Console;
using Lemma.GInterfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Lemma.Components;
using Lemma.Factories;
using Lemma.Util;
using System.Linq;
using BEPUphysics;
using System.Xml.Serialization;
using System.Reflection;
using System.Globalization;
using GeeUI;

namespace Lemma
{
	public class Main : BaseMain
	{
		public const string InitialMap = "start";

		public const string MenuMap = "..\\menu";

		public class ExitException : Exception
		{
		}

		public const int ConfigVersion = 8;
		public const int MapVersion = 571;
		public const int Build = 571;

		public static Config.Lang[] Languages = new[] { Config.Lang.en, Config.Lang.ru };

		public class Config
		{
			public enum Lang { en, ru }
			public Property<Lang> Language = new Property<Lang>();
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
			public Property<bool> EnableSSAO = new Property<bool> { Value = true };
			public Property<bool> EnableGodRays = new Property<bool> { Value = true };
			public Property<bool> EnableBloom = new Property<bool> { Value = true };
			public Property<LightingManager.DynamicShadowSetting> DynamicShadows = new Property<LightingManager.DynamicShadowSetting> { Value = LightingManager.DynamicShadowSetting.High };
			public Property<bool> InvertMouseX = new Property<bool> { Value = false };
			public Property<bool> InvertMouseY = new Property<bool> { Value = false };
			public Property<float> MouseSensitivity = new Property<float> { Value = 1.0f };
			public Property<float> FieldOfView = new Property<float> { Value = MathHelper.ToRadians(80.0f) };
			public Property<bool> EnableVsync = new Property<bool> { Value = false };
			public int Version;
			public string UUID;
			public Property<PCInput.PCInputBinding> Forward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.W } };
			public Property<PCInput.PCInputBinding> Left = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.A } };
			public Property<PCInput.PCInputBinding> Right = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.D } };
			public Property<PCInput.PCInputBinding> Backward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.S } };
			public Property<PCInput.PCInputBinding> Jump = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Space, GamePadButton = Buttons.RightTrigger } };
			public Property<PCInput.PCInputBinding> Parkour = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.LeftShift, GamePadButton = Buttons.LeftTrigger } };
			public Property<PCInput.PCInputBinding> RollKick = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.LeftMouseButton, GamePadButton = Buttons.LeftStick } };
			public Property<PCInput.PCInputBinding> SpecialAbility = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.RightMouseButton, GamePadButton = Buttons.RightStick } };
			public Property<PCInput.PCInputBinding> TogglePhone = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Tab, GamePadButton = Buttons.Y } };
			public Property<PCInput.PCInputBinding> QuickSave = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F5, GamePadButton = Buttons.Back } };
			public Property<PCInput.PCInputBinding> ToggleFullscreen = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F11 } };
			public Property<PCInput.PCInputBinding> ToggleConsole = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.OemTilde } };
			public Property<float> SoundEffectVolume = new Property<float> { Value = 1.0f };
			public Property<float> MusicVolume = new Property<float> { Value = 1.0f };
		}

		public class SaveInfo
		{
			public string MapFile;
			public int Version;
		}

		public Config Settings;
		private string dataDirectory;
		public string SaveDirectory;
		private string analyticsDirectory;
		private string settingsFile;

		public Screenshot Screenshot;

		public Menu Menu;
		public UIFactory UIFactory;

		public Camera Camera;
		public AkListener Listener;

		public GraphicsDeviceManager Graphics;
		public Renderer Renderer;

		protected RenderParameters renderParameters;
		public RenderTarget2D RenderTarget;

#if PERFORMANCE_MONITOR
		private const float performanceUpdateTime = 0.5f;
		private float performanceInterval;

		private ListContainer performanceMonitor;

		private int frameSum;
		private Property<float> frameRate = new Property<float>();
		private double physicsSum;
		private Property<double> physicsTime = new Property<double>();
		private double updateSum;
		private Property<double> updateTime = new Property<double>();
		private double preframeSum;
		private Property<double> preframeTime = new Property<double>();
		private double rawRenderSum;
		private Property<double> rawRenderTime = new Property<double>();
		private double shadowRenderSum;
		private Property<double> shadowRenderTime = new Property<double>();
		private double postProcessSum;
		private Property<double> postProcessTime = new Property<double>();
		private double unPostProcessedSum;
		private Property<double> unPostProcessedTime = new Property<double>();
		private Property<int> drawCalls = new Property<int>();
		private int drawCallCounter;
		private Property<int> triangles = new Property<int>();
		private int triangleCounter;
#endif

		public Property<Point> ScreenSize = new Property<Point>();

		public LightingManager LightingManager;

		public UIRenderer UI;
		public GeeUIMain GeeUI;

		public Console.Console Console;
		public ConsoleUI ConsoleUI;

		private bool mapLoaded;

		public Space Space;

		private List<IDrawableComponent> drawables = new List<IDrawableComponent>();
		private List<IUpdateableComponent> updateables = new List<IUpdateableComponent>();
		private List<IDrawablePreFrameComponent> preframeDrawables = new List<IDrawablePreFrameComponent>();
		private List<INonPostProcessedDrawableComponent> nonPostProcessedDrawables = new List<INonPostProcessedDrawableComponent>();
		private List<IDrawableAlphaComponent> alphaDrawables = new List<IDrawableAlphaComponent>();
		private List<IDrawablePostAlphaComponent> postAlphaDrawables = new List<IDrawablePostAlphaComponent>();

		private Point? resize;

		[AutoConVar("map_file", "Game Map File")]
		public Property<string> MapFile = new Property<string>();

		public Spawner Spawner;

		public Property<KeyboardState> LastKeyboardState = new Property<KeyboardState>();
		public Property<KeyboardState> KeyboardState = new Property<KeyboardState>();
		public Property<MouseState> LastMouseState = new Property<MouseState>();
		public Property<MouseState> MouseState = new Property<MouseState>();
		public Property<GamePadState> LastGamePadState = new Property<GamePadState>();
		public Property<GamePadState> GamePadState = new Property<GamePadState>();
		public new Property<bool> IsMouseVisible = new Property<bool> { };
		public Property<bool> GamePadConnected = new Property<bool>();

		public Property<float> TimeMultiplier = new Property<float> { Value = 1.0f };
		public Property<float> PauseAudioEffect = new Property<float> { Value = 0.0f };

		[AutoConVar("game_time", "Total time the game has been played")]
		public static Property<float> TotalGameTime = new Property<float>(); 

		public Strings Strings = new Strings();

		public bool IsLoadingMap = false;

		public Command<string> LoadingMap = new Command<string>();

		public Command MapLoaded = new Command();

		protected NotifyBinding alphaDrawableBinding;
		protected bool alphaDrawablesModified;
		protected NotifyBinding postAlphaDrawableBinding;
		protected bool postAlphaDrawablesModified;
		protected NotifyBinding nonPostProcessedDrawableBinding;
		protected bool nonPostProcessedDrawablesModified;

		public void FlushComponents()
		{
			for (int i = 0; i < this.componentsToAdd.Count; i++)
			{
				IComponent c = this.componentsToAdd[i];
				Type t = c.GetType();
				if (typeof(IDrawableComponent).IsAssignableFrom(t))
					this.drawables.Add((IDrawableComponent)c);
				if (typeof(IUpdateableComponent).IsAssignableFrom(t))
					this.updateables.Add((IUpdateableComponent)c);
				if (typeof(IDrawablePreFrameComponent).IsAssignableFrom(t))
					this.preframeDrawables.Add((IDrawablePreFrameComponent)c);
				if (typeof(INonPostProcessedDrawableComponent).IsAssignableFrom(t))
					this.nonPostProcessedDrawables.Add((INonPostProcessedDrawableComponent)c);
				if (typeof(IDrawableAlphaComponent).IsAssignableFrom(t))
				{
					this.alphaDrawables.Add((IDrawableAlphaComponent)c);
					if (this.alphaDrawableBinding != null)
					{
						this.alphaDrawableBinding.Delete();
						this.alphaDrawableBinding = null;
					}
				}
				if (typeof(IDrawablePostAlphaComponent).IsAssignableFrom(t))
				{
					this.postAlphaDrawables.Add((IDrawablePostAlphaComponent)c);
					if (this.postAlphaDrawableBinding != null)
					{
						this.postAlphaDrawableBinding.Delete();
						this.postAlphaDrawableBinding = null;
					}
				}
			}
			this.componentsToAdd.Clear();

			for (int i = 0; i < this.componentsToRemove.Count; i++)
			{
				IComponent c = this.componentsToRemove[i];
				Type t = c.GetType();
				if (typeof(IUpdateableComponent).IsAssignableFrom(t))
					this.updateables.Remove((IUpdateableComponent)c);
				if (typeof(IDrawableComponent).IsAssignableFrom(t))
					this.drawables.Remove((IDrawableComponent)c);
				if (typeof(IDrawablePreFrameComponent).IsAssignableFrom(t))
					this.preframeDrawables.Remove((IDrawablePreFrameComponent)c);
				if (typeof(INonPostProcessedDrawableComponent).IsAssignableFrom(t))
					this.nonPostProcessedDrawables.Remove((INonPostProcessedDrawableComponent)c);
				if (typeof(IDrawableAlphaComponent).IsAssignableFrom(t))
					this.alphaDrawables.Remove((IDrawableAlphaComponent)c);
				c.delete();
			}
			this.componentsToRemove.Clear();
		}

		public void ClearEntities(bool deleteEditor)
		{
			if (this.MapContent != null)
				this.MapContent.Unload();
			this.MapContent = new ContentManager(this.Services);
			this.MapContent.RootDirectory = "Content";

			while (this.Entities.Count > (deleteEditor ? 0 : 1))
			{
				foreach (Entity entity in this.Entities.ToList())
				{
					if (entity.Type == "Editor")
					{
						if (deleteEditor)
							this.Remove(entity);
						else
						{
							// Deselect all entities, since they'll be gone anyway
							Editor editor = entity.Get<Editor>();
							editor.SelectedEntities.Clear();
							if (editor.VoxelEditMode)
								editor.VoxelEditMode.Value = false;
							editor.TransformMode.Value = Editor.TransformModes.None;
						}
					}
					else
						this.Remove(entity);
				}
			}
			this.FlushComponents();
			Factory<Main>.Initialize(); // Clear factories to clear out any relationships that might confuse the garbage collector
			GC.Collect();

			this.TotalTime.Value = 0.0f;
			this.Renderer.BlurAmount.Value = 0.0f;
			this.Renderer.Tint.Value = Vector3.One;
			this.Renderer.Brightness.Value = 0.0f;
			this.Renderer.SpeedBlurAmount.Value = 0.0f;
			this.TimeMultiplier.Value = 1.0f;
			this.PauseAudioEffect.Value = 0.0f;
			this.Camera.Angles.Value = Vector3.Zero;
			this.Menu.ClearMessages();

			AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL);
			AkSoundEngine.SetState(AK.STATES.WATER.GROUP, AK.STATES.WATER.STATE.NORMAL);
		}

		public Command ReloadedContent = new Command();

		public ContentManager MapContent;

		public Main()
		{
			Factory<Main>.Initialize();

#if STEAMWORKS
			SteamWorker.Init();
#endif

			this.Space = new Space();
			this.ScreenSize.Value = new Point(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

			// Give the space some threads to work with.
			// Just throw a thread at every processor. The thread scheduler will take care of where to put them.
			for (int i = 0; i < Environment.ProcessorCount - 1; i++)
				this.Space.ThreadManager.AddThread();
			this.Space.ForceUpdater.Gravity = new Vector3(0, -18.0f, 0);

			this.IsFixedTimeStep = false;

			this.Window.AllowUserResizing = true;
			this.Window.ClientSizeChanged += new EventHandler<EventArgs>(delegate(object obj, EventArgs e)
			{
				if (!this.Graphics.IsFullScreen)
				{
					Rectangle bounds = this.Window.ClientBounds;
					this.ScreenSize.Value = new Point(bounds.Width, bounds.Height);
					this.resize = new Point(bounds.Width, bounds.Height);
				}
			});

			this.Graphics = new GraphicsDeviceManager(this);
			this.Graphics.SynchronizeWithVerticalRetrace = false;

			this.Content = new ContentManager(this.Services);
			this.Content.RootDirectory = "Content";

			this.Entities = new List<Entity>();

			this.Camera = new Camera();
			this.AddComponent(this.Camera);

			this.IsMouseVisible.Set = delegate(bool value)
			{
				base.IsMouseVisible = value;
			};
			this.IsMouseVisible.Get = delegate()
			{
				return base.IsMouseVisible;
			};

			this.TimeMultiplier.Set = delegate(float value)
			{
				this.TimeMultiplier.InternalValue = value;
				AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SLOWMOTION, Math.Min(1.0f, (1.0f - value) / 0.6f));
			};

			this.PauseAudioEffect.Set = delegate(float value)
			{
				value = MathHelper.Clamp(value, 0.0f, 1.0f);
				this.PauseAudioEffect.InternalValue = value;
				AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.PAUSE_PARAMETER, value);
			};

			new CommandBinding(this.MapLoaded, delegate()
			{
				this.mapLoaded = true;
			});

			Action updateLanguage = delegate()
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(this.Strings.Language.Value.ToString());
			};
			new NotifyBinding(updateLanguage, this.Strings.Language);
			updateLanguage();

#if DEVELOPMENT
			this.EditorEnabled.Value = true;
#else
			this.EditorEnabled.Value = false;
#endif

			this.dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lemma");
			if (!Directory.Exists(this.dataDirectory))
				Directory.CreateDirectory(this.dataDirectory);
			this.settingsFile = Path.Combine(this.dataDirectory, "settings.xml");
			this.SaveDirectory = Path.Combine(this.dataDirectory, "saves");
			if (!Directory.Exists(this.SaveDirectory))
				Directory.CreateDirectory(this.SaveDirectory);
			this.analyticsDirectory = Path.Combine(this.dataDirectory, "analytics");
			if (!Directory.Exists(this.analyticsDirectory))
				Directory.CreateDirectory(this.analyticsDirectory);

			try
			{
				// Attempt to load previous window state
				using (Stream stream = new FileStream(this.settingsFile, FileMode.Open, FileAccess.Read, FileShare.None))
					this.Settings = (Config)new XmlSerializer(typeof(Config)).Deserialize(stream);
				if (this.Settings.Version != Main.ConfigVersion)
					throw new Exception();
			}
			catch (Exception) // File doesn't exist, there was a deserialization error, or we are on a new version. Use default window settings
			{
				this.Settings = new Config { Version = Main.ConfigVersion, };
			}

			if (string.IsNullOrEmpty(this.Settings.UUID))
				this.Settings.UUID = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32);
			
			TextElement.BindableProperties.Add("Forward", this.Settings.Forward);
			TextElement.BindableProperties.Add("Left", this.Settings.Left);
			TextElement.BindableProperties.Add("Backward", this.Settings.Backward);
			TextElement.BindableProperties.Add("Right", this.Settings.Right);
			TextElement.BindableProperties.Add("Jump", this.Settings.Jump);
			TextElement.BindableProperties.Add("Parkour", this.Settings.Parkour);
			TextElement.BindableProperties.Add("RollKick", this.Settings.RollKick);
			TextElement.BindableProperties.Add("TogglePhone", this.Settings.TogglePhone);
			TextElement.BindableProperties.Add("QuickSave", this.Settings.QuickSave);
			TextElement.BindableProperties.Add("ToggleFullscreen", this.Settings.ToggleFullscreen);

			if (this.Settings.FullscreenResolution.Value.X == 0)
			{
				Microsoft.Xna.Framework.Graphics.DisplayMode display = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				this.Settings.FullscreenResolution.Value = new Point(display.Width, display.Height);
			}

			// Have to create the menu here so it can catch the PreparingDeviceSettings event
			// We call AddComponent(this.Menu) later on in LoadContent.
			this.Menu = new Menu();
			this.Graphics.PreparingDeviceSettings += delegate(object sender, PreparingDeviceSettingsEventArgs args)
			{
				DisplayModeCollection supportedDisplayModes = args.GraphicsDeviceInformation.Adapter.SupportedDisplayModes;
				int displayModeIndex = 0;
				foreach (DisplayMode mode in supportedDisplayModes)
				{
					if (mode.Format == SurfaceFormat.Color && mode.Width == this.Settings.FullscreenResolution.Value.X && mode.Height == this.Settings.FullscreenResolution.Value.Y)
						break;
					displayModeIndex++;
				}
				this.Menu.SetupDisplayModes(supportedDisplayModes, displayModeIndex);
			};

			this.Screenshot = new Screenshot();
			this.AddComponent(this.Screenshot);

			// Restore window state
			this.Graphics.SynchronizeWithVerticalRetrace = this.Settings.EnableVsync;
			new NotifyBinding(delegate()
			{
				this.Graphics.SynchronizeWithVerticalRetrace = this.Settings.EnableVsync;
				this.Graphics.ApplyChanges();
			}, this.Settings.EnableVsync);
			if (this.Settings.Fullscreen)
				this.ResizeViewport(this.Settings.FullscreenResolution.Value.X, this.Settings.FullscreenResolution.Value.Y, true);
			else
				this.ResizeViewport(this.Settings.Size.Value.X, this.Settings.Size.Value.Y, false, false);
		}

		private void copySave(string src, string dst)
		{
			if (!Directory.Exists(dst))
				Directory.CreateDirectory(dst);

			string[] whitelistExtensions = new[] { ".map", };

			foreach (string path in Directory.GetFiles(src))
			{
				string filename = Path.GetFileName(path);
				if (whitelistExtensions.Contains(Path.GetExtension(filename)))
					File.Copy(path, Path.Combine(dst, filename));
			}

			foreach (string path in Directory.GetDirectories(src))
				this.copySave(path, Path.Combine(dst, Path.GetFileName(path)));
		}

#if ANALYTICS
		public Session.Recorder SessionRecorder;

		public void SaveAnalytics()
		{
			string map = this.MapFile;
			string filename = Build + "-" + (string.IsNullOrEmpty(map) ? "null" : Path.GetFileName(map)) + "-" + Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32) + ".xml";
			this.SessionRecorder.Save(Path.Combine(this.analyticsDirectory, filename), map, this.TotalTime);
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
				Session s;
				try
				{
					s = Session.Load(file);
				}
				catch (Exception)
				{
					Log.d("Error loading analytics file " + file);
					continue;
				}

				if (s.Build == Main.Build)
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

		public Property<string> CurrentSave = new Property<string>();
		public void Cleanup()
		{
			// Terminate Wwise
			if (AkSoundEngine.IsInitialized())
			{
				AkSoundEngine.Term();
				// NOTE: AkCallbackManager needs to handle last few events after sound engine terminates
				// So it has to terminate after sound engine does.
				AkCallbackManager.Term();
			}

			Rumble.Reset();
		}

		protected bool firstLoadContentCall = true;

		protected override void LoadContent()
		{
			if (this.firstLoadContentCall)
			{
				this.GeeUI = new GeeUIMain();
				this.AddComponent(GeeUI);

				this.ConsoleUI = new ConsoleUI();
				this.AddComponent(ConsoleUI);

				this.Console = new Console.Console();
				this.AddComponent(Console);

				Lemma.Console.Console.BindType(null, this);
				Lemma.Console.Console.BindType(null, Console);

				// Initialize Wwise
				AkGlobalSoundEngineInitializer initializer = new AkGlobalSoundEngineInitializer(Path.Combine(this.Content.RootDirectory, "Wwise"));
				this.AddComponent(initializer);

				this.Listener = new AkListener();
				this.Listener.Add(new Binding<Vector3>(this.Listener.Position, this.Camera.Position));
				this.Listener.Add(new Binding<Vector3>(this.Listener.Forward, this.Camera.Forward));
				this.Listener.Add(new Binding<Vector3>(this.Listener.Up, this.Camera.Up));
				this.AddComponent(this.Listener);

				// Create the renderer.
				this.LightingManager = new LightingManager();
				this.AddComponent(this.LightingManager);
				this.Renderer = new Renderer(this, this.ScreenSize, true, true, true, true, true);

				this.AddComponent(this.Renderer);
				this.renderParameters = new RenderParameters
				{
					Camera = this.Camera,
					IsMainRender = true
				};
				this.firstLoadContentCall = false;


				this.UI = new UIRenderer();
				this.AddComponent(this.UI);

				PCInput input = new PCInput();
				this.AddComponent(input);

				Lemma.Console.Console.BindType(null, input);
				Lemma.Console.Console.BindType(null, UI);
				Lemma.Console.Console.BindType(null, Renderer);
				Lemma.Console.Console.BindType(null, LightingManager);


#if DEVELOPMENT
				input.Add(new CommandBinding(input.GetChord(new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.S }), delegate()
				{
					// High-resolution screenshot
					Screenshot s = new Screenshot();
					this.AddComponent(s);
					s.Take(new Point(4096, 2304), delegate()
					{
						string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
						string path;
						int i = 0;
						do
						{
							path = Path.Combine(desktop, "lemma-screen" + i.ToString() + ".png");
							i++;
						}
						while (File.Exists(path));

						using (Stream stream = File.OpenWrite(path))
							s.Buffer.SaveAsPng(stream, s.Size.X, s.Size.Y);
						s.Delete.Execute();
					});
				}));
#endif

#if PERFORMANCE_MONITOR
				this.performanceMonitor = new ListContainer();
				this.performanceMonitor.Add(new Binding<Vector2, Point>(performanceMonitor.Position, x => new Vector2(0, x.Y), this.ScreenSize));
				this.performanceMonitor.AnchorPoint.Value = new Vector2(0, 1);
				this.performanceMonitor.Visible.Value = false;
				this.performanceMonitor.Name.Value = "PerformanceMonitor";
				this.UI.Root.Children.Add(this.performanceMonitor);

				Action<string, Property<double>> addTimer = delegate(string label, Property<double> property)
				{
					TextElement text = new TextElement();
					text.FontFile.Value = "Font";
					text.Add(new Binding<string, double>(text.Text, x => label + ": " + (x * 1000.0).ToString("F") + "ms", property));
					this.performanceMonitor.Children.Add(text);
				};

				Action<string, Property<int>> addCounter = delegate(string label, Property<int> property)
				{
					TextElement text = new TextElement();
					text.FontFile.Value = "Font";
					text.Add(new Binding<string, int>(text.Text, x => label + ": " + x.ToString(), property));
					this.performanceMonitor.Children.Add(text);
				};

				TextElement frameRateText = new TextElement();
				frameRateText.FontFile.Value = "Font";
				frameRateText.Add(new Binding<string, float>(frameRateText.Text, x => "FPS: " + x.ToString("0"), this.frameRate));
				this.performanceMonitor.Children.Add(frameRateText);

				addTimer("Physics", this.physicsTime);
				addTimer("Update", this.updateTime);
				addTimer("Pre-frame", this.preframeTime);
				addTimer("Raw render", this.rawRenderTime);
				addTimer("Shadow render", this.shadowRenderTime);
				addTimer("Post-process", this.postProcessTime);
				addTimer("Non-post-processed", this.unPostProcessedTime);
				addCounter("Draw calls", this.drawCalls);
				addCounter("Triangles", this.triangles);

				input.Add(new CommandBinding(input.GetChord(new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.P }), delegate()
				{
					this.performanceMonitor.Visible.Value = !this.performanceMonitor.Visible;
				}));
#endif

				try
				{
					IEnumerable<string> globalStaticScripts = Directory.GetFiles(Path.Combine(this.Content.RootDirectory, "GlobalStaticScripts"), "*", SearchOption.AllDirectories).Select(x => Path.Combine("..\\GlobalStaticScripts", Path.GetFileNameWithoutExtension(x)));
					foreach (string scriptName in globalStaticScripts)
						this.executeStaticScript(scriptName);
				}
				catch (IOException)
				{

				}

				this.UIFactory = new UIFactory();
				this.AddComponent(this.UIFactory);
				this.AddComponent(this.Menu); // Have to do this here so the menu's Awake can use all our loaded stuff

				this.Spawner = new Spawner();
				this.AddComponent(this.Spawner);

				this.IsMouseVisible.Value = true;

				if (AkBankLoader.LoadBank("SFX_Bank_01.bnk") != AKRESULT.AK_Success)
					Log.d("Failed to load sound bank");

				AkBankLoader.LoadBank("Music.bnk");

#if ANALYTICS
				this.SessionRecorder = new Session.Recorder();
				this.AddComponent(this.SessionRecorder);

				this.SessionRecorder.Add("Position", delegate()
				{
					Entity p = PlayerFactory.Instance;
					if (p != null && p.Active)
						return p.Get<Transform>().Position;
					else
						return Vector3.Zero;
				});

				this.SessionRecorder.Add("Health", delegate()
				{
					Entity p = PlayerFactory.Instance;
					if (p != null && p.Active)
						return p.Get<Player>().Health;
					else
						return 0.0f;
				});
#endif

				this.MapFile.Set = delegate(string value)
				{
					if (string.IsNullOrEmpty(value))
					{
						this.MapFile.InternalValue = null;
						return;
					}

					try
					{
						string directory = this.CurrentSave.Value == null ? null : Path.Combine(this.SaveDirectory, this.CurrentSave);
						if (value == Main.MenuMap)
							directory = null; // Don't try to load the menu from a save game
						IO.MapLoader.Load(this, directory, value, false);
					}
					catch (FileNotFoundException)
					{
						this.MapFile.InternalValue = value;

						this.ClearEntities(false);

						// Create a new map
						Entity world = Factory.Get<WorldFactory>().CreateAndBind(this);
						world.Get<Transform>().Position.Value = new Vector3(0, 3, 0);
						this.Add(world);

						Entity ambientLight = Factory.Get<AmbientLightFactory>().CreateAndBind(this);
						ambientLight.Get<Transform>().Position.Value = new Vector3(0, 5.0f, 0);
						ambientLight.Get<AmbientLight>().Color.Value = new Vector3(0.25f, 0.25f, 0.25f);
						this.Add(ambientLight);

						Entity map = Factory.Get<VoxelFactory>().CreateAndBind(this);
						map.Get<Transform>().Position.Value = new Vector3(0, 1, 0);
						this.Add(map);

						this.MapLoaded.Execute();
					}
				};

				this.Renderer.LightRampTexture.Value = "Images\\default-ramp";
				this.Renderer.EnvironmentMap.Value = "Images\\env0";

				this.Settings.SoundEffectVolume.Set = delegate(float value)
				{
					value = MathHelper.Clamp(value, 0.0f, 1.0f);
					this.Settings.SoundEffectVolume.InternalValue = value;
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.VOLUME_SFX, value);
				};
				this.Settings.SoundEffectVolume.Reset();

				this.Settings.MusicVolume.Set = delegate(float value)
				{
					value = MathHelper.Clamp(value, 0.0f, 1.0f);
					this.Settings.MusicVolume.InternalValue = value;
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.VOLUME_MUSIC, value);
				};
				this.Settings.MusicVolume.Reset();

				new TwoWayBinding<LightingManager.DynamicShadowSetting>(this.Settings.DynamicShadows, this.LightingManager.DynamicShadows);
				new TwoWayBinding<float>(this.Settings.MotionBlurAmount, this.Renderer.MotionBlurAmount);
				new TwoWayBinding<float>(this.Settings.Gamma, this.Renderer.Gamma);
				new TwoWayBinding<bool>(this.Settings.EnableBloom, this.Renderer.EnableBloom);
				new TwoWayBinding<bool>(this.Settings.EnableSSAO, this.Renderer.EnableSSAO);
				new TwoWayBinding<float>(this.Settings.FieldOfView, this.Camera.FieldOfView);

				// Load strings
				this.Strings.Load(Path.Combine(this.Content.RootDirectory, "Strings.xlsx"));

				foreach (string file in Directory.GetFiles(Path.Combine(this.Content.RootDirectory, "Game"), "*.xlsx", SearchOption.TopDirectoryOnly))
					this.Strings.Load(file);

				new Binding<string, Config.Lang>(this.Strings.Language, x => x.ToString(), this.Settings.Language);
				new NotifyBinding(this.SaveSettings, this.Settings.Language);

				new CommandBinding(this.MapLoaded, delegate()
				{
					this.Renderer.BlurAmount.Value = 0.0f;
					this.Renderer.Tint.Value = new Vector3(1.0f);
				});
				this.MapFile.Value = MenuMap;
				this.Menu.Pause();

				//Editor is an external option mate
#if !DEVELOPMENT
					// Main menu

					
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
			else
			{
				foreach (IDrawableComponent c in this.drawables)
					c.LoadContent(true);
				foreach (IDrawableAlphaComponent c in this.alphaDrawables)
					c.LoadContent(true);
				foreach (IDrawablePostAlphaComponent c in this.postAlphaDrawables)
					c.LoadContent(true);
				foreach (IDrawablePreFrameComponent c in this.preframeDrawables)
					c.LoadContent(true);
				foreach (INonPostProcessedDrawableComponent c in this.nonPostProcessedDrawables)
					c.LoadContent(true);
				this.ReloadedContent.Execute();
			}

			this.GraphicsDevice.RasterizerState = new RasterizerState { MultiSampleAntiAlias = false };
		}

		private bool componentEnabled(IComponent c)
		{
			return c.Active && c.Enabled && !c.Suspended && (!this.EditorEnabled || c.EnabledInEditMode) && (!this.Paused || c.EnabledWhenPaused);
		}

		protected void executeScript(string scriptName)
		{
			string id = "global_script_" + scriptName;
			Entity existingEntity = this.GetByID(id);
			if (existingEntity != null)
				existingEntity.Get<Script>().Execute.Execute();
			else
			{
				Entity scriptEntity = Factory.Get<ScriptFactory>().Create(this);
				scriptEntity.ID.Value = id;
				Factory.Get<ScriptFactory>().Bind(scriptEntity, this, true);
				scriptEntity.Serialize = false;
				this.Add(scriptEntity);
				scriptEntity.GetProperty<bool>("ExecuteOnLoad").Value = false;
				Script script = scriptEntity.Get<Script>();
				script.Name.Value = scriptName;
				if (!string.IsNullOrEmpty(script.Errors))
					throw new Exception(script.Errors);
				else
					script.Execute.Execute();
			}
		}

		protected void executeStaticScript(string scriptName)
		{
			Entity scriptEntity = Factory.Get<ScriptFactory>().CreateAndBind(this);
			scriptEntity.Serialize = false;
			this.Add(scriptEntity);
			scriptEntity.GetProperty<bool>("ExecuteOnLoad").Value = false;
			Script script = scriptEntity.Get<Script>();
			script.Name.Value = scriptName;
			if (!string.IsNullOrEmpty(script.Errors))
				throw new Exception(script.Errors);
			else
				script.Execute.Execute();
			scriptEntity.Delete.Execute();
		}

		private void createNewSave()
		{
			string newSave = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
			if (newSave != this.CurrentSave)
			{
				this.copySave(this.CurrentSave.Value == null ? IO.MapLoader.MapDirectory : Path.Combine(this.SaveDirectory, this.CurrentSave), Path.Combine(this.SaveDirectory, newSave));
				this.CurrentSave.Value = newSave;
			}
		}

		public void SaveNew()
		{
			this.save(null); // null means don't delete any old saves
		}

		public void SaveOverwrite(string oldSave = null)
		{
			if (oldSave == null)
				oldSave = this.CurrentSave;
			this.save(oldSave);
		}

		private void save(string oldSave)
		{
			Action doSave = delegate()
			{
				// Delete the old save thumbnail.
				if (oldSave != null)
					this.Menu.RemoveSaveGame(oldSave);

				// Create the new save.
				this.createNewSave();

				this.SaveCurrentMap(this.Screenshot.Buffer, this.Screenshot.Size);

				this.Screenshot.Clear();

				this.Menu.AddSaveGame(this.CurrentSave);

				// Delete the old save files.
				// We have to do this AFTER creating the new save
				// because it copies the old save to create the new one
				if (oldSave != null)
					Directory.Delete(Path.Combine(this.SaveDirectory, oldSave), true);
			};

			if (this.Screenshot.Buffer == null)
				this.Screenshot.Take(this.ScreenSize, doSave);
			else
				doSave();
		}

		public void SaveCurrentMap(RenderTarget2D screenshot = null, Point screenshotSize = default(Point))
		{
			if (this.CurrentSave.Value == null)
				this.createNewSave();

			string currentSaveDirectory = Path.Combine(this.SaveDirectory, this.CurrentSave);
			if (screenshot != null)
			{
				string screenshotPath = Path.Combine(currentSaveDirectory, "thumbnail.jpg");
				using (Stream stream = File.OpenWrite(screenshotPath))
					screenshot.SaveAsJpeg(stream, 256, (int)(screenshotSize.Y * (256.0f / screenshotSize.X)));
			}

			IO.MapLoader.Save(this, currentSaveDirectory, this.MapFile);

			try
			{
				using (Stream stream = new FileStream(Path.Combine(currentSaveDirectory, "save.xml"), FileMode.Create, FileAccess.Write, FileShare.None))
					new XmlSerializer(typeof(Main.SaveInfo)).Serialize(stream, new Main.SaveInfo { MapFile = this.MapFile, Version = Main.MapVersion });
			}
			catch (InvalidOperationException e)
			{
				throw new Exception("Failed to save game.", e);
			}
		}

		public void SaveSettings()
		{
			// Save settings
			using (Stream stream = new FileStream(this.settingsFile, FileMode.Create, FileAccess.Write, FileShare.None))
				new XmlSerializer(typeof(Config)).Serialize(stream, this.Settings);
		}

		public void EnterFullscreen()
		{
			if (!this.Graphics.IsFullScreen)
			{
				Point res = this.Settings.FullscreenResolution;
				this.ResizeViewport(res.X, res.Y, true);
			}
		}

		public void ExitFullscreen()
		{
			if (this.Graphics.IsFullScreen)
			{
				Point res = this.Settings.Size;
				this.ResizeViewport(res.X, res.Y, false);
			}
		}

		protected override void Update(GameTime gameTime)
		{
			if (gameTime.ElapsedGameTime.TotalSeconds > 0.1f)
				gameTime = new GameTime(gameTime.TotalGameTime, new TimeSpan((long)(0.1f * (float)TimeSpan.TicksPerSecond)), true);
			this.GameTime = gameTime;
			this.ElapsedTime.Value = (float)gameTime.ElapsedGameTime.TotalSeconds * this.TimeMultiplier;
			if (!this.Paused)
				this.TotalTime.Value += this.ElapsedTime;

			if (!this.EditorEnabled && this.mapLoaded)
			{
				try
				{
					IEnumerable<string> mapGlobalScripts = Directory.GetFiles(Path.Combine(this.Content.RootDirectory, "GlobalScripts"), "*", SearchOption.AllDirectories).Select(x => Path.Combine("..\\GlobalScripts", Path.GetFileNameWithoutExtension(x)));
					foreach (string scriptName in mapGlobalScripts)
						this.executeScript(scriptName);
				}
				catch (IOException)
				{

				}
			}
			this.mapLoaded = false;

			this.LastKeyboardState.Value = this.KeyboardState;
			this.KeyboardState.Value = Microsoft.Xna.Framework.Input.Keyboard.GetState();
			this.LastMouseState.Value = this.MouseState;
			this.MouseState.Value = Microsoft.Xna.Framework.Input.Mouse.GetState();

			this.LastGamePadState.Value = this.GamePadState;
			this.GamePadState.Value = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One);
			if (this.GamePadState.Value.IsConnected != this.GamePadConnected)
				this.GamePadConnected.Value = this.GamePadState.Value.IsConnected;

#if PERFORMANCE_MONITOR
			Stopwatch timer = new Stopwatch();
			timer.Start();
#endif
			for (int i = 0; i < this.updateables.Count; i++)
			{
				IUpdateableComponent c = this.updateables[i];
				if (this.componentEnabled(c))
					c.Update(this.ElapsedTime);
			}
			this.FlushComponents();

			if (this.alphaDrawableBinding == null)
			{
				this.alphaDrawableBinding = new NotifyBinding(delegate() { this.alphaDrawablesModified = true; }, this.alphaDrawables.Select(x => x.DrawOrder).ToArray());
				this.alphaDrawablesModified = true;
			}
			if (this.alphaDrawablesModified)
			{
				this.alphaDrawables.InsertionSort(delegate(IDrawableAlphaComponent a, IDrawableAlphaComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
			}

			if (this.postAlphaDrawableBinding == null)
			{
				this.postAlphaDrawableBinding = new NotifyBinding(delegate() { this.postAlphaDrawablesModified = true; }, this.postAlphaDrawables.Select(x => x.DrawOrder).ToArray());
				this.postAlphaDrawablesModified = true;
			}
			if (this.postAlphaDrawablesModified)
			{
				this.postAlphaDrawables.InsertionSort(delegate(IDrawablePostAlphaComponent a, IDrawablePostAlphaComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
			}

			if (this.nonPostProcessedDrawableBinding == null)
			{
				this.nonPostProcessedDrawableBinding = new NotifyBinding(delegate() { this.nonPostProcessedDrawablesModified = true; }, this.nonPostProcessedDrawables.Select(x => x.DrawOrder).ToArray());
				this.nonPostProcessedDrawablesModified = true;
			}
			if (this.nonPostProcessedDrawablesModified)
			{
				this.nonPostProcessedDrawables.InsertionSort(delegate(INonPostProcessedDrawableComponent a, INonPostProcessedDrawableComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
			}

			if (this.resize != null && this.resize.Value.X > 0 && this.resize.Value.Y > 0)
			{
				this.ResizeViewport(this.resize.Value.X, this.resize.Value.Y, false);
				this.resize = null;
			}

#if PERFORMANCE_MONITOR
			timer.Stop();
			this.updateSum = Math.Max(this.updateSum, timer.Elapsed.TotalSeconds);

			timer.Restart();
#endif
			if (!this.Paused && !this.EditorEnabled)
				this.Space.Update(this.ElapsedTime);
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.physicsSum = Math.Max(this.physicsSum, timer.Elapsed.TotalSeconds);

			this.frameSum++;
			this.performanceInterval += (float)this.GameTime.ElapsedGameTime.TotalSeconds;
			if (this.performanceInterval > Main.performanceUpdateTime)
			{
				if (this.performanceMonitor.Visible)
				{
					this.frameRate.Value = this.frameSum / this.performanceInterval;
					this.physicsTime.Value = this.physicsSum;
					this.updateTime.Value = this.updateSum;
					this.preframeTime.Value = this.preframeSum;
					this.rawRenderTime.Value = this.rawRenderSum;
					this.shadowRenderTime.Value = this.shadowRenderSum;
					this.postProcessTime.Value = this.postProcessSum;
					this.unPostProcessedTime.Value = this.unPostProcessedSum;
					this.drawCalls.Value = this.drawCallCounter;
					this.triangles.Value = this.triangleCounter;
					this.drawCallCounter = 0;
					this.triangleCounter = 0;
				}
				this.physicsSum = 0;
				this.updateSum = 0;
				this.preframeSum = 0;
				this.rawRenderSum = 0;
				this.shadowRenderSum = 0;
				this.postProcessSum = 0;
				this.unPostProcessedSum = 0;
				this.frameSum = 0;
				this.performanceInterval = 0;
			}
#endif

			AkSoundEngine.RenderAudio();

			TotalGameTime.Value += this.ElapsedTime.Value;
#if STEAMWORKS
			SteamWorker.SetStat("stat_time_played", (int)TotalGameTime.Value);
			SteamWorker.Update();
#endif
		}

		protected override void Draw(GameTime gameTime)
		{
			if (this.GraphicsDevice == null || this.GraphicsDevice.IsDisposed || this.GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
				return;

			Lemma.Components.Model.DrawCallCounter = 0;
			Lemma.Components.Model.TriangleCounter = 0;

#if PERFORMANCE_MONITOR
			Stopwatch timer = new Stopwatch();
			timer.Start();
#endif
			this.renderParameters.Technique = Technique.Render;

			foreach (IDrawablePreFrameComponent c in this.preframeDrawables)
			{
				if (this.componentEnabled(c))
					c.DrawPreFrame(gameTime, this.renderParameters);
			}
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.preframeSum = Math.Max(timer.Elapsed.TotalSeconds, this.preframeSum);
#endif

			this.Renderer.SetRenderTargets(this.renderParameters);

#if PERFORMANCE_MONITOR
			timer.Restart();
#endif
			this.DrawScene(this.renderParameters);
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.rawRenderSum = Math.Max(this.rawRenderSum, timer.Elapsed.TotalSeconds);

			timer.Restart();
#endif
			this.LightingManager.UpdateGlobalLights();
			this.LightingManager.RenderShadowMaps(this.Camera);
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.shadowRenderSum = Math.Max(this.shadowRenderSum, timer.Elapsed.TotalSeconds);

			timer.Restart();
#endif
			this.Renderer.PostProcess(this.RenderTarget, this.renderParameters);

#if PERFORMANCE_MONITOR
			timer.Stop();
			this.postProcessSum = Math.Max(this.postProcessSum, timer.Elapsed.TotalSeconds);

			timer.Restart();
#endif

			foreach (INonPostProcessedDrawableComponent c in this.nonPostProcessedDrawables)
			{
				if (this.componentEnabled(c))
					c.DrawNonPostProcessed(gameTime, this.renderParameters);
			}

#if PERFORMANCE_MONITOR
			timer.Stop();
			this.unPostProcessedSum = Math.Max(this.unPostProcessedSum, timer.Elapsed.TotalSeconds);
			this.drawCallCounter = Math.Max(this.drawCallCounter, Lemma.Components.Model.DrawCallCounter);
			this.triangleCounter = Math.Max(this.triangleCounter, Lemma.Components.Model.TriangleCounter);
#endif

			if (this.RenderTarget != null)
			{
				// We just rendered to a target other than the screen.
				// So make it so we're rendering to the screen again, then copy the render target to the screen.

				this.GraphicsDevice.SetRenderTarget(null);

				SpriteBatch spriteBatch = new SpriteBatch(this.GraphicsDevice);
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
				spriteBatch.Draw(this.RenderTarget, Vector2.Zero, Color.White);
				spriteBatch.End();

				this.RenderTarget = null;
			}
			//SpriteBatch GeeUISpriteBatch = new SpriteBatch(this.GraphicsDevice);
			//GeeUISpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
			//GeeUI.GeeUI.Draw(GeeUISpriteBatch);
			//GeeUISpriteBatch.End();
		}

		public void DrawScene(RenderParameters parameters)
		{
			RasterizerState originalState = this.GraphicsDevice.RasterizerState;
			RasterizerState reverseCullState = null;

			if (parameters.ReverseCullOrder)
			{
				reverseCullState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };
				this.GraphicsDevice.RasterizerState = reverseCullState;
			}

			Vector3 cameraPos = parameters.Camera.Position;
			BoundingFrustum frustum = parameters.Camera.BoundingFrustum;
			List<IDrawableComponent> drawables = this.drawables.Where(c => this.componentEnabled(c) && c.IsVisible(frustum)).ToList();
			drawables.Sort(delegate(IDrawableComponent a, IDrawableComponent b)
			{
				return a.GetDistance(cameraPos).CompareTo(b.GetDistance(cameraPos));
			});

			foreach (IDrawableComponent c in drawables)
				c.Draw(this.GameTime, parameters);

			if (reverseCullState != null)
				this.GraphicsDevice.RasterizerState = originalState;
		}

		public void DrawAlphaComponents(RenderParameters parameters)
		{
			foreach (IDrawableAlphaComponent c in this.alphaDrawables)
			{
				if (this.componentEnabled(c))
					c.DrawAlpha(this.GameTime, parameters);
			}
		}

		public void DrawPostAlphaComponents(RenderParameters parameters)
		{
			foreach (IDrawablePostAlphaComponent c in this.postAlphaDrawables)
			{
				if (this.componentEnabled(c))
					c.DrawPostAlpha(this.GameTime, parameters);
			}
		}

		public void ResizeViewport(int width, int height, bool fullscreen, bool applyChanges = true)
		{
			bool needApply = false;
			if (this.Graphics.IsFullScreen != fullscreen)
			{
				this.Graphics.IsFullScreen = fullscreen;
				needApply = true;
			}
			if (this.Graphics.PreferredBackBufferWidth != width)
			{
				this.Graphics.PreferredBackBufferWidth = width;
				needApply = true;
			}
			if (this.Graphics.PreferredBackBufferHeight != height)
			{
				this.Graphics.PreferredBackBufferHeight = height;
				needApply = true;
			}
			if (applyChanges && needApply)
				this.Graphics.ApplyChanges();

			this.ScreenSize.Value = new Point(width, height);

			if (this.Renderer != null)
				this.Renderer.ReallocateBuffers(this.ScreenSize);

			if (this.GeeUI != null)
			{
				GeeUI.RootView.Width.Value = width;
				GeeUI.RootView.Height.Value = height;
			}

			this.Settings.Fullscreen.Value = fullscreen;
			if (fullscreen)
				this.Settings.FullscreenResolution.Value = new Point(width, height);
			else
				this.Settings.Size.Value = new Point(width, height);
			this.SaveSettings();
		}
	}
}
