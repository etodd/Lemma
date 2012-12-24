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

			PointLight positionLight = null;
			if (!main.EditorEnabled)
			{
				positionLight = new PointLight();
				positionLight.Serialize = false;
				positionLight.Color.Value = new Vector3(1.5f, 0.5f, 0.5f);
				positionLight.Attenuation.Value = 20.0f;
				positionLight.Shadowed.Value = false;
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
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			Action<Vector3> updatePath = delegate(Vector3 target)
			{
				Map m = enemy.Map.Value.Target.Get<Map>();
				path.Clear();
				foreach (Map.Coordinate c in m.CustomAStar(coordinate, m.GetCoordinate(target)))
					path.Add(c);
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
				updatePath(currentPosition + new Vector3(((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f) * 15.0f);
			};

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
								Agent agent = Agent.Query(currentPosition, 20.0f, 8.0f);
								if (agent != null)
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
									Agent agent = Agent.Query(currentPosition, 20.0f, 8.0f);
									if (agent != null)
									{
										targetAgent.Value = agent.Entity;
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
							Interval = 1.0f,
							Action = delegate()
							{
								updatePath(targetAgent.Value.Target.Get<Agent>().Position);
							},
						},
						new AI.Task
						{
							Interval = 0.075f,
							Action = delegate()
							{
								Vector3 targetPosition = targetAgent.Value.Target.Get<Agent>().Position;
								float targetDistance = (targetPosition - currentPosition).Length();
								if (targetDistance > 30.0f || ai.TimeInCurrentState > 20.0f) // He got away
									ai.CurrentState.Value = "Idle";
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
								Agent agent = targetAgent.Value.Target.Get<Agent>();
								agent.Health.Value -= 0.01f / 1.5f; // seconds to kill
								if ((agent.Position - currentPosition).Length() > 5.0f) // They're getting away
									ai.CurrentState.Value = "Chase";
								advancePath(); 
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
