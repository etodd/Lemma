#region Using Statements
using System; using ComponentBind;
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
using System.Threading;
#endregion

namespace Lemma
{
	public class GameMain : Main
	{
		public const string InitialMap = "start";

		public const string MenuMap = "..\\Menu\\menu";

		public class ExitException : Exception
		{
		}

		public const int ConfigVersion = 6;
		public const int MapVersion = 353;
		public const int Build = 353;

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
			public Property<PCInput.PCInputBinding> RollKick = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.LeftMouseButton, GamePadButton = Buttons.RightStick } };
			public Property<PCInput.PCInputBinding> TogglePhone = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Tab, GamePadButton = Buttons.Y } };
			public Property<PCInput.PCInputBinding> QuickSave = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F5, GamePadButton = Buttons.Back } };
			public Property<PCInput.PCInputBinding> ToggleFullscreen = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.F11 } };
			public Property<PCInput.PCInputBinding> ToggleConsole = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.OemTilde } };
		}

		public class SaveInfo
		{
			public string MapFile;
			public int Version;
		}

		public bool CanSpawn = true;

		public Config Settings;
		private string dataDirectory;
		public string SaveDirectory;
		private string analyticsDirectory;
		private string settingsFile;

		public Property<Entity> Player = new Property<Entity>();
		private Entity editor;

		private bool loadingSavedGame;

		public Property<string> StartSpawnPoint = new Property<string>();

		public Command<Entity> PlayerSpawned = new Command<Entity>();

		private float respawnTimer = -1.0f;

		public Screenshot Screenshot;

		private const float startGamma = 10.0f;
		private static Vector3 startTint = new Vector3(2.0f);

		public const int RespawnMemoryLength = 200;
		public const float DefaultRespawnDistance = 0.0f;
		public const float DefaultRespawnInterval = 0.5f;
		public const float KilledRespawnDistance = 40.0f;
		public const float KilledRespawnInterval = 3.0f;

		public float RespawnDistance = DefaultRespawnDistance;
		public float RespawnInterval = DefaultRespawnInterval;

		private Vector3 lastPlayerPosition;

		public Menu Menu;

		public GameMain()
			: base()
		{
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
				if (this.Settings.Version != GameMain.ConfigVersion)
					throw new Exception();
			}
			catch (Exception) // File doesn't exist, there was a deserialization error, or we are on a new version. Use default window settings
			{
				this.Settings = new Config { Version = GameMain.ConfigVersion, };
			}

			if (this.Settings.UUID == null)
				Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32);
			
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

		public override void ClearEntities(bool deleteEditor)
		{
			base.ClearEntities(deleteEditor);
			this.Menu.ClearMessages();
			// TODO: XACT -> Wwise
			/*
			this.AudioEngine.GetCategory("Music").Stop(AudioStopOptions.Immediate);
			this.AudioEngine.GetCategory("Default").Stop(AudioStopOptions.Immediate);
			this.AudioEngine.GetCategory("Ambient").Stop(AudioStopOptions.Immediate);
			*/
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
			string filename = GameMain.Build.ToString() + "-" + (string.IsNullOrEmpty(map) ? "null" : Path.GetFileName(map)) + "-" + Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 32) + ".xml";
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

		public Property<string> CurrentSave = new Property<string>();

		protected override void LoadContent()
		{
			bool firstInitialization = this.firstLoadContentCall;
			base.LoadContent();

			if (firstInitialization)
			{
				this.AddComponent(this.Menu); // Have to do this here so the menu's Awake can use all our loaded stuff

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
					if (value == null || value.Length == 0)
					{
						this.MapFile.InternalValue = null;
						return;
					}

					try
					{
						string directory = this.CurrentSave.Value == null ? null : Path.Combine(this.SaveDirectory, this.CurrentSave);
						if (value == GameMain.MenuMap)
							directory = null; // Don't try to load the menu from a save game
						IO.MapLoader.Load(this, directory, value, false);
						this.loadingSavedGame = this.CurrentSave.Value != null;
					}
					catch (FileNotFoundException)
					{
						this.MapFile.InternalValue = value;
						// Create a new map
						Entity world = Factory.Get<WorldFactory>().CreateAndBind(this);
						world.Get<Transform>().Position.Value = new Vector3(0, 3, 0);
						this.Add(world);

						Entity ambientLight = Factory.Get<AmbientLightFactory>().CreateAndBind(this);
						ambientLight.Get<Transform>().Position.Value = new Vector3(0, 5.0f, 0);
						ambientLight.Get<AmbientLight>().Color.Value = new Vector3(0.25f, 0.25f, 0.25f);
						this.Add(ambientLight);

						Entity map = Factory.Get<MapFactory>().CreateAndBind(this);
						map.Get<Transform>().Position.Value = new Vector3(0, 1, 0);
						this.Add(map);

						this.MapLoaded.Execute();
					}
					this.respawnTimer = 0;
				};

				this.Renderer.LightRampTexture.Value = "Images\\default-ramp";
				this.Renderer.EnvironmentMap.Value = "Images\\env0";

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
					editorMsg.Text.Value = "\\editor menu";
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
					// Main menu

					this.MapFile.Value = GameMain.MenuMap;
					this.Menu.Pause();
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
				this.Screenshot.Take(doSave);
			else
				doSave();
		}

		public void SaveCurrentMap(RenderTarget2D screenshot, Point screenshotSize)
		{
			if (this.CurrentSave.Value == null)
				this.createNewSave();

			string currentSaveDirectory = Path.Combine(this.SaveDirectory, this.CurrentSave);
			string screenshotPath = Path.Combine(currentSaveDirectory, "thumbnail.jpg");
			using (Stream stream = File.OpenWrite(screenshotPath))
				screenshot.SaveAsJpeg(stream, 256, (int)(screenshotSize.Y * (256.0f / screenshotSize.X)));
			this.Screenshot.Clear();

			IO.MapLoader.Save(this, currentSaveDirectory, this.MapFile);

			try
			{
				using (Stream stream = new FileStream(Path.Combine(currentSaveDirectory, "save.xml"), FileMode.Create, FileAccess.Write, FileShare.None))
					new XmlSerializer(typeof(GameMain.SaveInfo)).Serialize(stream, new GameMain.SaveInfo { MapFile = this.MapFile, Version = GameMain.MapVersion });
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

		private bool mapJustLoaded = false;

		private Vector3 lastEditorPosition;
		private Vector2 lastEditorMouse;
		private string lastEditorSpawnPoint;

		protected override void update()
		{
			if (this.mapJustLoaded)
			{
				// If we JUST loaded a map, wait one frame for any scripts to execute before we spawn a player
				this.mapJustLoaded = false;
				return;
			}

			// Spawn an editor or a player if needed
			if (this.EditorEnabled)
			{
				this.Player.Value = null;
				this.Renderer.InternalGamma.Value = 0.0f;
				this.Renderer.Brightness.Value = 0.0f;
				if (this.editor == null || !this.editor.Active)
				{
					this.editor = Factory.Get<EditorFactory>().CreateAndBind(this);
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

				bool setupSpawn = this.Player.Value == null || !this.Player.Value.Active;

				if (setupSpawn)
					this.Player.Value = PlayerFactory.Instance;

				bool createPlayer = this.Player.Value == null || !this.Player.Value.Active;

				if (createPlayer || setupSpawn)
				{
					if (this.loadingSavedGame)
					{
						this.Renderer.InternalGamma.Value = 0.0f;
						this.Renderer.Brightness.Value = 0.0f;
						this.PlayerSpawned.Execute(this.Player);
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
							if (createPlayer)
							{
								this.Player.Value = Factory.Get<PlayerFactory>().CreateAndBind(this);
								this.Add(this.Player);
							}

							bool spawnFound = false;

							RespawnLocation foundSpawnLocation = default(RespawnLocation);
							Vector3 foundSpawnAbsolutePosition = Vector3.Zero;

							if (string.IsNullOrEmpty(this.StartSpawnPoint.Value))
							{
								// Look for an autosaved spawn point
								Entity playerData = Factory.Get<PlayerDataFactory>().Instance;
								if (playerData != null)
								{
									ListProperty<RespawnLocation> respawnLocations = playerData.GetOrMakeListProperty<RespawnLocation>("RespawnLocations");
									int supportedLocations = 0;
									while (respawnLocations.Count > 0)
									{
										RespawnLocation respawnLocation = respawnLocations[respawnLocations.Count - 1];
										Entity respawnMapEntity = respawnLocation.Map.Target;
										if (respawnMapEntity != null && respawnMapEntity.Active)
										{
											Map respawnMap = respawnMapEntity.Get<Map>();
											Vector3 absolutePos = respawnMap.GetAbsolutePosition(respawnLocation.Coordinate);
											if (respawnMap.Active
												&& respawnMap[respawnLocation.Coordinate].ID != 0
												&& respawnMap.GetAbsoluteVector(respawnMap.GetRelativeDirection(Direction.PositiveY).GetVector()).Y > 0.5f
												&& Agent.Query(absolutePos, 0.0f, 20.0f) == null)
											{
												supportedLocations++;
												DynamicMap dynamicMap = respawnMap as DynamicMap;
												if (dynamicMap == null || absolutePos.Y > respawnLocation.OriginalPosition.Y - 1.0f)
												{
													Map.GlobalRaycastResult hit = Map.GlobalRaycast(absolutePos + new Vector3(0, 1, 0), Vector3.Up, 2);
													if (hit.Map == null)
													{
														// We can spawn here
														spawnFound = true;
														foundSpawnLocation = respawnLocation;
														foundSpawnAbsolutePosition = absolutePos;
													}
												}
											}
										}
										respawnLocations.RemoveAt(respawnLocations.Count - 1);
										if (supportedLocations >= 40 || (foundSpawnAbsolutePosition - this.lastPlayerPosition).Length() > this.RespawnDistance)
											break;
									}
								}
							}

							if (spawnFound)
							{
								// Spawn at an autosaved location
								Vector3 absolutePos = foundSpawnLocation.Map.Target.Get<Map>().GetAbsolutePosition(foundSpawnLocation.Coordinate);
								this.Player.Value.Get<Transform>().Position.Value = this.Camera.Position.Value = absolutePos + new Vector3(0, 3, 0);

								FPSInput.RecenterMouse();
								Property<Vector2> mouse = this.Player.Value.Get<FPSInput>().Mouse;
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
									this.Player.Value.Get<Transform>().Position.Value = this.Camera.Position.Value = spawnEntity.Get<Transform>().Position;

								if (spawn != null)
								{
									spawn.IsActivated.Value = true;
									FPSInput.RecenterMouse();
									Property<Vector2> mouse = this.Player.Value.Get<FPSInput>().Mouse;
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

							this.PlayerSpawned.Execute(this.Player);

							this.RespawnInterval = GameMain.DefaultRespawnInterval;
							this.RespawnDistance = GameMain.DefaultRespawnDistance;
						}
						else
							this.respawnTimer += this.ElapsedTime;
					}
				}
				else
					this.lastPlayerPosition = this.Player.Value.Get<Transform>().Position;
			}
		}

		public override void ResizeViewport(int width, int height, bool fullscreen, bool applyChanges = true)
		{
			base.ResizeViewport(width, height, fullscreen, applyChanges);
			this.Settings.Fullscreen.Value = fullscreen;
			if (fullscreen)
				this.Settings.FullscreenResolution.Value = new Point(width, height);
			else
				this.Settings.Size.Value = new Point(width, height);
			this.SaveSettings();
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
	}
}