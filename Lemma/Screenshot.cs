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

				if (
#if VR
				!this.main.VR &&
#endif
				(this.Size.X > screenSize.X || this.Size.Y > screenSize.Y))
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

				if (
#if VR
				!this.main.VR &&
#endif
				(this.Size.X > screenSize.X || this.Size.Y > screenSize.Y))
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

		public static void SavePng(RenderTarget2D t, string path, int width = 0, int height = 0)
		{
			if (width == 0)
				width = t.Width;
			if (height == 0)
				height = t.Height;

			byte[] imageData = new byte[4 * t.Width * t.Height];
			t.GetData<byte>(imageData);
			// Flip R and B.
			for (int i = 0; i < imageData.Length; i+= 4)
			{
				byte tmp = imageData[i];
				imageData[i] = imageData[i + 2];
				imageData[i + 2] = tmp;
			}

			using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(t.Width, t.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			{
				System.Drawing.Imaging.BitmapData bmData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, t.Width, t.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
				IntPtr pnative = bmData.Scan0;
				System.Runtime.InteropServices.Marshal.Copy(imageData, 0, pnative, 4 * t.Width * t.Height);
				bitmap.UnlockBits(bmData);
				if (width != t.Width || height != t.Height)
				{
					using (System.Drawing.Bitmap resized = new System.Drawing.Bitmap(bitmap, width, height))
						resized.Save(path, System.Drawing.Imaging.ImageFormat.Png);
				}
				else
					bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
			}
		}
	}
}