using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework.Audio;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;

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
			return new Entity(main, "EvilBlocks");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			BlockCloud blockCloud = entity.GetOrCreate<BlockCloud>("BlockCloud");
			blockCloud.Add(new CommandBinding(blockCloud.Delete, entity.Delete));
			blockCloud.Add(new CommandBinding<Collidable, ContactCollection>(blockCloud.Collided, delegate(Collidable other, ContactCollection contacts)
			{
				if (other.Tag != null && other.Tag.GetType() == typeof(Character))
				{
					// Damage the player
					Entity p = PlayerFactory.Instance;
					if (p != null && p.Active)
						p.Get<Agent>().Damage.Execute(0.1f);
				}
			}));
			blockCloud.Type.Value = Voxel.t.Black;

			Transform transform = entity.GetOrCreate<Transform>("Transform");

			AkGameObjectTracker.Attach(entity);

			AI ai = entity.GetOrCreate<AI>("AI");

			if (!main.EditorEnabled)
			{
				entity.Add(new PostInitialization
				{
					delegate()
					{
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_EVIL_CUBES, entity);
						AkSoundEngine.PostEvent(ai.CurrentState == "Chase" ? AK.EVENTS.EVIL_CUBES_CHASE : AK.EVENTS.EVIL_CUBES_IDLE, entity);
					}
				});

				SoundKiller.Add(entity, AK.EVENTS.STOP_EVIL_CUBES);
			}

			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAI raycastAI = entity.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.BlendTime.Value = 1.0f;
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Quaternion>(transform.Quaternion, raycastAI.Orientation));

			RaycastAIMovement movement = entity.GetOrCreate<RaycastAIMovement>("Movement");

			blockCloud.Add(new Binding<Vector3>(blockCloud.Position, transform.Position));

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

			const float sightDistance = 25.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.AIState
			{
				Name = "Idle",
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

			ai.Add(new AI.AIState
			{
				Name = "Alert",
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
									ai.TargetAgent.Value = a.Entity;
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
					Entity target = ai.TargetAgent.Value.Target;
					if (target == null || !target.Active || (target.Get<Transform>().Position.Value - transform.Position.Value).Length() > sightDistance * 1.25f)
					{
						ai.TargetAgent.Value = null;
						ai.CurrentState.Value = "Alert";
					}
				},
			};

			Action findNextPosition = delegate()
			{
				movement.LastPosition.Value = transform.Position.Value;
				float radius = 5.0f;
				Vector3 center = ai.TargetAgent.Value.Target.Get<Transform>().Position;
				Vector3 candidate;
				do
				{
					candidate = center + new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble(), (float)this.random.NextDouble() - 0.5f) * radius;
					radius += 1.0f;
				}
				while (!RaycastAI.DefaultPositionFilter(candidate));

				Vector3 toCandidate = candidate - movement.LastPosition;
				float distance = toCandidate.Length();
				const float maxMovementDistance = 9.0f;
				if (distance > maxMovementDistance)
					toCandidate *= maxMovementDistance / distance;

				movement.NextPosition.Value = movement.LastPosition.Value + toCandidate;
				movement.PositionBlend.Value = 0.0f;
			};

			// Chase AI state

			ai.Add(new AI.AIState
			{
				Name = "Chase",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.EVIL_CUBES_CHASE, entity);
					findNextPosition();
				},
				Exit = delegate(AI.AIState next)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.EVIL_CUBES_IDLE, entity);
				},
				Tasks = new[]
				{
					checkOperationalRadius,
					checkTargetAgent,
					new AI.Task
					{
						Action = delegate()
						{
							if (ai.TimeInCurrentState.Value > 15.0f)
							{
								Entity voxel = raycastAI.Voxel.Value.Target;
								if (voxel != null && voxel.Active)
									raycastAI.Coord.Value = raycastAI.LastCoord.Value = voxel.Get<Voxel>().GetCoordinate(transform.Position);
								raycastAI.Move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
								ai.CurrentState.Value = "Idle";
							}
							else
							{
								movement.PositionBlend.Value += (main.ElapsedTime.Value / 1.0f);
								if (movement.PositionBlend > 1.0f)
									findNextPosition();
								transform.Position.Value = Vector3.Lerp(movement.LastPosition, movement.NextPosition, movement.PositionBlend);
							}
						}
					},
				},
			});

			this.SetMain(entity, main);
		}
	}
}