using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework.Audio;

namespace Lemma.Factories
{
	public class OrbFactory : Factory
	{
		private Random random = new Random();

		public OrbFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Orb");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PointLight light = result.GetOrCreate<PointLight>("PointLight");
			light.Serialize = false;
			light.Shadowed.Value = true;

			const float defaultLightAttenuation = 15.0f;
			light.Attenuation.Value = defaultLightAttenuation;

			Transform transform = result.GetOrCreate<Transform>("Transform");
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			Sound sound = result.GetOrCreate<Sound>("LoopSound");
			sound.Serialize = false;
			sound.Cue.Value = "Orb Loop";
			sound.Is3D.Value = true;
			sound.IsPlaying.Value = true;
			sound.Add(new Binding<Vector3>(sound.Position, transform.Position));
			Property<float> volume = sound.GetProperty("Volume");
			Property<float> pitch = sound.GetProperty("Pitch");

			const float defaultVolume = 0.5f;
			volume.Value = defaultVolume;

			AI ai = result.GetOrCreate<AI>("AI");

			ModelAlpha model = result.GetOrCreate<ModelAlpha>();
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Filename.Value = "Models\\alpha-box";
			model.Editable = false;
			model.Serialize = false;

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

			result.Add(new Updater
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

			Agent agent = result.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));

			Property<int> operationalRadius = result.GetOrMakeProperty<int>("OperationalRadius", true, 100);

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

			Property<Entity.Handle> map = result.GetOrMakeProperty<Entity.Handle>("Map");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("Coordinate");
			Property<Entity.Handle> lastMap = result.GetOrMakeProperty<Entity.Handle>("LastMap");
			Property<Map.Coordinate> lastCoord = result.GetOrMakeProperty<Map.Coordinate>("LastCoordinate");
			Property<Direction> normal = result.GetOrMakeProperty<Direction>("Normal");

			float blend = 0.0f;

			const float blendTime = 0.1f;

			AI.Task updatePosition = new AI.Task
			{
				Action = delegate()
				{
					Entity mapEntity = map.Value.Target;
					if (mapEntity != null && mapEntity.Active)
					{
						Map currentMap = mapEntity.Get<Map>();
						Vector3 currentPosition = currentMap.GetAbsolutePosition(coord);
						Entity lastMapEntity = lastMap.Value.Target;
						if (blend < 1.0f && lastMapEntity != null && lastMapEntity.Active)
						{
							Map lastM = lastMapEntity.Get<Map>();
							transform.Position.Value = Vector3.Lerp(lastM.GetAbsolutePosition(lastCoord), currentPosition, blend);
							transform.Orientation.Value = Matrix.Lerp(lastM.Transform, currentMap.Transform, blend);
						}
						else
						{
							transform.Position.Value = currentPosition;
							transform.Orientation.Value = currentMap.Transform;
						}
						blend += main.ElapsedTime.Value / blendTime;
					}
					else
						map.Value = null;
				},
			};

			ai.Add(new AI.State
			{
				Name = "Suspended",
				Tasks = new[] { checkOperationalRadius, },
			});

			const float sightDistance = 40.0f;
			const float hearingDistance = 15.0f;
			const float movementDistance = 15.0f;

			Map.CellState avoid = WorldFactory.StatesByName["AvoidAI"];

			Action<Vector3> move = delegate(Vector3 dir)
			{
				float dirLength = dir.Length();
				if (dirLength == 0.0f)
					return; // We're already where we need to be
				else
					dir /= dirLength; // Normalize

				Vector3 pos;

				if (map.Value.Target != null && map.Value.Target.Active)
				{
					Map m = map.Value.Target.Get<Map>();
					Map.Coordinate adjacent = coord.Value.Move(normal);
					if (m[adjacent].ID == 0)
						pos = m.GetAbsolutePosition(adjacent);
					else
						pos = transform.Position;
				}
				else
					pos = transform.Position;

				float radius = 0.0f;
				const int attempts = 20;

				Vector3 up = Vector3.Up;
				if ((float)Math.Abs(dir.Y) == 1.0f)
					up = Vector3.Right;

				Matrix mat = Matrix.Identity;
				mat.Forward = -dir;
				mat.Right = Vector3.Cross(dir, up);
				mat.Up = Vector3.Cross(mat.Right, dir);
				
				for (int i = 0; i < attempts; i++)
				{
					float x = ((float)Math.PI * 0.5f) + (((float)this.random.NextDouble() * 2.0f) - 1.0f) * radius;
					float y = (((float)this.random.NextDouble() * 2.0f) - 1.0f) * radius;
					Vector3 ray = new Vector3((float)Math.Cos(x) * (float)Math.Cos(y), (float)Math.Sin(y), (float)Math.Sin(x) * (float)Math.Cos(y));
					Map.GlobalRaycastResult hit = Map.GlobalRaycast(pos, Vector3.TransformNormal(ray, mat), movementDistance);
					if (hit.Map != null && hit.Distance > 2.0f && hit.Coordinate.Value.Data != avoid)
					{
						foreach (Water w in Water.ActiveInstances)
						{
							if (w.Fluid.BoundingBox.Contains(hit.Position) != ContainmentType.Disjoint)
								continue;
						}

						Map.Coordinate newCoord = hit.Coordinate.Value.Move(hit.Normal);
						if (hit.Map[newCoord].ID == 0)
						{
							lastCoord.Value = coord;
							coord.Value = newCoord;
							lastMap.Value = map;
							map.Value = hit.Map.Entity;
							normal.Value = hit.Normal;
							blend = 0.0f;
							break;
						}
					}
					radius += (float)Math.PI * 2.0f / (float)attempts;
				}
			};

			ai.Add(new AI.State
			{
				Name = "Idle",
				Enter = delegate(AI.State previous)
				{
					pitch.Value = -0.5f;
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
							move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
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

			Property<Entity.Handle> targetAgent = result.GetOrMakeProperty<Entity.Handle>("TargetAgent");

			ai.Add(new AI.State
			{
				Name = "Alert",
				Enter = delegate(AI.State previous)
				{
					volume.Value = 0.0f;
				},
				Exit = delegate(AI.State next)
				{
					volume.Value = defaultVolume;
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
					pitch.Value = 0.0f;
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
							move(targetAgent.Value.Target.Get<Transform>().Position.Value - transform.Position);
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

			ListProperty<Map.Coordinate> coordQueue = result.GetOrMakeListProperty<Map.Coordinate>("CoordQueue");

			Property<Map.Coordinate> explosionOriginalCoord = result.GetOrMakeProperty<Map.Coordinate>("ExplosionOriginalCoord");

			EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
			Map.CellState infectedState = WorldFactory.StatesByName["Infected"];

			ai.Add(new AI.State
			{
				Name = "Explode",
				Enter = delegate(AI.State previous)
				{
					coordQueue.Clear();
					
					Map m = map.Value.Target.Get<Map>();

					Map.Coordinate c = coord.Value;

					Direction toSupport = Direction.None;

					foreach (Direction dir in DirectionExtensions.Directions)
					{
						if (m[coord.Value.Move(dir)].ID != 0)
						{
							toSupport = dir;
							break;
						}
					}

					Direction up = toSupport.GetReverse();

					explosionOriginalCoord.Value = coord;

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
					volume.Value = defaultVolume;
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
								lastCoord.Value = coord;
								lastMap.Value = map;
								coord.Value = coordQueue[0];
								blend = 0.0f;

								coordQueue.RemoveAt(0);

								Entity block = factory.CreateAndBind(main);
								infectedState.ApplyToEffectBlock(block.Get<ModelInstance>());

								Entity mapEntity = map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									Map m = map.Value.Target.Get<Map>();

									block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(coord);

									Vector3 absolutePos = m.GetAbsolutePosition(coord);

									block.GetProperty<Vector3>("StartPosition").Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f);
									block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f) * Matrix.CreateRotationY(0.15f);
									block.GetProperty<float>("TotalLifetime").Value = 0.05f;
									factory.Setup(block, map.Value.Target, coord, infectedState.ID);
									main.Add(block);
								}
							}
						}
					},
					new AI.Task
					{
						Action = delegate()
						{
							volume.Value = MathHelper.Lerp(defaultVolume, 1.0f, ai.TimeInCurrentState.Value / 2.0f);
							pitch.Value = MathHelper.Lerp(0.0f, 0.5f, ai.TimeInCurrentState.Value / 2.0f);
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

			Property<bool> exploded = result.GetOrMakeProperty<bool>("Exploded");

			ai.Add(new AI.State
			{
				Name = "Exploding",
				Enter = delegate(AI.State previous)
				{
					exploded.Value = false;
					sound.Stop.Execute(AudioStopOptions.AsAuthored);
				},
				Exit = delegate(AI.State next)
				{
					exploded.Value = false;
					volume.Value = defaultVolume;
					sound.Play.Execute();
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
								Entity mapEntity = map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
									Explosion.Explode(main, map.Value.Target.Get<Map>(), coord, radius, 18.0f);

								exploded.Value = true;
							}

							if (timeInCurrentState > 2.0f)
							{
								Entity mapEntity = map.Value.Target;
								if (mapEntity != null && mapEntity.Active)
								{
									Map m = map.Value.Target.Get<Map>();
									Map.Coordinate? closestCell = m.FindClosestFilledCell(coord, radius + 1);
									if (closestCell.HasValue)
									{
										move(m.GetAbsolutePosition(closestCell.Value) - transform.Position);
										ai.CurrentState.Value = "Alert";
									}
									else
										result.Delete.Execute();
								}
								else // Our map got deleted. Hope we find a new one.
								{
									move(new Vector3(((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f, ((float)this.random.NextDouble() * 2.0f) - 1.0f));
									ai.CurrentState.Value = "Alert";
								}
							}
						},
					},
					updatePosition,
				},
			});

			this.SetMain(result, main);
		}
	}
}
