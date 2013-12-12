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

			VoxelChaseAI chase = result.GetOrCreate<VoxelChaseAI>("VoxelChaseAI");

			chase.Filter = delegate(Map.CellState state)
			{
				return state.ID == 0 ? VoxelChaseAI.Cell.Empty : VoxelChaseAI.Cell.Filled;
			};

			chase.Add(new TwoWayBinding<Vector3>(transform.Position, chase.Position));
			result.Add(new CommandBinding(chase.Delete, result.Delete));

			Sound sound = result.GetOrCreate<Sound>("LoopSound");
			sound.Serialize = false;
			sound.Cue.Value = "Orb Loop";
			sound.Is3D.Value = true;
			sound.IsPlaying.Value = true;
			sound.Add(new Binding<Vector3>(sound.Position, chase.Position));
			Property<float> volume = sound.GetProperty("Volume");
			Property<float> pitch = sound.GetProperty("Pitch");

			const float defaultVolume = 0.5f;
			volume.Value = defaultVolume;

			AI ai = result.GetOrCreate<AI>("AI");

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
					float source = ((float)this.random.NextDouble() - 0.5f) * 2.0f;
					model.Scale.Value = new Vector3(defaultModelScale * (1.0f + (source * 0.5f)));
					light.Attenuation.Value = defaultLightAttenuation * (1.0f + (source * 0.05f));
				}
			});

			model.Add(new Binding<bool, string>(model.Enabled, x => x != "Exploding", ai.CurrentState));

			light.Add(new Binding<Vector3>(light.Color, model.Color));

			Agent agent = result.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, chase.Position));

			Property<int> operationalRadius = result.GetOrMakeProperty<int>("OperationalRadius", true, 100);

			AI.Task checkOperationalRadius = new AI.Task
			{
				Interval = 2.0f,
				Action = delegate()
				{
					bool shouldBeActive = (chase.Position.Value - main.Camera.Position).Length() < operationalRadius;
					if (shouldBeActive && ai.CurrentState == "Suspended")
						ai.CurrentState.Value = "Idle";
					else if (!shouldBeActive && ai.CurrentState != "Suspended")
						ai.CurrentState.Value = "Suspended";
				},
			};

			ai.Add(new AI.State
			{
				Name = "Suspended",
				Enter = delegate(AI.State previous)
				{
					chase.Enabled.Value = false;
				},
				Exit = delegate(AI.State next)
				{
					chase.Enabled.Value = true;
				},
				Tasks = new[] { checkOperationalRadius, },
			});

			const float sightDistance = 30.0f;
			const float hearingDistance = 15.0f;

			ai.Add(new AI.State
			{
				Name = "Idle",
				Enter = delegate(AI.State previous)
				{
					chase.Speed.Value = 3.0f;
					pitch.Value = -0.5f;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					new AI.Task
					{
						Interval = 1.0f,
						Action = delegate()
						{
							Agent a = Agent.Query(chase.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
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
					chase.Enabled.Value = false;
					volume.Value = 0.0f;
				},
				Exit = delegate(AI.State next)
				{
					chase.Enabled.Value = true;
					volume.Value = defaultVolume;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
					new AI.Task
					{
						Interval = 1.0f,
						Action = delegate()
						{
							if (ai.TimeInCurrentState > 3.0f)
								ai.CurrentState.Value = "Idle";
							else
							{
								Agent a = Agent.Query(chase.Position, sightDistance, hearingDistance, x => x.Entity.Type == "Player");
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
					chase.Speed.Value = 10.0f;
					chase.TargetActive.Value = true;
					pitch.Value = 0.0f;
				},
				Exit = delegate(AI.State next)
				{
					chase.TargetActive.Value = false;
				},
				Tasks = new[]
				{
					checkOperationalRadius,
					checkTargetAgent,
					new AI.Task
					{
						Action = delegate()
						{
							Entity target = targetAgent.Value.Target;
							Vector3 targetPosition = target.Get<Transform>().Position;
							chase.Target.Value = targetPosition;
							if ((targetPosition - chase.Position).Length() < 10.0f)
								ai.CurrentState.Value = "Explode";
						}
					}
				},
			});

			ListProperty<Map.Coordinate> coordQueue = result.GetOrMakeListProperty<Map.Coordinate>("CoordQueue");

			Property<Map.Coordinate> explosionOriginalCoord = result.GetOrMakeProperty<Map.Coordinate>("ExplosionOriginalCoord");

			ai.Add(new AI.State
			{
				Name = "Explode",
				Enter = delegate(AI.State previous)
				{
					chase.Speed.Value = 5.0f;
					coordQueue.Clear();
					chase.EnablePathfinding.Value = false;
					
					Map map = chase.Map.Value.Target.Get<Map>();

					Map.Coordinate coord = chase.Coord.Value;

					Direction toSupport = Direction.None;

					foreach (Direction dir in DirectionExtensions.Directions)
					{
						if (map[coord.Move(dir)].ID != 0)
						{
							toSupport = dir;
							break;
						}
					}

					if (toSupport == Direction.None)
					{
						// Try again with the last coord
						coord = chase.LastCoord.Value;
						foreach (Direction dir in DirectionExtensions.Directions)
						{
							if (map[coord.Move(dir)].ID != 0)
							{
								toSupport = dir;
								break;
							}
						}
						if (toSupport == Direction.None)
						{
							ai.CurrentState.Value = "Idle";
							return;
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

					for (Map.Coordinate y = coord.Clone(); y.GetComponent(up) < coord.GetComponent(up) + 3; y = y.Move(up))
					{
						for (Map.Coordinate x = y.Clone(); x.GetComponent(right) < coord.GetComponent(right) + 2; x = x.Move(right))
						{
							for (Map.Coordinate z = x.Clone(); z.GetComponent(forward) < coord.GetComponent(forward) + 2; z = z.Move(forward))
								coordQueue.Add(z);
						}
					}
				},
				Exit = delegate(AI.State next)
				{
					coordQueue.Clear();
					chase.EnablePathfinding.Value = true;
					chase.LastCoord.Value = chase.Coord.Value = explosionOriginalCoord;
					volume.Value = defaultVolume;
				},
				Tasks = new[]
				{ 
					checkOperationalRadius,
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
				},
			});

			Property<bool> exploded = result.GetOrMakeProperty<bool>("Exploded");

			ai.Add(new AI.State
			{
				Name = "Exploding",
				Enter = delegate(AI.State previous)
				{
					chase.EnablePathfinding.Value = false;
					exploded.Value = false;
					sound.Stop.Execute(AudioStopOptions.AsAuthored);
				},
				Exit = delegate(AI.State next)
				{
					chase.EnablePathfinding.Value = true;
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
							const int radius = 8;

							float timeInCurrentState = ai.TimeInCurrentState;
							if (timeInCurrentState > 1.0f && !exploded)
							{
								Map map = chase.Map.Value.Target.Get<Map>();
								Explosion.Explode(main, map, chase.Coord, radius, 18.0f);
								exploded.Value = true;
							}

							if (timeInCurrentState > 2.0f)
							{
								Map map = chase.Map.Value.Target.Get<Map>();
								Map.Coordinate? closestCell = map.FindClosestFilledCell(chase.Coord, radius + 1);
								if (closestCell.HasValue)
								{
									chase.Blend.Value = 0.0f;
									chase.Coord.Value = closestCell.Value;
									ai.CurrentState.Value = "Alert";
								}
								else
									result.Delete.Execute();
							}
						},
					},
				},
			});

			EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
			Map.CellState infectedState = WorldFactory.StatesByName["Infected"];
			chase.Add(new CommandBinding<Map, Map.Coordinate>(chase.Moved, delegate(Map m, Map.Coordinate c)
			{
				if (chase.Active)
				{
					if (coordQueue.Count > 0)
					{
						Map.Coordinate coord = chase.Coord.Value = coordQueue[0];
						coordQueue.RemoveAt(0);

						Entity block = factory.CreateAndBind(main);
						infectedState.ApplyToEffectBlock(block.Get<ModelInstance>());

						Map map = chase.Map.Value.Target.Get<Map>();

						block.GetProperty<Vector3>("Offset").Value = map.GetRelativePosition(coord);

						Vector3 absolutePos = map.GetAbsolutePosition(coord);

						block.GetProperty<Vector3>("StartPosition").Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f);
						block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f) * Matrix.CreateRotationY(0.15f);
						block.GetProperty<float>("TotalLifetime").Value = 0.05f;
						factory.Setup(block, chase.Map.Value.Target, coord, infectedState.ID);
						main.Add(block);
					}
				}
			}));

			this.SetMain(result, main);
		}
	}
}
