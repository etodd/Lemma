using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class BlockPredictor : Component<Main>, IUpdateableComponent
	{
		public class Possibility
		{
			public Map Map;
			public Map.Coordinate StartCoord;
			public Map.Coordinate EndCoord;
			public ModelAlpha Model;
		}

		private class Prediction
		{
			public Vector3 Position;
			public float Time;
			public int Level;
		}

		// Input properties
		public Property<float> Rotation = new Property<float>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector3> FootPosition = new Property<Vector3>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<float> JumpSpeed = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();

		private const float blockPossibilityFadeInTime = 0.075f;
		private const float blockPossibilityTotalLifetime = 2.0f;
		private const float blockPossibilityInitialAlpha = 0.5f;

		private float blockPossibilityLifetime = 0.0f;

		private Dictionary<Map, List<Possibility>> possibilities = new Dictionary<Map, List<Possibility>>();
		
		public IEnumerable<Possibility> AllPossibilities
		{
			get
			{
				return this.possibilities.Values.SelectMany(x => x);
			}
		}

		private Map.CellState temporary;

		private static Direction[] platformBuildableDirections = DirectionExtensions.HorizontalDirections.Union(new[] { Direction.NegativeY }).ToArray();

		private ParticleSystem particleSystem;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
			this.temporary = Map.States[Map.t.Temporary];
			this.particleSystem = ParticleSystem.Get(main, "Distortion");
		}

		public void ClearPossibilities()
		{
			foreach (Possibility block in this.AllPossibilities)
				block.Model.Delete.Execute();
			this.possibilities.Clear();
		}

		public void AddPossibility(Possibility block)
		{
			if (block.Model == null)
			{
				Vector3 start = block.Map.GetRelativePosition(block.StartCoord), end = block.Map.GetRelativePosition(block.EndCoord);

				Vector3 scale = new Vector3(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y), Math.Abs(end.Z - start.Z));
				Matrix matrix = Matrix.CreateScale(scale) * Matrix.CreateTranslation(new Vector3(-0.5f) + (start + end) * 0.5f);

				ModelAlpha box = new ModelAlpha();
				box.Filename.Value = "Models\\distortion-box";
				box.Distortion.Value = true;
				box.Color.Value = new Vector3(2.8f, 3.0f, 3.2f);
				box.Alpha.Value = blockPossibilityInitialAlpha;
				box.Editable = false;
				box.Serialize = false;
				box.DrawOrder.Value = 11; // In front of water
				box.BoundingBox.Value = new BoundingBox(new Vector3(-0.5f), new Vector3(0.5f));
				box.GetVector3Parameter("Scale").Value = scale;
				box.Add(new Binding<Matrix>(box.Transform, x => matrix * x, block.Map.Transform));
				this.Entity.Add(box);
				block.Model = box;
			}

			List<Possibility> mapList;
			if (!this.possibilities.TryGetValue(block.Map, out mapList))
			{
				mapList = new List<Possibility>();
				possibilities[block.Map] = mapList;
			}
			mapList.Add(block);
			blockPossibilityLifetime = 0.0f;
		}

		public List<Possibility> GetPossibilities(Map m)
		{
			List<Possibility> result;
			this.possibilities.TryGetValue(m, out result);
			return result;
		}

		public void InstantiatePossibility(Possibility block)
		{
			block.Map.Empty(block.StartCoord.CoordinatesBetween(block.EndCoord), false, false);
			block.Map.Fill(block.StartCoord, block.EndCoord, this.temporary);
			block.Map.Regenerate();
			block.Model.Delete.Execute();
			List<Possibility> mapList = possibilities[block.Map];
			mapList.Remove(block);
			if (mapList.Count == 0)
				possibilities.Remove(block.Map);
			
			const float prePrime = 2.0f;
			// Front and back faces
			for (int x = block.StartCoord.X; x < block.EndCoord.X; x++)
			{
				for (int y = block.StartCoord.Y; y < block.EndCoord.Y; y++)
				{
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(x, y, block.EndCoord.Z - 1), Vector3.Zero, -1.0f, prePrime);
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(x, y, block.StartCoord.Z), Vector3.Zero, -1.0f, prePrime);
				}
			}

			// Left and right faces
			for (int z = block.StartCoord.Z; z < block.EndCoord.Z; z++)
			{
				for (int y = block.StartCoord.Y; y < block.EndCoord.Y; y++)
				{
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(block.StartCoord.X, y, z), Vector3.Zero, -1.0f, prePrime);
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(block.EndCoord.X - 1, y, z), Vector3.Zero, -1.0f, prePrime);
				}
			}

			// Top and bottom faces
			for (int z = block.StartCoord.Z; z < block.EndCoord.Z; z++)
			{
				for (int x = block.StartCoord.X; x < block.EndCoord.X; x++)
				{
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(x, block.StartCoord.Y, z), Vector3.Zero, -1.0f, prePrime);
					particleSystem.AddParticle(block.Map.GetAbsolutePosition(x, block.EndCoord.Y - 1, z), Vector3.Zero, -1.0f, prePrime);
				}
			}

			AkSoundEngine.PostEvent("Play_block_instantiate", 0.5f * (block.Map.GetAbsolutePosition(block.StartCoord) + block.Map.GetAbsolutePosition(block.EndCoord)));
		}

		// Function for finding a platform to build for the player
		public Possibility FindPlatform(Vector3 position)
		{
			const int searchDistance = 20;
			const int platformSize = 3;

			int shortestDistance = searchDistance;
			Direction relativeShortestDirection = Direction.None, absoluteShortestDirection = Direction.None;
			Map.Coordinate shortestCoordinate = new Map.Coordinate();
			Map shortestMap = null;

			EffectBlockFactory blockFactory = Factory.Get<EffectBlockFactory>();
			foreach (Map map in Map.ActivePhysicsMaps)
			{
				List<Matrix> results = new List<Matrix>();
				Map.Coordinate absolutePlayerCoord = map.GetCoordinate(position);
				bool inMap = map.GetChunk(absolutePlayerCoord, false) != null;
				foreach (Direction absoluteDir in platformBuildableDirections)
				{
					Map.Coordinate playerCoord = absoluteDir == Direction.NegativeY ? absolutePlayerCoord : map.GetCoordinate(position + new Vector3(0, platformSize / -2.0f, 0));
					Direction relativeDir = map.GetRelativeDirection(absoluteDir);
					if (!inMap && map.GetChunk(playerCoord.Move(relativeDir, searchDistance), false) == null)
						continue;

					for (int i = 1; i < shortestDistance; i++)
					{
						Map.Coordinate coord = playerCoord.Move(relativeDir, i);
						Map.CellState state = map[coord];

						if (state.ID != 0 || blockFactory.IsAnimating(new EffectBlockFactory.BlockEntry { Map = map, Coordinate = coord, }))
						{
							// Check we're not in a no-build zone
							if (state.ID != Map.t.AvoidAI && Zone.CanBuild(map.GetAbsolutePosition(coord)))
							{
								shortestDistance = i;
								relativeShortestDirection = relativeDir;
								absoluteShortestDirection = absoluteDir;
								shortestCoordinate = playerCoord;
								shortestMap = map;
							}
							break;
						}
					}
				}
			}

			if (shortestMap != null && shortestDistance > 1)
			{
				Direction yDir = relativeShortestDirection.IsParallel(Direction.PositiveY) ? Direction.PositiveX : Direction.PositiveY;
				Direction zDir = relativeShortestDirection.Cross(yDir);

				int initialOffset = absoluteShortestDirection == Direction.NegativeY ? 0 : -2;
				Map.Coordinate startCoord = shortestCoordinate.Move(relativeShortestDirection, initialOffset).Move(yDir, platformSize / -2).Move(zDir, platformSize / -2);
				Map.Coordinate endCoord = startCoord.Move(relativeShortestDirection, -initialOffset + shortestDistance).Move(yDir, platformSize).Move(zDir, platformSize);

				return new Possibility
				{
					Map = shortestMap,
					StartCoord = new Map.Coordinate { X = Math.Min(startCoord.X, endCoord.X), Y = Math.Min(startCoord.Y, endCoord.Y), Z = Math.Min(startCoord.Z, endCoord.Z) },
					EndCoord = new Map.Coordinate { X = Math.Max(startCoord.X, endCoord.X), Y = Math.Max(startCoord.Y, endCoord.Y), Z = Math.Max(startCoord.Z, endCoord.Z) },
				};
			}
			return null;
		}

		// Function for finding a wall to build for the player
		public Possibility FindWall(Vector3 position, Vector2 direction)
		{
			const int searchDistance = 20;
			const int additionalDistance = 6;

			Map shortestMap = null;
			Map.Coordinate shortestPlayerCoord = new Map.Coordinate();
			Direction shortestWallDirection = Direction.None;
			Direction shortestBuildDirection = Direction.None;
			int shortestDistance = searchDistance;

			EffectBlockFactory blockFactory = Factory.Get<EffectBlockFactory>();
			foreach (Map map in Map.ActivePhysicsMaps)
			{
				foreach (Direction absoluteWallDir in DirectionExtensions.HorizontalDirections)
				{
					Direction relativeWallDir = map.GetRelativeDirection(absoluteWallDir);
					Vector3 wallVector = map.GetAbsoluteVector(relativeWallDir.GetVector());
					float dot = Vector2.Dot(direction, Vector2.Normalize(new Vector2(wallVector.X, wallVector.Z)));
					if (dot > -0.25f && dot < 0.8f)
					{
						Map.Coordinate coord = map.GetCoordinate(position).Move(relativeWallDir, 2);
						foreach (Direction dir in DirectionExtensions.Directions.Where(x => x.IsPerpendicular(relativeWallDir)))
						{
							for (int i = 0; i < shortestDistance; i++)
							{
								Map.Coordinate c = coord.Move(dir, i);
								Map.CellState state = map[c];

								if (state.ID != 0 || blockFactory.IsAnimating(new EffectBlockFactory.BlockEntry { Map = map, Coordinate = c, }))
								{
									// Check we're not in a no-build zone
									if (state.ID != Map.t.AvoidAI && Zone.CanBuild(map.GetAbsolutePosition(c)))
									{
										shortestMap = map;
										shortestBuildDirection = dir;
										shortestWallDirection = relativeWallDir;
										shortestDistance = i;
										shortestPlayerCoord = coord;
									}

									break;
								}
							}
						}
					}
				}
			}

			if (shortestMap != null)
			{
				// Found something to build a wall on.
				Direction dirU = shortestBuildDirection;
				Direction dirV = dirU.Cross(shortestWallDirection);
				Map.Coordinate startCoord = shortestPlayerCoord.Move(dirU, shortestDistance).Move(dirV, additionalDistance);
				Map.Coordinate endCoord = shortestPlayerCoord.Move(dirU, -additionalDistance).Move(dirV, -additionalDistance).Move(shortestWallDirection);
				return new Possibility
				{
					Map = shortestMap,
					StartCoord = new Map.Coordinate { X = Math.Min(startCoord.X, endCoord.X), Y = Math.Min(startCoord.Y, endCoord.Y), Z = Math.Min(startCoord.Z, endCoord.Z) },
					EndCoord = new Map.Coordinate { X = Math.Max(startCoord.X, endCoord.X), Y = Math.Max(startCoord.Y, endCoord.Y), Z = Math.Max(startCoord.Z, endCoord.Z) },
				};
			}

			return null;
		}

		private float getPredictionInterval()
		{
			// Interval is the time in seconds between locations where we will check for buildable platforms
			return 0.3f * (8.0f / Math.Max(5.0f, this.LinearVelocity.Value.Length()));
		}

		private void predictJump(Queue<Prediction> predictions, Vector3 start, Vector3 v, float interval, int level)
		{
			for (float time = interval; time < (level == 0 ? 1.5f : 1.0f); time += interval)
				predictions.Enqueue(new Prediction { Position = start + (v * time) + (time * time * 0.5f * main.Space.ForceUpdater.Gravity), Time = time, Level = level });
		}

		private Vector3 startSlowMo(Queue<Prediction> predictions, float interval)
		{
			Vector3 straightAhead = Matrix.CreateRotationY(this.Rotation).Forward * -this.MaxSpeed;
			Vector3 velocity;
			Vector3 startPosition;

			if (this.IsSupported)
			{
				startPosition = this.FootPosition + straightAhead;

				velocity = straightAhead + new Vector3(0, this.JumpSpeed, 0);
			}
			else
			{
				startPosition = this.FootPosition;

				velocity = this.LinearVelocity;
				if (velocity.Length() < this.MaxSpeed * 0.25f)
					velocity += straightAhead * 0.5f;
			}

			this.predictJump(predictions, startPosition, velocity, interval, 0);

			Vector3 jumpVelocity = velocity;
			jumpVelocity.Y = this.JumpSpeed;

			return jumpVelocity;
		}

		public void PredictPlatforms()
		{
			float interval = this.getPredictionInterval();

			Queue<Prediction> predictions = new Queue<Prediction>();
			Vector3 jumpVelocity = this.startSlowMo(predictions, interval);

			float[] lastPredictionHit = new float[] { 0.0f, 0.0f };

			while (predictions.Count > 0)
			{
				Prediction prediction = predictions.Dequeue();

				if (prediction.Time > lastPredictionHit[prediction.Level] + (interval * 1.5f))
				{
					Possibility possibility = this.FindPlatform(prediction.Position);
					if (possibility != null)
					{
						lastPredictionHit[prediction.Level] = prediction.Time;
						this.AddPossibility(possibility);
						if (prediction.Level == 0)
							this.predictJump(predictions, prediction.Position, jumpVelocity, interval, prediction.Level + 1);
					}
				}
			}
		}

		public void PredictWalls()
		{
			// Predict block possibilities
			Queue<Prediction> predictions = new Queue<Prediction>();
			this.startSlowMo(predictions, this.getPredictionInterval());
			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);
			Vector2 direction = new Vector2(-rotationMatrix.Forward.X, -rotationMatrix.Forward.Z);

			while (predictions.Count > 0)
			{
				Prediction prediction = predictions.Dequeue();
				Possibility possibility = this.FindWall(prediction.Position, direction);
				if (possibility != null)
				{
					this.AddPossibility(possibility);
					break;
				}
			}
		}

		public void Update(float dt)
		{
			if (this.possibilities.Count > 0)
			{
				this.blockPossibilityLifetime += dt;
				if (this.blockPossibilityLifetime > blockPossibilityTotalLifetime)
					this.ClearPossibilities();
				else
				{
					float alpha;
					if (this.blockPossibilityLifetime < blockPossibilityFadeInTime)
						alpha = blockPossibilityInitialAlpha * (this.blockPossibilityLifetime / blockPossibilityFadeInTime);
					else
						alpha = blockPossibilityInitialAlpha * (1.0f - (this.blockPossibilityLifetime / blockPossibilityTotalLifetime));

					Vector3 offset = new Vector3(this.blockPossibilityLifetime * 0.2f);
					foreach (Possibility block in this.possibilities.Values.SelectMany(x => x))
					{
						block.Model.Alpha.Value = alpha;
						block.Model.GetVector3Parameter("Offset").Value = offset;
					}
				}
			}
		}
	}
}
