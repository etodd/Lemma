using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class VoxelChaseAI : Component, IUpdateableComponent
	{
		private Random random = new Random();

		public enum Cell
		{
			Empty, Penetrable, Filled, Avoid
		}

		private static Cell filter(Map.CellState state)
		{
			return state.ID == 0 ? Cell.Empty : Cell.Filled;
		}

		private static bool penetrable(Cell cell)
		{
			return cell == Cell.Penetrable || cell == Cell.Empty;
		}

		private static bool supported(Cell cell)
		{
			return cell == Cell.Penetrable || cell == Cell.Filled;
		}

		public Property<Entity.Handle> Map = new Property<Entity.Handle> { Editable = false };
		public Property<Map.Coordinate> LastCoord = new Property<Map.Coordinate> { Editable = false };
		public Property<float> Blend = new Property<float> { Editable = false };
		public Property<Map.Coordinate> Coord = new Property<Map.Coordinate> { Editable = false };
		public Property<Direction> Direction = new Property<Direction> { Editable = false };
		public ListProperty<Map.Coordinate> History = new ListProperty<Map.Coordinate> { Editable = false };
		public Property<bool> EnablePathfinding = new Property<bool> { Value = true, Editable = false };
		public Property<float> Speed = new Property<float> { Value = 8.0f, Editable = false };

		[XmlIgnore]
		public Func<Map.CellState, Cell> Filter = VoxelChaseAI.filter;
		[XmlIgnore]
		public Command<Map, Map.Coordinate> Moved = new Command<Map, Map.Coordinate>();

		public Property<bool> TargetActive = new Property<bool> { Editable = false };
		public Property<Vector3> Target = new Property<Vector3> { Editable = false };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };

		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
			this.Serialize = true;
		}

		private class AStarEntry
		{
			public AStarEntry Parent;
			public Map.Box Box;
			public int G;
			public float F;
			public int BoxSize;
			public int PathIndex;
		}

		private static Map.Box reconstructPath(AStarEntry entry, out int length)
		{
			length = 0;
			while (entry.PathIndex > 2)
			{
				entry = entry.Parent;
				length++;
			}
			return entry.Box;
		}

		public static Map.Box AStar(Map m, Map.Box start, Vector3 target, out int pathLength)
		{
			Dictionary<Map.Box, int> closed = new Dictionary<Map.Box, int>();

			PriorityQueue<AStarEntry> queue = new PriorityQueue<AStarEntry>(new LambdaComparer<AStarEntry>((x, y) => x.F.CompareTo(y.F)));

			Dictionary<Map.Box, AStarEntry> queueLookup = new Dictionary<Map.Box, AStarEntry>();

			AStarEntry startEntry = new AStarEntry
			{
				Parent = null,
				Box = start,
				G = 0,
				F = (target - start.GetCenter()).Length(),
				BoxSize = Math.Max(start.Width, Math.Max(start.Height, start.Depth)),
				PathIndex = 0,
			};
			queue.Push(startEntry);
			queueLookup[start] = startEntry;

			const int iterationLimit = 20;

			int iteration = 0;
			while (queue.Count > 0)
			{
				AStarEntry entry = queue.Pop();

				if (iteration >= iterationLimit || entry.F < entry.BoxSize)
					return VoxelChaseAI.reconstructPath(entry, out pathLength);

				iteration++;

				queueLookup.Remove(entry.Box);

				closed[entry.Box] = entry.G;
				lock (entry.Box.Adjacent)
				{
					foreach (Map.Box adjacent in entry.Box.Adjacent)
					{
						if (adjacent == null)
							continue;

						int boxSize = (int)((adjacent.Width + adjacent.Height + adjacent.Depth) / 3.0f);

						int tentativeGScore = entry.G + boxSize;

						int previousGScore;
						bool hasPreviousGScore = closed.TryGetValue(adjacent, out previousGScore);

						if (hasPreviousGScore && tentativeGScore > previousGScore)
							continue;

						AStarEntry alreadyInQueue;
						bool throwaway = queueLookup.TryGetValue(adjacent, out alreadyInQueue);

						if (alreadyInQueue == null || tentativeGScore < previousGScore)
						{
							AStarEntry newEntry = alreadyInQueue != null ? alreadyInQueue : new AStarEntry();

							newEntry.Parent = entry;
							newEntry.G = tentativeGScore;
							newEntry.F = tentativeGScore + (target - adjacent.GetCenter()).Length();
							newEntry.PathIndex = entry.PathIndex + 1;

							if (alreadyInQueue == null)
							{
								newEntry.Box = adjacent;
								newEntry.BoxSize = boxSize;
								queue.Push(newEntry);
								queueLookup[adjacent] = newEntry;
							}
						}
					}
				}
			}
			pathLength = 0;
			return null;
		}

		public void Update(float dt)
		{
			const int historySize = 5;

			Entity mapEntity = this.Map.Value.Target;
			if (mapEntity == null || !mapEntity.Active)
			{
				// Find closest map
				int closest = 10;
				Map.Coordinate newCoord = default(Map.Coordinate);
				foreach (Map m in Lemma.Components.Map.ActiveMaps)
				{
					Map.Coordinate mCoord = m.GetCoordinate(this.Position);
					Map.Coordinate? c = m.FindClosestFilledCell(mCoord, closest);
					if (c.HasValue)
					{
						mapEntity = m.Entity;
						Map.Coordinate cValue = c.Value;

						Direction dir = DirectionExtensions.GetDirectionFromVector(new Vector3(mCoord.X - cValue.X, mCoord.Y - cValue.Y, mCoord.Z - cValue.Z));
						newCoord = cValue.Move(dir);
						closest = Math.Min(Math.Abs(mCoord.X - cValue.X), Math.Min(Math.Abs(mCoord.Y - cValue.Y), Math.Abs(mCoord.Z - cValue.Z)));
					}
				}
				if (mapEntity == null)
					this.Delete.Execute();
				else
				{
					this.Map.Value = mapEntity;
					this.Coord.Value = this.LastCoord.Value = newCoord;
					this.Blend.Value = 1.0f;
				}
			}

			if (mapEntity != null && mapEntity.Active)
			{
				Map m = mapEntity.Get<Map>();
				this.Blend.Value += dt * this.Speed;
				if (this.Blend > 1.0f)
				{
					this.Blend.Value = 0.0f;
					Map.Coordinate c = this.Coord.Value;

					this.Moved.Execute(m, c);

					this.LastCoord.Value = c;

					if (this.EnablePathfinding)
					{
						Cell[, ,] cells = new Cell[3, 3, 3];

						// Negative X
						cells[0, 0, 1] = this.Filter(m[c.X - 1, c.Y - 1, c.Z + 0]);
						cells[0, 1, 1] = this.Filter(m[c.X - 1, c.Y + 0, c.Z + 0]);
						cells[0, 2, 1] = this.Filter(m[c.X - 1, c.Y + 1, c.Z + 0]);

						cells[0, 1, 0] = this.Filter(m[c.X - 1, c.Y + 0, c.Z - 1]);
						cells[0, 1, 2] = this.Filter(m[c.X - 1, c.Y + 0, c.Z + 1]);

						// Positive X
						cells[2, 0, 1] = this.Filter(m[c.X + 1, c.Y - 1, c.Z + 0]);
						cells[2, 1, 1] = this.Filter(m[c.X + 1, c.Y + 0, c.Z + 0]);
						cells[2, 2, 1] = this.Filter(m[c.X + 1, c.Y + 1, c.Z + 0]);

						cells[2, 1, 0] = this.Filter(m[c.X + 1, c.Y + 0, c.Z - 1]);
						cells[2, 1, 2] = this.Filter(m[c.X + 1, c.Y + 0, c.Z + 1]);

						// Negative Y
						cells[1, 0, 0] = this.Filter(m[c.X + 0, c.Y - 1, c.Z - 1]);
						cells[1, 0, 1] = this.Filter(m[c.X + 0, c.Y - 1, c.Z + 0]);
						cells[1, 0, 2] = this.Filter(m[c.X + 0, c.Y - 1, c.Z + 1]);

						cells[0, 0, 1] = this.Filter(m[c.X - 1, c.Y - 1, c.Z + 0]);
						cells[2, 0, 1] = this.Filter(m[c.X + 1, c.Y - 1, c.Z + 0]);

						// Positive Y
						cells[1, 2, 0] = this.Filter(m[c.X + 0, c.Y + 1, c.Z - 1]);
						cells[1, 2, 1] = this.Filter(m[c.X + 0, c.Y + 1, c.Z + 0]);
						cells[1, 2, 2] = this.Filter(m[c.X + 0, c.Y + 1, c.Z + 1]);

						cells[0, 2, 1] = this.Filter(m[c.X - 1, c.Y + 1, c.Z + 0]);
						cells[2, 2, 1] = this.Filter(m[c.X + 1, c.Y + 1, c.Z + 0]);

						// Negative Z
						cells[0, 1, 0] = this.Filter(m[c.X - 1, c.Y + 0, c.Z - 1]);
						cells[1, 1, 0] = this.Filter(m[c.X + 0, c.Y + 0, c.Z - 1]);
						cells[2, 1, 0] = this.Filter(m[c.X + 1, c.Y + 0, c.Z - 1]);

						cells[1, 0, 0] = this.Filter(m[c.X + 0, c.Y - 1, c.Z - 1]);
						cells[1, 2, 0] = this.Filter(m[c.X + 0, c.Y + 1, c.Z - 1]);

						// Positive Z
						cells[0, 1, 2] = this.Filter(m[c.X - 1, c.Y + 0, c.Z + 1]);
						cells[1, 1, 2] = this.Filter(m[c.X + 0, c.Y + 0, c.Z + 1]);
						cells[2, 1, 2] = this.Filter(m[c.X + 1, c.Y + 0, c.Z + 1]);

						cells[1, 0, 2] = this.Filter(m[c.X + 0, c.Y - 1, c.Z + 1]);
						cells[1, 2, 2] = this.Filter(m[c.X + 0, c.Y + 1, c.Z + 1]);

						List<Direction> directions = new List<Direction>();

						bool xMiddle = supported(cells[1, 0, 1])
							|| supported(cells[1, 2, 1])
							|| supported(cells[1, 1, 0])
							|| supported(cells[1, 1, 2]);

						if (penetrable(cells[0, 1, 1])
						&& (
							xMiddle
							|| supported(cells[0, 0, 1])
							|| supported(cells[0, 2, 1])
							|| supported(cells[0, 1, 0])
							|| supported(cells[0, 1, 2])
						))
							directions.Add(Lemma.Util.Direction.NegativeX);

						if (penetrable(cells[2, 1, 1])
						&& (
							xMiddle
							|| supported(cells[2, 0, 1])
							|| supported(cells[2, 2, 1])
							|| supported(cells[2, 1, 0])
							|| supported(cells[2, 1, 2])
						))
							directions.Add(Lemma.Util.Direction.PositiveX);

						bool yMiddle = supported(cells[1, 1, 0])
							|| supported(cells[1, 1, 2])
							|| supported(cells[0, 1, 1])
							|| supported(cells[2, 1, 1]);

						if (penetrable(cells[1, 0, 1])
						&& (
							yMiddle
							|| supported(cells[1, 0, 0])
							|| supported(cells[1, 0, 2])
							|| supported(cells[0, 0, 1])
							|| supported(cells[2, 0, 1])
						))
							directions.Add(Lemma.Util.Direction.NegativeY);

						if (penetrable(cells[1, 2, 1])
						&& (
							yMiddle
							|| supported(cells[1, 2, 0])
							|| supported(cells[1, 2, 2])
							|| supported(cells[0, 2, 1])
							|| supported(cells[2, 2, 1])
						))
							directions.Add(Lemma.Util.Direction.PositiveY);

						bool zMiddle = supported(cells[0, 1, 1])
							|| supported(cells[2, 1, 1])
							|| supported(cells[1, 0, 1])
							|| supported(cells[1, 2, 1]);

						if (penetrable(cells[1, 1, 0])
						&& (
							zMiddle
							|| supported(cells[0, 1, 0])
							|| supported(cells[2, 1, 0])
							|| supported(cells[1, 0, 0])
							|| supported(cells[1, 2, 0])
						))
							directions.Add(Lemma.Util.Direction.NegativeZ);

						if (penetrable(cells[1, 1, 2])
						&& (
							zMiddle
							|| supported(cells[0, 1, 2])
							|| supported(cells[2, 1, 2])
							|| supported(cells[1, 0, 2])
							|| supported(cells[1, 2, 2])
						))
							directions.Add(Lemma.Util.Direction.PositiveZ);

						if (directions.Count == 0)
						{
							this.Delete.Execute();
							return;
						}

						Vector3 toTarget = this.Target - this.Position.Value;
						// The higher the number, the less likely we are to change direction
						int oddsOfChangingDirection = 2;
						float distanceToTarget = toTarget.Length();
						if (!this.TargetActive)
							oddsOfChangingDirection = 6;

						if (!directions.Contains(this.Direction) || this.random.Next(oddsOfChangingDirection) == 0)
						{
							bool randomDirection = false;
							Direction randomDirectionOtherThan = Lemma.Util.Direction.None;

							if (!this.TargetActive)
								randomDirection = true;
							else
							{
								if (distanceToTarget > 5)
								{
									Direction supportedDirection = Lemma.Util.Direction.None;
									foreach (Direction dir in DirectionExtensions.Directions)
									{
										Map.Coordinate cellLookup = dir.GetCoordinate();
										if (supported(cells[cellLookup.X + 1, cellLookup.Y + 1, cellLookup.Z + 1]))
										{
											supportedDirection = dir;
											break;
										}
									}

									if (supportedDirection != Lemma.Util.Direction.None)
									{
										Map.Box box = m.GetBox(c.Move(supportedDirection));

										int pathLength;
										box = VoxelChaseAI.AStar(m, box, this.Target, out pathLength);

										if (pathLength > 1)
											toTarget = m.GetAbsolutePosition(box.GetCenter()) - this.Position;
									}
								}
								
								Direction closestDir = Lemma.Util.Direction.None;
								float closestDot = -2.0f;
								foreach (Direction dir in directions)
								{
									float dot = Vector3.Dot(m.GetAbsoluteVector(dir.GetVector()), toTarget);
									if (dot > closestDot)
									{
										closestDir = dir;
										closestDot = dot;
									}
								}

								Map.Coordinate nextCoord = c.Move(closestDir);
								if (this.History.Contains(nextCoord))
								{
									randomDirection = true;
									randomDirectionOtherThan = closestDir;
								}
								else
									this.Direction.Value = closestDir;
							}

							if (randomDirection)
							{
								if (directions.Count == 1)
									this.Direction.Value = directions[0];
								else
								{
									while (true)
									{
										Direction dir = directions[new Random().Next(directions.Count)];
										if (dir != randomDirectionOtherThan)
										{
											this.Direction.Value = dir;
											break;
										}
									}
								}
							}

						}

						this.Coord.Value = c.Move(this.Direction);
					}

					this.History.Add(this.Coord);

					while (this.History.Count > historySize)
						this.History.RemoveAt(0);
				}

				Vector3 last = m.GetAbsolutePosition(this.LastCoord), current = m.GetAbsolutePosition(this.Coord);
				this.Position.Value = Vector3.Lerp(last, current, this.Blend);
			}
		}
	}
}
