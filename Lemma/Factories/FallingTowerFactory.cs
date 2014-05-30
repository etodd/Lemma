using System; using ComponentBind;
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
	public class FallingTowerFactory : Factory<Main>
	{
		public FallingTowerFactory()
		{
			
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "FallingTower");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			EnemyBase enemy = entity.GetOrCreate<EnemyBase>("Base");
			PlayerCylinderTrigger trigger = entity.GetOrCreate<PlayerCylinderTrigger>("Trigger");

			PointLight light = entity.GetOrCreate<PointLight>();
			light.Color.Value = new Vector3(1.3f, 0.5f, 0.5f);
			light.Attenuation.Value = 15.0f;
			light.Serialize = false;

			ListProperty<Entity.Handle> dynamicVoxels = entity.GetOrMakeListProperty<Entity.Handle>("DynamicVoxels");
			Property<float> timeUntilRebuild = entity.GetOrMakeProperty<float>("TimeUntilRebuild");
			Property<float> timeUntilRebuildComplete = entity.GetOrMakeProperty<float>("TimeUntilRebuildComplete");
			Property<float> rebuildDelay = entity.GetOrMakeProperty<float>("RebuildDelay", true, 4.0f);
			Property<float> rebuildTime = entity.GetOrMakeProperty<float>("RebuildTime", true, 1.0f);

			const float rebuildTimeMultiplier = 0.03f;

			enemy.Add(new CommandBinding(enemy.Delete, entity.Delete));
			enemy.Add(new Binding<Matrix>(enemy.Transform, transform.Matrix));
			light.Add(new Binding<Vector3>(light.Position, enemy.Position));

			trigger.Add(new Binding<Matrix>(trigger.Transform, () => Matrix.CreateTranslation(0.0f, 0.0f, enemy.Offset) * transform.Matrix, transform.Matrix, enemy.Offset));

			Action fall = delegate()
			{
				if (timeUntilRebuild.Value > 0 || timeUntilRebuildComplete.Value > 0)
					return;

				if (!enemy.IsValid)
				{
					entity.Delete.Execute();
					return;
				}

				// Disable the cell-emptied notification.
				// This way, we won't think that the base has been destroyed by the player.
				// We are not in fact dying, we're just destroying the base so we can fall over.
				enemy.EnableCellEmptyBinding = false;

				Voxel m = enemy.Voxel.Value.Target.Get<Voxel>();

				m.Empty(enemy.BaseBoxes.SelectMany(x => x.GetCoords()));

				m.Regenerate(delegate(List<DynamicVoxel> spawnedMaps)
				{
					if (spawnedMaps.Count == 0)
						entity.Delete.Execute();
					else
					{
						Vector3 playerPos = PlayerFactory.Instance.Get<Transform>().Position;
						playerPos += PlayerFactory.Instance.Get<Player>().Character.LinearVelocity.Value * 0.65f;
						foreach (DynamicVoxel newMap in spawnedMaps)
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
							dynamicVoxels.Add(newMap.Entity);
						}
					}
				});

				timeUntilRebuild.Value = rebuildDelay;
			};

			entity.Add(new PostInitialization
			{
				delegate()
				{
					foreach (Entity.Handle map in dynamicVoxels)
					{
						if (map.Target != null)
						{
							BEPUphysics.Entities.MorphableEntity e = map.Target.Get<DynamicVoxel>().PhysicsEntity;
							e.Material.KineticFriction = 1.0f;
							e.Material.StaticFriction = 1.0f;
						}
					}
				}
			});

			entity.Add(new CommandBinding(trigger.PlayerEntered, fall));

			entity.Add(new Updater
			{
				delegate(float dt)
				{
					if (timeUntilRebuild > 0)
					{
						if (enemy.Voxel.Value.Target == null || !enemy.Voxel.Value.Target.Active)
						{
							entity.Delete.Execute();
							return;
						}

						float newValue = Math.Max(0.0f, timeUntilRebuild.Value - dt);
						timeUntilRebuild.Value = newValue;
						if (newValue == 0.0f)
						{
							// Rebuild
							Voxel m = enemy.Voxel.Value.Target.Get<Voxel>();

							int index = 0;

							Vector3 baseCenter = Vector3.Zero;

							EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();

							Entity targetMap = enemy.Voxel.Value.Target;

							foreach (Voxel.Coord c in enemy.BaseBoxes.SelectMany(x => x.GetCoords()))
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

							foreach (Entity.Handle e in dynamicVoxels)
							{
								Entity dynamicMap = e.Target;
								Voxel dynamicMapComponent = dynamicMap != null && dynamicMap.Active ? dynamicMap.Get<Voxel>() : null;

								if (dynamicMap == null || !dynamicMap.Active)
									continue;

								Matrix orientation = dynamicMapComponent.Transform.Value;
								orientation.Translation = Vector3.Zero;

								List<Voxel.Coord> coords = new List<Voxel.Coord>();

								foreach (Voxel.Coord c in dynamicMapComponent.Chunks.SelectMany(x => x.Boxes).SelectMany(x => x.GetCoords()))
								{
									if (m[c].ID == 0)
										coords.Add(c);
								}

								foreach (Voxel.Coord c in coords.OrderBy(x => (new Vector3(x.X, x.Y, x.Z) - baseCenter).LengthSquared()))
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
							dynamicVoxels.Clear();
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
								entity.Delete.Execute();
							else
							{
								enemy.EnableCellEmptyBinding = !main.EditorEnabled;
								if (trigger.IsTriggered)
									fall();
							}
						}
					}
				}
			});

			this.SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			EnemyBase.AttachEditorComponents(entity, main, this.Color);

			PlayerCylinderTrigger.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
