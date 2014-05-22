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
				AkSoundEngine.PostEvent("Play_cube_drone", entity);

			// TODO: figure out Wwise volume parameter
			/*
			Property<float> volume = sound.GetProperty("Volume");
			Property<float> pitch = sound.GetProperty("Pitch");

			const float defaultVolume = 0.5f;
			volume.Value = defaultVolume;
			*/

			AI ai = entity.GetOrCreate<AI>("AI");

			ModelAlpha model = entity.GetOrCreate<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "Models\\alpha-box";
			model.Editable = false;
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

			Property<int> operationalRadius = entity.GetOrMakeProperty<int>("OperationalRadius", true, 100);

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

			ai.Add(new AI.State
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, },
			});

			const float sightDistance = 40.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.State
			{
				Name = "Idle",
				Enter = delegate(AI.State previous)
				{
					//pitch.Value = -0.5f;
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

			Property<Entity.Handle> targetAgent = entity.GetOrMakeProperty<Entity.Handle>("TargetAgent");

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

			ai.Add(new AI.State
			{
				Name = "Chase",
				Enter = delegate(AI.State previous)
				{
					//pitch.Value = 0.0f;
				},
				Exit = delegate(AI.State next)
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
							raycastAI.Move(targetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
						}
					},
					new AI.Task
					{
						Action = delegate()
						{
							if ((targetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position).Length() < 10.0f)
								ai.CurrentState.Value = "Explode";
						}
					},
					updatePosition,
				},
			});

			ListProperty<Map.Coordinate> coordQueue = entity.GetOrMakeListProperty<Map.Coordinate>("CoordQueue");

			Property<Map.Coordinate> explosionOriginalCoord = entity.GetOrMakeProperty<Map.Coordinate>("ExplosionOriginalCoord");

			EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
			Map.CellState infectedState = Map.States[Map.t.Infected];

			ai.Add(new AI.State
			{
				Name = "Explode",
				Enter = delegate(AI.State previous)
				{
					coordQueue.Clear();
					
					Map m = raycastAI.Map.Value.Target.Get<Map>();

					Map.Coordinate c = raycastAI.Coord.Value;

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

					explosionOriginalCoord.Value = raycastAI.Coord;

					Direction right;
					if (up.IsParallel(Direction.PositiveX))
						right = Direction.PositiveZ;
					else
						right = Direction.PositiveX;
					Direction forward = up.Cross(right);

					for (Map.Coordinate y = c.Clone(); y.GetComponent(up) < c.GetComponent(up) + 3; y = y.Move(up))
					{
						for (Map.Coordinate x = y.Clone(); x.GetComponent(right) < c.GetComponent(right) + 2; x = x.Move(right))
						{
							for (Map.Coordinate z = x.Clone(); z.GetComponent(forward) < c.GetComponent(forward) + 2; z = z.Move(forward))
								coordQueue.Add(z);
						}
					}
				},
				Exit = delegate(AI.State next)
				{
					coordQueue.Clear();
					//volume.Value = defaultVolume;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					new AI.Task
					{
						Interval = 0.2f,
						Action = delegate()
						{
							if (coordQueue.Count > 0)
							{
								raycastAI.MoveTo(coordQueue[0]);

								coordQueue.RemoveAt(0);

								Entity block = factory.CreateAndBind(main);
								infectedState.ApplyToEffectBlock(block.Get<ModelInstance>());

								Entity mapEntity = raycastAI.Map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									Map m = raycastAI.Map.Value.Target.Get<Map>();

									block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(raycastAI.Coord);

									Vector3 absolutePos = m.GetAbsolutePosition(raycastAI.Coord);

									block.GetProperty<Vector3>("StartPosition").Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f);
									block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f) * Matrix.CreateRotationY(0.15f);
									block.GetProperty<float>("TotalLifetime").Value = 0.05f;
									factory.Setup(block, raycastAI.Map.Value.Target, raycastAI.Coord, Map.t.Infected);
									main.Add(block);
								}
							}
						}
					},
					new AI.Task
					{
						Action = delegate()
						{
							//volume.Value = MathHelper.Lerp(defaultVolume, 1.0f, ai.TimeInCurrentState.Value / 2.0f);
							//pitch.Value = MathHelper.Lerp(0.0f, 0.5f, ai.TimeInCurrentState.Value / 2.0f);
							if (coordQueue.Count == 0)
							{
								// Explode
								ai.CurrentState.Value = "Exploding";
							}
						},
					},
					updatePosition,
				},
			});

			Property<bool> exploded = entity.GetOrMakeProperty<bool>("Exploded");

			ai.Add(new AI.State
			{
				Name = "Exploding",
				Enter = delegate(AI.State previous)
				{
					exploded.Value = false;
					AkSoundEngine.PostEvent("Stop_cube_drone", entity);
				},
				Exit = delegate(AI.State next)
				{
					exploded.Value = false;
					AkSoundEngine.PostEvent("Play_cube_drone", entity);
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
							if (timeInCurrentState > 1.0f && !exploded)
							{
								Entity mapEntity = raycastAI.Map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
									Explosion.Explode(main, raycastAI.Map.Value.Target.Get<Map>(), raycastAI.Coord, radius, 18.0f);

								exploded.Value = true;
							}

							if (timeInCurrentState > 2.0f)
							{
								Entity mapEntity = raycastAI.Map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									Map m = raycastAI.Map.Value.Target.Get<Map>();
									Map.Coordinate? closestCell = m.FindClosestFilledCell(raycastAI.Coord, radius + 1);
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
		}
	}
}
