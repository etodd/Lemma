using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public static class BoundingBoxExtensions
	{
		public static BoundingBox Transform(this BoundingBox box, Matrix matrix)
		{
			Vector3[] corners = box.GetCorners();
			Vector3.Transform(corners, ref matrix, corners);
			box.Min = new Vector3(float.MaxValue);
			box.Max = new Vector3(float.MinValue);
			foreach (Vector3 corner in corners)
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
