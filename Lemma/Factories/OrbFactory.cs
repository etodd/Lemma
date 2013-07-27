using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class OrbFactory : Factory
	{
		public OrbFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Orb");

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.IsInstanced.Value = false;
			model.Scale.Value = new Vector3(0.25f);
			model.Editable = false;
			result.Add("Model", model);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>("PointLight");
			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new TwoWayBinding<Vector3>(light.Position, transform.Position));

			Property<Entity.Handle> map = result.GetOrMakeProperty<Entity.Handle>("Map");
			Property<Map.Coordinate> lastCoord = result.GetOrMakeProperty<Map.Coordinate>("LastCoord");
			Property<float> blend = result.GetOrMakeProperty<float>("CoordBlend");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("Coord");
			Property<Direction> direction = result.GetOrMakeProperty<Direction>("Direction");
			ListProperty<Map.Coordinate> history = result.GetOrMakeListProperty<Map.Coordinate>("History");

			AI.Task findMap =
			new AI.Task
			{
				Action = delegate()
				{
					Entity mapEntity = map.Value.Target;
					if (mapEntity == null || !mapEntity.Active)
					{
						// Find closest map
						int closest = 10;
						Map.Coordinate newCoord = default(Map.Coordinate);
						foreach (Map m in Map.ActiveMaps)
						{
							Map.Coordinate mCoord = m.GetCoordinate(transform.Position);
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
							result.Delete.Execute();
						else
						{
							map.Value = mapEntity;
							coord.Value = lastCoord.Value = newCoord;
							blend.Value = 1.0f;
						}
					}
				},
				Interval = 0.25f,
			};

			AI ai = result.GetOrCreate<AI>("AI");
			ai.Add(new AI.State
			{
				Name = "Idle",
				Tasks = new[]
				{ 
					findMap,
					new AI.Task
					{
						Action = delegate()
						{
							Entity mapEntity = map.Value.Target;
							if (mapEntity != null && mapEntity.Active)
							{
								Map m = mapEntity.Get<Map>();
								const float speed = 8.0f; // cells per second
								blend.Value += main.ElapsedTime * speed;
								if (blend > 1.0f)
								{
									blend.Value = 0.0f;
									Map.Coordinate c = coord.Value;
									lastCoord.Value = c;

									bool[, ,] cells = new bool[3, 3, 3];

									// Negative X
									cells[0, 0, 1] = m[c.X - 1, c.Y - 1, c.Z + 0].ID != 0;
									cells[0, 1, 1] = m[c.X - 1, c.Y + 0, c.Z + 0].ID != 0;
									cells[0, 2, 1] = m[c.X - 1, c.Y + 1, c.Z + 0].ID != 0;

									cells[0, 1, 0] = m[c.X - 1, c.Y + 0, c.Z - 1].ID != 0;
									cells[0, 1, 2] = m[c.X - 1, c.Y + 0, c.Z + 1].ID != 0;
									
									// Positive X
									cells[2, 0, 1] = m[c.X + 1, c.Y - 1, c.Z + 0].ID != 0;
									cells[2, 1, 1] = m[c.X + 1, c.Y + 0, c.Z + 0].ID != 0;
									cells[2, 2, 1] = m[c.X + 1, c.Y + 1, c.Z + 0].ID != 0;

									cells[2, 1, 0] = m[c.X + 1, c.Y + 0, c.Z - 1].ID != 0;
									cells[2, 1, 2] = m[c.X + 1, c.Y + 0, c.Z + 1].ID != 0;

									// Negative Y
									cells[1, 0, 0] = m[c.X + 0, c.Y - 1, c.Z - 1].ID != 0;
									cells[1, 0, 1] = m[c.X + 0, c.Y - 1, c.Z + 0].ID != 0;
									cells[1, 0, 2] = m[c.X + 0, c.Y - 1, c.Z + 1].ID != 0;

									cells[0, 0, 1] = m[c.X - 1, c.Y - 1, c.Z + 0].ID != 0;
									cells[2, 0, 1] = m[c.X + 1, c.Y - 1, c.Z + 0].ID != 0;

									// Positive Y
									cells[1, 2, 0] = m[c.X + 0, c.Y + 1, c.Z - 1].ID != 0;
									cells[1, 2, 1] = m[c.X + 0, c.Y + 1, c.Z + 0].ID != 0;
									cells[1, 2, 2] = m[c.X + 0, c.Y + 1, c.Z + 1].ID != 0;

									cells[0, 2, 1] = m[c.X - 1, c.Y + 1, c.Z + 0].ID != 0;
									cells[2, 2, 1] = m[c.X + 1, c.Y + 1, c.Z + 0].ID != 0;

									// Negative Z
									cells[0, 1, 0] = m[c.X - 1, c.Y + 0, c.Z - 1].ID != 0;
									cells[1, 1, 0] = m[c.X + 0, c.Y + 0, c.Z - 1].ID != 0;
									cells[2, 1, 0] = m[c.X + 1, c.Y + 0, c.Z - 1].ID != 0;

									cells[1, 0, 0] = m[c.X + 0, c.Y - 1, c.Z - 1].ID != 0;
									cells[1, 2, 0] = m[c.X + 0, c.Y + 1, c.Z - 1].ID != 0;

									// Positive Z
									cells[0, 1, 2] = m[c.X - 1, c.Y + 0, c.Z + 1].ID != 0;
									cells[1, 1, 2] = m[c.X + 0, c.Y + 0, c.Z + 1].ID != 0;
									cells[2, 1, 2] = m[c.X + 1, c.Y + 0, c.Z + 1].ID != 0;

									cells[1, 0, 2] = m[c.X + 0, c.Y - 1, c.Z + 1].ID != 0;
									cells[1, 2, 2] = m[c.X + 0, c.Y + 1, c.Z + 1].ID != 0;

									List<Direction> directions = new List<Direction>();

									bool xMiddle = cells[1, 0, 1]
										|| cells[1, 2, 1]
										|| cells[1, 1, 0]
										|| cells[1, 1, 2];

									if (!cells[0, 1, 1]
									&& (
										xMiddle
										|| cells[0, 0, 1]
										|| cells[0, 2, 1]
										|| cells[0, 1, 0]
										|| cells[0, 1, 2]
									))
										directions.Add(Direction.NegativeX);

									if (!cells[2, 1, 1]
									&& (
										xMiddle
										|| cells[2, 0, 1]
										|| cells[2, 2, 1]
										|| cells[2, 1, 0]
										|| cells[2, 1, 2]
									))
										directions.Add(Direction.PositiveX);

									bool yMiddle = cells[1, 1, 0]
										|| cells[1, 1, 2]
										|| cells[0, 1, 1]
										|| cells[2, 1, 1];

									if (!cells[1, 0, 1]
									&& (
										yMiddle
										|| cells[1, 0, 0]
										|| cells[1, 0, 2]
										|| cells[0, 0, 1]
										|| cells[2, 0, 1]
									))
										directions.Add(Direction.NegativeY);

									if (!cells[1, 2, 1]
									&& (
										yMiddle
										|| cells[1, 2, 0]
										|| cells[1, 2, 2]
										|| cells[0, 2, 1]
										|| cells[2, 2, 1]
									))
										directions.Add(Direction.PositiveY);

									bool zMiddle = cells[0, 1, 1]
										|| cells[2, 1, 1]
										|| cells[1, 0, 1]
										|| cells[1, 2, 1];

									if (!cells[1, 1, 0]
									&& (
										zMiddle
										|| cells[0, 1, 0]
										|| cells[2, 1, 0]
										|| cells[1, 0, 0]
										|| cells[1, 2, 0]
									))
										directions.Add(Direction.NegativeZ);

									if (!cells[1, 1, 2]
									&& (
										zMiddle
										|| cells[0, 1, 2]
										|| cells[2, 1, 2]
										|| cells[1, 0, 2]
										|| cells[1, 2, 2]
									))
										directions.Add(Direction.PositiveZ);

									if (directions.Count == 0)
									{
										result.Delete.Execute();
										return;
									}

									Entity player = main.Get("Player").FirstOrDefault();
									Vector3 target = Vector3.Zero;
									if (player != null)
										target = player.Get<Transform>().Position - transform.Position.Value;

									int randomness = 2;
									if (player == null || target.Length() > 15)
										randomness = 6;

									if (!directions.Contains(direction) || new Random().Next(randomness) == 0)
									{
										bool randomDirection = false;
										Direction randomDirectionOtherThan = Direction.None;

										if (player == null)
											randomDirection = true;
										else
										{
											Direction closestDir = Direction.None;
											float closestDot = -2.0f;
											foreach (Direction dir in directions)
											{
												float dot = Vector3.Dot(m.GetAbsoluteVector(dir.GetVector()), target);
												if (dot > closestDot)
												{
													closestDir = dir;
													closestDot = dot;
												}
											}

											Map.Coordinate nextCoord = c.Move(closestDir);
											if (history.Contains(nextCoord))
											{
												randomDirection = true;
												randomDirectionOtherThan = closestDir;
											}
											else
												direction.Value = closestDir;
										}

										if (randomDirection)
										{
											if (directions.Count == 1)
												direction.Value = directions[0];
											else
											{
												while (true)
												{
													Direction dir = directions[new Random().Next(directions.Count)];
													if (dir != randomDirectionOtherThan)
													{
														direction.Value = dir;
														break;
													}
												}
											}
										}

									}

									coord.Value = c.Move(direction);
									history.Add(coord);

									if (history.Count > 4)
										history.RemoveAt(0);
								}

								Vector3 last = m.GetAbsolutePosition(lastCoord), current = m.GetAbsolutePosition(coord);
								transform.Position.Value = Vector3.Lerp(last, current, blend);
							}
						},
					}
				}
			});

			Model model = result.Get<Model>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Add(new Binding<Vector3>(model.Color, light.Color));

			this.SetMain(result, main);
		}
	}
}
