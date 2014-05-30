using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Util
{
	public enum Direction { PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ, None }

	public static class VectorDirectionExtensions
	{
		public static float GetComponent(this Vector3 vector, Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return -vector.X;
				case Direction.PositiveX:
					return vector.X;
				case Direction.NegativeY:
					return -vector.Y;
				case Direction.PositiveY:
					return vector.Y;
				case Direction.NegativeZ:
					return -vector.Z;
				case Direction.PositiveZ:
					return vector.Z;
				default:
					return 0;
			}
		}

		public static Vector3 SetComponent(this Vector3 vector, Direction dir, float value)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return new Vector3(-value, vector.Y, vector.Z);
				case Direction.PositiveX:
					return new Vector3(value, vector.Y, vector.Z);
				case Direction.NegativeY:
					return new Vector3(vector.X, -value, vector.Z);
				case Direction.PositiveY:
					return new Vector3(vector.X, value, vector.Z);
				case Direction.NegativeZ:
					return new Vector3(vector.X, vector.Y, -value);
				case Direction.PositiveZ:
					return new Vector3(vector.X, vector.Y, value);
				default:
					return vector;
			}
		}
	}

	public static class DirectionExtensions
	{
		public static Direction[] Directions = new Direction[] { Direction.PositiveX, Direction.NegativeX, Direction.PositiveY, Direction.NegativeY, Direction.PositiveZ, Direction.NegativeZ };
		public static Direction[] HorizontalDirections = new Direction[] { Direction.PositiveX, Direction.NegativeX, Direction.PositiveZ, Direction.NegativeZ };

		public static Direction GetDirectionFromVector(Vector2 dir)
		{
			if (Math.Abs(dir.X) > Math.Abs(dir.Y))
				return dir.X > 0.0f ? Direction.PositiveX : (dir.X < 0.0f ? Direction.NegativeX : Direction.None);
			else
				return dir.Y > 0.0f ? Direction.PositiveZ : (dir.Y < 0.0f ? Direction.NegativeZ : Direction.None);
		}

		public static bool IsNegative(this Direction dir)
		{
			return dir == Direction.NegativeX || dir == Direction.NegativeY || dir == Direction.NegativeZ;
		}

		public static Direction Cross(this Direction a, Direction b)
		{
			if (a.IsParallel(Direction.PositiveX) && b.IsParallel(Direction.PositiveY))
				return Direction.PositiveZ;
			else if (a.IsParallel(Direction.PositiveX) && b.IsParallel(Direction.PositiveZ))
				return Direction.PositiveY;
			else if (a.IsParallel(Direction.PositiveY) && b.IsParallel(Direction.PositiveX))
				return Direction.PositiveZ;
			else if (a.IsParallel(Direction.PositiveY) && b.IsParallel(Direction.PositiveZ))
				return Direction.PositiveX;
			else if (a.IsParallel(Direction.PositiveZ) && b.IsParallel(Direction.PositiveX))
				return Direction.PositiveY;
			else if (a.IsParallel(Direction.PositiveZ) && b.IsParallel(Direction.PositiveY))
				return Direction.PositiveX;
			return Direction.None;
		}

		public static Direction GetDirectionFromVector(Vector3 dir)
		{
			float dx = Math.Abs(dir.X), dy = Math.Abs(dir.Y), dz = Math.Abs(dir.Z);
			if (dx > dy && dx > dz)
				return dir.X > 0.0f ? Direction.PositiveX : Direction.NegativeX;
			else if (dy > dz)
				return dir.Y > 0.0f ? Direction.PositiveY : Direction.NegativeY;
			else
				return dir.Z > 0.0f ? Direction.PositiveZ : (dir.Z < 0.0f ? Direction.NegativeZ : Direction.None);
		}

		public static Direction GetDirectionFromName(string name)
		{
			switch (name)
			{
				case "NegativeX":
					return Direction.NegativeX;
				case "PositiveX":
					return Direction.PositiveX;
				case "NegativeY":
					return Direction.NegativeY;
				case "PositiveY":
					return Direction.PositiveY;
				case "NegativeZ":
					return Direction.NegativeZ;
				case "PositiveZ":
					return Direction.PositiveZ;
			}
			return Direction.None;
		}

		public static bool IsPositive(this Direction dir)
		{
			return dir == Direction.PositiveX || dir == Direction.PositiveY || dir == Direction.PositiveZ;
		}

		public static bool IsPerpendicular(this Direction a, Direction b)
		{
			return DirectionExtensions.isPerpendicular(a, b) || DirectionExtensions.isPerpendicular(b, a);
		}

		public static bool IsParallel(this Direction a, Direction b)
		{
			return !DirectionExtensions.IsPerpendicular(a, b);
		}

		public static bool IsOpposite(this Direction a, Direction b)
		{
			return b == a.GetReverse();
		}

		private static bool isPerpendicular(Direction a, Direction b)
		{
			return ((a == Direction.NegativeX || a == Direction.PositiveX) && !(b == Direction.NegativeX || b == Direction.PositiveX))
				|| ((a == Direction.NegativeY || a == Direction.PositiveY) && !(b == Direction.NegativeY || b == Direction.PositiveY))
				|| ((a == Direction.NegativeZ || a == Direction.PositiveZ) && !(b == Direction.NegativeZ || b == Direction.PositiveZ));
		}

		public static Vector3 GetVector(this Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return new Vector3(-1.0f, 0.0f, 0.0f);
				case Direction.PositiveX:
					return new Vector3(1.0f, 0.0f, 0.0f);
				case Direction.NegativeZ:
					return new Vector3(0.0f, 0.0f, -1.0f);
				case Direction.PositiveZ:
					return new Vector3(0.0f, 0.0f, 1.0f);
				case Direction.PositiveY:
					return new Vector3(0.0f, 1.0f, 0.0f);
				case Direction.NegativeY:
					return new Vector3(0.0f, -1.0f, 0.0f);
			}
			return Vector3.Zero;
		}

		public static Voxel.Coord GetCoordinate(this Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return new Voxel.Coord { X = -1, Y = 0, Z = 0 };
				case Direction.PositiveX:
					return new Voxel.Coord { X = 1, Y = 0, Z = 0 };
				case Direction.NegativeZ:
					return new Voxel.Coord { X = 0, Y = 0, Z = -1 };
				case Direction.PositiveZ:
					return new Voxel.Coord { X = 0, Y = 0, Z = 1 };
				case Direction.PositiveY:
					return new Voxel.Coord { X = 0, Y = 1, Z = 0 };
				case Direction.NegativeY:
					return new Voxel.Coord { X = 0, Y = -1, Z = 0 };
			}
			return new Voxel.Coord { X = 0, Y = 0, Z = 0 };
		}

		public static Direction RotateCounterClockwise(this Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return Direction.PositiveZ;
				case Direction.PositiveZ:
					return Direction.PositiveX;
				case Direction.PositiveX:
					return Direction.NegativeZ;
				case Direction.NegativeZ:
					return Direction.NegativeX;
			}
			return Direction.None;
		}

		public static Direction RotateClockwise(this Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return Direction.NegativeZ;
				case Direction.NegativeZ:
					return Direction.PositiveX;
				case Direction.PositiveX:
					return Direction.PositiveZ;
				case Direction.PositiveZ:
					return Direction.NegativeX;
			}
			return Direction.None;
		}

		public static Direction GetReverse(this Direction dir)
		{
			switch (dir)
			{
				case Direction.NegativeX:
					return Direction.PositiveX;
				case Direction.PositiveX:
					return Direction.NegativeX;
				case Direction.NegativeZ:
					return Direction.PositiveZ;
				case Direction.PositiveZ:
					return Direction.NegativeZ;
				case Direction.PositiveY:
					return Direction.NegativeY;
				case Direction.NegativeY:
					return Direction.PositiveY;
			}
			return Direction.None;
		}
	}
}
