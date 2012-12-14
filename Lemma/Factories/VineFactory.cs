using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using System.Threading;

namespace Lemma.Factories
{
	public class VineFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Vine");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(0.5f, 1.3f, 0.5f);
			light.Attenuation.Value = 10.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			result.Add("Coordinate", new Property<Map.Coordinate> { Editable = false });

			result.Add("OperationalRadius", new Property<float> { Editable = true, Value = 100.0f });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;
			Transform transform = result.Get<Transform>();
			PointLight light = result.Get<PointLight>();
			EnemyBase enemy = result.GetOrCreate<EnemyBase>("Base");
			Property<bool> hasCoordinate = result.GetOrMakeProperty<bool>("HasCoordinate");
			hasCoordinate.Editable = false;

			enemy.Add(new Binding<Matrix>(enemy.Transform, transform.Matrix));
			enemy.Add(new CommandBinding(enemy.Delete, result.Delete));

			Property<Map.Coordinate> coordinate = result.GetProperty<Map.Coordinate>("Coordinate");
			Property<float> operationalRadius = result.GetOrMakeProperty<float>("OperationalRadius", true, 100.0f);

			if (!hasCoordinate)
			{
				// Find our starting coordinate
				result.Add(new NotifyBinding(delegate()
				{
					coordinate.Value = enemy.BaseBoxes.First().GetCoords().First();
					hasCoordinate.Value = true;
				}, enemy.Map));
			}

			if (!main.EditorEnabled)
			{
				PointLight positionLight = new PointLight();
				positionLight.Serialize = false;
				positionLight.Color.Value = new Vector3(1.5f, 0.5f, 0.5f);
				positionLight.Attenuation.Value = 20.0f;
				positionLight.Shadowed.Value = false;
				result.Add("PositionLight", positionLight);

				positionLight.Add(new Binding<Vector3>(positionLight.Position, () => enemy.Map.Value.Target != null && enemy.Map.Value.Target.Active ? enemy.Map.Value.Target.Get<Map>().GetAbsolutePosition(coordinate) : Vector3.Zero, enemy.Map, coordinate));
			}

			light.Add(new Binding<Vector3>(light.Position, enemy.Position));

			Entity player = null;

			const float movementInterval = 0.075f;
			const float cageMovementInterval = 0.015f;
			const float damageTime = 3.0f;

			float lastMovement = -1.0f;
			float lastPathCalculation = -1.0f;

			List<Map.Coordinate> path = new List<Map.Coordinate>();

			bool lastClosingInValue = false;

			List<Map.Coordinate> cageCoordinates = new List<Map.Coordinate>();

			result.Add(new Updater
			{
				delegate(float dt)
				{
					if (player == null || !player.Active)
					{
						player = main.Get("Player").FirstOrDefault();
						if (player == null || !player.Active)
							return;
					}

					if (path == null || path.Count == 0 || main.TotalTime - lastPathCalculation > 2.0f)
					{
						lastMovement = main.TotalTime;

						Vector3 target = player.Get<Transform>().Position;

						if (enemy.Map.Value.Target != null && enemy.Map.Value.Target.Active)
						{
							Map m = enemy.Map.Value.Target.Get<Map>();
							if ((m.GetAbsolutePosition(coordinate) - target).Length() > operationalRadius && (m.GetAbsolutePosition(enemy.BaseBoxes.First().GetCoords().First()) - target).Length() > operationalRadius)
								return;
							path = m.CustomAStar(coordinate, m.GetCoordinate(target));
							lastPathCalculation = main.TotalTime;
						}
						else
						{
							result.Delete.Execute();
							return;
						}
					}

					float interval = movementInterval;
					Map m2 = enemy.Map.Value.Target.Get<Map>();

					Vector3 currentPosition = m2.GetAbsolutePosition(coordinate);

					bool closingIn = (currentPosition - player.Get<Transform>().Position).Length() < 5.0f;

					if (closingIn)
						interval = cageMovementInterval;

					if (closingIn && !lastClosingInValue)
					{
						// Set up cage
						cageCoordinates.Clear();
						Map.Coordinate center = m2.GetCoordinate(player.Get<Transform>().Position);

						// Coordinates are built in backwards order.

						int radius = 1;

						// Top
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								cageCoordinates.Add(new Map.Coordinate { X = x, Y = center.Y + 3, Z = z });
						}

						// Outer shell
						radius = 2;
						for (int y = center.Y + 3; y >= center.Y - 3; y--)
						{
							// Left
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								cageCoordinates.Add(new Map.Coordinate { X = center.X - radius, Y = y, Z = z });

							// Right
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								cageCoordinates.Add(new Map.Coordinate { X = center.X + radius, Y = y, Z = z });

							// Backward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								cageCoordinates.Add(new Map.Coordinate { X = x, Y = y, Z = center.Z - radius });

							// Forward
							for (int x = center.X - radius; x <= center.X + radius; x++)
								cageCoordinates.Add(new Map.Coordinate { X = x, Y = y, Z = center.Z + radius });
						}

						// Bottom
						for (int x = center.X - radius; x <= center.X + radius; x++)
						{
							for (int z = center.Z - radius; z <= center.Z + radius; z++)
								cageCoordinates.Add(new Map.Coordinate { X = x, Y = center.Y - 4, Z = z });
						}
					}
					lastClosingInValue = closingIn;

					bool regenerate = false;
					while (main.TotalTime - lastMovement > interval)
					{
						if (closingIn)
						{
							player.Get<Player>().Health.Value -= interval / damageTime;
							if (!player.Active)
								break; // We killed her
							if (cageCoordinates.Count > 0)
							{
								coordinate.Value = cageCoordinates[cageCoordinates.Count - 1];
								cageCoordinates.RemoveAt(cageCoordinates.Count - 1);
							}
						}
						else if (path != null && path.Count > 0)
						{
							coordinate.Value = path[0];
							path.RemoveAt(0);
						}
						else
							break;
						
						if (enemy.BaseBoxes.FirstOrDefault(x => x.Contains(coordinate)) == null)
						{
							regenerate |= m2.Empty(coordinate);
							regenerate |= m2.Fill(coordinate, WorldFactory.StatesByName["Gravel"]);
						}
						Sound.PlayCue(main, "VineMove", currentPosition);
						lastMovement += interval;
					}
					if (regenerate)
						m2.Regenerate();
				}
			});

			EnemyBase.SpawnPickupsOnDeath(main, result);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			EnemyBase.AttachEditorComponents(result, main, this.Color);
		}
	}
}
