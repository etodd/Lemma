using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Lemma.Factories;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public class VoxelRip
	{
		private static Random random = new Random();

		public static bool Go(Voxel voxel, Voxel.Coord center, int radius, Action<List<DynamicVoxel>> callback = null)
		{
			if (!voxel[center].Permanent)
			{
				// Break off a chunk of this voxel into a new DynamicMap.

				List<Voxel.Coord> edges = new List<Voxel.Coord>();

				Voxel.Coord ripStart = center.Move(-radius, -radius, -radius);
				Voxel.Coord ripEnd = center.Move(radius, radius, radius);

				Dictionary<Voxel.Box, bool> permanentBoxes = new Dictionary<Voxel.Box, bool>();
				foreach (Voxel.Coord c in ripStart.CoordinatesBetween(ripEnd))
				{
					Voxel.Box box = voxel.GetBox(c);
					if (box != null && box.Type.Permanent)
						permanentBoxes[box] = true;
				}

				foreach (Voxel.Box b in permanentBoxes.Keys)
				{
					// Top and bottom
					for (int x = b.X - 1; x <= b.X + b.Width; x++)
					{
						for (int z = b.Z - 1; z <= b.Z + b.Depth; z++)
						{
							Voxel.Coord coord = new Voxel.Coord { X = x, Y = b.Y + b.Height, Z = z };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);

							coord = new Voxel.Coord { X = x, Y = b.Y - 1, Z = z };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);
						}
					}

					// Outer shell
					for (int y = b.Y; y < b.Y + b.Height; y++)
					{
						// Left and right
						for (int z = b.Z - 1; z <= b.Z + b.Depth; z++)
						{
							Voxel.Coord coord = new Voxel.Coord { X = b.X - 1, Y = y, Z = z };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);

							coord = new Voxel.Coord { X = b.X + b.Width, Y = y, Z = z };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);
						}

						// Backward and forward
						for (int x = b.X; x < b.X + b.Width; x++)
						{
							Voxel.Coord coord = new Voxel.Coord { X = x, Y = y, Z = b.Z - 1 };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);

							coord = new Voxel.Coord { X = x, Y = y, Z = b.Z + b.Depth };
							if (coord.Between(ripStart, ripEnd))
								edges.Add(coord);
						}
					}
				}

				if (edges.Contains(center))
					return false;

				// Top and bottom
				for (int x = ripStart.X; x <= ripEnd.X; x++)
				{
					for (int z = ripStart.Z; z <= ripEnd.Z; z++)
					{
						Voxel.Coord c = new Voxel.Coord { X = x, Y = ripStart.Y, Z = z };
						Voxel.State s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
						c = new Voxel.Coord { X = x, Y = ripEnd.Y, Z = z };
						s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
					}
				}

				// Sides
				for (int y = ripStart.Y + 1; y <= ripEnd.Y - 1; y++)
				{
					// Left and right
					for (int z = ripStart.Z; z <= ripEnd.Z; z++)
					{
						Voxel.Coord c = new Voxel.Coord { X = ripStart.X, Y = y, Z = z };
						Voxel.State s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
						c = new Voxel.Coord { X = ripEnd.X, Y = y, Z = z };
						s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
					}

					// Backward and forward
					for (int x = ripStart.X; x <= ripEnd.X; x++)
					{
						Voxel.Coord c = new Voxel.Coord { X = x, Y = y, Z = ripStart.Z };
						Voxel.State s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
						c = new Voxel.Coord { X = x, Y = y, Z = ripEnd.Z };
						s = voxel[c];
						if (s != Voxel.States.Empty && !s.Permanent)
							edges.Add(c);
					}
				}

				Propagator p = WorldFactory.Instance.Get<Propagator>();
				foreach (Voxel.Coord c in edges)
					p.SparksLowPriority(voxel.GetAbsolutePosition(c), Propagator.Spark.Dangerous);

				voxel.Empty(edges);

				voxel.Regenerate(callback);
				return true;
			}
			return false;
		}

		public static void Consolidate(Main main, DynamicVoxel voxel, float interval = 1.0f)
		{
			int maxDistance = 12;
			Voxel closestVoxel = null;
			Voxel.Coord closestCoord = new Voxel.Coord();
			foreach (Voxel m in Voxel.ActivePhysicsVoxels)
			{
				if (m == voxel)
					continue;

				Voxel.Coord relativeCoord = m.GetCoordinate(voxel.Transform.Value.Translation);
				Voxel.Coord? closestFilled = m.FindClosestFilledCell(relativeCoord, maxDistance);
				if (closestFilled != null)
				{
					maxDistance = Math.Min(Math.Abs(relativeCoord.X - closestFilled.Value.X), Math.Min(Math.Abs(relativeCoord.Y - closestFilled.Value.Y), Math.Abs(relativeCoord.Z - closestFilled.Value.Z)));
					closestVoxel = m;
					closestCoord = closestFilled.Value;
				}
			}

			if (closestVoxel != null)
				VoxelRip.Consolidate(main, voxel, closestVoxel, closestCoord, interval);
		}
		
		public static void Consolidate(Main main, DynamicVoxel voxel, Voxel targetVoxel, Voxel.Coord targetCoord, float interval = 1.0f)
		{
			if (targetVoxel != null)
			{
				// Combine this map with the other one

				Direction x = targetVoxel.GetRelativeDirection(voxel.GetAbsoluteVector(Vector3.Right));
				Direction y = targetVoxel.GetRelativeDirection(voxel.GetAbsoluteVector(Vector3.Up));
				Direction z = targetVoxel.GetRelativeDirection(voxel.GetAbsoluteVector(Vector3.Backward));

				if (x.IsParallel(y))
					x = y.Cross(z);
				else if (y.IsParallel(z))
					y = x.Cross(z);

				Voxel.Coord offset = new Voxel.Coord();
				float closestCoordDistance = float.MaxValue;
				Vector3 closestCoordPosition = targetVoxel.GetAbsolutePosition(targetCoord);
				lock (voxel.MutationLock)
				{
					foreach (Voxel.Coord c in voxel.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()))
					{
						float distance = (voxel.GetAbsolutePosition(c) - closestCoordPosition).LengthSquared();
						if (distance < closestCoordDistance)
						{
							closestCoordDistance = distance;
							offset = c;
						}
					}
				}
				Vector3 toLevitatingMap = voxel.Transform.Value.Translation - targetVoxel.GetAbsolutePosition(targetCoord);
				offset = offset.Move(voxel.GetRelativeDirection(-toLevitatingMap));

				Quaternion orientation = Quaternion.CreateFromRotationMatrix(voxel.Transform.Value);

				EffectBlockFactory blockFactory = Factory.Get<EffectBlockFactory>();

				int index = 0;
				List<Voxel.Coord> coords;
				lock (voxel.MutationLock)
					coords = voxel.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()).ToList();
				Voxel.Coord camera = voxel.GetCoordinate(main.Camera.Position);
				foreach (Voxel.Coord c in coords.OrderBy(c2 => new Vector3(c2.X - camera.X, c2.Y - camera.Y, c2.Z - camera.Z).LengthSquared()))
				{
					Voxel.Coord offsetFromCenter = c.Move(-offset.X, -offset.Y, -offset.Z);
					Voxel.Coord targetCoord2 = new Voxel.Coord();
					targetCoord2.SetComponent(x, offsetFromCenter.GetComponent(Direction.PositiveX));
					targetCoord2.SetComponent(y, offsetFromCenter.GetComponent(Direction.PositiveY));
					targetCoord2.SetComponent(z, offsetFromCenter.GetComponent(Direction.PositiveZ));
					targetCoord2 = targetCoord2.Move(targetCoord.X, targetCoord.Y, targetCoord.Z);
					if (targetVoxel[targetCoord2].ID == 0)
					{
						Entity blockEntity = blockFactory.CreateAndBind(main);
						c.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
						EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
						effectBlock.Offset.Value = targetVoxel.GetRelativePosition(targetCoord2);
						effectBlock.DoScale = false;
						effectBlock.StartPosition = voxel.GetAbsolutePosition(c);
						effectBlock.StartOrientation = orientation;
						effectBlock.TotalLifetime = (0.05f + (index * 0.0075f)) * interval;
						effectBlock.Setup(targetVoxel.Entity, targetCoord2, c.Data.ID);
						main.Add(blockEntity);
						index++;
					}
				}

				// Delete the map
				voxel.Entity.Delete.Execute();
			}
		}
	}
}