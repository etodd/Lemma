using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Util
{
	public class VoxelAStar
	{
		private class BroadphaseEntry
		{
			public BroadphaseEntry Parent;
			public Voxel.Box Box;
			public int G;
			public float F;
			public int BoxSize;
		}

		private static void reconstructBroadphasePath(BroadphaseEntry entry, Stack<Voxel.Box> result)
		{
			while (entry != null)
			{
				result.Push(entry.Box);
				entry = entry.Parent;
			}
		}

		private static Dictionary<Voxel.Box, int> broadphaseClosed = new Dictionary<Voxel.Box,int>();
		private static PriorityQueue<BroadphaseEntry> broadphaseQueue = new PriorityQueue<BroadphaseEntry>(new LambdaComparer<BroadphaseEntry>((x, y) => x.F.CompareTo(y.F)));
		private static Dictionary<Voxel.Box, BroadphaseEntry> broadphaseQueueLookup = new Dictionary<Voxel.Box, BroadphaseEntry>();

		public static bool Broadphase(Voxel m, Voxel.Box start, Voxel.Coord target, Func<Voxel.State, bool> filter, Stack<Voxel.Box> result, int maxIterations = 50)
		{
			BroadphaseEntry closestEntry = null;
			bool found = false;
			lock (m.MutationLock)
			{
				Vector3 targetPos = m.GetRelativePosition(target);
				BroadphaseEntry startEntry = new BroadphaseEntry
				{
					Parent = null,
					Box = start,
					G = 0,
					F = (targetPos - start.GetCenter()).Length(),
					BoxSize = Math.Max(start.Width, Math.Max(start.Height, start.Depth)),
				};
				broadphaseQueue.Push(startEntry);
				broadphaseQueueLookup[start] = startEntry;

				float closestHeuristic = float.MaxValue;

				int iterations = 0;
				while (broadphaseQueue.Count > 0 && iterations < maxIterations)
				{
					iterations++;

					BroadphaseEntry entry = broadphaseQueue.Pop();

					if (entry.Box.Contains(target))
					{
						closestEntry = entry;
						found = true;
						break;
					}

					broadphaseQueueLookup.Remove(entry.Box);

					broadphaseClosed[entry.Box] = entry.G;
					lock (entry.Box.Adjacent)
					{
						for (int i = 0; i < entry.Box.Adjacent.Count; i++)
						{
							Voxel.Box adjacent = entry.Box.Adjacent[i];
							if (adjacent == null || !filter(adjacent.Type))
								continue;

							int boxSize = (int)((adjacent.Width + adjacent.Height + adjacent.Depth) / 3.0f);

							int tentativeGScore = entry.G + boxSize;

							int previousGScore;
							bool hasPreviousGScore = broadphaseClosed.TryGetValue(adjacent, out previousGScore);

							if (hasPreviousGScore && tentativeGScore > previousGScore)
								continue;

							BroadphaseEntry alreadyInQueue;
							broadphaseQueueLookup.TryGetValue(adjacent, out alreadyInQueue);

							if (alreadyInQueue == null || tentativeGScore < previousGScore)
							{
								BroadphaseEntry newEntry = alreadyInQueue != null ? alreadyInQueue : new BroadphaseEntry();

								newEntry.Parent = entry;
								newEntry.G = tentativeGScore;
								float heuristic = (targetPos - adjacent.GetCenter()).Length();
								newEntry.F = tentativeGScore + heuristic;

								if (heuristic < closestHeuristic)
								{
									closestEntry = newEntry;
									closestHeuristic = heuristic;
								}

								if (alreadyInQueue == null)
								{
									newEntry.Box = adjacent;
									newEntry.BoxSize = boxSize;
									broadphaseQueue.Push(newEntry);
									broadphaseQueueLookup[adjacent] = newEntry;
								}
							}
						}
					}
				}
			}
			broadphaseClosed.Clear();
			broadphaseQueue.Clear();
			broadphaseQueueLookup.Clear();
			if (closestEntry != null)
				VoxelAStar.reconstructBroadphasePath(closestEntry, result);
			return found;
		}

		private class NarrowphaseEntry
		{
			public NarrowphaseEntry Parent;
			public Voxel.Coord Coord;
			public int G;
			public float F;
		}

		private static void reconstructNarrowphasePath(NarrowphaseEntry entry, Stack<Voxel.Coord> result)
		{
			while (entry != null)
			{
				result.Push(entry.Coord);
				entry = entry.Parent;
			}
		}

		private static Dictionary<Voxel.Coord, int> narrowphaseClosed = new Dictionary<Voxel.Coord, int>();
		private static PriorityQueue<NarrowphaseEntry> narrowphaseQueue = new PriorityQueue<NarrowphaseEntry>(new LambdaComparer<NarrowphaseEntry>((x, y) => x.F.CompareTo(y.F)));
		private static Dictionary<Voxel.Coord, NarrowphaseEntry> narrowphaseQueueLookup = new Dictionary<Voxel.Coord, NarrowphaseEntry>();

		public static void Narrowphase(Voxel m, Voxel.Coord start, Voxel.Box target, Stack<Voxel.Coord> result)
		{
			Voxel.Box currentBox = m.GetBox(start);
			Vector3 targetPos = target.GetCenter();
			NarrowphaseEntry startEntry = new NarrowphaseEntry
			{
				Parent = null,
				Coord = start,
				G = 0,
				F = (targetPos - m.GetRelativePosition(start)).Length(),
			};
			narrowphaseQueue.Push(startEntry);
			narrowphaseQueueLookup[start] = startEntry;

			NarrowphaseEntry closestEntry = null;
			float closestHeuristic = float.MaxValue;

			int iterations = 0;
			while (narrowphaseQueue.Count > 0 && iterations < 80)
			{
				iterations++;

				NarrowphaseEntry entry = narrowphaseQueue.Pop();

				if (m.GetBox(entry.Coord) == target)
				{
					closestEntry = entry;
					break;
				}

				narrowphaseQueueLookup.Remove(entry.Coord);

				narrowphaseClosed[entry.Coord] = entry.G;
				for (int i = 0; i < 6; i++)
				{
					Voxel.Coord adjacent = entry.Coord.Move(DirectionExtensions.Directions[i]);
					if (!currentBox.Contains(adjacent) && !target.Contains(adjacent))
						continue;

					int tentativeGScore = entry.G + 1;

					int previousGScore;
					bool hasPreviousGScore = narrowphaseClosed.TryGetValue(adjacent, out previousGScore);

					if (hasPreviousGScore && tentativeGScore > previousGScore)
						continue;

					NarrowphaseEntry alreadyInQueue;
					narrowphaseQueueLookup.TryGetValue(adjacent, out alreadyInQueue);

					if (alreadyInQueue == null || tentativeGScore < previousGScore)
					{
						NarrowphaseEntry newEntry = alreadyInQueue != null ? alreadyInQueue : new NarrowphaseEntry();

						newEntry.Parent = entry;
						newEntry.G = tentativeGScore;
						float heuristic = (targetPos - m.GetRelativePosition(adjacent)).Length();
						newEntry.F = tentativeGScore + heuristic;

						if (heuristic < closestHeuristic)
						{
							closestEntry = newEntry;
							closestHeuristic = heuristic;
						}

						if (alreadyInQueue == null)
						{
							newEntry.Coord = adjacent;
							narrowphaseQueue.Push(newEntry);
							narrowphaseQueueLookup[adjacent] = newEntry;
						}
					}
				}
			}
			narrowphaseClosed.Clear();
			narrowphaseQueue.Clear();
			narrowphaseQueueLookup.Clear();
			if (closestEntry != null)
				VoxelAStar.reconstructNarrowphasePath(closestEntry, result);
		}

		// Search for nearby boxes that meet a filter criteria within a vaguely defined radius
		public static Voxel.Box BroadphaseSearch(Voxel v, Voxel.Coord coord, int radius, Func<Voxel.Box, bool> filter)
		{
			Queue<Voxel.Box> queue = new Queue<Voxel.Box>();
			Dictionary<Voxel.Box, int> visited = new Dictionary<Voxel.Box, int>();
			Voxel.Box startBox = v.GetBox(coord);
			queue.Enqueue(startBox);
			visited[startBox] = 0;
			int maxSearch = radius * radius * radius;
			int searchIndex = 0;
			while (queue.Count > 0 && searchIndex < maxSearch)
			{
				searchIndex++;
				Voxel.Box b = queue.Dequeue();
				lock (b.Adjacent)
				{
					int parentGScore = visited[b];
					for (int i = 0; i < b.Adjacent.Count; i++)
					{
						Voxel.Box adjacent = b.Adjacent[i];
						int tentativeGScore = parentGScore + adjacent.Width * adjacent.Height * adjacent.Depth;
						int previousGScore;
						if (!visited.TryGetValue(adjacent, out previousGScore) || tentativeGScore < previousGScore)
						{
							visited[adjacent] = tentativeGScore;

							if (parentGScore < radius * radius
								&& coord.X >= adjacent.X - radius && coord.X < adjacent.X + adjacent.Width + radius
								&& coord.Y >= adjacent.Y - radius && coord.Y < adjacent.Y + adjacent.Height + radius
								&& coord.Z >= adjacent.Z - radius && coord.Z < adjacent.Z + adjacent.Depth + radius)
							{
								if (filter(adjacent))
									return adjacent;
								else
									queue.Enqueue(adjacent);
							}
						}
					}
				}
			}
			return null;
		}
	}
}