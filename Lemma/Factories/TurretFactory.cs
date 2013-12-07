using System;
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
	public class TurretFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Turret");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>();
			light.Serialize = false;

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			Sound blastChargeSound = new Sound();
			blastChargeSound.Cue.Value = "Blast Charge";
			blastChargeSound.Serialize = false;
			result.Add("BlastChargeSound", blastChargeSound);

			Sound blastFireSound = new Sound();
			blastFireSound.Cue.Value = "Blast Fire";
			blastFireSound.Serialize = false;
			result.Add("BlastFireSound", blastFireSound);

			blastFireSound.Add(new Binding<Vector3>(blastFireSound.Position, transform.Position));

			blastChargeSound.Add(new Binding<Vector3>(blastChargeSound.Position, transform.Position));

			LineDrawer laser = new LineDrawer { Serialize = false };
			result.Add(laser);

			Property<Vector3> reticle = result.GetOrMakeProperty<Vector3>("Reticle");

			AI ai = result.GetOrCreate<AI>();

			Model model = result.GetOrCreate<Model>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "Models\\sphere";
			model.Editable = false;
			model.Serialize = false;

			const float defaultModelScale = 0.25f;
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
					case "Idle":
						return new Vector3(0.5f, 1.0f, 0.5f);
					default:
						return new Vector3(0.0f, 0.0f, 0.0f);
				}
			}, ai.CurrentState));

			light.Add(new Binding<Vector3>(light.Color, model.Color));

			Random random = new Random();
			result.Add(new Updater
			{
				delegate(float dt)
				{
					float source = ((float)random.NextDouble() - 0.5f) * 2.0f;
					model.Scale.Value = new Vector3(defaultModelScale * (1.0f + (source * 0.5f)));
					light.Attenuation.Value = defaultLightAttenuation * (1.0f + (source * 0.05f));
				}
			});

			Map.GlobalRaycastResult rayHit = new Map.GlobalRaycastResult();
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
					rayHit = Map.GlobalRaycast(transform.Position, toReticle, 300.0f);
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

			ai.Add(new AI.State
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

			Property<Entity.Handle> targetAgent = result.GetOrMakeProperty<Entity.Handle>("TargetAgent");

			ai.Add(new AI.State
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

			ai.Add(new AI.State
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

			ai.Add(new AI.State
			{
				Name = "Firing",
				Enter = delegate(AI.State last)
				{
					
				},
				Exit = delegate(AI.State next)
				{
					if (rayHit.Map != null)
						Explosion.Explode(main, rayHit.Map, rayHit.Coordinate.Value, 5, 8.0f);
					else
					{
						Entity target = targetAgent.Value.Target;
						BEPUutilities.RayHit physicsHit;
						if (target.Get<Player>().Body.CollisionInformation.RayCast(new Ray(transform.Position, toReticle), rayHit.Distance, out physicsHit))
							Explosion.Explode(main, target.Get<Transform>().Position, 5, 8.0f);
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

			this.SetMain(result, main);
		}
	}
}
