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
			return new Entity(main, "Levitator");
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
				Sound.AttachTracker(entity);
				SoundKiller.Add(entity, AK.EVENTS.STOP_GLOWSQUARE);
				entity.Add(new PostInitialization(delegate()
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_GLOWSQUARE, entity);
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, -1.0f, entity);
				}));
			}

			AI ai = entity.GetOrCreate<AI>("AI");

			ModelAlpha model = entity.GetOrCreate<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "AlphaModels\\box";
			model.Serialize = false;
			model.DrawOrder.Value = 15;

			RaycastAIMovement movement = entity.GetOrCreate<RaycastAIMovement>("Movement");
			Levitator levitator = entity.GetOrCreate<Levitator>("Levitator");

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

			entity.Add(new Updater
			(
				delegate(float dt)
				{
					float source = 1.0f + ((float)this.random.NextDouble() - 0.5f) * 2.0f * 0.05f;
					model.Scale.Value = new Vector3(defaultModelScale * source);
					light.Attenuation.Value = defaultLightAttenuation * source;
				}
			));

			light.Add(new Binding<Vector3>(light.Color, model.Color));

			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAI raycastAI = entity.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Quaternion>(transform.Quaternion, raycastAI.Orientation));

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

			const float sightDistance = 30.0f;
			const float hearingDistance = 0.0f;

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
					if (target == null || !target.Active)
					{
						ai.TargetAgent.Value = null;
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			// Levitate

			const int levitateRipRadius = 4;

			Func<bool> tryLevitate = delegate()
			{
				Entity voxelEntity = raycastAI.Voxel.Value.Target;
				if (voxelEntity == null)
					return false;

				Voxel map = voxelEntity.Get<Voxel>();
				Voxel.Coord? candidate = map.FindClosestFilledCell(raycastAI.Coord, 3);

				if (!candidate.HasValue)
					return false;

				if (VoxelRip.Go(map, candidate.Value, levitateRipRadius, delegate(List<DynamicVoxel> spawnedMaps)
				{
					foreach (DynamicVoxel spawnedMap in spawnedMaps)
					{
						if (spawnedMap[candidate.Value] != Voxel.States.Empty)
						{
							spawnedMap.Dangerous.Value = true;
							levitator.LevitatingVoxel.Value = spawnedMap.Entity;
							break;
						}
					}
				}))
				{
					levitator.GrabCoord.Value = candidate.Value;
					return true;
				}

				return false;
			};

			Action delevitateMap = delegate()
			{
				Entity levitatingMapEntity = levitator.LevitatingVoxel.Value.Target;
				if (levitatingMapEntity == null || !levitatingMapEntity.Active)
					return;

				DynamicVoxel dynamicMap = levitatingMapEntity.Get<DynamicVoxel>();
				VoxelRip.Consolidate(main, dynamicMap);
			};

			// Chase AI state

			ai.Add(new AI.AIState
			{
				Name = "Chase",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SFX_GLOWSQUARE_PITCH, 0.0f, entity);
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
							raycastAI.Move(ai.TargetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
						}
					},
					updatePosition,
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							Entity target = ai.TargetAgent.Value.Target;
							Vector3 targetPosition = target.Get<Transform>().Position;
							if ((targetPosition - transform.Position).Length() < 15.0f)
							{
								if (tryLevitate())
									ai.CurrentState.Value = "Levitating";
							}
						}
					}
				},
			});

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

				movement.NextPosition.Value = candidate;
				movement.PositionBlend.Value = 0.0f;
			};

			ai.Add(new AI.AIState
			{
				Name = "Levitating",
				Enter = delegate(AI.AIState previous)
				{
					findNextPosition();
				},
				Exit = delegate(AI.AIState next)
				{
					delevitateMap();
					levitator.LevitatingVoxel.Value = null;

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
							Entity levitatingMapEntity = levitator.LevitatingVoxel.Value.Target;
							if (levitatingMapEntity == null || !levitatingMapEntity.Active || ai.TimeInCurrentState.Value > 8.0f)
							{
								Entity voxel = raycastAI.Voxel.Value.Target;
								if (voxel != null && voxel.Active)
									raycastAI.Coord.Value = raycastAI.LastCoord.Value = voxel.Get<Voxel>().GetCoordinate(transform.Position);
								raycastAI.Move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
								ai.CurrentState.Value = "Alert";
								return;
							}

							DynamicVoxel dynamicMap = levitatingMapEntity.Get<DynamicVoxel>();

							movement.PositionBlend.Value += (main.ElapsedTime.Value / 1.0f);
							if (movement.PositionBlend > 1.0f)
								findNextPosition();

							transform.Position.Value = Vector3.Lerp(movement.LastPosition, movement.NextPosition, movement.PositionBlend);

							Vector3 grabPoint = dynamicMap.GetAbsolutePosition(levitator.GrabCoord);
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

			this.SetMain(entity, main);
		}
	}
}