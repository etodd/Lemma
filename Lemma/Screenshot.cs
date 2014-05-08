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
		public Point Size = Point.Zero;

		public void Clear()
		{
			if (this.Buffer != null)
				this.Buffer.Dispose();
			this.Buffer = null;
			this.Size = Point.Zero;
		}

		private Action callback;
		private bool take;

		public void Take(Action callback = null)
		{
			this.Size = this.main.ScreenSize;
			this.Buffer = new RenderTarget2D(this.main.GraphicsDevice, this.Size.X, this.Size.Y, false, SurfaceFormat.Color, DepthFormat.Depth16);
			this.main.RenderTarget = this.Buffer;
			this.take = true;
			this.callback = callback;
		}

		public void Update(float dt)
		{
			if (this.take)
			{
				this.main.RenderTarget = null;
				if (this.callback != null)
					this.callback();
				this.callback = null;
			}
		}
	}
}
