using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class FallingTowerFactory : Factory
	{
		public FallingTowerFactory()
		{
			
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "FallingTower");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			result.Add("Trigger", new PlayerCylinderTrigger());

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(0.5f, 1.3f, 0.5f);
			light.Attenuation.Value = 10.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			result.Add("DynamicMaps", new ListProperty<Entity.Handle> { Editable = false });
			result.Add("TimeUntilRebuild", new Property<float> { Editable = false });
			result.Add("TimeUntilRebuildComplete", new Property<float> { Editable = false });
			result.Add("RebuildDelay", new Property<float> { Editable = true, Value = 4.0f });
			result.Add("RebuildTime", new Property<float> { Editable = true, Value = 1.0f });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			EnemyBase enemy = result.GetOrCreate<EnemyBase>("Base");
			PlayerCylinderTrigger trigger = result.Get<PlayerCylinderTrigger>();
			PointLight light = result.Get<PointLight>();
			ListProperty<Entity.Handle> dynamicMaps = result.GetListProperty<Entity.Handle>("DynamicMaps");
			Property<float> timeUntilRebuild = result.GetProperty<float>("TimeUntilRebuild");
			Property<float> timeUntilRebuildComplete = result.GetProperty<float>("TimeUntilRebuildComplete");
			Property<float> rebuildDelay = result.GetProperty<float>("RebuildDelay");
			Property<float> rebuildTime = result.GetProperty<float>("RebuildTime");

			const float rebuildTimeMultiplier = 0.03f;

			enemy.Add(new CommandBinding(enemy.Delete, result.Delete));
			enemy.Add(new Binding<Matrix>(enemy.Transform, transform.Matrix));
			light.Add(new Binding<Vector3>(light.Position, enemy.Position));

			trigger.Add(new Binding<Matrix>(trigger.Transform, () => Matrix.CreateTranslation(0.0f, 0.0f, enemy.Offset) * transform.Matrix, transform.Matrix, enemy.Offset));

			Action<Entity> fall = delegate(Entity player)
			{
				if (timeUntilRebuild.Value > 0 || timeUntilRebuildComplete.Value > 0)
					return;

				if (!enemy.IsValid)
				{
					result.Delete.Execute();
					return;
				}

				// Disable the cell-emptied notification.
				// This way, we won't think that the base has been destroyed by the player.
				// We are not in fact dying, we're just destroying the base so we can fall over.
				enemy.EnableCellEmptyBinding = false;

				Map m = enemy.Map.Value.Target.Get<Map>();

				m.Empty(enemy.BaseBoxes.SelectMany(x => x.GetCoords()));

				m.Regenerate(delegate(List<DynamicMap> spawnedMaps)
				{
					Vector3 playerPos = player.Get<Transform>().Position;
					playerPos += player.Get<Player>().LinearVelocity.Value * 0.65f;
					foreach (DynamicMap newMap in spawnedMaps)
					{
						Vector3 toPlayer = playerPos - newMap.PhysicsEntity.Position;
						toPlayer.Normalize();
						if (Math.Abs(toPlayer.Y) < 0.9f)
						{
							toPlayer *= 25.0f * newMap.PhysicsEntity.Mass;

							Vector3 positionAtPlayerHeight = newMap.PhysicsEntity.Position;
							Vector3 impulseAtBase = toPlayer * -0.75f;
							impulseAtBase.Y = 0.0f;
							positionAtPlayerHeight.Y = playerPos.Y;
							newMap.PhysicsEntity.ApplyImpulse(ref positionAtPlayerHeight, ref impulseAtBase);

							newMap.PhysicsEntity.ApplyLinearImpulse(ref toPlayer);
						}
						newMap.PhysicsEntity.Material.KineticFriction = 1.0f;
						newMap.PhysicsEntity.Material.StaticFriction = 1.0f;
						dynamicMaps.Add(newMap.Entity);
					}
				});

				timeUntilRebuild.Value = rebuildDelay;
			};

			result.Add(new PostInitialization
			{
				delegate()
				{
					foreach (Entity.Handle map in dynamicMaps)
					{
						if (map.Target != null)
						{
							BEPUphysics.Entities.MorphableEntity e = map.Target.Get<DynamicMap>().PhysicsEntity;
							e.Material.KineticFriction = 1.0f;
							e.Material.StaticFriction = 1.0f;
						}
					}
				}
			});

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, fall));

			result.Add(new Updater
			{
				delegate(float dt)
				{
					if (timeUntilRebuild > 0)
					{
						if (enemy.Map.Value.Target == null || !enemy.Map.Value.Target.Active)
						{
							result.Delete.Execute();
							return;
						}

						float newValue = Math.Max(0.0f, timeUntilRebuild.Value - dt);
						timeUntilRebuild.Value = newValue;
						if (newValue == 0.0f)
						{
							// Rebuild
							Map m = enemy.Map.Value.Target.Get<Map>();

							int index = 0;

							Vector3 baseCenter = Vector3.Zero;

							EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();

							Entity targetMap = enemy.Map.Value.Target;

							foreach (Map.Coordinate c in enemy.BaseBoxes.SelectMany(x => x.GetCoords()))
							{
								if (m[c].ID == 0)
								{
									Entity block = factory.CreateAndBind(main);
									c.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
									block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(c);
									block.GetProperty<Vector3>("StartPosition").Value = m.GetAbsolutePosition(c) + new Vector3(0.25f, 0.5f, 0.25f) * index;
									block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * index) * Matrix.CreateRotationY(0.15f * index);
									block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * rebuildTimeMultiplier * rebuildTime);
									factory.Setup(block, targetMap, c, c.Data.ID);
									main.Add(block);
									index++;
									baseCenter += new Vector3(c.X, c.Y, c.Z);
								}
							}

							baseCenter /= index; // Get the average position of the base cells

							foreach (Entity.Handle e in dynamicMaps)
							{
								Entity dynamicMap = e.Target;
								Map dynamicMapComponent = dynamicMap != null && dynamicMap.Active ? dynamicMap.Get<Map>() : null;

								if (dynamicMap == null || !dynamicMap.Active)
									continue;

								Matrix orientation = dynamicMapComponent.Transform.Value;
								orientation.Translation = Vector3.Zero;

								List<Map.Coordinate> coords = new List<Map.Coordinate>();

								foreach (Map.Coordinate c in dynamicMapComponent.Chunks.SelectMany(x => x.Boxes).SelectMany(x => x.GetCoords()))
								{
									if (m[c].ID == 0)
										coords.Add(c);
								}

								foreach (Map.Coordinate c in coords.OrderBy(x => (new Vector3(x.X, x.Y, x.Z) - baseCenter).LengthSquared()))
								{
									Entity block = factory.CreateAndBind(main);
									c.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
									block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(c);
									block.GetProperty<bool>("Scale").Value = dynamicMapComponent == null;
									if (dynamicMapComponent != null && dynamicMapComponent[c].ID == c.Data.ID)
									{
										block.GetProperty<Vector3>("StartPosition").Value = dynamicMapComponent.GetAbsolutePosition(c);
										block.GetProperty<Matrix>("StartOrientation").Value = orientation;
									}
									else
									{
										block.GetProperty<Vector3>("StartPosition").Value = m.GetAbsolutePosition(c) + new Vector3(0.25f, 0.5f, 0.25f) * index;
										block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * index) * Matrix.CreateRotationY(0.15f * index);
									}
									block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * rebuildTimeMultiplier * rebuildTime);
									factory.Setup(block, targetMap, c, c.Data.ID);
									main.Add(block);
									index++;
								}
								dynamicMap.Delete.Execute();
							}
							timeUntilRebuildComplete.Value = 0.05f + (index * rebuildTimeMultiplier * rebuildTime);
							dynamicMaps.Clear();
						}
					}
					else if (timeUntilRebuildComplete > 0)
					{
						// Rebuilding
						float newValue = Math.Max(0.0f, timeUntilRebuildComplete.Value - dt);
						timeUntilRebuildComplete.Value = newValue;
						if (newValue == 0.0f)
						{
							// Done rebuilding
							if (!enemy.IsValid)
								result.Delete.Execute();
							else
							{
								enemy.EnableCellEmptyBinding = !main.EditorEnabled;
								if (trigger.IsTriggered)
									fall(trigger.Player.Value.Target);
							}
						}
					}
				}
			});

			EnemyBase.SpawnPickupsOnDeath(main, result);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			EnemyBase.AttachEditorComponents(result, main, this.Color);

			PlayerCylinderTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
