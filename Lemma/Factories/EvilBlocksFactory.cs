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
	public class EvilBlocksFactory : Factory<Main>
	{
		private Random random = new Random();

		public EvilBlocksFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "EvilBlocks");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>("PointLight");
			light.Serialize = false;

			ListProperty<Entity.Handle> blockEntities = result.GetOrMakeListProperty<Entity.Handle>("Blocks");
			List<PhysicsBlock> blocks = new List<PhysicsBlock>();

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			AkSoundEngine.PostEvent("Play_evil_cubes", result);

			// TODO: Figure out Wwise volume property
			/*
			Property<float> volume = sound.GetProperty("Volume");

			const float defaultVolume = 0.5f;
			volume.Value = defaultVolume;
			*/

			AI ai = result.GetOrCreate<AI>();

			light.Add(new Binding<Vector3, string>(light.Color, delegate(string state)
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

			Agent agent = result.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAI raycastAI = result.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Matrix>(transform.Orientation, raycastAI.Orientation));

			Property<int> operationalRadius = result.GetOrMakeProperty<int>("OperationalRadius", true, 100);

			result.Add(new PostInitialization
			{
				delegate()
				{
					if (!main.EditorEnabled)
					{
						SceneryBlockFactory factory = Factory.Get<SceneryBlockFactory>();
						Random random = new Random();
						Vector3 scale = new Vector3(0.6f);
						Vector3 blockSpawnPoint = transform.Position;
						for (int i = 0; i < 30; i++)
						{
							Entity block = factory.CreateAndBind(main);
							block.Get<Transform>().Position.Value = blockSpawnPoint + new Vector3(((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f);
							block.Get<PhysicsBlock>().Size.Value = scale;
							block.Get<ModelInstance>().Scale.Value = scale;
							block.GetOrMakeProperty<string>("Type").Value = "Black";
							blockEntities.Add(block);
							main.Add(block);
						}
					}
				}
			});

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

			AI.Task dragBlocks = new AI.Task
			{
				Action = delegate()
				{
					if (blocks.Count < blockEntities.Count)
					{
						foreach (Entity.Handle e in blockEntities)
						{
							PhysicsBlock block = e.Target.Get<PhysicsBlock>();
							if (!blocks.Contains(block))
							{
								block.Add(new CommandBinding<BEPUphysics.BroadPhaseEntries.Collidable, BEPUphysics.NarrowPhaseSystems.Pairs.ContactCollection>(block.Collided, delegate(BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.ContactCollection contacts)
								{
									if (other.Tag != null && other.Tag.GetType().IsAssignableFrom(typeof(Character)))
									{
										// Damage the player
										Entity p = PlayerFactory.Instance;
										if (p != null && p.Active)
											p.Get<Player>().Health.Value -= 0.1f;
									}
								}));
								blocks.Add(block);
							}
						}
					}

					foreach (PhysicsBlock block in blocks)
					{
						Vector3 force = (transform.Position - block.Box.Position) * main.ElapsedTime * 4.0f;
						block.Box.ApplyLinearImpulse(ref force);
					}
				}
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
				Tasks = new[]
				{ 
					checkOperationalRadius,
					updatePosition,
					dragBlocks,
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
					dragBlocks,
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
					dragBlocks,
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							Entity target = targetAgent.Value.Target;
							Vector3 targetPosition = target.Get<Transform>().Position;
							if ((targetPosition - transform.Position).Length() < 10.0f)
								ai.CurrentState.Value = "Levitating";
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
					Map map = raycastAI.Map.Value.Target.Get<Map>();
					Map.Coordinate currentCoord = map.GetCoordinate(transform.Position);
					Map.Coordinate? closest = map.FindClosestFilledCell(currentCoord, 10);
					if (closest.HasValue)
						raycastAI.MoveTo(closest.Value);
					//volume.Value = defaultVolume;
				},
				Tasks = new[]
				{ 
					checkTargetAgent,
					new AI.Task
					{
						Action = delegate()
						{
							//volume.Value = 1.0f;
							if (ai.TimeInCurrentState.Value > 8.0f)
							{
								ai.CurrentState.Value = "Alert";
								return;
							}

							positionBlend.Value += (main.ElapsedTime.Value / 1.0f);
							if (positionBlend > 1.0f)
								findNextPosition();

							transform.Position.Value = Vector3.Lerp(lastPosition, nextPosition, positionBlend);
						},
					},
					dragBlocks,
				},
			});


			this.SetMain(result, main);
		}
	}
}
