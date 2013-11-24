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
	public class UIRenderer : Component, IUpdateableComponent, INonPostProcessedDrawableComponent
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

		public UIRenderer()
		{
			this.DrawOrder = new Property<int> { Editable = false, Value = 0 };
			this.RasterizerState = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
			this.Root = new RootUIComponent(this);
			this.Root.AnchorPoint.Value = Vector2.Zero;
			this.Serialize = false;
		}

		public override void LoadContent(bool reload)
		{
			base.LoadContent(reload);
			this.Batch = new SpriteBatch(this.main.GraphicsDevice);
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
				MouseState current = this.main.MouseState, last = this.main.LastMouseState;
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
				}
			}
			this.Root.CheckLayout();
		}

		void INonPostProcessedDrawableComponent.DrawNonPostProcessed(GameTime time, RenderParameters parameters)
		{
			RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
			Point screenSize = this.main.ScreenSize;
			this.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, this.RasterizerState, null, Matrix.Identity);
			this.Root.Draw(time, Matrix.Identity, new Rectangle(0, 0, screenSize.X, screenSize.Y));
			this.Batch.End();
			this.main.GraphicsDevice.RasterizerState = originalState;
		}

		protected override void delete()
		{
			base.delete();
			this.main.RemoveComponent(this.Root);
		}
	}
}
