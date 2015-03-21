using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using System.Threading;

namespace Lemma.Factories
{
	public class SnakeFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			return new Entity(main, "Snake");
		}

		private Random random = new Random();

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			if (ParticleSystem.Get(main, "SnakeSparks") == null)
			{
				ParticleSystem.Add(main, "SnakeSparks",
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
				ParticleSystem.Add(main, "SnakeSparksRed",
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
					MinColor = new Vector4(1.4f, 0.8f, 0.7f, 1.0f),
					MaxColor = new Vector4(1.4f, 0.8f, 0.7f, 1.0f),
				});
				ParticleSystem.Add(main, "SnakeSparksYellow",
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
					MinColor = new Vector4(1.4f, 1.4f, 0.7f, 1.0f),
					MaxColor = new Vector4(1.4f, 1.4f, 0.7f, 1.0f),
				});
			}

			Snake snake = entity.GetOrCreate<Snake>("Snake");

			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			AI ai = entity.GetOrCreate<AI>("AI");

			Agent agent = entity.GetOrCreate<Agent>("Agent");

			const float defaultSpeed = 5.0f;
			const float chaseSpeed = 18.0f;
			const float closeChaseSpeed = 12.0f;
			const float crushSpeed = 125.0f;

			VoxelChaseAI chase = entity.GetOrCreate<VoxelChaseAI>("VoxelChaseAI");
			chase.Add(new TwoWayBinding<Vector3>(transform.Position, chase.Position));
			chase.Speed.Value = defaultSpeed;
			chase.EnablePathfinding.Value = ai.CurrentState.Value == "Chase";
			chase.Filter = delegate(Voxel.State state)
			{
				if (state == Voxel.States.Infected || state == Voxel.States.Neutral || state == Voxel.States.Hard || state == Voxel.States.HardInfected)
					return true;
				return false;
			};
			entity.Add(new CommandBinding(chase.Delete, entity.Delete));

			PointLight positionLight = null;
			if (!main.EditorEnabled)
			{
				positionLight = new PointLight();
				positionLight.Serialize = false;
				positionLight.Color.Value = new Vector3(1.5f, 0.5f, 0.5f);
				positionLight.Attenuation.Value = 20.0f;
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
							return new Vector3(1.0f, 1.0f, 1.0f);
					}
				}, ai.CurrentState));
				entity.Add("PositionLight", positionLight);
				ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("Particles");
				emitter.Serialize = false;
				emitter.ParticlesPerSecond.Value = 100;
				emitter.Add(new Binding<string>(emitter.ParticleType, delegate(string state)
				{
					switch (state)
					{
						case "Chase":
						case "Crush":
							return "SnakeSparksRed";
						case "Alert":
							return "SnakeSparksYellow";
						default:
							return "SnakeSparks";
					}
				}, ai.CurrentState));
				emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
				emitter.Add(new Binding<bool, string>(emitter.Enabled, x => x != "Suspended", ai.CurrentState));

				positionLight.Add(new Binding<Vector3>(positionLight.Position, transform.Position));
				emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
				agent.Add(new Binding<Vector3>(agent.Position, transform.Position));
				AkGameObjectTracker.Attach(entity);
			}

			AI.Task checkMap = new AI.Task
			{
				Action = delegate()
				{
					if (chase.Voxel.Value.Target == null || !chase.Voxel.Value.Target.Active)
						entity.Delete.Execute();
				},
			};

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (transform.Position.Value - main.Camera.Position).Length() < snake.OperationalRadius;
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
					Entity target = ai.TargetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						ai.TargetAgent.Value = null;
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			Func<Voxel, Direction> randomValidDirection = delegate(Voxel m)
			{
				Voxel.Coord c = chase.Coord;
				Direction[] dirs = new Direction[6];
				Array.Copy(DirectionExtensions.Directions, dirs, 6);

				// Shuffle directions
				int i = 5;
				while (i > 0)
				{
					int k = this.random.Next(i);
					Direction temp = dirs[i];
					dirs[i] = dirs[k];
					dirs[k] = temp;
					i--;
				}

				foreach (Direction dir in dirs)
				{
					if (chase.Filter(m[c.Move(dir)]))
						return dir;
				}
				return Direction.None;
			};

			Direction currentDir = Direction.None;

			chase.Add(new CommandBinding<Voxel, Voxel.Coord>(chase.Moved, delegate(Voxel m, Voxel.Coord c)
			{
				if (chase.Active)
				{
					string currentState = ai.CurrentState.Value;
					Voxel.t id = m[c].ID;
					if (id == Voxel.t.Hard)
					{
						m.Empty(c);
						m.Fill(c, Voxel.States.HardInfected);
						m.Regenerate();
					}
					else if (id == Voxel.t.Neutral)
					{
						m.Empty(c);
						m.Fill(c, Voxel.States.Infected);
						m.Regenerate();
					}
					else if (id == Voxel.t.Empty)
					{
						m.Fill(c, Voxel.States.Infected);
						m.Regenerate();
					}
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SNAKE_MOVE, entity);

					if (currentState == "Idle")
					{
						if (currentDir == Direction.None || !chase.Filter(m[chase.Coord.Value.Move(currentDir)]) || this.random.Next(8) == 0)
							currentDir = randomValidDirection(m);
						chase.Coord.Value = chase.Coord.Value.Move(currentDir);
					}
					else if (snake.Path.Length > 0)
					{
						chase.Coord.Value = snake.Path[0];
						snake.Path.RemoveAt(0);
					}
				}
			}));
			
			const float sightDistance = 50.0f;
			const float hearingDistance = 0.0f;

			ai.Setup
			(
				new AI.AIState
				{
					Name = "Suspended",
					Tasks = new[] { checkOperationalRadius },
				},
				new AI.AIState
				{
					Name = "Idle",
					Enter = delegate(AI.AIState previous)
					{
						Entity voxelEntity = chase.Voxel.Value.Target;
						if (voxelEntity != null)
						{
							Voxel m = voxelEntity.Get<Voxel>();
							if (currentDir == Direction.None || !chase.Filter(m[chase.Coord.Value.Move(currentDir)]))
								currentDir = randomValidDirection(m);
							chase.Coord.Value = chase.Coord.Value.Move(currentDir);
						}
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
								Agent a = Agent.Query(transform.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
								if (a != null)
									ai.CurrentState.Value = "Alert";
							},
						},
					},
				},
				new AI.AIState
				{
					Name = "Alert",
					Enter = delegate(AI.AIState previous)
					{
						chase.EnableMovement.Value = false;
					},
					Exit = delegate(AI.AIState next)
					{
						chase.EnableMovement.Value = true;
					},
					Tasks = new[]
					{
						checkMap,
						checkOperationalRadius,
						new AI.Task
						{
							Interval = 0.4f,
							Action = delegate()
							{
								if (ai.TimeInCurrentState > 3.0f)
									ai.CurrentState.Value = "Idle";
								else
								{
									Agent a = Agent.Query(transform.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
									if (a != null)
									{
										ai.TargetAgent.Value = a.Entity;
										ai.CurrentState.Value = "Chase";
									}
								}
							},
						},
					},
				},
				new AI.AIState
				{
					Name = "Chase",
					Enter = delegate(AI.AIState previousState)
					{
						chase.EnablePathfinding.Value = true;
						chase.Speed.Value = chaseSpeed;
					},
					Exit = delegate(AI.AIState nextState)
					{
						chase.EnablePathfinding.Value = false;
						chase.Speed.Value = defaultSpeed;
					},
					Tasks = new[]
					{
						checkMap,
						checkOperationalRadius,
						checkTargetAgent,
						new AI.Task
						{
							Interval = 0.07f,
							Action = delegate()
							{
								Vector3 targetPosition = ai.TargetAgent.Value.Target.Get<Agent>().Position;

								float targetDistance = (targetPosition - transform.Position).Length();

								chase.Speed.Value = targetDistance < 15.0f ? closeChaseSpeed : chaseSpeed;

								if (targetDistance > 50.0f || ai.TimeInCurrentState > 30.0f) // He got away
									ai.CurrentState.Value = "Alert";
								else if (targetDistance < 5.0f) // We got 'im
								{
									// First, make sure we're not near a reset block
									Voxel v = chase.Voxel.Value.Target.Get<Voxel>();
									if (VoxelAStar.BroadphaseSearch(v, chase.Coord, 6, x => x.Type == Lemma.Components.Voxel.States.Reset) == null)
										ai.CurrentState.Value = "Crush";
								}
								else
									chase.Target.Value = targetPosition;
							},
						},
					},
				},
				new AI.AIState
				{
					Name = "Crush",
					Enter = delegate(AI.AIState lastState)
					{
						// Set up cage
						Voxel.Coord center = chase.Voxel.Value.Target.Get<Voxel>().GetCoordinate(ai.TargetAgent.Value.Target.Get<Agent>().Position);

						int radius = 1;

						// Bottom
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								snake.Path.Add(new Voxel.Coord { X = x, Y = center.Y - 4, Z = z });
						}

						// Outer shell
						radius = 2;
						for (int y = center.Y - 3; y <= center.Y + 3; y++)
						{
							// Left
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								snake.Path.Add(new Voxel.Coord { X = center.X - radius, Y = y, Z = z });

							// Right
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								snake.Path.Add(new Voxel.Coord { X = center.X + radius, Y = y, Z = z });

							// Backward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								snake.Path.Add(new Voxel.Coord { X = x, Y = y, Z = center.Z - radius });

							// Forward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								snake.Path.Add(new Voxel.Coord { X = x, Y = y, Z = center.Z + radius });
						}

						// Top
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								snake.Path.Add(new Voxel.Coord { X = x, Y = center.Y + 3, Z = z });
						}

						chase.EnablePathfinding.Value = false;
						chase.Speed.Value = crushSpeed;

						snake.CrushCoordinate.Value = chase.Coord;
					},
					Exit = delegate(AI.AIState nextState)
					{
						chase.Speed.Value = defaultSpeed;
						chase.Coord.Value = chase.LastCoord.Value = snake.CrushCoordinate;
						snake.Path.Clear();
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
								Agent a = ai.TargetAgent.Value.Target.Get<Agent>();
								a.Damage.Execute(0.01f / 1.5f); // seconds to kill
								if (!a.Active)
									ai.CurrentState.Value = "Alert";
								else
								{
									if ((a.Position - transform.Position.Value).Length() > 5.0f) // They're getting away
										ai.CurrentState.Value = "Chase";
								}
							}
						}
					},
				}
			);

			this.SetMain(entity, main);

			entity.Add("OperationalRadius", snake.OperationalRadius);
		}
	}
}
