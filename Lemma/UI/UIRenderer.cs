using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class UIRenderer : Component, IUpdateableComponent, INonPostProcessedDrawableComponent, IDrawablePreFrameComponent
	{
		public class RootUIComponent : UIComponent
		{
			public RootUIComponent(UIRenderer renderer)
			{
				this.renderer = renderer;
			}
		}

		public Property<int> DrawOrder { get; set; }

		[XmlIgnore]
		public RasterizerState RasterizerState;

		[XmlIgnore]
		public SpriteBatch Batch;

		[XmlIgnore]
		public RootUIComponent Root;

		[XmlIgnore]
		public Command SwallowMouseEvents = new Command();

		public Property<bool> EnableMouse = new Property<bool> { Value = true };

		public Property<Point> RenderTargetSize = new Property<Point>();

		public Property<Color> RenderTargetBackground = new Property<Color>();

		[XmlIgnore]
		public Property<RenderTarget2D> RenderTarget = new Property<RenderTarget2D>();

		[XmlIgnore]
		public Func<MouseState, MouseState> MouseFilter = x => x;

		private MouseState lastMouseState;

		private void resize()
		{
			if (this.RenderTarget.Value != null)
				this.RenderTarget.Value.Dispose();
			this.RenderTarget.Value = null;

			Point size = this.RenderTargetSize;
			if (size.X > 0 && size.Y > 0)
				this.RenderTarget.Value = new RenderTarget2D(this.main.GraphicsDevice, size.X, size.Y);

			this.needResize = false;
		}

		public UIRenderer()
		{
			this.DrawOrder = new Property<int> { Editable = false, Value = 0 };
			this.RasterizerState = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
			this.Root = new RootUIComponent(this);
			this.Root.AnchorPoint.Value = Vector2.Zero;
			this.Serialize = false;
		}

		private bool needResize = false;
		public override void InitializeProperties()
		{
			this.Add(new NotifyBinding(delegate() { this.needResize = true; }, this.RenderTargetSize));
			this.Add(new NotifyBinding(delegate() { this.needResize = true; }, this.main.ScreenSize));
			this.lastMouseState = this.main.LastMouseState;
		}

		public override void LoadContent(bool reload)
		{
			this.Batch = new SpriteBatch(this.main.GraphicsDevice);
			this.needResize = true;
		}

		public override void SetMain(Main _main)
		{
			base.SetMain(_main);
			this.main.AddComponent(this.Root);
			this.Root.Add(new Binding<Vector2, Point>(this.Root.Size, x => new Vector2(x.X, x.Y), main.ScreenSize));
		}

		void IUpdateableComponent.Update(float dt)
		{
			if (this.main.IsActive && this.EnableMouse)
			{
				MouseState current = this.MouseFilter(this.main.MouseState), last = this.lastMouseState;
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
			this.Root.Draw(time, Matrix.Identity, new Rectangle(0, 0, screenSize.X, screenSize.Y));
			this.Batch.End();
			this.main.GraphicsDevice.RasterizerState = originalState;
		}

		void INonPostProcessedDrawableComponent.DrawNonPostProcessed(GameTime time, RenderParameters parameters)
		{
			if (this.RenderTarget.Value == null)
				this.draw(time, this.main.ScreenSize);
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

		protected override void delete()
		{
			base.delete();
			if (this.RenderTarget.Value != null)
			{
				this.RenderTarget.Value.Dispose();
				this.RenderTarget.Value = null;
			}
			this.main.RemoveComponent(this.Root);
		}
	}
}
