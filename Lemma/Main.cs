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
using System.Reflection;
using System.Globalization;
using GeeUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ICSharpCode.SharpZipLib.GZip;
using System.Xml.Serialization;

namespace Lemma
{
	public class Main : BaseMain
	{
		static Main()
		{
			JsonConvert.DefaultSettings = delegate()
			{
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Converters.Add(new StringEnumConverter());
				return settings;
			};
		}

		public const string InitialMap = "rain";

		public string Font
		{
			get
			{
#if VR
				if (this.VR)
					return "FontVR";
				else
#endif
					return "Font";
			}
		}

		public float FontMultiplier
		{
			get
			{
#if VR
				if (this.VR)
					return 16.0f / 11.0f;
				else
#endif
					return 1.0f;
			}
		}

		public string FontLarge
		{
			get
			{
#if VR
				if (this.VR)
					return "FontLargeVR";
				else
#endif
					return "FontLarge";
			}
		}

		public const int SteamAppID = 300340;

		public const string MenuMap = "..\\menu";
		public const string TemplateMap = "..\\template";

		public class ExitException : Exception
		{
		}

		public const int ConfigVersion = 9;
		public const int MapVersion = 1027;
		public const int Build = 1088;

		public class Config
		{
			public enum Lang
			{
				en,
				es,
				fr,
				pl,
				hu,
				tr,
			}
			public enum RecordAnalytics
			{
				Ask,
				On,
				Off,
			}
			public Property<Lang> Language = new Property<Lang>();
			public Property<bool> Fullscreen = new Property<bool>();
			public Property<Point> Size = new Property<Point>();
			public Property<bool> Borderless = new Property<bool>();
			public Property<Point> FullscreenResolution = new Property<Point>();
			public Property<float> MotionBlurAmount = new Property<float>();
			public Property<float> Gamma = new Property<float>();
			public Property<bool> Reflections = new Property<bool>();
			public Property<bool> SSAO = new Property<bool>();
			public Property<bool> VolumetricLighting = new Property<bool>();
			public Property<bool> Bloom = new Property<bool>();
			public Property<LightingManager.DynamicShadowSetting> DynamicShadows = new Property<LightingManager.DynamicShadowSetting>();
			public Property<bool> InvertMouseX = new Property<bool>();
			public Property<bool> InvertMouseY = new Property<bool>();
			public Property<bool> ControllerVibration = new Property<bool>();
			public Property<bool> EnableReticle = new Property<bool>();
			public Property<bool> EnableReticleVR = new Property<bool>();
			public Property<bool> EnableWaypoints = new Property<bool>();
			public Property<float> MouseSensitivity = new Property<float>();
			public Property<float> FieldOfView = new Property<float>();
			public Property<bool> Vsync = new Property<bool>();
			public Property<float> SoundEffectVolume = new Property<float> { Value = 1.0f };
			public Property<float> MusicVolume = new Property<float> { Value = 1.0f };
			public Property<int> FPSLimit = new Property<int>();
			public Property<RecordAnalytics> Analytics = new Property<RecordAnalytics>();
			public Property<bool> MinimizeCameraMovement = new Property<bool>();
			public Property<bool> MinimizeCameraMovementVR = new Property<bool>();
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public bool GodMode;
			[JsonIgnore]
			public Property<bool> GodModeProperty = new Property<bool>();
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public int LevelIndex;
			[JsonIgnore]
			public Property<int> LevelIndexProperty = new Property<int>();
			public int Version;
			public string UUID;
			public Property<PCInput.PCInputBinding> Forward = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> Left = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> Right = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> Backward = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> Jump = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> Parkour = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> RollKick = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> TogglePhone = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> QuickSave = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> ToggleFullscreen = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> ToggleConsole = new Property<PCInput.PCInputBinding>();
			public Property<PCInput.PCInputBinding> RecenterVRPose = new Property<PCInput.PCInputBinding>();

			public Config()
			{
				this.FactoryDefaults();
			}

			public void DefaultControls()
			{
				this.Forward.Value = new PCInput.PCInputBinding { Key = Keys.W };
				this.Left.Value = new PCInput.PCInputBinding { Key = Keys.A };
				this.Right.Value = new PCInput.PCInputBinding { Key = Keys.D };
				this.Backward.Value = new PCInput.PCInputBinding { Key = Keys.S };
				this.Jump.Value = new PCInput.PCInputBinding { Key = Keys.Space, GamePadButton = Buttons.RightTrigger };
				this.Parkour.Value = new PCInput.PCInputBinding { Key = Keys.LeftShift, GamePadButton = Buttons.LeftTrigger };
				this.RollKick.Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.LeftMouseButton, GamePadButton = Buttons.LeftStick };
				this.TogglePhone.Value = new PCInput.PCInputBinding { Key = Keys.Tab, GamePadButton = Buttons.Y };
				this.QuickSave.Value = new PCInput.PCInputBinding { Key = Keys.F5 };
				this.ToggleFullscreen.Value = new PCInput.PCInputBinding { Key = Keys.F11 };
				this.ToggleConsole.Value = new PCInput.PCInputBinding { Key = Keys.OemTilde };
				this.RecenterVRPose.Value = new PCInput.PCInputBinding { Key = Keys.F2, GamePadButton = Buttons.Back };
				this.InvertMouseX.Value = false;
				this.InvertMouseY.Value = false;
				this.ControllerVibration.Value = true;
				this.MouseSensitivity.Value = 1.0f;
			}

			public void DefaultOptions()
			{
				this.Version = Main.ConfigVersion;
				this.Language.Value = default(Lang);
				this.Fullscreen.Value = true;
				this.Size.Value = new Point(1280, 720);
				this.Borderless.Value = true;
				this.FullscreenResolution.Value = Point.Zero;
				this.MotionBlurAmount.Value = 0.5f;
				this.Gamma.Value = 1.0f;
				this.Reflections.Value = true;
				this.VolumetricLighting.Value = true;
				this.Bloom.Value = true;
				this.DynamicShadows.Value = LightingManager.DynamicShadowSetting.High;
				this.EnableReticle.Value = false;
				this.EnableReticleVR.Value = true;
				this.EnableWaypoints.Value = true;
				this.FieldOfView.Value = MathHelper.ToRadians(80.0f);
				this.Vsync.Value = false;
				this.SoundEffectVolume.Value = 1.0f;
				this.MusicVolume.Value = 1.0f;
				this.FPSLimit.Value = 120;
			}

			public void FactoryDefaults()
			{
				this.DefaultOptions();
				this.DefaultControls();
			}
		}

		public class SaveInfo
		{
			public static XmlSerializer Serializer = new XmlSerializer(typeof(SaveInfo), IO.MapLoader.IncludedTypes);
			public string MapFile;
			public int Version;
			public Entity PlayerData;
		}

		public Config Settings;

		public static string DataDirectory
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lemma");
			}
		}

		public string SaveDirectory;
		private string analyticsDirectory;
		public string CustomMapDirectory;
		public string MapDirectory;
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

		private const float performanceUpdateTime = 0.5f;
		private float performanceInterval;

		private ListContainer performanceMonitor;

		private int frameSum;
		private Property<float> frameRate = new Property<float>();
		private double physicsSum;
		private Property<double> physicsTime = new Property<double>();
		private double updateSum;
		private Property<double> updateTime = new Property<double>();
		private double renderSum;
		private Property<double> renderTime = new Property<double>();
		private Property<int> drawCalls = new Property<int>();
		private Property<int> workingSet = new Property<int>();
		private int drawCallCounter;
		private Property<int> triangles = new Property<int>();
		private int triangleCounter;

		private Animation scheduledSave;

		public Property<Point> ScreenSize = new Property<Point>();

		public Property<bool> MinimizeCameraMovement;

		public LightingManager LightingManager;

		public UIRenderer UI;
		public GeeUIMain GeeUI;

		public Console.Console Console;
		public ConsoleUI ConsoleUI;

		private bool mapLoaded;

		public Space Space;

		private TimeSpan targetElapsedTime;

		private List<IGraphicsComponent> graphicsComponents = new List<IGraphicsComponent>();
		private List<IDrawableComponent> drawables = new List<IDrawableComponent>();
		private List<IEarlyUpdateableComponent> earlyUpdateables = new List<IEarlyUpdateableComponent>();
		private List<IUpdateableComponent> updateables = new List<IUpdateableComponent>();
		private List<IDrawablePreFrameComponent> preframeDrawables = new List<IDrawablePreFrameComponent>();
		private List<INonPostProcessedDrawableComponent> nonPostProcessedDrawables = new List<INonPostProcessedDrawableComponent>();
		private List<IDrawableAlphaComponent> alphaDrawables = new List<IDrawableAlphaComponent>();
		private List<IDrawablePostAlphaComponent> postAlphaDrawables = new List<IDrawablePostAlphaComponent>();

		private Point? resize;

		public Property<string> MapFile = new Property<string>();

		public Spawner Spawner;

		public Property<KeyboardState> LastKeyboardState = new Property<KeyboardState>();
		public Property<KeyboardState> KeyboardState = new Property<KeyboardState>();
		public Property<MouseState> LastMouseState = new Property<MouseState>();
		public Property<MouseState> MouseState = new Property<MouseState>();
		public Property<GamePadState> LastGamePadState = new Property<GamePadState>();
		public Property<GamePadState> GamePadState = new Property<GamePadState>();
		public Property<bool> GamePadConnected = new Property<bool>();

		public Property<float> TimeMultiplier = new Property<float> { Value = 1.0f };
		public Property<float> BaseTimeMultiplier = new Property<float> { Value = 1.0f };
		public Property<float> PauseAudioEffect = new Property<float> { Value = 0.0f };

		public static Property<float> TotalGameTime = new Property<float>(); 

		public Strings Strings = new Strings();

		public bool IsLoadingMap = false;

		public Command<string> LoadingMap = new Command<string>();

		public Command MapLoaded = new Command();

		protected bool drawablesModified;
		protected bool earlyUpdateablesModified;
		protected bool alphaDrawablesModified;
		protected bool postAlphaDrawablesModified;
		protected bool nonPostProcessedDrawablesModified;

		private Dictionary<string, float> times;
		private string timesFile;

		private SpriteBatch spriteBatch;

#if VR
		public bool VR;
		public const float VRUnitToWorldUnit = 1.5f;
		public Ovr.Hmd VRHmd;
		private Ovr.HmdDesc vrHmdDesc;

		private RenderTarget2D vrLeftEyeTarget;
		private RenderTarget2D vrRightEyeTarget;
		public Property<Point> VRActualScreenSize = new Property<Point>();
		private Effect vrEffect;
		private Oculus.DistortionMesh vrLeftMesh = new Oculus.DistortionMesh();
		private Oculus.DistortionMesh vrRightMesh = new Oculus.DistortionMesh();
		private Ovr.FovPort vrLeftFov;
		private Ovr.FovPort vrRightFov;
		private Ovr.EyeRenderDesc vrLeftEyeRenderDesc;
		private Ovr.EyeRenderDesc vrRightEyeRenderDesc;
		private Camera vrCamera;
		public Lemma.Components.ModelNonPostProcessed VRUI;
		public Property<Matrix> VRLastViewProjection = new Property<Matrix>();
		private bool oculusNotFound;
#endif

		public bool IsChallengeMap(string map)
		{
			if (map == null)
				return false;
			else
			{
				string directory = Path.GetDirectoryName(Path.IsPathRooted(map) ? map : Path.GetFullPath(Path.Combine(this.MapDirectory, map)));
				return directory == this.CustomMapDirectory || directory == Path.GetFullPath(Path.Combine(this.MapDirectory, "Challenge")) || Path.GetDirectoryName(directory) == SteamWorker.DownloadedMaps.FullName;
			}
		}

		public string GetFullMapPath()
		{
			// Don't try to load the menu from a save game
			string filename = this.MapFile;
			if (filename == null)
				return null;

			string directory;
			if (this.CurrentSave.Value == null || filename == Main.MenuMap)
				directory = this.MapDirectory;
			else
				directory = Path.Combine(this.SaveDirectory, this.CurrentSave);

			string filenameWithExtension = filename;
			if (!filenameWithExtension.EndsWith(IO.MapLoader.MapExtension))
				filenameWithExtension += IO.MapLoader.MapExtension;

			return Path.IsPathRooted(filenameWithExtension) ? filenameWithExtension : Path.Combine(directory, filenameWithExtension);
		}

		public void FlushComponents()
		{
			for (int i = 0; i < this.componentsToAdd.Count; i++)
			{
				IComponent c = this.componentsToAdd[i];
				c.Start();
				Type t = c.GetType();
				if (typeof(IGraphicsComponent).IsAssignableFrom(t))
					this.graphicsComponents.Add((IGraphicsComponent)c);
				if (typeof(IDrawableComponent).IsAssignableFrom(t))
				{
					this.drawables.Add((IDrawableComponent)c);
					this.drawablesModified = true;
				}
				if (typeof(IEarlyUpdateableComponent).IsAssignableFrom(t))
				{
					this.earlyUpdateables.Add((IEarlyUpdateableComponent)c);
					this.earlyUpdateablesModified = true;
				}
				if (typeof(IUpdateableComponent).IsAssignableFrom(t))
					this.updateables.Add((IUpdateableComponent)c);
				if (typeof(IDrawablePreFrameComponent).IsAssignableFrom(t))
					this.preframeDrawables.Add((IDrawablePreFrameComponent)c);
				if (typeof(INonPostProcessedDrawableComponent).IsAssignableFrom(t))
				{
					this.nonPostProcessedDrawables.Add((INonPostProcessedDrawableComponent)c);
					this.nonPostProcessedDrawablesModified = true;
				}
				if (typeof(IDrawableAlphaComponent).IsAssignableFrom(t))
				{
					this.alphaDrawables.Add((IDrawableAlphaComponent)c);
					this.alphaDrawablesModified = true;
				}
				if (typeof(IDrawablePostAlphaComponent).IsAssignableFrom(t))
				{
					this.postAlphaDrawables.Add((IDrawablePostAlphaComponent)c);
					this.postAlphaDrawablesModified = true;
				}
			}
			this.componentsToAdd.Clear();

			for (int i = 0; i < this.componentsToRemove.Count; i++)
			{
				IComponent c = this.componentsToRemove[i];
				Type t = c.GetType();
				if (typeof(IGraphicsComponent).IsAssignableFrom(t))
					this.graphicsComponents.Remove((IGraphicsComponent)c);
				if (typeof(IEarlyUpdateableComponent).IsAssignableFrom(t))
					this.earlyUpdateables.Remove((IEarlyUpdateableComponent)c);
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
			ParticleSystem.Reset();
			this.LightingManager.EnvironmentMap.Value = null;
			this.MapContent = new ContentManager(this.Services);
			this.MapContent.RootDirectory = this.Content.RootDirectory;

			if (ConsoleUI.Showing)
				ConsoleUI.Showing.Value = false;

			int index = 0;
			int targetCount = deleteEditor ? 0 : 1;
			while (this.Entities.Length > targetCount)
			{
				Entity entity = this.Entities[index];
				if (deleteEditor)
					this.Remove(entity);
				else if (entity.Type == "Editor")
					index++;
				else
					this.Remove(entity);
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
			this.BaseTimeMultiplier.Value = 1.0f;
			this.PauseAudioEffect.Value = 0.0f;
			this.Camera.Angles.Value = Vector3.Zero;
			this.Camera.FieldOfView.Value = this.Settings.FieldOfView;
			this.Menu.ClearMessages();

			AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL);
			AkSoundEngine.SetState(AK.STATES.WATER.GROUP, AK.STATES.WATER.STATE.NORMAL);
			AkSoundEngine.RenderAudio();
		}

		public Command ReloadingContent = new Command();
		public Command ReloadedContent = new Command();

		public ContentManager MapContent;

		const int maxPhysicsFramerate = 75;
		
		private int monitor;
		private DisplayMode nativeDisplayMode;
#if VR
		public Main(int monitor, bool vr)
		{
			this.monitor = monitor;
			this.VR = vr;
			this.nativeDisplayMode = GraphicsAdapter.Adapters[this.monitor].CurrentDisplayMode;
#else
		public Main(int monitor)
		{
#endif
			Factory<Main>.Initialize();
			Voxel.States.Init();
			Editor.SetupDefaultEditorComponents();

#if STEAMWORKS
			if (!SteamWorker.Init())
				Log.d("Failed to initialize Steamworks.");
#if VR
			if (SteamWorker.Initialized && Steamworks.SteamUtils.IsSteamRunningInVR())
				this.VR = vr = true;
#endif
#endif

#if VR
			if (this.VR)
			{
				if (!Ovr.Hmd.Initialize(new Ovr.InitParams()))
					throw new Exception("Failed to initialize Oculus runtime.");
				this.VRHmd = new Ovr.Hmd(0);
				if (this.VRHmd == null)
				{
					Log.d("Error: no Oculus found.");
					this.VR = false;
					this.oculusNotFound = true;
				}
				else
				{
					if (!this.VRHmd.ConfigureTracking(
						(uint)Ovr.TrackingCaps.Orientation
						| (uint)Ovr.TrackingCaps.MagYawCorrection
						| (uint)Ovr.TrackingCaps.Position, 0))
						throw new Exception("Failed to configure head tracking.");
					this.vrHmdDesc = this.VRHmd.GetDesc();
					this.vrLeftFov = this.vrHmdDesc.MaxEyeFov[0];
					this.vrRightFov = this.vrHmdDesc.MaxEyeFov[1];
					Ovr.FovPort maxFov = new Ovr.FovPort();
					maxFov.UpTan = Math.Max(this.vrLeftFov.UpTan, this.vrRightFov.UpTan);
					maxFov.DownTan = Math.Max(this.vrLeftFov.DownTan, this.vrRightFov.DownTan);
					maxFov.LeftTan = Math.Max(this.vrLeftFov.LeftTan, this.vrRightFov.LeftTan);
					maxFov.RightTan = Math.Max(this.vrLeftFov.RightTan, this.vrRightFov.RightTan);
					float combinedTanHalfFovHorizontal = Math.Max(maxFov.LeftTan, maxFov.RightTan);
					float combinedTanHalfFovVertical = Math.Max(maxFov.UpTan, maxFov.DownTan);
					this.vrLeftEyeRenderDesc = this.VRHmd.GetRenderDesc(Ovr.Eye.Left, this.vrLeftFov);
					this.vrRightEyeRenderDesc = this.VRHmd.GetRenderDesc(Ovr.Eye.Right, this.vrRightFov);
				}
			}
#endif

			this.Space = new Space();
			this.Space.TimeStepSettings.TimeStepDuration = 1.0f / (float)maxPhysicsFramerate;
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
				if (!this.Settings.Fullscreen)
				{
					Rectangle bounds = this.Window.ClientBounds;
					this.ScreenSize.Value = new Point(bounds.Width, bounds.Height);
					this.resize = new Point(bounds.Width, bounds.Height);
				}
			});

			this.Graphics = new GraphicsDeviceManager(this);

			this.Content = new ContentManager(this.Services);
			this.Content.RootDirectory = "Content";

			this.Entities = new ListProperty<Entity>();

			this.Camera = new Camera();
			this.AddComponent(this.Camera);

			Lemma.Console.Console.AddConVar(new ConVar("player_speed", "Player speed.", s =>
			{
				Entity playerData = PlayerDataFactory.Instance;
				if (playerData != null)
					playerData.Get<PlayerData>().MaxSpeed.Value = (float)Lemma.Console.Console.GetConVar("player_speed").GetCastedValue();
			}, "10") { TypeConstraint = typeof(float), Validate = o => (float)o > 0 && (float)o < 200 });

			Lemma.Console.Console.AddConCommand(new ConCommand("help", "List all commands or get info about a specific command.",
			delegate(ConCommand.ArgCollection args)
			{
				string cmd = (string)args.Get("command");
				if (string.IsNullOrEmpty(cmd))
					Lemma.Console.Console.Instance.ListAllConsoleStuff();
				else
					Lemma.Console.Console.Instance.PrintConCommandDescription(cmd);
			},
			new ConCommand.CommandArgument() { Name = "command", Optional = true }));

			Lemma.Console.Console.AddConVar(new ConVar("time_scale", "Time scale (percentage).", s =>
			{
				float result;
				if (float.TryParse(s, out result))
					this.BaseTimeMultiplier.Value = result / 100.0f;
			}, "100") { TypeConstraint = typeof(int), Validate = o => (int)o > 0 && (int)o <= 400 });

			Lemma.Console.Console.AddConCommand(new ConCommand
			(
				"load", "Load a map.", collection =>
				{
					ConsoleUI.Showing.Value = false;
					this.Menu.Toggle();
					IO.MapLoader.Transition(this, collection.ParsedArgs[0].StrValue);
				},
				new ConCommand.CommandArgument { Name = "map", CommandType = typeof(string), Optional = false }
			));

			Lemma.Console.Console.AddConVar(new ConVar("blocks", "Player block cheat (white, yellow, blue)", s =>
			{
				Entity player = PlayerFactory.Instance;
				if (player != null && player.Active)
				{
					Voxel.t result = Voxel.t.Empty;
					switch (s.ToLower())
					{
						case "white":
							result = Voxel.t.WhitePermanent;
							break;
						case "yellow":
							result = Voxel.t.GlowYellow;
							break;
						case "blue":
							result = Voxel.t.GlowBlue;
							break;
						default:
							break;
					}
					BlockCloud cloud = player.Get<BlockCloud>();
					cloud.Blocks.Clear();
					cloud.Type.Value = result;
				}
			}, "none"));

			Lemma.Console.Console.AddConCommand(new ConCommand("reset_cheevos", "Reset all achievements.", delegate(ConCommand.ArgCollection args)
			{
				SteamWorker.ResetAllStats(true);
			}));

			Lemma.Console.Console.AddConCommand(new ConCommand("cheevos", "Unlock all achievements.", delegate(ConCommand.ArgCollection args)
			{
				SteamWorker.SetAchievement("cheevo_ending_a");
				SteamWorker.SetAchievement("cheevo_ending_b");
				SteamWorker.SetAchievement("cheevo_ending_c");
				SteamWorker.SetAchievement("cheevo_ending_d");
				SteamWorker.SetAchievement("cheevo_pillar_crushed");
				SteamWorker.SetAchievement("cheevo_flashback");
				SteamWorker.SetAchievement("cheevo_god_mode");
				SteamWorker.SetAchievement("cheevo_level_editor");

				SteamWorker.SetStat("stat_orbs_collected", 49);
				SteamWorker.SetStat("stat_notes_read", 36);
				SteamWorker.SetStat("stat_challenge_levels_played", 1);
			}));

			Lemma.Console.Console.AddConCommand(new ConCommand("moves", "Enable all parkour moves.", delegate(ConCommand.ArgCollection args)
			{
				if (PlayerDataFactory.Instance != null)
				{
					PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();
					playerData.EnableRoll.Value = true;
					playerData.EnableKick.Value = true;
					playerData.EnableWallRun.Value = true;
					playerData.EnableWallRunHorizontal.Value = true;
					playerData.EnableMoves.Value = true;
					playerData.EnableCrouch.Value = true;
					playerData.EnableSlowMotion.Value = true;
				}
			}));

#if DEVELOPMENT
			Lemma.Console.Console.AddConCommand(new ConCommand("diavar", "Set a dialogue variable.", delegate(ConCommand.ArgCollection args)
			{
				if (args.ParsedArgs.Length == 2)
				{
					if (PlayerDataFactory.Instance != null)
					{
						Phone phone = PlayerDataFactory.Instance.Get<Phone>();
						phone[args.ParsedArgs[0].StrValue] = args.ParsedArgs[1].StrValue;
					}
				}
			},
			new ConCommand.CommandArgument { Name = "variable", CommandType = typeof(string), Optional = false, },
			new ConCommand.CommandArgument { Name = "value", CommandType = typeof(string), Optional = false }
			));
#endif

			Lemma.Console.Console.AddConCommand(new ConCommand("specials", "Enable all special abilities.", delegate(ConCommand.ArgCollection args)
			{
				if (PlayerDataFactory.Instance != null)
				{
					PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();
					playerData.EnableEnhancedWallRun.Value = true;
				}
			}));

			new SetBinding<float>(this.PauseAudioEffect, delegate(float value)
			{
				AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.PAUSE_PARAMETER, MathHelper.Clamp(value, 0.0f, 1.0f));
			});

			new CommandBinding(this.MapLoaded, delegate()
			{
				this.mapLoaded = true;
			});

#if DEVELOPMENT
			this.EditorEnabled.Value = true;
#else
			this.EditorEnabled.Value = false;
#endif

			if (!Directory.Exists(Main.DataDirectory))
				Directory.CreateDirectory(Main.DataDirectory);
			this.settingsFile = Path.Combine(Main.DataDirectory, "settings.json");
			this.SaveDirectory = Path.Combine(Main.DataDirectory, "saves");
			if (!Directory.Exists(this.SaveDirectory))
				Directory.CreateDirectory(this.SaveDirectory);
			this.analyticsDirectory = Path.Combine(Main.DataDirectory, "analytics");
			if (!Directory.Exists(this.analyticsDirectory))
				Directory.CreateDirectory(this.analyticsDirectory);
			this.CustomMapDirectory = Path.Combine(Main.DataDirectory, "maps");
			if (!Directory.Exists(this.CustomMapDirectory))
				Directory.CreateDirectory(this.CustomMapDirectory);
			this.MapDirectory = Path.Combine(this.Content.RootDirectory, IO.MapLoader.MapDirectory);

			this.timesFile = Path.Combine(Main.DataDirectory, "times.dat");
			try
			{
				using (Stream fs = new FileStream(this.timesFile, FileMode.Open, FileAccess.Read, FileShare.None))
				using (Stream stream = new GZipInputStream(fs))
				using (StreamReader reader = new StreamReader(stream))
					this.times = JsonConvert.DeserializeObject<Dictionary<string, float>>(reader.ReadToEnd());
			}
			catch (Exception)
			{
			}

			if (this.times == null)
				this.times = new Dictionary<string, float>();

			try
			{
				// Attempt to load previous window state
				this.Settings = JsonConvert.DeserializeObject<Config>(File.ReadAllText(this.settingsFile));
				if (this.Settings.Version != Main.ConfigVersion)
					throw new Exception();
			}
			catch (Exception) // File doesn't exist, there was a deserialization error, or we are on a new version. Use default window settings
			{
				this.Settings = new Config();
			}

			if (string.IsNullOrEmpty(this.Settings.UUID))
				this.Settings.UUID = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32);
			this.Settings.MinimizeCameraMovementVR.Value |= this.Settings.MinimizeCameraMovement;
			this.MinimizeCameraMovement = this.VR ? this.Settings.MinimizeCameraMovementVR : this.Settings.MinimizeCameraMovement;
			this.Settings.GodModeProperty.Value = this.Settings.GodMode;
			this.Settings.LevelIndexProperty.Value = this.Settings.LevelIndex;
			new NotifyBinding(delegate() { this.Settings.GodMode = this.Settings.GodModeProperty; }, this.Settings.GodModeProperty);
			new NotifyBinding(delegate() { this.Settings.LevelIndex = this.Settings.LevelIndexProperty; }, this.Settings.LevelIndexProperty);
			
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
			TextElement.BindableProperties.Add("RecenterVRPose", this.Settings.RecenterVRPose);
			TextElement.BindableProperties.Add("ToggleConsole", this.Settings.ToggleConsole);

			new NotifyBinding
			(
				this.updateTimesteps,
				this.BaseTimeMultiplier, this.TimeMultiplier, this.Settings.FPSLimit
			);
			this.updateTimesteps();

			if (this.Settings.FullscreenResolution.Value.X == 0)
				this.Settings.FullscreenResolution.Value = new Point(this.nativeDisplayMode.Width, this.nativeDisplayMode.Height);

			// Have to create the menu here so it can catch the PreparingDeviceSettings event
			// We call AddComponent(this.Menu) later on in LoadContent.
			this.Menu = new Menu();
			this.Graphics.PreparingDeviceSettings += delegate(object sender, PreparingDeviceSettingsEventArgs args)
			{
				args.GraphicsDeviceInformation.Adapter = GraphicsAdapter.Adapters[this.monitor];

				args.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.Immediate;

				List<DisplayMode> supportedDisplayModes = args.GraphicsDeviceInformation.Adapter.SupportedDisplayModes.Where(x => x.Width <= 4096 && x.Height <= 4096).ToList();
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

			this.Graphics.SynchronizeWithVerticalRetrace = this.Settings.Vsync;
			new NotifyBinding(delegate()
			{
				this.Graphics.SynchronizeWithVerticalRetrace = this.Settings.Vsync;
				if (this.Settings.Fullscreen)
					this.Graphics.ApplyChanges();
			}, this.Settings.Vsync);

#if VR
			if (this.VR)
				this.ResizeViewport(this.nativeDisplayMode.Width, this.nativeDisplayMode.Height, true, false, false);
			else
#endif
			if (this.Settings.Fullscreen)
				this.ResizeViewport(this.Settings.FullscreenResolution.Value.X, this.Settings.FullscreenResolution.Value.Y, true, this.Settings.Borderless, false);
			else
				this.ResizeViewport(this.Settings.Size.Value.X, this.Settings.Size.Value.Y, false, this.Settings.Borderless, false);
		}

		private void updateTimesteps()
		{
			float value = this.TimeMultiplier * this.BaseTimeMultiplier;
			this.targetElapsedTime = TimeSpan.FromSeconds(1.0f / (float)this.Settings.FPSLimit);
			this.Space.TimeStepSettings.TimeStepDuration = (1.0f / (float)Math.Min(maxPhysicsFramerate, this.Settings.FPSLimit)) * value;
			AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SLOWMOTION, Math.Min(1.0f, (1.0f - value) / 0.6f));
		}

		private void saveTimes()
		{
			using (Stream fs = new FileStream(this.timesFile, FileMode.Create, FileAccess.Write, FileShare.None))
			using (Stream stream = new GZipOutputStream(fs))
			using (StreamWriter writer = new StreamWriter(stream))
				writer.Write(JsonConvert.SerializeObject(this.times));
		}

		public float GetMapTime(string uuid)
		{
			float existingTime;
			this.times.TryGetValue(uuid, out existingTime);
			return existingTime;
		}

		public float SaveMapTime(string uuid, float time)
		{
			float existingTime;
			if (this.times.TryGetValue(uuid, out existingTime))
			{
				if (existingTime <= 0.0f || time < existingTime)
				{
					this.times[uuid] = time;
					this.saveTimes();
					return time;
				}
				else
					return existingTime;
			}
			else
			{
				this.times[uuid] = time;
				this.saveTimes();
				return time;
			}
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
		}

#if ANALYTICS
		public Session.Recorder SessionRecorder;

		public void SaveAnalytics()
		{
			if (this.Settings.Analytics.Value == Config.RecordAnalytics.On)
				this.SessionRecorder.Save(this.analyticsDirectory, Main.Build, Path.GetFileNameWithoutExtension(this.MapFile), this.TotalTime);
		}

		public string[] AnalyticsSessionFiles
		{
			get
			{
				return Directory.GetFiles(this.analyticsDirectory, "*", SearchOption.TopDirectoryOnly);
			}
		}

		protected override void OnExiting(object sender, EventArgs args)
		{
			base.OnExiting(sender, args);
			this.SessionRecorder.RecordEvent("Exit");
			if (!this.IsChallengeMap(this.MapFile) && this.MapFile.Value != Main.MenuMap)
				this.SaveAnalytics();
		}
#endif

		public List<Session> LoadAnalytics(string map)
		{
			map = Path.GetFileNameWithoutExtension(map);
			List<Session> result = new List<Session>();
			foreach (string file in Directory.GetFiles(this.analyticsDirectory, "*", SearchOption.TopDirectoryOnly))
			{
				Session s;
				if (Debugger.IsAttached)
					s = Session.Load(file);
				else
				{
					try
					{
						s = Session.Load(file);
					}
					catch (Exception)
					{
						Log.d("Error loading analytics file " + file);
						continue;
					}
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
			this.MapContent = new ContentManager(this.Services);
			this.MapContent.RootDirectory = this.Content.RootDirectory;

			GeeUIMain.Font = this.Content.Load<SpriteFont>(this.Font);

			if (this.firstLoadContentCall)
			{
				this.firstLoadContentCall = false;

				if (!Directory.Exists(this.MapDirectory))
					Directory.CreateDirectory(this.MapDirectory);
				string challengeDirectory = Path.Combine(this.MapDirectory, "Challenge");
				if (!Directory.Exists(challengeDirectory))
					Directory.CreateDirectory(challengeDirectory);

#if VR
				if (this.VR)
				{
					this.vrLeftMesh.Load(this, Ovr.Eye.Left, this.vrLeftFov);
					this.vrRightMesh.Load(this, Ovr.Eye.Right, this.vrRightFov);
					new CommandBinding(this.ReloadedContent, (Action)this.vrLeftMesh.Reload);
					new CommandBinding(this.ReloadedContent, (Action)this.vrRightMesh.Reload);
					this.reallocateVrTargets();

					this.vrCamera = new Camera();
					this.AddComponent(this.vrCamera);
				}
#endif

				this.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
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
				this.Renderer = new Renderer(this, true, true, true, true, true);

				this.AddComponent(this.Renderer);
				this.Renderer.ReallocateBuffers(this.ScreenSize);

				this.renderParameters = new RenderParameters
				{
					Camera = this.Camera,
					IsMainRender = true
				};

				// Load strings
				this.Strings.Load(Path.Combine(this.Content.RootDirectory, "Strings.xlsx"));

				this.UI = new UIRenderer();
				this.UI.GeeUI = this.GeeUI;
				this.AddComponent(this.UI);

				PCInput input = new PCInput();
				this.AddComponent(input);

				Lemma.Console.Console.BindType(null, input);
				Lemma.Console.Console.BindType(null, UI);
				Lemma.Console.Console.BindType(null, Renderer);
				Lemma.Console.Console.BindType(null, LightingManager);

				input.Add(new CommandBinding(input.GetChord(new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.S }), delegate()
				{
					// High-resolution screenshot
					bool originalModelsVisible = Editor.EditorModelsVisible;
					Editor.EditorModelsVisible.Value = false;
					Screenshot s = new Screenshot();
					this.AddComponent(s);
					s.Take(new Point(4096, 2304), delegate()
					{
						string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
						string path;
						int i = 0;
						do
						{
							path = Path.Combine(desktop, string.Format("lemma-screen{0}.png", i));
							i++;
						}
						while (File.Exists(path));

						Screenshot.SavePng(s.Buffer, path);

						Editor.EditorModelsVisible.Value = originalModelsVisible;

						s.Delete.Execute();
					});
				}));

				this.performanceMonitor = new ListContainer();
				this.performanceMonitor.Add(new Binding<Vector2, Point>(performanceMonitor.Position, x => new Vector2(x.X, 0), this.ScreenSize));
				this.performanceMonitor.AnchorPoint.Value = new Vector2(1, 0);
				this.performanceMonitor.Visible.Value = false;
				this.performanceMonitor.Name.Value = "PerformanceMonitor";
				this.UI.Root.Children.Add(this.performanceMonitor);

				Action<string, Property<double>> addTimer = delegate(string label, Property<double> property)
				{
					TextElement text = new TextElement();
					text.FontFile.Value = this.Font;
					text.Add(new Binding<string, double>(text.Text, x => label + ": " + (x * 1000.0).ToString("F") + "ms", property));
					this.performanceMonitor.Children.Add(text);
				};

				Action<string, Property<int>> addCounter = delegate(string label, Property<int> property)
				{
					TextElement text = new TextElement();
					text.FontFile.Value = this.Font;
					text.Add(new Binding<string, int>(text.Text, x => label + ": " + x.ToString(), property));
					this.performanceMonitor.Children.Add(text);
				};

				TextElement frameRateText = new TextElement();
				frameRateText.FontFile.Value = this.Font;
				frameRateText.Add(new Binding<string, float>(frameRateText.Text, x => "FPS: " + x.ToString("0"), this.frameRate));
				this.performanceMonitor.Children.Add(frameRateText);

				addTimer("Physics", this.physicsTime);
				addTimer("Update", this.updateTime);
				addTimer("Render", this.renderTime);
				addCounter("Draw calls", this.drawCalls);
				addCounter("Triangles", this.triangles);
				addCounter("Working set", this.workingSet);

				Lemma.Console.Console.AddConCommand(new ConCommand("perf", "Toggle the performance monitor", delegate(ConCommand.ArgCollection args)
				{
					this.performanceMonitor.Visible.Value = !this.performanceMonitor.Visible;
				}));

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

				this.UI.IsMouseVisible.Value = true;

				AKRESULT akresult = AkBankLoader.LoadBank("SFX_Bank_01.bnk");
				if (akresult != AKRESULT.AK_Success)
					Log.d(string.Format("Failed to load main sound bank: {0}", akresult));

#if ANALYTICS
				this.SessionRecorder = new Session.Recorder(this);

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

				this.SessionRecorder.Add("Framerate", delegate()
				{
					return this.frameRate;
				});

				this.SessionRecorder.Add("WorkingSet", delegate()
				{
					return this.workingSet;
				});
				this.AddComponent(this.SessionRecorder);
				this.SessionRecorder.Add(new Binding<bool, Config.RecordAnalytics>(this.SessionRecorder.EnableUpload, x => x == Config.RecordAnalytics.On, this.Settings.Analytics));
#endif

				this.DefaultLighting();

				new SetBinding<float>(this.Settings.SoundEffectVolume, delegate(float value)
				{
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.VOLUME_SFX, MathHelper.Clamp(value, 0.0f, 1.0f));
				});

				new SetBinding<float>(this.Settings.MusicVolume, delegate(float value)
				{
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.VOLUME_MUSIC, MathHelper.Clamp(value, 0.0f, 1.0f));
				});

				new TwoWayBinding<LightingManager.DynamicShadowSetting>(this.Settings.DynamicShadows, this.LightingManager.DynamicShadows);
				new TwoWayBinding<float>(this.Settings.MotionBlurAmount, this.Renderer.MotionBlurAmount);
				new TwoWayBinding<float>(this.Settings.Gamma, this.Renderer.Gamma);
				new TwoWayBinding<bool>(this.Settings.Bloom, this.Renderer.EnableBloom);
				new TwoWayBinding<bool>(this.Settings.SSAO, this.Renderer.EnableSSAO);
				new Binding<float>(this.Camera.FieldOfView, this.Settings.FieldOfView);

				foreach (string file in Directory.GetFiles(this.MapDirectory, "*.xlsx", SearchOption.TopDirectoryOnly))
					this.Strings.Load(file);

				new Binding<string, Config.Lang>(this.Strings.Language, x => x.ToString(), this.Settings.Language);
				new NotifyBinding(this.SaveSettings, this.Settings.Language);

				new CommandBinding(this.MapLoaded, delegate()
				{
					this.Renderer.BlurAmount.Value = 0.0f;
					this.Renderer.Tint.Value = new Vector3(1.0f);
				});

#if VR
				if (this.VR)
				{
					Action loadVrEffect = delegate()
					{
						this.vrEffect = this.Content.Load<Effect>("Effects\\Oculus");
					};
					loadVrEffect();
					new CommandBinding(this.ReloadedContent, loadVrEffect);

					this.UI.Add(new Binding<Point>(this.UI.RenderTargetSize, this.ScreenSize));

					this.VRUI = new Lemma.Components.ModelNonPostProcessed();
					this.VRUI.MapContent = false;
					this.VRUI.DrawOrder.Value = 100000; // On top of everything
					this.VRUI.Filename.Value = "Models\\plane";
					this.VRUI.EffectFile.Value = "Effects\\VirtualUI";
					this.VRUI.Add(new Binding<Microsoft.Xna.Framework.Graphics.RenderTarget2D>(this.VRUI.GetRenderTarget2DParameter("Diffuse" + Lemma.Components.Model.SamplerPostfix), this.UI.RenderTarget));
					this.VRUI.Add(new Binding<Matrix>(this.VRUI.Transform, delegate()
					{
						Matrix rot = this.Camera.RotationMatrix;
						Matrix mat = Matrix.Identity;
						mat.Forward = rot.Right;
						mat.Right = rot.Forward;
						mat.Up = rot.Up;
						mat *= Matrix.CreateScale(7);
						mat.Translation = this.Camera.Position + rot.Forward * 4.0f;
						return mat;
					}, this.Camera.Position, this.Camera.RotationMatrix));
					this.AddComponent(this.VRUI);

					this.UI.Setup3D(this.VRUI.Transform);
				}
#endif

#if ANALYTICS
				bool editorLastEnabled = this.EditorEnabled;
				new CommandBinding<string>(this.LoadingMap, delegate(string newMap)
				{
					if (this.MapFile.Value != null && !editorLastEnabled)
					{
						this.SessionRecorder.RecordEvent("ChangedMap", newMap);
						if (!this.IsChallengeMap(this.MapFile) && this.MapFile.Value != Main.MenuMap)
							this.SaveAnalytics();
					}
					this.SessionRecorder.Reset();
					editorLastEnabled = this.EditorEnabled;
				});
#endif
				new CommandBinding<string>(this.LoadingMap, delegate(string newMap)
				{
					this.CancelScheduledSave();
				});

#if !DEVELOPMENT
				IO.MapLoader.Load(this, MenuMap);
				this.Menu.Show(initial: true);
#endif

#if ANALYTICS
				if (this.Settings.Analytics.Value == Config.RecordAnalytics.Ask)
				{
					this.Menu.ShowDialog("\\analytics prompt", "\\enable analytics", delegate()
					{
						this.Settings.Analytics.Value = Config.RecordAnalytics.On;
					},
					"\\disable analytics", delegate()
					{
						this.Settings.Analytics.Value = Config.RecordAnalytics.Off;
					});
				}
#endif

#if VR
				if (this.oculusNotFound)
					this.Menu.HideMessage(null, this.Menu.ShowMessage(null, "Error: no Oculus found."), 6.0f);

				if (this.VR)
				{
					this.Menu.EnableInput(false);
					Container vrMsg = this.Menu.BuildMessage("\\vr message", 300.0f);
					vrMsg.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
					vrMsg.Add(new Binding<Vector2, Point>(vrMsg.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.ScreenSize));
					this.UI.Root.Children.Add(vrMsg);
					input.Bind(this.Settings.RecenterVRPose, PCInput.InputState.Down, this.VRHmd.RecenterPose);
					input.Bind(this.Settings.RecenterVRPose, PCInput.InputState.Down, delegate()
					{
						if (vrMsg != null)
						{
							vrMsg.Delete.Execute();
							vrMsg = null;
						}
						this.Menu.EnableInput(true);
					});
				}
				else
#endif
				{
					input.Bind(this.Settings.ToggleFullscreen, PCInput.InputState.Down, delegate()
					{
						if (this.Settings.Fullscreen) // Already fullscreen. Go to windowed mode.
							this.ExitFullscreen();
						else // In windowed mode. Go to fullscreen.
							this.EnterFullscreen();
					});
				}

				input.Bind(this.Settings.QuickSave, PCInput.InputState.Down, delegate()
				{
					this.SaveWithNotification(true);
				});
			}
			else
			{
				this.ReloadingContent.Execute();
				foreach (IGraphicsComponent c in this.graphicsComponents)
					c.LoadContent(true);
				this.ReloadedContent.Execute();
			}

			this.GraphicsDevice.RasterizerState = new RasterizerState { MultiSampleAntiAlias = false };

			if (this.spriteBatch != null)
				this.spriteBatch.Dispose();
			this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
		}

		public void DefaultLighting()
		{
			this.Renderer.LightRampTexture.Value = "LightRamps\\default";
			this.LightingManager.EnvironmentMap.Value = "EnvironmentMaps\\env0";
			this.LightingManager.BackgroundColor.Value = new Color(32, 52, 76);
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
				scriptEntity.Get<Script>().ExecuteOnLoad.Value = false;
				Script script = scriptEntity.Get<Script>();
				script.Name.Value = scriptName;
				script.Execute.Execute();
			}
		}

		protected void executeStaticScript(string scriptName)
		{
			Entity scriptEntity = Factory.Get<ScriptFactory>().CreateAndBind(this);
			scriptEntity.Serialize = false;
			this.Add(scriptEntity);
			scriptEntity.Get<Script>().ExecuteOnLoad.Value = false;
			Script script = scriptEntity.Get<Script>();
			script.Name.Value = scriptName;
			script.Execute.Execute();
			scriptEntity.Delete.Execute();
		}

		private void createNewSave(string oldSave = null)
		{
			if (oldSave == null)
				oldSave = this.CurrentSave;
			string newSave = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
			if (newSave != oldSave)
			{
				Directory.CreateDirectory(Path.Combine(this.SaveDirectory, newSave));
				if (oldSave != null)
					this.copySave(Path.Combine(this.SaveDirectory, oldSave), Path.Combine(this.SaveDirectory, newSave));
				this.CurrentSave.Value = newSave;
			}
		}

		public void SaveNew()
		{
			this.save(null); // null means don't delete any old saves
		}

		private bool saving;
		public void SaveWithNotification(bool overwrite)
		{
			if (!this.saving && !this.Paused && this.Menu.CanPause && this.MapFile != Main.MenuMap && !this.IsChallengeMap(this.MapFile) && PlayerFactory.Instance != null)
			{
				this.saving = true;
				Container saveNotification = new Container();
				saveNotification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				saveNotification.Opacity.Value = UIFactory.Opacity;
				TextElement saveNotificationText = new TextElement();
				saveNotificationText.Name.Value = "Text";
				saveNotificationText.FontFile.Value = this.Font;
				saveNotificationText.Text.Value = "\\saving";
				saveNotification.Children.Add(saveNotificationText);
				this.UI.Root.GetChildByName("Notifications").Children.Add(saveNotification);
				this.AddComponent(new Animation
				(
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						if (overwrite)
							this.SaveOverwrite();
						else
							this.SaveNew();
					}),
					new Animation.Delay(0.01f),
					new Animation.Set<string>(saveNotificationText.Text, "\\saved"),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(saveNotification.Opacity, 0.0f, 1.0f),
						new Animation.FloatMoveTo(saveNotificationText.Opacity, 0.0f, 1.0f)
					),
					new Animation.Execute(saveNotification.Delete),
					new Animation.Execute(delegate()
					{
						this.saving = false;
					})
				));
			}
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
			{
				Point size;
#if VR
				if (this.VR)
					size = this.VRActualScreenSize;
				else
#endif
					size = this.ScreenSize;
				this.Screenshot.Take(size, doSave);
			}
			else
				doSave();
		}

		public void ScheduleSave()
		{
			if (this.MapFile.Value != Main.MenuMap && (this.scheduledSave == null || !this.scheduledSave.Active))
			{
				bool originalCanPause = this.Menu.CanPause;
				this.Menu.CanPause.Value = false;
				Container saveNotification = new Container();
				TextElement saveNotificationText = new TextElement();
				this.scheduledSave = new Animation
				(
					new Animation.Delay(0.6f),
					new Animation.Execute(delegate()
					{
						Point size;
#if VR
						if (this.VR)
							size = this.VRActualScreenSize;
						else
#endif
							size = this.ScreenSize;
						this.Screenshot.Take(size);
					}),
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						saveNotification.Tint.Value = Microsoft.Xna.Framework.Color.Black;
						saveNotification.Opacity.Value = UIFactory.Opacity;
						saveNotificationText.Name.Value = "Text";
						saveNotificationText.FontFile.Value = this.Font;
						saveNotificationText.Text.Value = "\\saving";
						saveNotification.Children.Add(saveNotificationText);
						this.UI.Root.GetChildByName("Notifications").Children.Add(saveNotification);
					}),
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						this.SaveOverwrite();
					}),
					new Animation.Delay(0.01f),
					new Animation.Set<string>(saveNotificationText.Text, "\\saved"),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(saveNotification.Opacity, 0.0f, 1.0f),
						new Animation.FloatMoveTo(saveNotificationText.Opacity, 0.0f, 1.0f)
					),
					new Animation.Execute(saveNotification.Delete)
				);
				this.scheduledSave.Add(new CommandBinding(this.scheduledSave.Delete, delegate()
				{
					this.Screenshot.Clear();
					this.UI.Root.GetChildByName("Notifications").Children.Clear();
					this.Menu.CanPause.Value = originalCanPause;
					this.scheduledSave = null;
				}));
				WorldFactory.Instance.Add(this.scheduledSave);
			}
		}

		public void CancelScheduledSave()
		{
			if (this.scheduledSave != null && this.scheduledSave.Active)
				this.scheduledSave.Delete.Execute();
		}

		public void SaveCurrentMap(RenderTarget2D screenshot = null, Point screenshotSize = default(Point))
		{
			if (this.CurrentSave.Value == null)
				this.createNewSave();

			string currentSaveDirectory = Path.Combine(this.SaveDirectory, this.CurrentSave);
			if (screenshot != null)
			{
				string screenshotPath = Path.Combine(currentSaveDirectory, "thumbnail.png");
				Screenshot.SavePng(screenshot, screenshotPath, 256, (int)(screenshotSize.Y * (256.0f / screenshotSize.X)));
			}

			IO.MapLoader.Save(this, currentSaveDirectory, this.MapFile);

			try
			{
				using (Stream fs = new FileStream(Path.Combine(currentSaveDirectory, "save.dat"), FileMode.Create, FileAccess.Write, FileShare.None))
				using (Stream stream = new GZipOutputStream(fs))
				using (StreamWriter writer = new StreamWriter(stream))
					Main.SaveInfo.Serializer.Serialize(writer, new Main.SaveInfo { MapFile = Path.GetFileNameWithoutExtension(this.MapFile), Version = Main.MapVersion, PlayerData = PlayerDataFactory.Instance });
			}
			catch (InvalidOperationException e)
			{
				throw new Exception("Failed to save game.", e);
			}
		}

		public void SaveSettings()
		{
			File.WriteAllText(this.settingsFile, JsonConvert.SerializeObject(this.Settings, Formatting.Indented));
		}

		public void EnterFullscreen()
		{
			if (!this.Settings.Fullscreen)
			{
				Point res = this.Settings.FullscreenResolution;
				this.ResizeViewport(res.X, res.Y, true, this.Settings.Borderless);
			}
		}

		public void ExitFullscreen()
		{
			if (this.Settings.Fullscreen && !this.VR)
			{
				Point res = this.Settings.Size;
				this.ResizeViewport(res.X, res.Y, false, this.Settings.Borderless);
			}
		}

		public void DrawablesModified()
		{
			this.drawablesModified = true;
		}

		public void EarlyUpdateablesModified()
		{
			this.earlyUpdateablesModified = true;
		}

		public void AlphaDrawablesModified()
		{
			this.alphaDrawablesModified = true;
		}

		public void PostAlphaDrawablesModified()
		{
			this.postAlphaDrawablesModified = true;
		}

		public void NonPostProcessedDrawablesModified()
		{
			this.nonPostProcessedDrawablesModified = true;
		}

		private Stopwatch timer = new Stopwatch();
		private Stopwatch frameTimer = new Stopwatch();

		private TimeSpan lastFrameTime;

		public bool LastActive { get {  return this.lastLastActive; } }
		private bool lastLastActive;
		private bool lastActive;

		protected override void Update(GameTime gameTime)
		{
			this.lastLastActive = this.lastActive;
			if (this.IsActive && !this.lastActive)
				AkSoundEngine.PostEvent(AK.EVENTS.RESUME_ALL);
			else if (!this.IsActive && this.lastActive)
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PAUSE_ALL);
				AkSoundEngine.RenderAudio();
			}
			this.lastActive = this.IsActive;

			if (!this.lastActive)
				return;

			if (this.targetElapsedTime > this.lastFrameTime)
			{
				TimeSpan diff = this.targetElapsedTime - this.lastFrameTime;
				Thread.Sleep(diff);
			}

			this.frameTimer.Restart();

			if (gameTime.ElapsedGameTime.TotalSeconds > 0.1f)
				gameTime = new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(0.1f), false);
			this.GameTime = gameTime;
			float dt = this.ElapsedTime.Value = (float)gameTime.ElapsedGameTime.TotalSeconds * this.TimeMultiplier * this.BaseTimeMultiplier;
			if (!this.Paused)
				this.TotalTime.Value += dt;

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

			this.timer.Restart();
			for (int i = 0; i < this.earlyUpdateables.Count; i++)
			{
				IEarlyUpdateableComponent c = this.earlyUpdateables[i];
				if (this.componentEnabled(c))
					c.Update(dt);
			}

			for (int i = 0; i < this.updateables.Count; i++)
			{
				IUpdateableComponent c = this.updateables[i];
				if (this.componentEnabled(c))
					c.Update(dt);
			}
			this.FlushComponents();

			if (this.earlyUpdateablesModified)
			{
				this.earlyUpdateables.InsertionSort(delegate(IEarlyUpdateableComponent a, IEarlyUpdateableComponent b)
				{
					return a.UpdateOrder.Value.CompareTo(b.UpdateOrder.Value);
				});
				this.earlyUpdateablesModified = false;
			}

			if (this.drawablesModified)
			{
				this.drawables.InsertionSort(delegate(IDrawableComponent a, IDrawableComponent b)
				{
					return a.OrderKey.Value.CompareTo(b.OrderKey.Value);
				});
				this.drawablesModified = false;
			}

			if (this.alphaDrawablesModified)
			{
				this.alphaDrawables.InsertionSort(delegate(IDrawableAlphaComponent a, IDrawableAlphaComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
				this.alphaDrawablesModified = false;
			}

			if (this.postAlphaDrawablesModified)
			{
				this.postAlphaDrawables.InsertionSort(delegate(IDrawablePostAlphaComponent a, IDrawablePostAlphaComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
				this.postAlphaDrawablesModified = false;
			}

			if (this.nonPostProcessedDrawablesModified)
			{
				this.nonPostProcessedDrawables.InsertionSort(delegate(INonPostProcessedDrawableComponent a, INonPostProcessedDrawableComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
				this.nonPostProcessedDrawablesModified = false;
			}

			this.timer.Stop();
			this.updateSum = Math.Max(this.updateSum, this.timer.Elapsed.TotalSeconds);

			this.timer.Restart();
			if (!this.Paused && !this.EditorEnabled && this.Space.TimeStepSettings.TimeStepDuration > 0.0f)
				this.Space.Update(dt);
			this.timer.Stop();
			this.physicsSum = Math.Max(this.physicsSum, this.timer.Elapsed.TotalSeconds);

			this.frameSum++;
			this.performanceInterval += (float)this.GameTime.ElapsedGameTime.TotalSeconds;
			if (this.performanceInterval > Main.performanceUpdateTime)
			{
				this.frameRate.Value = this.frameSum / this.performanceInterval;
				if (this.performanceMonitor.Visible)
				{
					this.physicsTime.Value = this.physicsSum;
					this.updateTime.Value = this.updateSum;
					this.drawCalls.Value = this.drawCallCounter;
					this.triangles.Value = this.triangleCounter;
					this.renderTime.Value = this.renderSum;
					this.drawCallCounter = 0;
					this.triangleCounter = 0;
				}
				this.renderSum = 0;
				this.physicsSum = 0;
				this.updateSum = 0;
				this.frameSum = 0;
				this.performanceInterval -= Main.performanceUpdateTime;
				this.workingSet.Value = (int)(Environment.WorkingSet / (long)1048576);
			}

			AkSoundEngine.RenderAudio();

			TotalGameTime.Value += dt;
#if STEAMWORKS
			SteamWorker.Update(dt);
#endif

#if ANALYTICS
			this.SessionRecorder.Update(dt);
#endif

			if (this.resize != null && this.resize.Value.X > 0 && this.resize.Value.Y > 0)
			{
				this.ResizeViewport(this.resize.Value.X, this.resize.Value.Y, false, false);
				this.resize = null;
			}
		}

#if VR
		private void drawEye(Camera originalCamera, GameTime gameTime, Ovr.Posef pose, RenderTarget2D renderTarget, bool updateLights)
		{
			Quaternion quat = new Quaternion(pose.Orientation.x, pose.Orientation.y, pose.Orientation.z, pose.Orientation.w);
			this.vrCamera.RotationMatrix.Value = Matrix.CreateFromQuaternion(quat) * originalCamera.RotationMatrix;
			Vector3 viewAdjust = Vector3.TransformNormal(new Vector3(pose.Position.x, pose.Position.y, pose.Position.z), originalCamera.RotationMatrix);
			this.vrCamera.Position.Value = originalCamera.Position.Value + viewAdjust * Main.VRUnitToWorldUnit;
			Ovr.Matrix4f proj = Ovr.Hmd.GetProjection(this.vrLeftFov, originalCamera.NearPlaneDistance, originalCamera.FarPlaneDistance, (uint)Ovr.Hmd.ProjectionModifier.RightHanded);
			this.vrCamera.Projection.Value = Oculus.MatrixOvrToXna(proj);

			for (int i = 0; i < this.preframeDrawables.Count; i++)
			{
				IDrawablePreFrameComponent c = this.preframeDrawables[i];
				if (this.componentEnabled(c))
					c.DrawPreFrame(gameTime, this.renderParameters);
			}

			this.Renderer.SetRenderTargets(this.renderParameters);

			this.DrawScene(this.renderParameters);

			if (updateLights)
			{
				this.LightingManager.UpdateGlobalLights();
				this.LightingManager.RenderShadowMaps(originalCamera);
			}

			this.Renderer.PostProcess(renderTarget, this.renderParameters);

			for (int i = 0; i < this.nonPostProcessedDrawables.Count; i++)
			{
				INonPostProcessedDrawableComponent c = this.nonPostProcessedDrawables[i];
				if (this.componentEnabled(c))
					c.DrawNonPostProcessed(gameTime, this.renderParameters);
			}
		}
#endif

		protected override void Draw(GameTime gameTime)
		{
			if (this.GraphicsDevice == null || this.GraphicsDevice.IsDisposed || this.GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
				return;

			Lemma.Components.Model.DrawCallCounter = 0;
			Lemma.Components.Model.TriangleCounter = 0;

			this.renderParameters.Technique = Technique.Render;

			this.timer.Restart();

#if VR
			Ovr.FrameTiming frameTiming = new Ovr.FrameTiming();
			if (this.VR)
			{
				frameTiming = this.VRHmd.BeginFrameTiming(0);

				Camera originalCamera = this.renderParameters.Camera;
				this.vrCamera.SetFromCamera(originalCamera);
				this.vrCamera.ProjectionType.Value = Camera.ProjectionMode.Custom;
				this.renderParameters.Camera = this.vrCamera;

				Ovr.Posef[] eyes = this.VRHmd.GetEyePoses(0);

				// Position limitation
				{
					Vector3 left = new Vector3(eyes[0].Position.x, eyes[0].Position.y, eyes[0].Position.z);
					Vector3 right = new Vector3(eyes[1].Position.x, eyes[1].Position.y, eyes[1].Position.z);
					Vector3 target = new Vector3(0, 0.0f / VRUnitToWorldUnit, -0.25f / VRUnitToWorldUnit);
					Vector3 targetToEyes = ((left + right) * 0.5f) - target;
					float distance = targetToEyes.Length();
					const float limit = 0.5f / VRUnitToWorldUnit;
					if (distance > limit)
					{
						Vector3 correction = targetToEyes * ((limit / distance) - 1.0f);
						left += correction;
						right += correction;
						eyes[0].Position.x = left.X;
						eyes[0].Position.y = left.Y;
						eyes[0].Position.z = left.Z;
						eyes[1].Position.x = right.X;
						eyes[1].Position.y = right.Y;
						eyes[1].Position.z = right.Z;
					}
				}

				if (this.vrHmdDesc.EyeRenderOrder[0] == Ovr.Eye.Left)
				{
					this.drawEye(originalCamera, gameTime, eyes[0], this.vrLeftEyeTarget, true);
					this.drawEye(originalCamera, gameTime, eyes[1], this.vrRightEyeTarget, false);
				}
				else
				{
					this.drawEye(originalCamera, gameTime, eyes[1], this.vrRightEyeTarget, true);
					this.drawEye(originalCamera, gameTime, eyes[0], this.vrLeftEyeTarget, false);
				}

				this.GraphicsDevice.SetRenderTarget(this.RenderTarget);
				this.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

				/*
				this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
				this.spriteBatch.Draw(this.vrLeftEyeTarget, Vector2.Zero, Color.White);
				this.spriteBatch.Draw(this.vrRightEyeTarget, new Vector2(this.vrLeftEyeTarget.Width, 0), Color.White);
				this.spriteBatch.End();
				*/
				Ovr.Hmd.WaitTillTime(frameTiming.TimewarpPointSeconds);

				this.vrLeftMesh.Render(this.vrLeftEyeTarget, eyes[0], this.vrEffect);
				this.vrRightMesh.Render(this.vrRightEyeTarget, eyes[1], this.vrEffect);

				// Update view projection matrix
				this.VRLastViewProjection.Value = this.vrCamera.ViewProjection;

				this.renderParameters.Camera = originalCamera;
			}
			else
#endif
			{
				for (int i = 0; i < this.preframeDrawables.Count; i++)
				{
					IDrawablePreFrameComponent c = this.preframeDrawables[i];
					if (this.componentEnabled(c))
						c.DrawPreFrame(gameTime, this.renderParameters);
				}

				this.Renderer.SetRenderTargets(this.renderParameters);

				this.DrawScene(this.renderParameters);

				this.LightingManager.UpdateGlobalLights();
				this.LightingManager.RenderShadowMaps(this.Camera);

				this.Renderer.PostProcess(this.RenderTarget, this.renderParameters);

				for (int i = 0; i < this.nonPostProcessedDrawables.Count; i++)
				{
					INonPostProcessedDrawableComponent c = this.nonPostProcessedDrawables[i];
					if (this.componentEnabled(c))
						c.DrawNonPostProcessed(gameTime, this.renderParameters);
				}
			}

			this.drawCallCounter = Math.Max(this.drawCallCounter, Lemma.Components.Model.DrawCallCounter);
			this.triangleCounter = Math.Max(this.triangleCounter, Lemma.Components.Model.TriangleCounter);

			if (this.RenderTarget != null)
			{
				// We just rendered to a target other than the screen.
				// So make it so we're rendering to the screen again, then copy the render target to the screen.
				this.GraphicsDevice.SetRenderTarget(null);

				if (!this.RenderTarget.IsDisposed)
				{
					this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
					this.spriteBatch.Draw(this.RenderTarget, Vector2.Zero, Color.White);
					this.spriteBatch.End();
				}
				this.RenderTarget = null;
			}
		}

		protected override void EndDraw()
		{
			base.EndDraw();
#if VR
			if (this.VR)
				this.VRHmd.EndFrameTiming();
#endif

			this.timer.Stop();
			this.frameTimer.Stop();
			this.renderSum = Math.Max(this.renderSum, this.timer.Elapsed.TotalSeconds);
			this.lastFrameTime = this.frameTimer.Elapsed;
		}

		public void DrawScene(RenderParameters parameters)
		{
			if (parameters.Technique != Technique.Shadow)
				this.LightingManager.ClearMaterials();

			RasterizerState originalState = this.GraphicsDevice.RasterizerState;
			RasterizerState reverseCullState = null;

			if (parameters.ReverseCullOrder)
			{
				reverseCullState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };
				this.GraphicsDevice.RasterizerState = reverseCullState;
			}

			Vector3 cameraPos = parameters.Camera.Position;
			BoundingFrustum frustum = parameters.Camera.BoundingFrustum;

			for (int i = 0; i < this.drawables.Count; i++)
			{
				IDrawableComponent c = this.drawables[i];
				if (this.componentEnabled(c) && c.IsVisible(frustum))
					c.Draw(this.GameTime, parameters);
			}

			if (reverseCullState != null)
				this.GraphicsDevice.RasterizerState = originalState;

			if (parameters.Technique != Technique.Shadow)
				this.LightingManager.SetMaterials(parameters);
		}

		public void DrawAlphaComponents(RenderParameters parameters)
		{
			for (int i = 0; i < this.alphaDrawables.Count; i++)
			{
				IDrawableAlphaComponent c = this.alphaDrawables[i];
				if (this.componentEnabled(c))
					c.DrawAlpha(this.GameTime, parameters);
			}
		}

		public void DrawPostAlphaComponents(RenderParameters parameters)
		{
			for (int i = 0; i < this.postAlphaDrawables.Count; i++)
			{
				IDrawablePostAlphaComponent c = this.postAlphaDrawables[i];
				if (this.componentEnabled(c))
					c.DrawPostAlpha(this.GameTime, parameters);
			}
		}

#if VR
		private void reallocateVrTargets()
		{
			if (this.vrLeftEyeTarget != null)
			{
				this.vrLeftEyeTarget.Dispose();
				this.vrRightEyeTarget.Dispose();
			}

			if (this.VRHmd != null)
			{
				Ovr.Sizei size = this.VRHmd.GetFovTextureSize(Ovr.Eye.Left, this.vrLeftFov, 1.0f);
				Point renderTargetSize = new Point(size.w, size.h);

				this.ScreenSize.Value = renderTargetSize;

				if (this.GraphicsDevice != null)
				{
					this.vrLeftEyeTarget = new RenderTarget2D(this.GraphicsDevice, renderTargetSize.X, renderTargetSize.Y, false, SurfaceFormat.Color, DepthFormat.None);
					this.vrRightEyeTarget = new RenderTarget2D(this.GraphicsDevice, renderTargetSize.X, renderTargetSize.Y, false, SurfaceFormat.Color, DepthFormat.None);

					PresentationParameters presentation = this.GraphicsDevice.PresentationParameters;
					this.VRActualScreenSize.Value = new Point(presentation.BackBufferWidth, presentation.BackBufferHeight);
				}
				else
					this.VRActualScreenSize.Value = new Point(this.Graphics.PreferredBackBufferWidth, this.Graphics.PreferredBackBufferHeight);
			}
		}
#endif

		public void ResizeViewport(int width, int height, bool fullscreen, bool borderless, bool applyChanges = true)
		{
			bool currentlyBorderless = !this.Graphics.IsFullScreen;

#if VR
			if (this.VR)
			{
				currentlyBorderless = false;
				borderless = false;
				fullscreen = true;
			}
#endif

			bool needApply = false;
			if (this.Settings.Fullscreen != fullscreen)
				needApply = true;

			if (fullscreen && (width != this.nativeDisplayMode.Width || height != this.nativeDisplayMode.Height))
				borderless = this.Settings.Borderless.Value = false;

			if (fullscreen && currentlyBorderless != borderless)
				needApply = true;
			this.Graphics.IsFullScreen = fullscreen && !borderless;
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

			this.Settings.Fullscreen.Value = fullscreen;
#if VR
			if (!this.VR)
#endif
				this.Settings.Borderless.Value = borderless;

			if (applyChanges && needApply)
				this.Graphics.ApplyChanges();

#if VR
			if (this.VR)
				this.reallocateVrTargets();
			else
#endif
			{
				if (this.GraphicsDevice != null)
				{
					PresentationParameters presentation = this.GraphicsDevice.PresentationParameters;
					width = presentation.BackBufferWidth;
					height = presentation.BackBufferHeight;
				}
				this.ScreenSize.Value = new Point(width, height);
				if (fullscreen)
					this.Settings.FullscreenResolution.Value = new Point(width, height);
				else
					this.Settings.Size.Value = new Point(width, height);
			}

			if (this.GeeUI != null)
			{
				this.GeeUI.RootView.Width.Value = width;
				this.GeeUI.RootView.Height.Value = height;
			}

			if (this.Renderer != null)
				this.Renderer.ReallocateBuffers(this.ScreenSize);

			if (!fullscreen || borderless)
			{
				System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(this.Window.Handle);
				System.Windows.Forms.Form form = control.FindForm();
				form.FormBorderStyle = fullscreen || borderless ? System.Windows.Forms.FormBorderStyle.None : System.Windows.Forms.FormBorderStyle.Sizable;
				if (fullscreen && borderless)
					form.Location = new System.Drawing.Point(0, 0);
				this.resize = null;
			}

#if ANALYTICS
			if (this.SessionRecorder != null)
				this.SessionRecorder.RecordEvent("ResizedViewport", string.Format("{0}x{1} {2}", width, height, fullscreen ? "fullscreen" : "windowed"));
#endif
			if (applyChanges && needApply && fullscreen && this.Renderer != null)
				this.LoadContent(); // Reload everything

			this.SaveSettings();
		}
	}
}