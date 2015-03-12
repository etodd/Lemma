using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public static class BoundingBoxExtensions
	{
		private static Vector3[] cornerCache = new Vector3[8];
		public static BoundingBox Transform(this BoundingBox box, Matrix matrix)
		{
			cornerCache[0] = new Vector3(box.Min.X, box.Min.Y, box.Min.Z);
			cornerCache[1] = new Vector3(box.Min.X, box.Min.Y, box.Max.Z);
			cornerCache[2] = new Vector3(box.Min.X, box.Max.Y, box.Min.Z);
			cornerCache[3] = new Vector3(box.Min.X, box.Max.Y, box.Max.Z);
			cornerCache[4] = new Vector3(box.Max.X, box.Min.Y, box.Min.Z);
			cornerCache[5] = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);
			cornerCache[6] = new Vector3(box.Max.X, box.Max.Y, box.Min.Z);
			cornerCache[7] = new Vector3(box.Max.X, box.Max.Y, box.Max.Z);
			Vector3.Transform(cornerCache, ref matrix, cornerCache);
			box.Min = new Vector3(float.MaxValue);
			box.Max = new Vector3(float.MinValue);
			foreach (Vector3 corner in cornerCache)
			{
				box.Min.X = Math.Min(box.Min.X, corner.X);
				box.Min.Y = Math.Min(box.Min.Y, corner.Y);
				box.Min.Z = Math.Min(box.Min.Z, corner.Z);
				box.Max.X = Math.Max(box.Max.X, corner.X);
				box.Max.Y = Math.Max(box.Max.Y, corner.Y);
				box.Max.Z = Math.Max(box.Max.Z, corner.Z);
			}
			return box;
		}

		public static BoundingBox Expand(this BoundingBox box, float amount)
		{
			return new BoundingBox(box.Min - new Vector3(amount), box.Max + new Vector3(amount));
		}
	}
}
