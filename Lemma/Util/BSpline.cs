using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public class BSpline
	{
		public struct ControlPoint
		{
			public Vector3 Position;
			public Quaternion Orientation;
			public float Offset;
			public float FOVMultiplier;

			public static ControlPoint Lerp(ControlPoint a, ControlPoint b, float x)
			{
				ControlPoint result;
				result.Position = Vector3.Lerp(a.Position, b.Position, x);
				result.Orientation = Quaternion.Lerp(a.Orientation, b.Orientation, x);
				result.Offset = MathHelper.Lerp(a.Offset, b.Offset, x);
				result.FOVMultiplier = MathHelper.Lerp(a.FOVMultiplier, b.FOVMultiplier, x);
				return result;
			}
		}

		const int order = 2;
		private List<ControlPoint> points = new List<ControlPoint>();

		public float Duration;

		private float knot(int i)
		{
			return MathHelper.Clamp((float)i / (float)this.points.Count, 0.0f, 1.0f);
		}

		private ControlPoint deBoor(int k, int i, float x)
		{
			if (k == 0)
				return this.points[Math.Max(0, Math.Min(i + 1, this.points.Count - 1))];
			else
			{
				float pre = (x - this.knot(i)) / (this.knot(i + order + 1 - k) - this.knot(i));
				ControlPoint a = this.deBoor(k - 1, i - 1, x);
				ControlPoint b = this.deBoor(k - 1, i, x);
				return ControlPoint.Lerp(a, b, pre);
			}
		}

		public void Add(Vector3 pos, Quaternion q = default(Quaternion), float offset = 0.0f, float fovmultiplier = 1.0f)
		{
			this.points.Add(new ControlPoint { Position = pos, Orientation = q, Offset = offset, FOVMultiplier = fovmultiplier });
		}

		public ControlPoint Evaluate(float x)
		{
			ControlPoint result = default(ControlPoint);
			if (this.points.Count == 2)
				result = ControlPoint.Lerp(this.points[0], this.points[1], x);
			else
				result = this.deBoor(order, (int)(x * this.points.Count), x);
			return result;
		}
	}
}
