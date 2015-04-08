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
			SpotLight light = entity.Create<SpotLight>();
			light.Enabled.Value = !main.EditorEnabled;

			light.FieldOfView.Value = 1.0f;
			light.Attenuation.Value = 75.0f;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			Turret turret = entity.GetOrCreate<Turret>("Turret");

			Command die = new Command
			{
				Action = delegate()
				{
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_TURRET_DEATH, entity);
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
			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main, true, true, die);
			attachable.Enabled.Value = true;
			attachable.Offset.Value = 2;

			PointLight pointLight = entity.GetOrCreate<PointLight>();
			pointLight.Serialize = false;
			pointLight.Add(new Binding<Vector3>(pointLight.Position, transform.Position));

			LineDrawer laser = new LineDrawer { Serialize = false };
			entity.Add(laser);
			laser.DrawOrder.Value = 0;

			AI ai = entity.GetOrCreate<AI>("AI");

			ModelAlpha model = entity.Create<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "AlphaModels\\pyramid";

			const float defaultModelScale = 0.75f;
			model.Scale.Value = new Vector3(defaultModelScale);

			const float sightDistance = 80.0f;
			const float hearingDistance = 0.0f;
			const float operationalRadius = 100.0f;

			model.Add(new Binding<Vector3, string>(model.Color, delegate(string state)
			{
				switch (state)
				{
					case "Alert":
						return new Vector3(1.0f, 1.0f, 0.25f);
					case "Aggressive":
						return new Vector3(1.0f, 0.5f, 0.25f);
					case "Firing":
						return new Vector3(1.0f, 0.0f, 0.0f);
					case "Disabled":
						return new Vector3(0.0f, 0.0f, 0.0f);
					default:
						return new Vector3(1.0f, 1.0f, 1.0f);
				}
			}, ai.CurrentState));
			laser.Add(new Binding<bool, string>(laser.Enabled, x => x != "Disabled" && x != "Suspended", ai.CurrentState));

			light.Add(new Binding<Vector3>(light.Color, model.Color));
			pointLight.Add(new Binding<Vector3>(pointLight.Color, model.Color));

			Voxel.GlobalRaycastResult rayHit = new Voxel.GlobalRaycastResult();
			Vector3 toReticle = Vector3.Zero;
			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (transform.Position.Value - main.Camera.Position).Length() < operationalRadius && Water.Get(transform.Position) == null;
					if (shouldBeActive && ai.CurrentState == "Suspended")
						ai.CurrentState.Value = "Idle";
					else if (!shouldBeActive && ai.CurrentState != "Suspended")
						ai.CurrentState.Value = "Suspended";
				},
			};

			turret.Add(new CommandBinding(turret.PowerOn, delegate()
			{
				if (ai.CurrentState == "Disabled")
				{
					ai.CurrentState.Value = "Suspended";
					checkOperationalRadius.Action();
				}
			}));

			turret.Add(new CommandBinding(turret.PowerOff, delegate()
			{
				ai.CurrentState.Value = "Disabled";
			}));

			light.Add(new Binding<Quaternion>(light.Orientation, delegate()
			{
				return Quaternion.CreateFromYawPitchRoll(-(float)Math.Atan2(toReticle.Z, toReticle.X) - (float)Math.PI * 0.5f, (float)Math.Asin(toReticle.Y), 0);
			}, transform.Position, turret.Reticle));

			AI.Task updateRay = new AI.Task
			{
				Action = delegate()
				{
					toReticle = Vector3.Normalize(turret.Reticle.Value - transform.Position.Value);
					rayHit = Voxel.GlobalRaycast(transform.Position, toReticle, operationalRadius);
					laser.Lines.Clear();

					Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(model.Color);
					laser.Lines.Add(new LineDrawer.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, color),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(rayHit.Position, color),
					});
				}
			};

			ai.Add(new AI.AIState
			{
				Name = "Disabled",
				Tasks = new AI.Task[] { },
			});

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
									ai.TargetAgent.Value = a.Entity;
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
					Entity target = ai.TargetAgent.Value.Target;
					if (target == null || !target.Active)
					{
						ai.TargetAgent.Value = null;
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
							Entity target = ai.TargetAgent.Value.Target;
							turret.Reticle.Value += (target.Get<Transform>().Position - turret.Reticle.Value) * Math.Min(3.0f * main.ElapsedTime, 1.0f);
						}
					},
					new AI.Task
					{
						Interval = 0.1f,
						Action = delegate()
						{
							if (Agent.Query(transform.Position, sightDistance, hearingDistance, ai.TargetAgent.Value.Target.Get<Agent>()))
								lastSpotted = main.TotalTime;

							if (ai.TimeInCurrentState.Value > 1.5f)
							{
								if (lastSpotted < main.TotalTime - 2.0f)
									ai.CurrentState.Value = "Alert";
								else
								{
									Vector3 targetPos = ai.TargetAgent.Value.Target.Get<Transform>().Position.Value;
									Vector3 toTarget = Vector3.Normalize(targetPos - transform.Position.Value);
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
					Voxel.State attachedState = attachable.AttachedVoxel.Value.Target.Get<Voxel>()[attachable.Coord];
					if (!attachedState.Permanent && rayHit.Voxel != null && (rayHit.Position - transform.Position).Length() < 8.0f)
						return; // Danger close, cease fire!

					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_TURRET_FIRE, entity);

					bool hitVoxel = true;

					Entity target = ai.TargetAgent.Value.Target;
					if (target != null && target.Active)
					{
						Vector3 targetPos = target.Get<Transform>().Position;
						Vector3 toTarget = targetPos - transform.Position.Value;
						if (Vector3.Dot(toReticle, Vector3.Normalize(toTarget)) > 0.2f)
						{
							float distance = toTarget.Length();
							if (distance < rayHit.Distance)
								Sound.PostEvent(AK.EVENTS.PLAY_TURRET_MISS, transform.Position + toReticle * distance);
						}

						BEPUutilities.RayHit physicsHit;
						if (target.Get<Player>().Character.Body.CollisionInformation.RayCast(new Ray(transform.Position, toReticle), rayHit.Voxel == null ? float.MaxValue : rayHit.Distance, out physicsHit))
						{
							Explosion.Explode(main, targetPos, 6, 8.0f);
							hitVoxel = false;
						}
					}

					if (hitVoxel && rayHit.Voxel != null)
						Explosion.Explode(main, rayHit.Position + rayHit.Voxel.GetAbsoluteVector(rayHit.Normal.GetVector()) * 0.5f, 6, 8.0f);

					Vector3 splashPos;
					Water w = Water.Raycast(transform.Position, toReticle, rayHit.Distance, out splashPos);
					if (w != null)
					{
						Sound.PostEvent(AK.EVENTS.PLAY_WATER_SPLASH, splashPos);
						Water.SplashParticles(main, splashPos, 3.0f);
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

			entity.Add("On", turret.On);
			entity.Add("PowerOn", turret.PowerOn);
			entity.Add("PowerOff", turret.PowerOff);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
