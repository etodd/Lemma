using System; using ComponentBind;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

namespace Lemma
{
	public class Main : BaseMain
	{
		public Camera Camera;
		public AkListener Listener;

		public new GraphicsDevice GraphicsDevice
		{
			get
			{
				return this.graphics.GraphicsDevice;
			}
		}

		protected GraphicsDeviceManager graphics;
		public Renderer Renderer;

		protected RenderParameters renderParameters;
		protected RenderTarget2D renderTarget;

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

		private bool mapLoaded;

		public Space Space;

		private List<IDrawableComponent> drawables = new List<IDrawableComponent>();
		private List<IUpdateableComponent> updateables = new List<IUpdateableComponent>();
		private List<IDrawablePreFrameComponent> preframeDrawables = new List<IDrawablePreFrameComponent>();
		private List<INonPostProcessedDrawableComponent> nonPostProcessedDrawables = new List<INonPostProcessedDrawableComponent>();
		private List<IDrawableAlphaComponent> alphaDrawables = new List<IDrawableAlphaComponent>();
		private List<IDrawablePostAlphaComponent> postAlphaDrawables = new List<IDrawablePostAlphaComponent>();

		private Point? resize;

		public Property<string> MapFile = new Property<string>();

		public Property<KeyboardState> LastKeyboardState = new Property<KeyboardState>();
		public Property<KeyboardState> KeyboardState = new Property<KeyboardState>();
		public Property<MouseState> LastMouseState = new Property<MouseState>();
		public Property<MouseState> MouseState = new Property<MouseState>();
		public Property<GamePadState> LastGamePadState = new Property<GamePadState>();
		public Property<GamePadState> GamePadState = new Property<GamePadState>();
		public new Property<bool> IsMouseVisible = new Property<bool> { };
		public Property<bool> GamePadConnected = new Property<bool>();

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

		public virtual void ClearEntities(bool deleteEditor)
		{
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
							if (editor.MapEditMode)
								editor.MapEditMode.Value = false;
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
			this.Camera.Angles.Value = Vector3.Zero;
		}

		public Command ReloadedContent = new Command();

		public Main()
		{
			Factory<Main>.Initialize();
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
				if (!this.graphics.IsFullScreen)
				{
					Rectangle bounds = this.Window.ClientBounds;
					this.ScreenSize.Value = new Point(bounds.Width, bounds.Height);
					this.resize = new Point(bounds.Width, bounds.Height);
				}
			});

			this.graphics = new GraphicsDeviceManager(this);
			this.graphics.SynchronizeWithVerticalRetrace = false;

			this.Content = new ContentManager(this.Services);
			this.Content.RootDirectory = "Content";

			this.Entities = new List<Entity>();

			this.Camera = new Camera();
			this.Camera.Add(new Binding<Point>(this.Camera.ViewportSize, this.ScreenSize));
			this.AddComponent(this.Camera);

			this.IsMouseVisible.Value = false;
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
		}

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
		}

		protected bool firstLoadContentCall = true;
		
		protected override void LoadContent()
		{
			if (this.firstLoadContentCall)
			{
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
				this.Renderer = new Renderer(this, this.ScreenSize, true, true, true, true);

				this.AddComponent(this.Renderer);
				this.renderParameters = new RenderParameters
				{
					Camera = this.Camera,
					IsMainRender = true
				};
				this.firstLoadContentCall = false;

				this.UI = new UIRenderer();
				this.AddComponent(this.UI);

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

				PCInput input = new PCInput();
				input.Add(new CommandBinding(input.GetChord(new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.P }), delegate()
				{
					this.performanceMonitor.Visible.Value = !this.performanceMonitor.Visible;
				}));
				this.AddComponent(input);
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
				Entity scriptEntity = Factory.Get<ScriptFactory>().CreateAndBind(this);
				scriptEntity.ID = id;
				scriptEntity.Serialize = true;
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

			AkSoundEngine.RenderAudio();

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

			this.update();
		}

		protected virtual void update()
		{

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
			this.Renderer.PostProcess(this.renderTarget, this.renderParameters);

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

		public virtual void ResizeViewport(int width, int height, bool fullscreen, bool applyChanges = true)
		{
			this.ScreenSize.Value = new Point(width, height);

			bool needApply = false;
			if (this.graphics.IsFullScreen != fullscreen)
			{
				this.graphics.IsFullScreen = fullscreen;
				needApply = true;
			}
			if (this.graphics.PreferredBackBufferWidth != width)
			{
				this.graphics.PreferredBackBufferWidth = width;
				needApply = true;
			}
			if (this.graphics.PreferredBackBufferHeight != height)
			{
				this.graphics.PreferredBackBufferHeight = height;
				needApply = true;
			}
			if (applyChanges && needApply)
				this.graphics.ApplyChanges();

			if (this.Renderer != null)
				this.Renderer.ReallocateBuffers(this.ScreenSize);
		}
	}
}
