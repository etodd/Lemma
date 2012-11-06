using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public static class RectangleExtensions
	{
		public static Rectangle Create(Vector2 min, Vector2 max)
		{
			return new Rectangle((int)Math.Round(min.X), (int)Math.Round(min.Y), (int)Math.Round(max.X - min.X), (int)Math.Round(max.Y - min.Y));
		}

		public static Rectangle Intersect(this Rectangle self, Rectangle other)
		{
			int x = Math.Max(self.X, other.X);
			int y = Math.Max(self.Y, other.Y);
			return new Rectangle(x, y, Math.Min(self.X + self.Width, other.X + other.Width) - x, Math.Min(self.Y + self.Height, other.Y + other.Height) - y);
		}

		public static Rectangle Union(this Rectangle self, Rectangle other)
		{
			int x = Math.Min(self.X, other.X);
			int y = Math.Min(self.Y, other.Y);
			return new Rectangle(x, y, Math.Max(self.X + self.Width, other.X + other.Width) - x, Math.Max(self.Y + self.Height, other.Y + other.Height) - y);
		}
	}
}
