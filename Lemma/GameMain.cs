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
			public Property<PCInput.PCInputBinding> Forward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.W } };
			public Property<PCInput.PCInputBinding> Left = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.A } };
			public Property<PCInput.PCInputBinding> Right = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.D } };
			public Property<PCInput.PCInputBinding> Backward = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.S } };
			public Property<PCInput.PCInputBinding> Jump = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Space } };
			public Property<PCInput.PCInputBinding> WallRun = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.LeftShift } };
			public Property<PCInput.PCInputBinding> Aim = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.RightMouseButton } };
			public Property<PCInput.PCInputBinding> FireBuildRoll = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { MouseButton = PCInput.MouseButton.LeftMouseButton } };
			public Property<PCInput.PCInputBinding> Reload = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.R } };
			public Property<PCInput.PCInputBinding> TogglePistol = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.D1 } };
			public Property<PCInput.PCInputBinding> ToggleLevitate = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.D2 } };
			public Property<PCInput.PCInputBinding> TogglePhone = new Property<PCInput.PCInputBinding> { Value = new PCInput.PCInputBinding { Key = Keys.Tab } };
		}

		public class SaveInfo
		{
			public string MapFile;
		}

		public bool CanSpawn = true;

		public Config Settings;
		private string settingsDirectory;
		private string saveDirectory;
		private string settingsFile;

		private Entity player;
		private Entity editor;
		private PCInput input;

		private string initialMapFile;
		private bool allowEditing;

		public Property<string> StartSpawnPoint = new Property<string>();

		private bool spawnedAtStartPoint = false;

		const float respawnInterval = 3.0f;

		private float respawnTimer = -1.0f;

		private bool saveAfterTakingScreenshot = false;

		private DisplayModeCollection supportedDisplayModes;

		private int displayModeIndex;

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

			try
			{
				// Attempt to load previous window state
				using (Stream stream = new FileStream(this.settingsFile, FileMode.Open, FileAccess.Read, FileShare.None))
					this.Settings = (Config)new XmlSerializer(typeof(Config)).Deserialize(stream);
			}
			catch (Exception) // File doesn't exist or there was a deserialization error. Use default window settings
			{
				this.Settings = new Config();
			}

			// Restore window state
			if (this.Settings.Fullscreen)
				this.ResizeViewport(this.Settings.FullscreenResolution.Value.X, this.Settings.FullscreenResolution.Value.Y, true);
			else
				this.ResizeViewport(this.Settings.Size.Value.X, this.Settings.Size.Value.Y, false, false);
		}

		private void copySave(string src, string dst)
		{
			if (!Directory.Exists(dst))
				Directory.CreateDirectory(dst);

			string[] ignoredExtensions = new[] { ".cs", ".dll", };

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

		public Command Save = new Command();

		public Command SaveCurrentMap = new Command();

		protected string currentSave;

		protected override void LoadContent()
		{
			bool firstInitialization = this.firstLoadContentCall;
			base.LoadContent();

			if (firstInitialization)
			{
				this.IsMouseVisible.Value = true;

				this.MapFile.Set = delegate(string value)
				{
					this.MapFile.InternalValue = value;

					this.ClearEntities(false);

					if (value == null || value.Length == 0)
						return;

					try
					{
						IO.MapLoader.Load(this, this.currentSave == null ? null : Path.Combine(this.saveDirectory, this.currentSave), value, false);
					}
					catch (FileNotFoundException)
					{
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

					this.spawnedAtStartPoint = false;
				};

				this.Renderer.LightRampTexture.Value = "Images\\default-ramp";

				this.input = new PCInput();
				this.AddComponent(this.input);

				new TwoWayBinding<LightingManager.DynamicShadowSetting>(this.Settings.DynamicShadows, this.LightingManager.DynamicShadows);
				new TwoWayBinding<float>(this.Settings.MotionBlurAmount, this.Renderer.MotionBlurAmount);
				new TwoWayBinding<float>(this.Settings.Gamma, this.Renderer.Gamma);
				new TwoWayBinding<bool>(this.Settings.EnableBloom, this.Renderer.EnableBloom);
				if (this.Settings.FullscreenResolution.Value.X == 0)
				{
					Microsoft.Xna.Framework.Graphics.DisplayMode display = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
					this.Settings.FullscreenResolution.Value = new Point(display.Width, display.Height);
				}

				// Toggle fullscreen
				this.input.Add(new CommandBinding(input.GetKeyDown(Keys.F11), delegate()
				{
					if (this.graphics.IsFullScreen) // Already fullscreen. Go to windowed mode.
						this.ExitFullscreen();
					else // In windowed mode. Go to fullscreen.
						this.EnterFullscreen();
				}));

				// Fullscreen message
				Container msgBackground = new Container();
				this.UI.Root.Children.Add(msgBackground);
				msgBackground.Tint.Value = Color.Black;
				msgBackground.Opacity.Value = 0.2f;
				msgBackground.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
				msgBackground.Add(new Binding<Vector2, Point>(msgBackground.Position, x => new Vector2(x.X * 0.5f, x.Y - 30.0f), this.ScreenSize));
				TextElement msg = new TextElement();
				msg.FontFile.Value = "Font";
				msg.Text.Value = "F11 - Toggle Fullscreen";
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
				pauseLabel.Text.Value = "P A U S E";
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
				};

				Action showPauseMenu = delegate()
				{
					pauseMenu.Visible.Value = true;
					if (pauseAnimation != null)
						pauseAnimation.Delete.Execute();
					pauseAnimation = new Animation(new Animation.Vector2MoveToSpeed(pauseMenu.AnchorPoint, new Vector2(0, 0.5f), 5.0f));
					this.AddComponent(pauseAnimation);
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
				};

				// Unpause
				Action restorePausedSettings = delegate()
				{
					if (pauseAnimation != null && pauseAnimation.Active)
						pauseAnimation.Delete.Execute();

					// Restore mouse
					Microsoft.Xna.Framework.Input.Mouse.SetPosition(originalMousePosition.X, originalMousePosition.Y);
					MouseState m = new MouseState(originalMousePosition.X, originalMousePosition.Y, this.MouseState.Value.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
					this.LastMouseState.Value = m;
					this.MouseState.Value = m;
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

					screenshot.Dispose();
					screenshot = null;
					screenshotSize = Point.Zero;
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

				Container loadSaveLabelContainer = new Container();
				loadSaveLabelContainer.Opacity.Value = 0.0f;
				loadSaveMenu.Children.Add(loadSaveLabelContainer);

				TextElement loadSaveLabel = new TextElement();
				loadSaveLabel.FontFile.Value = "Font";
				loadSaveLabel.Add(new Binding<string, bool>(loadSaveLabel.Text, x => x ? "S A V E" : "L O A D", saveMode));
				loadSaveLabelContainer.Children.Add(loadSaveLabel);

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
				loadSaveScroll.Size.Value = new Vector2(276.0f, 400.0f);
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
					}
					catch (Exception)
					{
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
							prompt.Text.Value = "Overwrite this save?";
							dialogLayout.Children.Add(prompt);

							ListContainer dialogButtons = new ListContainer();
							dialogButtons.Orientation.Value = ListContainer.ListOrientation.Horizontal;
							dialogLayout.Children.Add(dialogButtons);

							UIComponent overwrite = this.createMenuButton("Overwrite");
							dialogButtons.Children.Add(overwrite);
							overwrite.Add(new CommandBinding<Point>(overwrite.MouseLeftUp, delegate(Point p2)
							{
								dialog.Delete.Execute();
								dialog = null;
								container.Delete.Execute();
								save();
								Directory.Delete(Path.Combine(this.saveDirectory, timestamp), true);
								hideLoadSave();
								this.Paused.Value = false;
								restorePausedSettings();
							}));

							UIComponent cancel = this.createMenuButton("Cancel");
							dialogButtons.Children.Add(cancel);
							cancel.Add(new CommandBinding<Point>(cancel.MouseLeftUp, delegate(Point p2)
							{
								dialog.Delete.Execute();
								dialog = null;
							}));
						}
						else
						{
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
							new XmlSerializer(typeof(SaveInfo)).Serialize(stream, new SaveInfo { MapFile = this.MapFile });
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
						// Create the new save.
						string s = this.currentSave;
						save();

						this.saveAfterTakingScreenshot = false;
						screenshot.Dispose();
						screenshot = null;
						screenshotSize = Point.Zero;

						// Delete the old save.
						if (s != null && s != this.currentSave)
						{
							Directory.Delete(Path.Combine(this.saveDirectory, s), true);
							UIComponent container = loadSaveList.Children.FirstOrDefault(x => ((string)x.UserData.Value) == s);
							if (container != null)
								container.Delete.Execute();
						}
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

				Container settingsLabelContainer = new Container();
				settingsLabelContainer.Opacity.Value = 0.0f;

				TextElement settingsLabel = new TextElement();
				settingsLabel.FontFile.Value = "Font";
				settingsLabel.Text.Value = "O P T I O N S";
				settingsLabelContainer.Children.Add(settingsLabel);

				settingsMenu.Children.Add(settingsLabelContainer);

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
				fullscreenResolution.Add(new CommandBinding<Point, int>(fullscreenResolution.MouseScrolled, delegate(Point mouse, int scroll)
				{
					displayModeIndex = (displayModeIndex + scroll) % this.supportedDisplayModes.Count();
					DisplayMode mode = this.supportedDisplayModes.ElementAt(displayModeIndex);
					this.Settings.FullscreenResolution.Value = new Point(mode.Width, mode.Height);
				}));
				settingsMenu.Children.Add(fullscreenResolution);

				UIComponent gamma = this.createMenuButton<float>("Gamma", this.Renderer.Gamma, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				gamma.Add(new CommandBinding<Point, int>(gamma.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Renderer.Gamma.Value = Math.Max(0, Math.Min(2, this.Renderer.Gamma + (scroll * 0.1f)));
				}));
				settingsMenu.Children.Add(gamma);

				UIComponent motionBlurAmount = this.createMenuButton<float>("Motion Blur Amount", this.Renderer.MotionBlurAmount, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				motionBlurAmount.Add(new CommandBinding<Point, int>(motionBlurAmount.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Renderer.MotionBlurAmount.Value = Math.Max(0, Math.Min(1, this.Renderer.MotionBlurAmount + (scroll * 0.1f)));
				}));
				settingsMenu.Children.Add(motionBlurAmount);

				UIComponent reflectionsEnabled = this.createMenuButton<bool>("Reflections Enabled", this.Settings.EnableReflections);
				reflectionsEnabled.Add(new CommandBinding<Point>(reflectionsEnabled.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.EnableReflections.Value = !this.Settings.EnableReflections;
				}));
				settingsMenu.Children.Add(reflectionsEnabled);

				UIComponent bloomEnabled = this.createMenuButton<bool>("Bloom Enabled", this.Renderer.EnableBloom);
				bloomEnabled.Add(new CommandBinding<Point>(bloomEnabled.MouseLeftUp, delegate(Point mouse)
				{
					this.Renderer.EnableBloom.Value = !this.Renderer.EnableBloom;
				}));
				settingsMenu.Children.Add(bloomEnabled);

				UIComponent dynamicShadows = this.createMenuButton<LightingManager.DynamicShadowSetting>("Dynamic Shadows", this.LightingManager.DynamicShadows);
				int numDynamicShadowSettings = typeof(LightingManager.DynamicShadowSetting).GetFields(BindingFlags.Static | BindingFlags.Public).Length;
				dynamicShadows.Add(new CommandBinding<Point>(dynamicShadows.MouseLeftUp, delegate(Point mouse)
				{
					this.LightingManager.DynamicShadows.Value = (LightingManager.DynamicShadowSetting)Enum.ToObject(typeof(LightingManager.DynamicShadowSetting), (((int)this.LightingManager.DynamicShadows.Value) + 1) % numDynamicShadowSettings);
				}));
				settingsMenu.Children.Add(dynamicShadows);

				// Controls menu
				bool controlsShown = false;
				Animation controlsAnimation = null;

				ListContainer controlsMenu = new ListContainer();
				controlsMenu.Visible.Value = false;
				controlsMenu.Add(new Binding<Vector2, Point>(controlsMenu.Position, x => new Vector2(0, x.Y * 0.5f), this.ScreenSize));
				controlsMenu.AnchorPoint.Value = new Vector2(1, 0.5f);
				this.UI.Root.Children.Add(controlsMenu);
				controlsMenu.Orientation.Value = ListContainer.ListOrientation.Vertical;

				Container controlsLabelContainer = new Container();
				controlsLabelContainer.Opacity.Value = 0.0f;

				TextElement controlsLabel = new TextElement();
				controlsLabel.FontFile.Value = "Font";
				controlsLabel.Text.Value = "C O N T R O L S";
				controlsLabelContainer.Children.Add(controlsLabel);

				controlsMenu.Children.Add(controlsLabelContainer);

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

				UIComponent invertMouseX = this.createMenuButton<bool>("Invert Mouse X", this.Settings.InvertMouseX);
				invertMouseX.Add(new CommandBinding<Point>(invertMouseX.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.InvertMouseX.Value = !this.Settings.InvertMouseX;
				}));
				controlsMenu.Children.Add(invertMouseX);

				UIComponent invertMouseY = this.createMenuButton<bool>("Invert Mouse Y", this.Settings.InvertMouseY);
				invertMouseY.Add(new CommandBinding<Point>(invertMouseY.MouseLeftUp, delegate(Point mouse)
				{
					this.Settings.InvertMouseY.Value = !this.Settings.InvertMouseY;
				}));
				controlsMenu.Children.Add(invertMouseY);

				UIComponent mouseSensitivity = this.createMenuButton<float>("Mouse Sensitivity", this.Settings.MouseSensitivity, x => ((int)Math.Round(x * 100.0f)).ToString() + "%");
				mouseSensitivity.Add(new CommandBinding<Point, int>(mouseSensitivity.MouseScrolled, delegate(Point mouse, int scroll)
				{
					this.Settings.MouseSensitivity.Value = Math.Max(0, Math.Min(5, this.Settings.MouseSensitivity + (scroll * 0.1f)));
				}));
				controlsMenu.Children.Add(mouseSensitivity);

				Action<Property<PCInput.PCInputBinding>, string> addInputSetting = delegate(Property<PCInput.PCInputBinding> setting, string display)
				{
					UIComponent button = this.createMenuButton<PCInput.PCInputBinding>(display, setting);
					button.Add(new CommandBinding<Point>(button.MouseLeftUp, delegate(Point mouse)
					{
						PCInput.PCInputBinding originalValue = setting;
						setting.Value = new PCInput.PCInputBinding(); // Clear setting. Will display [?] for the key.
						this.UI.EnableMouse.Value = false;
						input.GetNextInput(delegate(PCInput.PCInputBinding binding)
						{
							if (binding.Key == Keys.Escape)
								setting.Value = originalValue;
							else
								setting.Value = binding;
							this.UI.EnableMouse.Value = true;
						});
					}));
					controlsMenu.Children.Add(button);
				};

				addInputSetting(this.Settings.Forward, "Move Forward");
				addInputSetting(this.Settings.Left, "Move Left");
				addInputSetting(this.Settings.Backward, "Move Backward");
				addInputSetting(this.Settings.Right, "Move Right");
				addInputSetting(this.Settings.Jump, "Jump");
				addInputSetting(this.Settings.WallRun, "Sprint / Wall Run");
				addInputSetting(this.Settings.FireBuildRoll, "Roll / Fire weapon / Build");
				addInputSetting(this.Settings.Aim, "Aim");
				addInputSetting(this.Settings.TogglePistol, "Toggle Pistol");
				addInputSetting(this.Settings.ToggleLevitate, "Toggle Levitation");
				addInputSetting(this.Settings.Reload, "Reload");
				addInputSetting(this.Settings.TogglePhone, "Toggle Phone");

				// Resume button
				UIComponent resume = this.createMenuButton("Resume");
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
				}));
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

				// Exit button
				UIComponent exit = this.createMenuButton("Exit");
				exit.Add(new CommandBinding<Point>(exit.MouseLeftUp, delegate(Point mouse)
				{
					this.Exit();
				}));
				pauseMenu.Children.Add(exit);

				// Escape key
				// Make sure we can only pause when there is a player currently spawned
				// Otherwise we could save the current map without the player. And that would be awkward.
				this.input.Add(new CommandBinding(input.GetKeyDown(Keys.Escape), () => !this.EditorEnabled && ((this.player != null && this.player.Active) || this.MapFile.Value == null), delegate()
				{
					if (settingsShown)
					{
						hideSettings();
						return;
					}
					else if (controlsShown)
					{
						hideControls();
						return;
					}
					else if (loadSaveShown)
					{
						hideLoadSave();
						return;
					}
					else if (dialog != null)
					{
						dialog.Delete.Execute();
						dialog = null;
						return;
					}

					this.Paused.Value = !this.Paused;

					if (this.Paused)
						savePausedSettings();
					else
						restorePausedSettings();
				}));

#if DEBUG
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

				TextElement header = new TextElement();
				header.FontFile.Value = "Font";
				header.Text.Value = "Alpha 2";
				header.AnchorPoint.Value = new Vector2(0.5f, 0);
				header.Add(new Binding<Vector2>(header.Position, () => logo.Position + new Vector2(0, 30 + (logo.InverseAnchorPoint.Value.Y * logo.ScaledSize.Value.Y)), logo.Position, logo.InverseAnchorPoint, logo.ScaledSize));
				this.UI.Root.Children.Add(header);

				ListContainer buttons = new ListContainer();
				buttons.AnchorPoint.Value = new Vector2(0.5f, 0);
				buttons.Add(new Binding<Vector2>(buttons.Position, () => logo.Position + new Vector2(0, 80 + (logo.InverseAnchorPoint.Value.Y * logo.ScaledSize.Value.Y)), logo.Position, logo.InverseAnchorPoint, logo.ScaledSize));
				this.UI.Root.Children.Add(buttons);

				TextElement instructions = new TextElement();
				instructions.FontFile.Value = "Font";
				instructions.Text.Value = "Esc: Menu";

				UIComponent startNew = this.createMenuButton("Start New");
				startNew.Add(new CommandBinding<Point>(startNew.MouseLeftUp, delegate(Point p)
				{
					instructions.Text.Value = "Loading...";
					instructions.Opacity.Value = 1.0f;
					logo.Opacity.Value = 1.0f;
					this.AddComponent(new Animation
					(
						new Animation.Delay(0.1f),
						new Animation.Set<string>(this.MapFile, this.initialMapFile)
					));
				}));
				buttons.Children.Add(startNew);

				UIComponent load2 = this.createMenuButton("Load");
				load2.Add(new CommandBinding<Point>(load2.MouseLeftUp, delegate(Point p)
				{
					this.Paused.Value = true;
					savePausedSettings();
					showLoad();
				}));
				buttons.Children.Add(load2);

				buttons.Children.Add(instructions);

				logo.Opacity.Value = 0.0f;
				instructions.Opacity.Value = 0.0f;

				Animation fadeAnimation = new Animation
				(
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(logo.Opacity, 1.0f, 1.0f),
						new Animation.FloatMoveTo(instructions.Opacity, 1.0f, 1.0f)
					)
				);

				this.AddComponent(fadeAnimation);

				Sound.PlayCue(this, "Music1 Stinger2");

				new CommandBinding(this.MapLoaded, buttons.Delete);
				new CommandBinding(this.MapLoaded, header.Delete);
#endif

				new CommandBinding(this.MapLoaded, logo.Delete);

				new CommandBinding(this.MapLoaded, delegate()
				{
					this.respawnTimer = GameMain.respawnInterval - 1.0f;
					this.player = this.Get("Player").FirstOrDefault();
				});
			}
		}

		public void EndGame()
		{
#if !DEBUG
			this.MapFile.Value = null; // Clears all the entities too
			this.Renderer.Tint.Value = Vector3.One;
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
			addLink("moddb.com/games/lemma", "http://moddb.com/games/lemma");
			addLink("facebook.com/lemmagame", "http://facebook.com/lemmagame");
			addLink("twitter.com/et1337", "http://twitter.com/et1337");

			addText("Writing, programming, and artwork by Evan Todd. Sound and music by Jack Menhorn, plus some sounds from freesound.org.");
#endif
		}

		protected void saveSettings()
		{
			// Save settings

			/*
			if (!this.Settings.Fullscreen)
			{
				System.Windows.Forms.Form window = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);
				this.Settings.Maximized.Value = window.WindowState == System.Windows.Forms.FormWindowState.Maximized;
				this.Settings.Size.Value = this.ScreenSize;
			}
			*/

			using (Stream stream = new FileStream(this.settingsFile, FileMode.Create, FileAccess.Write, FileShare.None))
				new XmlSerializer(typeof(Config)).Serialize(stream, this.Settings);
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// Spawn an editor or a player if needed
			if (this.EditorEnabled)
			{
				this.player = null;
				if (this.editor == null)
				{
					this.editor = Factory.Get("Editor").CreateAndBind(this);
					this.Add(this.editor);
				}
			}
			else
			{
				if (this.MapFile.Value == null || !this.CanSpawn)
					return;

				this.editor = null;

				bool setupSpawn = this.player == null || !this.player.Active;

				if (setupSpawn)
					this.player = this.Get("Player").FirstOrDefault();

				bool createPlayer = this.player == null || !this.player.Active;

				if (setupSpawn || createPlayer)
				{
					this.Renderer.Tint.Value = Vector3.Zero;
					this.Camera.Position.Value = new Vector3(0, -10000, 0);
					if (this.respawnTimer > GameMain.respawnInterval || this.respawnTimer < 0)
					{
						if (createPlayer)
						{
							this.player = Factory.CreateAndBind(this, "Player");
							this.Add(this.player);
						}

						PlayerSpawn spawn = null;
						Entity spawnEntity = null;
						if (!string.IsNullOrEmpty(this.StartSpawnPoint.Value) && !this.spawnedAtStartPoint)
						{
							spawnEntity = this.GetByID(this.StartSpawnPoint);
							if (spawnEntity != null)
							{
								spawn = spawnEntity.Get<PlayerSpawn>();
								this.spawnedAtStartPoint = true;
							}
						}

						if (spawnEntity == null)
						{
							spawn = this.Get("PlayerSpawn").Select(x => x.Get<PlayerSpawn>()).FirstOrDefault(x => x.IsActivated);
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
						
						this.AddComponent(new Animation(new Animation.Vector3MoveTo(this.Renderer.Tint, Vector3.One, 1.0f)));
						this.respawnTimer = 0;
					}
					else
						this.respawnTimer += this.ElapsedTime;
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