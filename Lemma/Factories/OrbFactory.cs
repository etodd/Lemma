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
	public class OrbFactory : Factory<Main>
	{
		private Random random = new Random();

		public OrbFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Orb");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			PointLight light = entity.GetOrCreate<PointLight>("PointLight");
			light.Serialize = false;

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			if (!main.EditorEnabled)
			{
				AkGameObjectTracker.Attach(entity);
				SoundKiller.Add(entity, AK.EVENTS.STOP_GLOWSQUARE);
				entity.Add(new PostInitialization
				{
					delegate()
					{
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_GLOWSQUARE, entity);
						AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, -1.0f, entity);
					}
				});
			}

			AI ai = entity.GetOrCreate<AI>("AI");

			ModelAlpha model = entity.GetOrCreate<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "AlphaModels\\box";
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
					case "Explode":
						return new Vector3(2.0f, 1.0f, 0.5f);
					default:
						return new Vector3(1.0f, 1.0f, 1.0f);
				}
			}, ai.CurrentState));

			entity.Add(new Updater
			{
				delegate(float dt)
				{
					float source = 1.0f + ((float)this.random.NextDouble() - 0.5f) * 2.0f * 0.05f;
					model.Scale.Value = new Vector3(defaultModelScale * source);
					light.Attenuation.Value = defaultLightAttenuation * source;
				}
			});

			model.Add(new Binding<bool, string>(model.Enabled, x => x != "Exploding", ai.CurrentState));

			light.Add(new Binding<Vector3>(light.Color, model.Color));

			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAIMovement movement = entity.GetOrCreate<RaycastAIMovement>("Movement");
			Orb orb = entity.GetOrCreate<Orb>("Orb");

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (transform.Position.Value - main.Camera.Position).Length() < movement.OperationalRadius;
					if (shouldBeActive && ai.CurrentState == "Suspended")
						ai.CurrentState.Value = "Idle";
					else if (!shouldBeActive && ai.CurrentState != "Suspended")
						ai.CurrentState.Value = "Suspended";
				},
			};

			RaycastAI raycastAI = entity.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Matrix>(transform.Orientation, raycastAI.Orientation));

			AI.Task updatePosition = new AI.Task
			{
				Action = delegate()
				{
					raycastAI.Update();
				},
			};

			ai.Add(new AI.AIState
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, },
			});

			const float sightDistance = 40.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.AIState
			{
				Name = "Idle",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, -1.0f, entity);
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
						Interval = 0.5f,
						Action = delegate()
						{
							Agent a = Agent.Query(transform.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
							if (a != null)
								ai.CurrentState.Value = "Alert";
						},
					},
				},
			});

			ai.Add(new AI.AIState
			{
				Name = "Alert",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_GLOWSQUARE, entity);
				},
				Exit = delegate(AI.AIState next)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_GLOWSQUARE, entity);
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
									movement.TargetAgent.Value = a.Entity;
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
					Entity target = movement.TargetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						movement.TargetAgent.Value = null;
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			ai.Add(new AI.AIState
			{
				Name = "Chase",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, 0.0f, entity);
				},
				Exit = delegate(AI.AIState next)
				{

				},
				Tasks = new[]
				{
					checkOperationalRadius,
					checkTargetAgent,
					new AI.Task
					{
						Interval = 0.35f,
						Action = delegate()
						{
							raycastAI.Move(movement.TargetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
						}
					},
					new AI.Task
					{
						Action = delegate()
						{
							if ((movement.TargetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position).Length() < 10.0f)
								ai.CurrentState.Value = "Explode";
						}
					},
					updatePosition,
				},
			});

			EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
			Voxel.State infectedState = Voxel.States[Voxel.t.Infected];

			ai.Add(new AI.AIState
			{
				Name = "Explode",
				Enter = delegate(AI.AIState previous)
				{
					orb.CoordQueue.Clear();
					
					Entity voxelEntity = raycastAI.Voxel.Value.Target;
					if (voxelEntity == null || !voxelEntity.Active)
					{
						ai.CurrentState.Value = "Alert";
						return;
					}
						
					Voxel m = voxelEntity.Get<Voxel>();

					Voxel.Coord c = raycastAI.Coord.Value;

					Direction toSupport = Direction.None;

					foreach (Direction dir in DirectionExtensions.Directions)
					{
						if (m[raycastAI.Coord.Value.Move(dir)].ID != 0)
						{
							toSupport = dir;
							break;
						}
					}

					Direction up = toSupport.GetReverse();

					orb.ExplosionOriginalCoord.Value = raycastAI.Coord;

					Direction right;
					if (up.IsParallel(Direction.PositiveX))
						right = Direction.PositiveZ;
					else
						right = Direction.PositiveX;
					Direction forward = up.Cross(right);

					for (Voxel.Coord y = c.Clone(); y.GetComponent(up) < c.GetComponent(up) + 3; y = y.Move(up))
					{
						for (Voxel.Coord x = y.Clone(); x.GetComponent(right) < c.GetComponent(right) + 2; x = x.Move(right))
						{
							for (Voxel.Coord z = x.Clone(); z.GetComponent(forward) < c.GetComponent(forward) + 2; z = z.Move(forward))
								orb.CoordQueue.Add(z);
						}
					}
				},
				Exit = delegate(AI.AIState next)
				{
					orb.CoordQueue.Clear();
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					new AI.Task
					{
						Interval = 0.2f,
						Action = delegate()
						{
							if (orb.CoordQueue.Count > 0)
							{
								raycastAI.MoveTo(orb.CoordQueue[0]);

								orb.CoordQueue.RemoveAt(0);

								Entity blockEntity = factory.CreateAndBind(main);
								infectedState.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());

								Entity mapEntity = raycastAI.Voxel.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
									Voxel m = raycastAI.Voxel.Value.Target.Get<Voxel>();

									effectBlock.Offset.Value = m.GetRelativePosition(raycastAI.Coord);

									Vector3 absolutePos = m.GetAbsolutePosition(raycastAI.Coord);

									effectBlock.StartPosition.Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f);
									effectBlock.StartOrientation.Value = Matrix.CreateRotationX(0.15f) * Matrix.CreateRotationY(0.15f);
									effectBlock.TotalLifetime.Value = 0.05f;
									effectBlock.Setup(raycastAI.Voxel.Value.Target, raycastAI.Coord, Voxel.t.Infected);
									main.Add(blockEntity);
								}
							}
						}
					},
					new AI.Task
					{
						Action = delegate()
						{
							AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, MathHelper.Lerp(0.0f, 1.0f, ai.TimeInCurrentState.Value / 2.0f), entity);
							if (orb.CoordQueue.Count == 0)
							{
								// Explode
								ai.CurrentState.Value = "Exploding";
							}
						},
					},
					updatePosition,
				},
			});

			ai.Add(new AI.AIState
			{
				Name = "Exploding",
				Enter = delegate(AI.AIState previous)
				{
					orb.Exploded.Value = false;
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_GLOWSQUARE, entity);
				},
				Exit = delegate(AI.AIState next)
				{
					orb.Exploded.Value = false;
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_GLOWSQUARE, entity);
				},
				Tasks = new[]
				{
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							const int radius = 9;

							float timeInCurrentState = ai.TimeInCurrentState;
							if (timeInCurrentState > 1.0f && !orb.Exploded)
							{
								Entity mapEntity = raycastAI.Voxel.Value.Target;
								if (mapEntity != null && mapEntity.Active)
									Explosion.Explode(main, raycastAI.Voxel.Value.Target.Get<Voxel>(), raycastAI.Coord, radius, 18.0f);

								orb.Exploded.Value = true;
							}

							if (timeInCurrentState > 2.0f)
							{
								Entity mapEntity = raycastAI.Voxel.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									Voxel m = raycastAI.Voxel.Value.Target.Get<Voxel>();
									Voxel.Coord? closestCell = m.FindClosestFilledCell(raycastAI.Coord, radius + 1);
									if (closestCell.HasValue)
									{
										raycastAI.Move(m.GetAbsolutePosition(closestCell.Value) - transform.Position);
										ai.CurrentState.Value = "Alert";
									}
									else
										entity.Delete.Execute();
								}
								else // Our map got deleted. Hope we find a new one.
								{
									raycastAI.Move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
									ai.CurrentState.Value = "Alert";
								}
							}
						},
					},
					updatePosition,
				},
			});

			this.SetMain(entity, main);

			entity.Add("OperationalRadius", movement.OperationalRadius);
		}
	}
}
