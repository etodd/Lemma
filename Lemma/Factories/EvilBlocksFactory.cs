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
			return new Entity(main, "EvilBlocks");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			PointLight light = entity.GetOrCreate<PointLight>("PointLight");
			light.Serialize = false;

			EvilBlocks evilBlocks = entity.GetOrCreate<EvilBlocks>("EvilBlocks");

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			AkGameObjectTracker.Attach(entity);
			if (!main.EditorEnabled)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_EVIL_CUBES, entity);
			SoundKiller.Add(entity, AK.EVENTS.STOP_EVIL_CUBES);

			AI ai = entity.GetOrCreate<AI>();

			light.Add(new Binding<Vector3, string>(light.Color, delegate(string state)
			{
				switch (state)
				{
					case "Alert":
						return new Vector3(1.5f, 1.5f, 0.5f);
					case "Chase":
						return new Vector3(2.0f, 1.0f, 0.5f);
					default:
						return new Vector3(1.0f, 1.0f, 1.0f);
				}
			}, ai.CurrentState));

			Agent agent = entity.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			RaycastAI raycastAI = entity.GetOrCreate<RaycastAI>("RaycastAI");
			raycastAI.Add(new TwoWayBinding<Vector3>(transform.Position, raycastAI.Position));
			raycastAI.Add(new Binding<Quaternion>(transform.Quaternion, raycastAI.Orientation));

			evilBlocks.Add(new Binding<Vector3>(evilBlocks.Position, transform.Position));

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (transform.Position.Value - main.Camera.Position).Length() < evilBlocks.OperationalRadius;
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
				}
			};

			ai.Add(new AI.AIState
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, dragBlocks, },
			});

			const float sightDistance = 30.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.AIState
			{
				Name = "Idle",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.EVIL_CUBES_IDLE, entity);
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
					AkSoundEngine.PostEvent(AK.EVENTS.STOP_EVIL_CUBES, entity);
				},
				Exit = delegate(AI.AIState next)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_EVIL_CUBES, entity);
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
									evilBlocks.TargetAgent.Value = a.Entity;
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
					Entity target = evilBlocks.TargetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						evilBlocks.TargetAgent.Value = null;
						ai.CurrentState.Value = "Idle";
					}
				},
			};

			// Chase AI state

			ai.Add(new AI.AIState
			{
				Name = "Chase",
				Enter = delegate(AI.AIState previous)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.EVIL_CUBES_CHASE, entity);
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
							raycastAI.Move(evilBlocks.TargetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
						}
					},
					updatePosition,
					dragBlocks,
				},
			});

			this.SetMain(entity, main);
		}
	}
}
