using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma
{
	public class Screenshot : Component<Main>, IUpdateableComponent
	{
		public RenderTarget2D Buffer = null;
		public Point Size { get; private set; }

		public void Clear()
		{
			if (this.Buffer != null)
				this.Buffer.Dispose();
			this.Buffer = null;
			this.Size = Point.Zero;
		}

		private Action callback;
		private bool take;

		public void Take(Point size, Action callback = null)
		{
			if (size.X > 0 && size.Y > 0)
			{
				this.Size = size;
				Point screenSize = this.main.ScreenSize;
				if (!this.main.VR && (this.Size.X > screenSize.X || this.Size.Y > screenSize.Y))
					this.main.Renderer.ReallocateBuffers(size);
				this.Buffer = new RenderTarget2D(this.main.GraphicsDevice, this.Size.X, this.Size.Y, false, SurfaceFormat.Color, DepthFormat.Depth16);
				this.main.RenderTarget = this.Buffer;
				this.take = true;
				this.callback = callback;
			}
			else if (callback != null)
				callback();
		}

		public void Update(float dt)
		{
			if (this.take)
			{
				this.main.RenderTarget = null;
				Point screenSize = this.main.ScreenSize;
				if (!this.main.VR && (this.Size.X > screenSize.X || this.Size.Y > screenSize.Y))
					this.main.Renderer.ReallocateBuffers(screenSize);
				if (this.callback != null)
					this.callback();
				this.callback = null;
			}
		}

		public override void delete()
		{
			base.delete();
			this.Clear();
		}
	}
}
