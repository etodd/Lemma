using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

namespace Lemma.Factories
{
	public class TurretFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			return new Entity(main, "Turret");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			SpotLight light = entity.GetOrCreate<SpotLight>();
			light.Serialize = false;
			light.Editable = false;
			light.Enabled.Value = !main.EditorEnabled;

			light.FieldOfView.Value = 1.0f;
			light.Attenuation.Value = 75.0f;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Quaternion>(light.Orientation, transform.Quaternion));

			PointLight pointLight = entity.GetOrCreate<PointLight>();
			pointLight.Serialize = false;
			pointLight.Editable = false;
			pointLight.Add(new Binding<Vector3>(pointLight.Position, transform.Position));

			LineDrawer laser = new LineDrawer { Serialize = false };
			entity.Add(laser);

			Property<Vector3> reticle = entity.GetOrMakeProperty<Vector3>("Reticle");

			AI ai = entity.GetOrCreate<AI>();

			Model model = entity.GetOrCreate<Model>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "Models\\pyramid";
			model.Editable = false;
			model.Serialize = false;

			Command die = new Command
			{
				Action = delegate()
				{
					AkSoundEngine.PostEvent("Play_infected_shatter", entity);
					ParticleSystem shatter = ParticleSystem.Get(main, "InfectedShatter");
					Random random = new Random();
					for (int i = 0; i < 50; i++)
					{
						Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
						shatter.AddParticle(transform.Position + offset, offset);
					}
					entity.Delete.Execute();
				}
			};

			VoxelAttachable.MakeAttachable(entity, main, true, true, die);

			const float defaultModelScale = 0.75f;
			model.Scale.Value = new Vector3(defaultModelScale);

			model.Add(new Binding<Vector3, string>(model.Color, delegate(string state)
			{
				switch (state)
				{
					case "Alert":
						return new Vector3(1.5f, 1.5f, 0.5f);
					case "Aggressive":
						return new Vector3(1.5f, 0.8f, 0.5f);
					case "Firing":
						return new Vector3(2.0f, 0.0f, 0.0f);
					default:
						return new Vector3(1.0f, 1.0f, 1.0f);
				}
			}, ai.CurrentState));

			light.Add(new Binding<Vector3>(light.Color, model.Color));
			pointLight.Add(new Binding<Vector3>(pointLight.Color, model.Color));

			Voxel.GlobalRaycastResult rayHit = new Voxel.GlobalRaycastResult();
			Vector3 toReticle = Vector3.Zero;
			const int operationalRadius = 100;

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

			AI.Task updateRay = new AI.Task
			{
				Action = delegate()
				{
					toReticle = Vector3.Normalize(reticle.Value - transform.Position.Value);
					rayHit = Voxel.GlobalRaycast(transform.Position, toReticle, 300.0f);
					laser.Lines.Clear();

					Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(model.Color);
					laser.Lines.Add(new LineDrawer.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, color),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(rayHit.Position, color),
					});
				}
			};

			const float sightDistance = 100.0f;
			const float hearingDistance = 20.0f;

			ai.Add(new AI.AIState
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, },
			});

			ai.Add(new AI.AIState
			{
				Name = "Idle",
				Tasks = new[]
				{ 
					checkOperationalRadius,
					updateRay,
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

			Property<Entity.Handle> targetAgent = entity.GetOrMakeProperty<Entity.Handle>("TargetAgent");

			ai.Add(new AI.AIState
			{
				Name = "Alert",
				Tasks = new[]
				{ 
					checkOperationalRadius,
					updateRay,
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
									ai.CurrentState.Value = "Aggressive";
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

			float lastSpotted = 0.0f;

			ai.Add(new AI.AIState
			{
				Name = "Aggressive",
				Tasks = new[]
				{
					checkTargetAgent,
					updateRay,
					new AI.Task
					{
						Action = delegate()
						{
							Entity target = targetAgent.Value.Target;
							reticle.Value += (target.Get<Transform>().Position - reticle.Value) * Math.Min(2.0f * main.ElapsedTime, 1.0f);
						}
					},
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							if (Agent.Query(transform.Position, sightDistance, hearingDistance, targetAgent.Value.Target.Get<Agent>()))
								lastSpotted = main.TotalTime;

							if (ai.TimeInCurrentState.Value > 2.0f)
							{
								if (lastSpotted < main.TotalTime - 2.0f)
									ai.CurrentState.Value = "Alert";
								else
								{
									Vector3 toTarget = Vector3.Normalize(targetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position.Value);
									if (Vector3.Dot(toReticle, toTarget) > 0.95f)
										ai.CurrentState.Value = "Firing";
								}
							}
						}
					},
				}
			});

			ai.Add(new AI.AIState
			{
				Name = "Firing",
				Enter = delegate(AI.AIState last)
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_TURRET_CHARGE, entity);
				},
				Exit = delegate(AI.AIState next)
				{
					if (rayHit.Voxel != null && (rayHit.Position - transform.Position).Length() < 8.0f)
						return; // Danger close, cease fire!

					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_TURRET_FIRE, entity);

					Entity target = targetAgent.Value.Target;
					if (target != null && target.Active)
					{
						Vector3 targetPos = target.Get<Transform>().Position;
						Vector3 toTarget = targetPos - transform.Position.Value;
						if (Vector3.Dot(toReticle, Vector3.Normalize(toTarget)) > 0.2f)
						{
							float distance = toTarget.Length();
							if (distance < rayHit.Distance)
								AkSoundEngine.PostEvent(AK.EVENTS.PLAY_TURRET_MISS, transform.Position + toReticle * distance);
						}
					}

					if (rayHit.Voxel != null)
						Explosion.Explode(main, rayHit.Voxel, rayHit.Coordinate.Value, 6, 8.0f);
					else if (target != null && target.Active)
					{
						Vector3 targetPos = target.Get<Transform>().Position;
						BEPUutilities.RayHit physicsHit;
						if (target.Get<Player>().Character.Body.CollisionInformation.RayCast(new Ray(transform.Position, toReticle), rayHit.Distance, out physicsHit))
							Explosion.Explode(main, targetPos, 6, 8.0f);
					}
				},
				Tasks = new[]
				{
					checkTargetAgent,
					updateRay,
					new AI.Task
					{
						Action = delegate()
						{
							if (ai.TimeInCurrentState.Value > 0.75f)
								ai.CurrentState.Value = "Aggressive"; // This actually fires (in the Exit function)
						}
					}
				}
			});

			this.SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
