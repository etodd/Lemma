using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework.Audio;

namespace Lemma.Factories
{
	public class LevitatorFactory : Factory<Main>
	{
		private Random random = new Random();

		public LevitatorFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Levitator");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>("PointLight");
			light.Serialize = false;

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			// TODO: Figure out Wwise volume parameter
			/*
			const float defaultVolume = 0.5f;
			volume.Value = defaultVolume;
			*/

			AI ai = result.GetOrCreate<AI>();

			ModelAlpha model = result.GetOrCreate<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "Models\\alpha-box";
			model.Editable = false;
			model.Serialize = false;
			model.DrawOrder.Value = 15;

			const float defaultModelScale = 1.0f;
			model.Scale.Value = new Vector3(defaultModelScale);

			model.Add(new Binding<Vector3, string>(model.Color, delegate(string state)
			{
				switch (state)
				{
					case "Alert":
						return new Vector3(1.5f, 1.5f, 0.5f);
					case "Chase":
						return new Vector3(1.5f, 0.5f, 0.5f);
					case "Levitating":
						return new Vector3(2.0f, 1.0f, 0.5f);
					default:
						return new Vector3(1.0f, 1.0f, 1.0f);
				}
			}, ai.CurrentState));

			result.Add(new Updater
			{
				delegate(float dt)
				{
					float source = 1.0f + ((float)this.random.NextDouble() - 0.5f) * 2.0f * 0.05f;
					model.Scale.Value = new Vector3(defaultModelScale * source);
					light.Attenuation.Value = defaultLightAttenuation * source;
				}
			});

			light.Add(new Binding<Vector3>(light.Color, model.Color));

			Agent agent = result.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAI raycastAI = result.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Matrix>(transform.Orientation, raycastAI.Orientation));

			Property<int> operationalRadius = result.GetOrMakeProperty<int>("OperationalRadius", true, 100);

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (transform.Position.Value - main.Camera.Position).Length() < operationalRadius;
					if (shouldBeActive && ai.CurrentState == "Suspended")
						ai.CurrentState.Value = "Idle";
					else if (!shouldBeActive && ai.CurrentState != "Suspended")
						ai.CurrentState.Value = "Suspended";
				},
			};

			AI.Task updatePosition = new AI.Task
			{
				Action = delegate()
				{
					raycastAI.Update();
				},
			};

			ai.Add(new AI.State
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, },
			});

			const float sightDistance = 30.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.State
			{
				Name = "Idle",
				Enter = delegate(AI.State previous)
				{
					//pitch.Value = -0.5f;
				},
				Exit = delegate(AI.State next)
				{
					//pitch.Value = 0.0f;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					updatePosition,
					new AI.Task
					{
						Interval = 1.0f,
						Action = delegate()
						{
							raycastAI.Move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
						}
					},
					new AI.Task
					{
						Interval = 1.0f,
						Action = delegate()
						{
							Agent a = Agent.Query(transform.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
							if (a != null)
								ai.CurrentState.Value = "Alert";
						},
					},
				},
			});

			Property<Entity.Handle> targetAgent = result.GetOrMakeProperty<Entity.Handle>("TargetAgent");

			ai.Add(new AI.State
			{
				Name = "Alert",
				Enter = delegate(AI.State previous)
				{
					//volume.Value = 0.0f;
				},
				Exit = delegate(AI.State next)
				{
					//volume.Value = defaultVolume;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					updatePosition,
					new AI.Task
					{
						Interval = 1.0f,
						Action = delegate()
						{
							if (ai.TimeInCurrentState > 3.0f)
								ai.CurrentState.Value = "Idle";
							else
							{
								Agent a = Agent.Query(transform.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
								if (a != null)
								{
									targetAgent.Value = a.Entity;
									ai.CurrentState.Value = "Chase";
								}
							}
						},
					},
				},
			});

			AI.Task checkTargetAgent = new AI.Task
			{
				Action = delegate()
				{
					Entity target = targetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						targetAgent.Value = null;
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			// Levitate

			Property<Entity.Handle> levitatingMap = result.GetOrMakeProperty<Entity.Handle>("LevitatingMap");
			Property<Map.Coordinate> grabCoord = result.GetOrMakeProperty<Map.Coordinate>("GrabCoord");

			const int levitateRipRadius = 4;

			Func<bool> tryLevitate = delegate()
			{
				Map map = raycastAI.Map.Value.Target.Get<Map>();
				Map.Coordinate? candidate = map.FindClosestFilledCell(raycastAI.Coord, 3);

				if (!candidate.HasValue)
					return false;

				Map.Coordinate center = candidate.Value;
				if (!map[center].Permanent)
				{
					// Break off a chunk of this map into a new DynamicMap.

					List<Map.Coordinate> edges = new List<Map.Coordinate>();

					Map.Coordinate ripStart = center.Move(-levitateRipRadius, -levitateRipRadius, -levitateRipRadius);
					Map.Coordinate ripEnd = center.Move(levitateRipRadius, levitateRipRadius, levitateRipRadius);

					Dictionary<Map.Box, bool> permanentBoxes = new Dictionary<Map.Box, bool>();
					foreach (Map.Coordinate c in ripStart.CoordinatesBetween(ripEnd))
					{
						Map.Box box = map.GetBox(c);
						if (box != null && box.Type.Permanent)
							permanentBoxes[box] = true;
					}

					foreach (Map.Box b in permanentBoxes.Keys)
					{
						// Top and bottom
						for (int x = b.X - 1; x <= b.X + b.Width; x++)
						{
							for (int z = b.Z - 1; z <= b.Z + b.Depth; z++)
							{
								Map.Coordinate coord = new Map.Coordinate { X = x, Y = b.Y + b.Height, Z = z };
								if (coord.Between(ripStart, ripEnd))
									edges.Add(coord);

								coord = new Map.Coordinate { X = x, Y = b.Y - 1, Z = z };
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
								Map.Coordinate coord = new Map.Coordinate { X = b.X - 1, Y = y, Z = z };
								if (coord.Between(ripStart, ripEnd))
									edges.Add(coord);

								coord = new Map.Coordinate { X = b.X + b.Width, Y = y, Z = z };
								if (coord.Between(ripStart, ripEnd))
									edges.Add(coord);
							}

							// Backward and forward
							for (int x = b.X; x < b.X + b.Width; x++)
							{
								Map.Coordinate coord = new Map.Coordinate { X = x, Y = y, Z = b.Z - 1 };
								if (coord.Between(ripStart, ripEnd))
									edges.Add(coord);

								coord = new Map.Coordinate { X = x, Y = y, Z = b.Z + b.Depth };
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
							edges.Add(new Map.Coordinate { X = x, Y = ripStart.Y, Z = z });
							edges.Add(new Map.Coordinate { X = x, Y = ripEnd.Y, Z = z });
						}
					}

					// Sides
					for (int y = ripStart.Y + 1; y <= ripEnd.Y - 1; y++)
					{
						// Left and right
						for (int z = ripStart.Z; z <= ripEnd.Z; z++)
						{
							edges.Add(new Map.Coordinate { X = ripStart.X, Y = y, Z = z });
							edges.Add(new Map.Coordinate { X = ripEnd.X, Y = y, Z = z });
						}

						// Backward and forward
						for (int x = ripStart.X; x <= ripEnd.X; x++)
						{
							edges.Add(new Map.Coordinate { X = x, Y = y, Z = ripStart.Z });
							edges.Add(new Map.Coordinate { X = x, Y = y, Z = ripEnd.Z });
						}
					}

					map.Empty(edges);
					map.Regenerate(delegate(List<DynamicMap> spawnedMaps)
					{
						foreach (DynamicMap spawnedMap in spawnedMaps)
						{
							if (spawnedMap[center].ID != 0)
							{
								levitatingMap.Value = spawnedMap.Entity;
								AkSoundEngine.PostEvent("Play_infected_shatter", result);
								break;
							}
						}
					});

					grabCoord.Value = center;
					return true;
				}
				return false;
			};

			Action delevitateMap = delegate()
			{
				Entity levitatingMapEntity = levitatingMap.Value.Target;
				if (levitatingMapEntity == null || !levitatingMapEntity.Active)
					return;

				DynamicMap dynamicMap = levitatingMapEntity.Get<DynamicMap>();

				int maxDistance = levitateRipRadius + 7;
				Map closestMap = null;
				Map.Coordinate closestCoord = new Map.Coordinate();
				foreach (Map m in Map.ActivePhysicsMaps)
				{
					if (m == dynamicMap)
						continue;

					Map.Coordinate relativeCoord = m.GetCoordinate(dynamicMap.Transform.Value.Translation);
					Map.Coordinate? closestFilled = m.FindClosestFilledCell(relativeCoord, maxDistance);
					if (closestFilled != null)
					{
						maxDistance = Math.Min(Math.Abs(relativeCoord.X - closestFilled.Value.X), Math.Min(Math.Abs(relativeCoord.Y - closestFilled.Value.Y), Math.Abs(relativeCoord.Z - closestFilled.Value.Z)));
						closestMap = m;
						closestCoord = closestFilled.Value;
					}
				}
				if (closestMap != null)
				{
					// Combine this map with the other one

					Direction x = closestMap.GetRelativeDirection(dynamicMap.GetAbsoluteVector(Vector3.Right));
					Direction y = closestMap.GetRelativeDirection(dynamicMap.GetAbsoluteVector(Vector3.Up));
					Direction z = closestMap.GetRelativeDirection(dynamicMap.GetAbsoluteVector(Vector3.Backward));

					if (x.IsParallel(y))
						x = y.Cross(z);
					else if (y.IsParallel(z))
						y = x.Cross(z);

					Map.Coordinate offset = new Map.Coordinate();
					float closestCoordDistance = float.MaxValue;
					Vector3 closestCoordPosition = closestMap.GetAbsolutePosition(closestCoord);
					foreach (Map.Coordinate c in dynamicMap.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()))
					{
						float distance = (dynamicMap.GetAbsolutePosition(c) - closestCoordPosition).LengthSquared();
						if (distance < closestCoordDistance)
						{
							closestCoordDistance = distance;
							offset = c;
						}
					}
					Vector3 toLevitatingMap = dynamicMap.Transform.Value.Translation - closestMap.GetAbsolutePosition(closestCoord);
					offset = offset.Move(dynamicMap.GetRelativeDirection(-toLevitatingMap));

					Matrix orientation = dynamicMap.Transform.Value;
					orientation.Translation = Vector3.Zero;

					EffectBlockFactory blockFactory = Factory.Get<EffectBlockFactory>();

					int index = 0;
					foreach (Map.Coordinate c in dynamicMap.Chunks.SelectMany(c => c.Boxes).SelectMany(b => b.GetCoords()).OrderBy(c2 => new Vector3(c2.X - offset.X, c2.Y - offset.Y, c2.Z - offset.Z).LengthSquared()))
					{
						Map.Coordinate offsetFromCenter = c.Move(-offset.X, -offset.Y, -offset.Z);
						Map.Coordinate targetCoord = new Map.Coordinate();
						targetCoord.SetComponent(x, offsetFromCenter.GetComponent(Direction.PositiveX));
						targetCoord.SetComponent(y, offsetFromCenter.GetComponent(Direction.PositiveY));
						targetCoord.SetComponent(z, offsetFromCenter.GetComponent(Direction.PositiveZ));
						targetCoord = targetCoord.Move(closestCoord.X, closestCoord.Y, closestCoord.Z);
						if (closestMap[targetCoord].ID == 0)
						{
							Entity block = blockFactory.CreateAndBind(main);
							c.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
							block.GetProperty<Vector3>("Offset").Value = closestMap.GetRelativePosition(targetCoord);
							block.GetProperty<bool>("Scale").Value = false;
							block.GetProperty<Vector3>("StartPosition").Value = dynamicMap.GetAbsolutePosition(c);
							block.GetProperty<Matrix>("StartOrientation").Value = orientation;
							block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * 0.0075f);
							blockFactory.Setup(block, closestMap.Entity, targetCoord, c.Data.ID);
							main.Add(block);
							index++;
						}
					}

					// Delete the map
					levitatingMapEntity.Delete.Execute();
				}
			};

			// Chase AI state

			ai.Add(new AI.State
			{
				Name = "Chase",
				Tasks = new[]
				{
					checkOperationalRadius,
					checkTargetAgent,
					new AI.Task
					{
						Interval = 0.35f,
						Action = delegate()
						{
							raycastAI.Move(targetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
						}
					},
					updatePosition,
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							Entity target = targetAgent.Value.Target;
							Vector3 targetPosition = target.Get<Transform>().Position;
							Entity levitatingMapEntity = levitatingMap.Value.Target;
							if ((targetPosition - transform.Position).Length() < 10.0f && (levitatingMapEntity == null || !levitatingMapEntity.Active))
							{
								if (tryLevitate())
									ai.CurrentState.Value = "Levitating";
							}
						}
					}
				},
			});

			Property<Vector3> lastPosition = result.GetOrMakeProperty<Vector3>("LastPosition");
			Property<Vector3> nextPosition = result.GetOrMakeProperty<Vector3>("NextPosition");
			Property<float> positionBlend = result.GetOrMakeProperty<float>("PositionBlend");

			Action findNextPosition = delegate()
			{
				lastPosition.Value = transform.Position.Value;
				nextPosition.Value = targetAgent.Value.Target.Get<Transform>().Position + new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble(), (float)this.random.NextDouble() - 0.5f) * 5.0f;
				positionBlend.Value = 0.0f;
			};

			ai.Add(new AI.State
			{
				Name = "Levitating",
				Enter = delegate(AI.State previous)
				{
					findNextPosition();
				},
				Exit = delegate(AI.State next)
				{
					delevitateMap();
					levitatingMap.Value = null;

					Map map = raycastAI.Map.Value.Target.Get<Map>();
					Map.Coordinate currentCoord = map.GetCoordinate(transform.Position);
					Map.Coordinate? closest = map.FindClosestFilledCell(currentCoord, 10);
					if (closest.HasValue)
						raycastAI.MoveTo(closest.Value);
					//volume.Value = defaultVolume;
					//pitch.Value = 0.0f;
				},
				Tasks = new[]
				{ 
					checkTargetAgent,
					new AI.Task
					{
						Action = delegate()
						{
							//volume.Value = 1.0f;
							//pitch.Value = 1.0f;
							Entity levitatingMapEntity = levitatingMap.Value.Target;
							if (levitatingMapEntity == null || !levitatingMapEntity.Active || ai.TimeInCurrentState.Value > 8.0f)
							{
								ai.CurrentState.Value = "Alert";
								return;
							}

							DynamicMap dynamicMap = levitatingMapEntity.Get<DynamicMap>();

							positionBlend.Value += (main.ElapsedTime.Value / 1.0f);
							if (positionBlend > 1.0f)
								findNextPosition();

							transform.Position.Value = Vector3.Lerp(lastPosition, nextPosition, positionBlend);

							Vector3 grabPoint = dynamicMap.GetAbsolutePosition(grabCoord);
							Vector3 diff = transform.Position.Value - grabPoint;
							if (diff.Length() > 15.0f)
							{
								ai.CurrentState.Value = "Chase";
								return;
							}

							diff *= (float)Math.Sqrt(dynamicMap.PhysicsEntity.Mass) * 0.5f;
							dynamicMap.PhysicsEntity.ApplyImpulse(ref grabPoint, ref diff);
						},
					},
				},
			});


			this.SetMain(result, main);
		}
	}
}
