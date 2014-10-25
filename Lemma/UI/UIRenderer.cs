using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Console;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using GeeUI;

namespace Lemma.Components
{
	public class UIRenderer : Component<Main>, IUpdateableComponent, INonPostProcessedDrawableComponent, IDrawablePreFrameComponent
	{
		public class RootUIComponent : UIComponent
		{
			public RootUIComponent(UIRenderer renderer)
			{
				this.renderer = renderer;
			}
		}

		public Property<int> DrawOrder { get; set; }

		public void Setup3D(Property<Matrix> transform)
		{
			this.MouseFilter = delegate(MouseState mouse)
			{
				Point screenSize = this.main.ScreenSize;
				Microsoft.Xna.Framework.Graphics.Viewport viewport = new Viewport(0, 0, screenSize.X, screenSize.Y);

				Matrix inverseTransform = Matrix.Invert(transform);
				Vector3 ray = Vector3.Normalize(viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1), main.Camera.Projection, main.Camera.View, Matrix.Identity) - viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0), main.Camera.Projection, main.Camera.View, Matrix.Identity));
				Vector3 rayStart = main.Camera.Position;

				ray = Vector3.TransformNormal(ray, inverseTransform);
				rayStart = Vector3.Transform(rayStart, inverseTransform);

				Point output;

				float? intersection = new Ray(rayStart, ray).Intersects(new Plane(Vector3.Right, 0.0f));
				if (intersection.HasValue)
				{
					Vector3 intersectionPoint = rayStart + ray * intersection.Value;
					Point size = this.RenderTargetSize;
					Vector2 sizeF = new Vector2(size.X, size.Y);
					output = new Point((int)((0.5f - intersectionPoint.Z) * sizeF.X), (int)((0.5f - intersectionPoint.Y) * sizeF.Y));
				}
				else
					output = new Point(-1, -1);

				return new MouseState
				(
					output.X,
					output.Y,
					mouse.ScrollWheelValue,
					mouse.LeftButton,
					mouse.MiddleButton,
					mouse.RightButton,
					mouse.XButton1,
					mouse.XButton2
				);
			};
		}

		public GeeUIMain GeeUI;

		[XmlIgnore]
		public RasterizerState RasterizerState;

		[XmlIgnore]
		public SpriteBatch Batch;

		[XmlIgnore]
		public RootUIComponent Root;

		[XmlIgnore]
		public Command SwallowMouseEvents = new Command();

		[AutoConVar("ui_mouse_enabled", "If true, mouse is enabled")]
		public Property<bool> EnableMouse = new Property<bool> { Value = true };

		public Property<Point> RenderTargetSize = new Property<Point>();

		public Property<Color> RenderTargetBackground = new Property<Color>();

		[XmlIgnore]
		public Property<RenderTarget2D> RenderTarget = new Property<RenderTarget2D>();

		[XmlIgnore]
		public Func<MouseState, MouseState> MouseFilter = x => x;

		[XmlIgnore]
		public Property<Vector2> Mouse = new Property<Vector2>();

		public Property<bool> IsMouseVisible = new Property<bool> { };

		private MouseState lastMouseState;

		private void resize()
		{
			if (this.RenderTarget.Value != null)
				this.RenderTarget.Value.Dispose();
			this.RenderTarget.Value = null;

			Point size = this.RenderTargetSize;
			Point screenSize = this.main.ScreenSize;
			if (size.X > 0 && size.Y > 0)
			{
				this.RenderTarget.Value = new RenderTarget2D(this.main.GraphicsDevice, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
				this.Root.Size.Value = new Vector2(size.X, size.Y);
			}
			else
				this.Root.Size.Value = new Vector2(screenSize.X, screenSize.Y);

#if VR
			if (this.main.VR)
			{
				this.mousePos.X = screenSize.X / 2;
				this.mousePos.Y = screenSize.Y / 2;
			}
#endif

			this.needResize = false;
		}

#if VR
		public Sprite Reticle;
#endif

		public UIRenderer()
		{
			this.DrawOrder = new Property<int> { Value = 0 };
			this.RasterizerState = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
			this.Root = new RootUIComponent(this);
			this.Root.AnchorPoint.Value = Vector2.Zero;
			this.Serialize = false;
		}

		private bool needResize = false;
		public override void Awake()
		{
			base.Awake();
			this.Add(new NotifyBinding(delegate() { this.needResize = true; }, this.RenderTargetSize));
			this.Add(new NotifyBinding(delegate() { this.needResize = true; }, this.main.ScreenSize));
			this.lastMouseState = this.main.LastMouseState;
			this.main.GraphicsDevice.DeviceReset += this.deviceReset;
			this.main.AddComponent(this.Root);
			this.Root.Add(new Binding<Vector2, Point>(this.Root.Size, x => new Vector2(x.X, x.Y), main.ScreenSize));

#if VR
			if (this.main.VR)
			{
				this.Reticle = new Sprite();
				this.Reticle.Image.Value = "Images\\cursor";
				this.Reticle.AnchorPoint.Value = new Vector2(0.0f, 0.0f);
				this.Root.Children.Add(this.Reticle);
				this.Reticle.Add(new Binding<Vector2>(this.Reticle.Position, this.Mouse));
				this.Reticle.Add(new Binding<bool>(this.Reticle.Visible, this.IsMouseVisible));
				// HACK: Make sure reticle is always on top.
				this.Root.Children.ItemAdded += delegate(int index, UIComponent c)
				{
					if (c != this.Reticle && this.Root.Children.Contains(this.Reticle))
					{
						this.Reticle.Detach();
						this.Root.Children.Add(this.Reticle);
					}
				};
			}
			else
#endif
			{
				this.Add(new NotifyBinding(delegate()
				{
					this.main.IsMouseVisible = this.IsMouseVisible;
				}, this.IsMouseVisible));
			}
		}

		private void deviceReset(object sender, EventArgs e)
		{
			this.needResize = true;
		}

		public void LoadContent(bool reload)
		{
			this.Batch = new SpriteBatch(this.main.GraphicsDevice);
			this.needResize = true;
		}

#if VR
		private Point mousePos;
#endif

		void IUpdateableComponent.Update(float dt)
		{
			if (this.main.IsActive && this.EnableMouse)
			{
				MouseState realMouseState = this.main.MouseState;
#if VR
				Point lastMousePos = new Point();
				if (this.main.VR)
				{
					lastMousePos = this.mousePos;
					this.mousePos.X += realMouseState.X - FPSInput.MouseCenter.X;
					this.mousePos.Y += realMouseState.Y - FPSInput.MouseCenter.Y;
					FPSInput.RecenterMouse();
					realMouseState = new MouseState
					(
						this.mousePos.X,
						this.mousePos.Y,
						realMouseState.ScrollWheelValue,
						realMouseState.LeftButton,
						realMouseState.MiddleButton,
						realMouseState.RightButton,
						realMouseState.XButton1,
						realMouseState.XButton2
					);
				}
#endif
				MouseState current = this.MouseFilter(realMouseState), last = this.lastMouseState;

#if VR
				if (this.main.VR)
				{
					Point size = this.RenderTargetSize;
					if (current.X > size.X)
						this.mousePos.X = Math.Min(this.mousePos.X, lastMousePos.X);
					if (current.X < 0)
						this.mousePos.X = Math.Max(this.mousePos.X, lastMousePos.X);
					if (current.Y > size.Y)
						this.mousePos.Y = Math.Min(this.mousePos.Y, lastMousePos.Y);
					if (current.Y < 0)
						this.mousePos.Y = Math.Max(this.mousePos.Y, lastMousePos.Y);
				}
#endif

				if (this.GeeUI != null)
					this.GeeUI.Update(dt, this.main.KeyboardState, current);

				this.Mouse.Value = new Vector2(current.X, current.Y);
				if (current.LeftButton != last.LeftButton
					|| current.RightButton != last.RightButton
					|| current.MiddleButton != last.MiddleButton
					|| current.ScrollWheelValue != last.ScrollWheelValue
					|| current.X != last.X
					|| current.Y != last.Y
					|| current.XButton1 != last.XButton1
					|| current.XButton2 != last.XButton2)
				{
					if (this.Root.HandleMouse(current, last, Matrix.Identity, true))
						this.SwallowMouseEvents.Execute();
					this.lastMouseState = current;
				}
			}
		}

		private void draw(GameTime time, Point screenSize)
		{
			this.Root.CheckLayout();

			RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;

			this.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.RasterizerState, null, Matrix.Identity);

			if (this.GeeUI != null)
				this.GeeUI.Draw(this.Batch);

			this.Root.Draw(time, Matrix.Identity, new Rectangle(0, 0, screenSize.X, screenSize.Y));

			this.Batch.End();

			this.main.GraphicsDevice.RasterizerState = originalState;
		}

		void INonPostProcessedDrawableComponent.DrawNonPostProcessed(GameTime time, RenderParameters parameters)
		{
			if (this.RenderTarget.Value == null)
			{
				Viewport vp = this.main.GraphicsDevice.Viewport;
				this.draw(time, new Point(vp.Width, vp.Height));
			}
		}

		void IDrawablePreFrameComponent.DrawPreFrame(GameTime time, RenderParameters parameters)
		{
			if (this.needResize)
				this.resize();

			if (this.RenderTarget.Value != null)
			{
				this.main.GraphicsDevice.SetRenderTarget(this.RenderTarget);
				this.main.GraphicsDevice.Clear(this.RenderTargetBackground);
				this.draw(time, this.RenderTargetSize);
			}
		}

		public override void delete()
		{
			base.delete();
			if (this.RenderTarget.Value != null)
			{
				this.RenderTarget.Value.Dispose();
				this.RenderTarget.Value = null;
			}
			this.main.GraphicsDevice.DeviceReset -= this.deviceReset;
			this.main.RemoveComponent(this.Root);
		}
	}
}
