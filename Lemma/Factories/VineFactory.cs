using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using System.Threading;

namespace Lemma.Factories
{
	public class VineFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Vine");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(0.5f, 1.3f, 0.5f);
			light.Attenuation.Value = 10.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			result.Add("Coordinate", new Property<Map.Coordinate> { Editable = false });

			result.Add("OperationalRadius", new Property<float> { Editable = true, Value = 100.0f });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			if (ParticleSystem.Get(main, "VineSparks") == null)
			{
				ParticleSystem.Add(main, "VineSparks",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\splash",
					MaxParticles = 1000,
					Duration = TimeSpan.FromSeconds(1.0f),
					MinHorizontalVelocity = -7.0f,
					MaxHorizontalVelocity = 7.0f,
					MinVerticalVelocity = 0.0f,
					MaxVerticalVelocity = 7.0f,
					Gravity = new Vector3(0.0f, -10.0f, 0.0f),
					MinRotateSpeed = -2.0f,
					MaxRotateSpeed = 2.0f,
					MinStartSize = 0.3f,
					MaxStartSize = 0.7f,
					MinEndSize = 0.0f,
					MaxEndSize = 0.0f,
					BlendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
					MinColor = new Vector4(2.0f, 2.0f, 2.0f, 1.0f),
					MaxColor = new Vector4(2.0f, 2.0f, 2.0f, 1.0f),
				});
			}

			result.CannotSuspendByDistance = true;
			Transform transform = result.Get<Transform>();
			PointLight light = result.Get<PointLight>();
			EnemyBase enemy = result.GetOrCreate<EnemyBase>("Base");

			Property<bool> hasCoordinate = result.GetOrMakeProperty<bool>("HasCoordinate");
			hasCoordinate.Editable = false;

			enemy.Add(new Binding<Matrix>(enemy.Transform, transform.Matrix));
			enemy.Add(new CommandBinding(enemy.Delete, result.Delete));

			Property<Map.Coordinate> coordinate = result.GetProperty<Map.Coordinate>("Coordinate");
			Property<float> operationalRadius = result.GetOrMakeProperty<float>("OperationalRadius", true, 100.0f);

			if (!hasCoordinate)
			{
				// Find our starting coordinate
				result.Add(new NotifyBinding(delegate()
				{
					coordinate.Value = enemy.BaseBoxes.First().GetCoords().First();
					hasCoordinate.Value = true;
				}, enemy.Map));
			}

			light.Add(new Binding<Vector3>(light.Position, enemy.Position));

			ListProperty<Map.Coordinate> path = result.GetOrMakeListProperty<Map.Coordinate>("PathCoordinates");

			Property<Entity.Handle> targetAgent = result.GetOrMakeProperty<Entity.Handle>("TargetAgent");

			AI ai = result.GetOrCreate<AI>("AI");

			Agent agent = result.GetOrCreate<Agent>("Agent");

			PointLight positionLight = null;
			Property<float> positionLightRadius = result.GetOrMakeProperty<float>("PositionLightRadius", true, 20.0f);
			if (!main.EditorEnabled)
			{
				positionLight = new PointLight();
				positionLight.Serialize = false;
				positionLight.Color.Value = new Vector3(1.5f, 0.5f, 0.5f);
				positionLight.Add(new Binding<float>(positionLight.Attenuation, positionLightRadius));
				positionLight.Shadowed.Value = false;
				positionLight.Add(new Binding<bool, string>(positionLight.Enabled, x => x != "Suspended", ai.CurrentState));
				positionLight.Add(new Binding<Vector3, string>(positionLight.Color, delegate(string state)
				{
					switch (state)
					{
						case "Chase":
						case "Crush":
							return new Vector3(1.5f, 0.5f, 0.5f);
						case "Alert":
							return new Vector3(1.5f, 1.5f, 0.5f);
						default:
							return new Vector3(0.5f, 1.3f, 0.5f);
					}
				}, ai.CurrentState));
				result.Add("PositionLight", positionLight);
				ParticleEmitter emitter = result.GetOrCreate<ParticleEmitter>("Particles");
				emitter.Editable = false;
				emitter.Serialize = false;
				emitter.ParticlesPerSecond.Value = 100;
				emitter.ParticleType.Value = "VineSparks";
				emitter.Add(new Binding<Vector3>(emitter.Position, positionLight.Position));
				emitter.Add(new Binding<bool, string>(emitter.Enabled, x => x != "Suspended", ai.CurrentState));

				agent.Add(new Binding<Vector3>(agent.Position, positionLight.Position));
				agent.Add(new Binding<float, string>(agent.Speed, delegate(string state)
				{
					switch (state)
					{
						case "Chase":
						case "Crush":
							return 10.0f;
						case "Alert":
							return 0.0f;
						default:
							return 5.0f;
					}
				}, ai.CurrentState));

			}
			
			Vector3 currentPosition = Vector3.Zero;

			AI.Task checkMap = new AI.Task
			{
				Action = delegate()
				{
					if (enemy.Map.Value.Target == null || !enemy.Map.Value.Target.Active)
						result.Delete.Execute();
					else
						currentPosition = positionLight.Position.Value = enemy.Map.Value.Target.Get<Map>().GetAbsolutePosition(coordinate);
				},
			};

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (currentPosition - main.Camera.Position).Length() < operationalRadius || (enemy.Map.Value.Target.Get<Map>().GetAbsolutePosition(enemy.BaseBoxes.First().GetCoords().First()) - main.Camera.Position).Length() < operationalRadius;
					if (shouldBeActive && ai.CurrentState == "Suspended")
						ai.CurrentState.Value = "Idle";
					else if (!shouldBeActive && ai.CurrentState != "Suspended")
						ai.CurrentState.Value = "Suspended";
				},
			};

			AI.Task checkTargetAgent = new AI.Task
			{
				Action = delegate()
				{
					Entity target = targetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						targetAgent.Value = null;
						ai.CurrentState.Value = "Alert";
					}
				},
			};

			Action<Vector3> updatePath = delegate(Vector3 target)
			{
				Map m = enemy.Map.Value.Target.Get<Map>();
				path.Clear();
				List<Map.Coordinate> calculatedPath = m.CustomAStar(coordinate, m.GetCoordinate(target));
				if (calculatedPath != null)
				{
					foreach (Map.Coordinate c in calculatedPath)
						path.Add(c);
				}
			};

			Action advancePath = delegate()
			{
				if (path.Count > 0)
				{
					coordinate.Value = path[0];
					path.RemoveAt(0);
					if (enemy.BaseBoxes.FirstOrDefault(x => x.Contains(coordinate)) == null)
					{
						Map m = enemy.Map.Value.Target.Get<Map>();
						bool regenerate = m.Empty(coordinate);
						regenerate |= m.Fill(coordinate, WorldFactory.StatesByName["Gravel"]);
						if (regenerate)
							m.Regenerate();
					}
					Sound.PlayCue(main, "VineMove", currentPosition);
				}
			};

			Action randomPath = delegate()
			{
				Random random = new Random();
				Vector3 goal = currentPosition;
				MapBoundaryFactory mapBoundaries = Factory.Get<MapBoundaryFactory>();
				while (true)
				{
					goal = currentPosition + new Vector3(((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f) * 15.0f;
					if (mapBoundaries.IsInsideMap(goal))
						break;
				}
				updatePath(goal);
			};

			IBinding cellEmptiedBinding = null;
			result.Add(new NotifyBinding(delegate()
			{
				if (cellEmptiedBinding != null)
					result.Remove(cellEmptiedBinding);
				cellEmptiedBinding = new CommandBinding<IEnumerable<Map.Coordinate>, Map>(enemy.Map.Value.Target.Get<Map>().CellsEmptied, delegate(IEnumerable<Map.Coordinate> coords, Map transferringToNewMap)
				{
					if (coords.Contains(coordinate))
						ai.CurrentState.Value = "Alert";
				});
				result.Add(cellEmptiedBinding);
			}, enemy.Map));

			ai.Setup
			(
				new AI.State
				{
					Name = "Suspended",
					Tasks = new[] { checkOperationalRadius },
				},
				new AI.State
				{
					Name = "Idle",
					Enter = delegate(AI.State oldState)
					{
						randomPath();
					},
					Tasks = new[]
					{
						checkMap,
						checkOperationalRadius,
						new AI.Task
						{
							Interval = 1.0f,
							Action = delegate()
							{
								Agent a = Agent.Query(currentPosition, 30.0f, 10.0f, agent);
								if (a != null)
									ai.CurrentState.Value = "Alert";
							},
						},
						new AI.Task
						{
							Interval = 3.0f,
							Action = randomPath,
						},
						new AI.Task
						{
							Interval = 0.15f,
							Action = advancePath,
						},
					},
				},
				new AI.State
				{
					Name = "Alert",
					Tasks = new[]
					{ 
						checkMap,
						checkOperationalRadius,
						new AI.Task
						{
							Interval = 1.0f,
							Action = delegate()
							{
								if (ai.TimeInCurrentState > 4.0f)
									ai.CurrentState.Value = "Idle";
								else
								{
									Agent a = Agent.Query(currentPosition, 30.0f, 10.0f, agent);
									if (a != null)
									{
										targetAgent.Value = a.Entity;
										ai.CurrentState.Value = "Chase";
									}
								}
							},
						},
					},
				},
				new AI.State
				{
					Name = "Chase",
					Enter = delegate(AI.State lastState)
					{
						updatePath(targetAgent.Value.Target.Get<Agent>().Position);
					},
					Tasks = new[]
					{
						checkMap,
						checkOperationalRadius,
						checkTargetAgent,
						new AI.Task
						{
							Interval = 0.75f,
							Action = delegate()
							{
								updatePath(targetAgent.Value.Target.Get<Agent>().Position);
							},
						},
						new AI.Task
						{
							Interval = 0.07f,
							Action = delegate()
							{
								Vector3 targetPosition = targetAgent.Value.Target.Get<Agent>().Position;
								float targetDistance = (targetPosition - currentPosition).Length();
								if (targetDistance > 30.0f || ai.TimeInCurrentState > 20.0f) // He got away
									ai.CurrentState.Value = "Alert";
								else if (targetDistance < 5.0f) // We got 'im
									ai.CurrentState.Value = "Crush";
								else
									advancePath();
							},
						},
					},
				},
				new AI.State
				{
					Name = "Crush",
					Enter = delegate(AI.State lastState)
					{
						// Set up cage
						Map.Coordinate center = enemy.Map.Value.Target.Get<Map>().GetCoordinate(targetAgent.Value.Target.Get<Agent>().Position);

						int radius = 1;

						// Bottom
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								path.Add(new Map.Coordinate { X = x, Y = center.Y - 4, Z = z });
						}

						// Outer shell
						radius = 2;
						for (int y = center.Y - 3; y <= center.Y + 3; y++)
						{
							// Left
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								path.Add(new Map.Coordinate { X = center.X - radius, Y = y, Z = z });

							// Right
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								path.Add(new Map.Coordinate { X = center.X + radius, Y = y, Z = z });

							// Backward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								path.Add(new Map.Coordinate { X = x, Y = y, Z = center.Z - radius });

							// Forward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								path.Add(new Map.Coordinate { X = x, Y = y, Z = center.Z + radius });
						}

						// Top
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								path.Add(new Map.Coordinate { X = x, Y = center.Y + 3, Z = z });
						}
					},
					Tasks = new[]
					{
						checkMap,
						checkOperationalRadius,
						checkTargetAgent,
						new AI.Task
						{
							Interval = 0.01f,
							Action = delegate()
							{
								Agent a = targetAgent.Value.Target.Get<Agent>();
								a.Health.Value -= 0.01f / 1.5f; // seconds to kill
								if (!a.Active)
									ai.CurrentState.Value = "Alert";
								else
								{
									if ((a.Position - currentPosition).Length() > 5.0f) // They're getting away
										ai.CurrentState.Value = "Chase";
									advancePath();
								}
							}
						}
					},
				}
			);

			EnemyBase.SpawnPickupsOnDeath(main, result);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			EnemyBase.AttachEditorComponents(result, main, this.Color);
		}
	}
}
