using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public enum VectorElement { X, Y, Z, W }
	public static class VectorExtensions
	{
		// returns Euler angles that point from one point to another
		public static Vector3 AngleTo(Vector3 from, Vector3 location)
		{
			Vector3 angle = new Vector3();
			Vector3 v3 = Vector3.Normalize(location - from);
			angle.X = (float)Math.Asin(v3.Y);
			angle.Y = (float)Math.Atan2(-v3.Z, -v3.X);
			return angle;
		}

		// converts a Quaternion to Euler angles (X = pitch, Y = yaw, Z = roll)
		public static Vector3 ToEuler(this Quaternion rotation)
		{
			Vector3 rotationaxes = new Vector3();

			Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
			Vector3 up = Vector3.Transform(Vector3.Up, rotation);
			rotationaxes = AngleTo(new Vector3(), forward);
			if (rotationaxes.X == MathHelper.PiOver2)
			{
				rotationaxes.Y = (float)Math.Atan2(up.Z, up.X);
				rotationaxes.Z = 0;
			}
			else if (rotationaxes.X == -MathHelper.PiOver2)
			{
				rotationaxes.Y = (float)Math.Atan2(-up.Z, -up.X);
				rotationaxes.Z = 0;
			}
			else
			{
				up = Vector3.Transform(up, Matrix.CreateRotationY(-rotationaxes.Y));
				up = Vector3.Transform(up, Matrix.CreateRotationX(-rotationaxes.X));
				rotationaxes.Z = (float)Math.Atan2(up.Y, -up.X);
			}
			return rotationaxes;
		}

		public static float GetElement(this Vector2 v, VectorElement element)
		{
			switch (element)
			{
				case VectorElement.X:
					return v.X;
				case VectorElement.Y:
					return v.Y;
				default:
					throw new Exception("Tried to get non-existent element from vector.");
			}
		}

		public static Vector2 SetElement(this Vector2 v, VectorElement element, float value)
		{
			switch (element)
			{
				case VectorElement.X:
					return new Vector2(value, v.Y);
				case VectorElement.Y:
					return new Vector2(v.X, value);
				default:
					throw new Exception("Tried to set non-existent element in vector.");
			}
		}

		public static float GetElement(this Vector3 v, VectorElement element)
		{
			switch (element)
			{
				case VectorElement.X:
					return v.X;
				case VectorElement.Y:
					return v.Y;
				case VectorElement.Z:
					return v.Z;
				default:
					throw new Exception("Tried to get non-existent element from vector.");
			}
		}

		public static Vector3 SetElement(this Vector3 v, VectorElement element, float value)
		{
			switch (element)
			{
				case VectorElement.X:
					return new Vector3(value, v.Y, v.Z);
				case VectorElement.Y:
					return new Vector3(v.X, value, v.Z);
				case VectorElement.Z:
					return new Vector3(v.X, v.Y, value);
				default:
					throw new Exception("Tried to set non-existent element in vector.");
			}
		}

		public static float GetElement(this Vector4 v, VectorElement element)
		{
			switch (element)
			{
				case VectorElement.X:
					return v.X;
				case VectorElement.Y:
					return v.Y;
				case VectorElement.Z:
					return v.Z;
				case VectorElement.W:
					return v.W;
				default:
					throw new Exception("Tried to get non-existent element from vector.");
			}
		}

		public static Vector4 SetElement(this Vector4 v, VectorElement element, float value)
		{
			switch (element)
			{
				case VectorElement.X:
					return new Vector4(value, v.Y, v.Z, v.W);
				case VectorElement.Y:
					return new Vector4(v.X, value, v.Z, v.W);
				case VectorElement.Z:
					return new Vector4(v.X, v.Y, value, v.W);
				case VectorElement.W:
					return new Vector4(v.X, v.Y, v.Z, value);
				default:
					throw new Exception("Tried to set non-existent element in vector.");
			}
		}

		public static float GetElement(this Quaternion v, VectorElement element)
		{
			switch (element)
			{
				case VectorElement.X:
					return v.X;
				case VectorElement.Y:
					return v.Y;
				case VectorElement.Z:
					return v.Z;
				case VectorElement.W:
					return v.W;
				default:
					throw new Exception("Tried to get non-existent element from vector.");
			}
		}

		public static Quaternion SetElement(this Quaternion v, VectorElement element, float value)
		{
			switch (element)
			{
				case VectorElement.X:
					return new Quaternion(value, v.Y, v.Z, v.W);
				case VectorElement.Y:
					return new Quaternion(v.X, value, v.Z, v.W);
				case VectorElement.Z:
					return new Quaternion(v.X, v.Y, value, v.W);
				case VectorElement.W:
					return new Quaternion(v.X, v.Y, v.Z, value);
				default:
					throw new Exception("Tried to set non-existent element in vector.");
			}
		}

		public static byte GetElement(this Color v, VectorElement element)
		{
			switch (element)
			{
				case VectorElement.X:
					return v.R;
				case VectorElement.Y:
					return v.G;
				case VectorElement.Z:
					return v.B;
				case VectorElement.W:
					return v.A;
				default:
					throw new Exception("Tried to get non-existent element from color.");
			}
		}

		public static Color SetElement(this Color v, VectorElement element, byte value)
		{
			switch (element)
			{
				case VectorElement.X:
					return new Color(value, v.G, v.B, v.A);
				case VectorElement.Y:
					return new Color(v.R, value, v.B, v.A);
				case VectorElement.Z:
					return new Color(v.R, v.G, value, v.A);
				case VectorElement.W:
					return new Color(v.R, v.G, v.B, value);
				default:
					throw new Exception("Tried to set non-existent element in color.");
			}
		}
	}
}
