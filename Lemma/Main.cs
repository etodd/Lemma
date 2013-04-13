using System;
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

namespace Lemma
{
	public class Main : Microsoft.Xna.Framework.Game
	{
		public Camera Camera;

		public new GraphicsDevice GraphicsDevice
		{
			get
			{
				return this.graphics.GraphicsDevice;
			}
		}

		public Command<Entity> EntityAdded = new Command<Entity>();
		public Command<Entity> EntityRemoved = new Command<Entity>();

		protected GraphicsDeviceManager graphics;
		public Renderer Renderer;

		protected RenderParameters renderParameters;
		protected RenderTarget2D renderTarget;

		public Property<float> ElapsedTime = new Property<float>();
		public Property<float> TotalTime = new Property<float>();
		public Property<float> TimeMultiplier = new Property<float> { Value = 1.0f };

		private const float performanceUpdateTime = 0.5f;
		private float performanceInterval;

#if PERFORMANCE_MONITOR
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
#endif

		public Property<Point> ScreenSize = new Property<Point>();

		public LightingManager LightingManager;

		public UIRenderer UI;

		public Property<bool> EditorEnabled = new Property<bool> { Value = false };

		// XACT stuff
		public AudioEngine AudioEngine;
		private WaveBank waveBank;
		private WaveBank musicBank;
		public SoundBank SoundBank;

		private bool mapLoaded;

		public Space Space;

		public List<Entity> Entities;

		private List<IComponent> componentsToRemove = new List<IComponent>();
		private List<IComponent> componentsToAdd = new List<IComponent>();
		private List<IComponent> components = new List<IComponent>();
		private List<IDrawableComponent> drawables = new List<IDrawableComponent>();
		private List<IUpdateableComponent> updateables = new List<IUpdateableComponent>();
		private List<IDrawablePreFrameComponent> preframeDrawables = new List<IDrawablePreFrameComponent>();
		private List<INonPostProcessedDrawableComponent> nonPostProcessedDrawables = new List<INonPostProcessedDrawableComponent>();
		private List<IDrawableAlphaComponent> alphaDrawables = new List<IDrawableAlphaComponent>();

		private Point? resize;

		public Property<string> MapFile = new Property<string>();

		public GameTime GameTime;

		public Property<KeyboardState> LastKeyboardState = new Property<KeyboardState>();
		public Property<KeyboardState> KeyboardState = new Property<KeyboardState>();
		public Property<MouseState> LastMouseState = new Property<MouseState>();
		public Property<MouseState> MouseState = new Property<MouseState>();
		public new Property<bool> IsMouseVisible = new Property<bool> { };

		public object ComponentFlushLock = new object();

		public void Add(Entity entity)
		{
			if (entity.Active)
			{
				this.Entities.Add(entity);
				this.EntityAdded.Execute(entity);
			}
		}

		public Command<string> LoadingMap = new Command<string>();

		public Command MapLoaded = new Command();

		public Property<bool> Paused = new Property<bool>();

		public void ClearEntities(bool deleteEditor)
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
			Factory.Initialize(); // Clear factories to clear out any relationships that might confuse the garbage collector
			GC.Collect();

			this.TotalTime.Value = 0.0f;
			Sound.Reset(this);
			this.Renderer.BlurAmount.Value = 0.0f;
			this.Renderer.Tint.Value = Vector3.One;
			this.TimeMultiplier.Value = 1.0f;
			this.Camera.Angles.Value = Vector3.Zero;
		}

		public void AddComponent(IComponent component)
		{
			if (this.EditorEnabled || component.Entity == null || component.Entity.CannotSuspend)
				component.Suspended.Value = false;
			if (component.NeedsAdded)
			{
				component.SetMain(this);
				this.componentsToAdd.Add(component);
			}
		}

		public void RemoveComponent(IComponent component)
		{
			this.componentsToRemove.Add(component);
		}

		public void Remove(Entity entity)
		{
			if (entity.Active)
				entity.Delete.Execute();
			else
				this.Entities.Remove(entity);
			this.EntityRemoved.Execute(entity);
		}

		public IEnumerable<Entity> Get(string type)
		{
			return this.Entities.Where(x => x.Type == type && x.Active);
		}

		public Entity GetByID(string id)
		{
			return this.Entities.FirstOrDefault(x => x.ID == id);
		}

		private NotifyBinding drawableBinding;
		private bool drawablesModified;
		private NotifyBinding alphaDrawableBinding;
		private bool alphaDrawablesModified;
		private NotifyBinding nonPostProcessedDrawableBinding;
		private bool nonPostProcessedDrawablesModified;

		public Command ReloadedContent = new Command();

		private bool updating;
		public void FlushComponents()
		{
			if (this.updating)
				return;

			lock (this.ComponentFlushLock)
			{
				foreach (IComponent c in this.componentsToAdd)
				{
					this.components.Add(c);
					Type t = c.GetType();
					if (typeof(IDrawableComponent).IsAssignableFrom(t))
					{
						this.drawables.Add((IDrawableComponent)c);
						if (this.drawableBinding != null)
						{
							this.drawableBinding.Delete();
							this.drawableBinding = null;
						}
					}
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
				}
				this.componentsToAdd.Clear();

				foreach (IComponent c in this.componentsToRemove)
				{
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
					this.components.Remove(c);
				}
				this.componentsToRemove.Clear();
			}
		}

		public Main()
		{
			this.Space = new Space();
			this.ScreenSize.Value = new Point(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);

			// Give the space some threads to work with.
			// Just throw a thread at every processor. The thread scheduler will take care of where to put them.
			for (int i = 0; i < Environment.ProcessorCount; i++)
				this.Space.ThreadManager.AddThread();
			this.Space.ForceUpdater.Gravity = new Vector3(0, -18.0f, 0);

			this.IsFixedTimeStep = false;
			//this.IsFixedTimeStep = true;
			//this.TargetElapsedTime = new TimeSpan((long)((1.0f / 30.0f) * (float)TimeSpan.TicksPerSecond));

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

			this.AudioEngine = new AudioEngine("Content\\Sounds\\WinSettings.xgs");
			this.waveBank = new WaveBank(this.AudioEngine, "Content\\Sounds\\Waves.xwb");
			this.musicBank = new WaveBank(this.AudioEngine, "Content\\Sounds\\Music.xwb");
			this.SoundBank = new SoundBank(this.AudioEngine, "Content\\Sounds\\Sounds.xsb");

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
				this.AudioEngine.SetGlobalVariable("TimeShift", (value - 1.0f) * 12.0f);
			};

			new CommandBinding(this.MapLoaded, delegate()
			{
				this.mapLoaded = true;
			});
		}

		protected bool firstLoadContentCall = true;
		
		protected override void LoadContent()
		{
			if (this.firstLoadContentCall)
			{
				// First time loading content. Create the renderer.
				this.LightingManager = new LightingManager();
				this.AddComponent(this.LightingManager);
				this.Renderer = new Renderer(this, this.ScreenSize, true, true, false);

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
				ListContainer performanceMonitor = new ListContainer();
				performanceMonitor.Add(new Binding<Vector2, Point>(performanceMonitor.Position, x => new Vector2(0, x.Y), this.ScreenSize));
				performanceMonitor.AnchorPoint.Value = new Vector2(0, 1);
				performanceMonitor.Visible.Value = false;
				performanceMonitor.Name.Value = "PerformanceMonitor";
				this.UI.Root.Children.Add(performanceMonitor);

				Action<string, Property<double>> addLabel = delegate(string label, Property<double> property)
				{
					TextElement text = new TextElement();
					text.FontFile.Value = "Font";
					text.Add(new Binding<string, double>(text.Text, x => label + ": " + (x * 1000.0).ToString("F") + "ms", property));
					performanceMonitor.Children.Add(text);
				};

				TextElement frameRateText = new TextElement();
				frameRateText.FontFile.Value = "Font";
				frameRateText.Add(new Binding<string, float>(frameRateText.Text, x => "FPS: " + x.ToString("0"), this.frameRate));
				performanceMonitor.Children.Add(frameRateText);

				addLabel("Physics", this.physicsTime);
				addLabel("Update", this.updateTime);
				addLabel("Pre-frame", this.preframeTime);
				addLabel("Raw render", this.rawRenderTime);
				addLabel("Shadow render", this.shadowRenderTime);
				addLabel("Post-process", this.postProcessTime);

				PCInput input = new PCInput();
				input.Add(new CommandBinding(input.GetChord(new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.P }), delegate()
				{
					performanceMonitor.Visible.Value = !performanceMonitor.Visible;
				}));
				this.AddComponent(input);
#endif
			}
			else
			{
				foreach (IComponent c in this.components)
					c.LoadContent(true);
				this.ReloadedContent.Execute();
			}
		}

		private bool componentEnabled(IComponent c)
		{
			return c.Active && c.Enabled && !c.Suspended && (!this.EditorEnabled || c.EnabledInEditMode) && (!this.Paused || c.EnabledWhenPaused);
		}

		protected override void Update(GameTime gameTime)
		{
			if (!this.EditorEnabled && this.mapLoaded)
			{
				IEnumerable<string> globalScripts = Directory.GetFiles(Path.Combine(this.Content.RootDirectory, "GlobalScripts"), "*", SearchOption.AllDirectories).Select(x => Path.Combine("GlobalScripts", Path.GetFileNameWithoutExtension(x)));
				IEnumerable<string> mapGlobalScripts = Directory.GetFiles(Path.Combine(this.Content.RootDirectory, "Maps", "GlobalScripts"), "*", SearchOption.AllDirectories).Select(x => Path.Combine("Maps", "GlobalScripts", Path.GetFileNameWithoutExtension(x)));
				foreach (string scriptName in globalScripts.Concat(mapGlobalScripts))
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
				}
			}
			this.mapLoaded = false;

			if (gameTime.ElapsedGameTime.TotalSeconds > 0.1f)
				gameTime = new GameTime(gameTime.TotalGameTime, new TimeSpan((long)(0.1f * (float)TimeSpan.TicksPerSecond)), true);
			this.GameTime = gameTime;
			this.ElapsedTime.Value = (float)gameTime.ElapsedGameTime.TotalSeconds * this.TimeMultiplier;
			if (!this.Paused)
				this.TotalTime.Value += this.ElapsedTime;

			this.LastKeyboardState.Value = this.KeyboardState;
			this.KeyboardState.Value = Microsoft.Xna.Framework.Input.Keyboard.GetState();
			this.LastMouseState.Value = this.MouseState;
			this.MouseState.Value = Microsoft.Xna.Framework.Input.Mouse.GetState();

#if PERFORMANCE_MONITOR
			Stopwatch timer = new Stopwatch();
			timer.Start();
#endif
			if (!this.Paused && !this.EditorEnabled)
				this.Space.Update(this.ElapsedTime);
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.physicsSum = Math.Max(this.physicsSum, timer.Elapsed.TotalSeconds);
#endif

#if PERFORMANCE_MONITOR
			timer.Restart();
#endif
			this.updating = true;
			foreach (IUpdateableComponent c in this.updateables)
			{
				if (this.componentEnabled((Component)c))
					c.Update(this.ElapsedTime);
			}
			this.updating = false;
			this.FlushComponents();

			if (this.drawableBinding == null)
			{
				this.drawableBinding = new NotifyBinding(delegate() { this.drawablesModified = true; }, this.drawables.Select(x => x.DrawOrder).ToArray());
				this.drawablesModified = true;
			}
			if (this.drawablesModified)
			{
				this.drawables.InsertionSort(delegate(IDrawableComponent a, IDrawableComponent b)
				{
					return a.DrawOrder.Value.CompareTo(b.DrawOrder.Value);
				});
				this.drawablesModified = false;
			}

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

			this.AudioEngine.Update();

			if (this.resize != null && this.resize.Value.X > 0 && this.resize.Value.Y > 0)
			{
				this.ResizeViewport(this.resize.Value.X, this.resize.Value.Y, false);
				this.resize = null;
			}

#if PERFORMANCE_MONITOR
			timer.Stop();
			this.updateSum = Math.Max(this.updateSum, timer.Elapsed.TotalSeconds);
			this.frameSum++;
			this.performanceInterval += this.ElapsedTime;
			if (this.performanceInterval > Main.performanceUpdateTime)
			{
				this.frameRate.Value = this.frameSum / this.performanceInterval;
				this.physicsTime.Value = this.physicsSum;
				this.updateTime.Value = this.updateSum;
				this.preframeTime.Value = this.preframeSum;
				this.rawRenderTime.Value = this.rawRenderSum;
				this.shadowRenderTime.Value = this.shadowRenderSum;
				this.postProcessTime.Value = this.postProcessSum;
				this.physicsSum = 0;
				this.updateSum = 0;
				this.preframeSum = 0;
				this.rawRenderSum = 0;
				this.shadowRenderSum = 0;
				this.postProcessSum = 0;
				this.frameSum = 0;
				this.performanceInterval = 0;
			}
#endif
		}

		protected override void Draw(GameTime gameTime)
		{
			if (this.GraphicsDevice == null || this.GraphicsDevice.IsDisposed || this.GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
				return;

#if PERFORMANCE_MONITOR
			Stopwatch timer = new Stopwatch();
			timer.Start();
#endif
			this.renderParameters.Technique = this.Renderer.MotionBlurAmount.Value > 0.0f && !this.Paused ? Technique.MotionBlur : Technique.Render;

			// This line prevents the game from crashing when resizing the window.
			// Do not ask questions.
			this.GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;

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
			this.Renderer.PostProcess(this.renderTarget, this.renderParameters, this.DrawAlphaComponents);

			foreach (INonPostProcessedDrawableComponent c in this.nonPostProcessedDrawables)
			{
				if (this.componentEnabled(c))
					c.DrawNonPostProcessed(gameTime, this.renderParameters);
			}
#if PERFORMANCE_MONITOR
			timer.Stop();
			this.postProcessSum = Math.Max(this.postProcessSum, timer.Elapsed.TotalSeconds);
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

			foreach (IDrawableComponent c in this.drawables)
			{
				if (this.componentEnabled(c))
					c.Draw(this.GameTime, parameters);
			}

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

		public virtual void ResizeViewport(int width, int height, bool fullscreen, bool applyChanges = true)
		{
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

			this.ScreenSize.Value = new Point(width, height);
			if (this.Renderer != null)
				this.Renderer.ReallocateBuffers(this.ScreenSize);
		}
	}
}
