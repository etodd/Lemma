using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public static class AngleTools
	{
		const float twoPi = (float)Math.PI * 2.0f;
		const float pi = (float)Math.PI;
		// Converts the given angle (in radians) to the range (-pi, pi)
		public static float ToAngleRange(this float angle)
		{
			while (angle > pi)
				angle -= twoPi;
			while (angle < -pi)
				angle += twoPi;
			return angle;
		}

		public static float ClosestAngle(this float angle, float other)
		{
			while (angle > other + pi)
				angle -= twoPi;
			while (angle < other - pi)
				angle += twoPi;
			return angle;
		}
	}
}
