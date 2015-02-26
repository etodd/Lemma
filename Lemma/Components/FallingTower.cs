using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class FallingTower : Component<Main>, IUpdateableComponent
	{
		// Input properties
		public Property<bool> IsTriggered = new Property<bool>();

		// Settings
		public Property<float> TimeUntilRebuild = new Property<float>();
		public Property<float> TimeUntilRebuildComplete = new Property<float>();

		public ListProperty<Entity.Handle> DynamicVoxels = new ListProperty<Entity.Handle>();

		private const float RebuildDelay = 2.5f;
		private const float RebuildTime = 1.0f;

		[XmlIgnore]
		public EnemyBase Base;

		[XmlIgnore]
		public Property<Entity.Handle> TargetVoxel = new Property<Entity.Handle>();

		const float rebuildTimeMultiplier = 0.03f;

		[XmlIgnore]
		public Command Fall = new Command();

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Fall.Action = this.fall;
		}

		private void fall()
		{
			if (this.TimeUntilRebuild.Value > 0 || this.TimeUntilRebuildComplete.Value > 0)
				return;

			Entity targetVoxel = this.TargetVoxel.Value.Target;
			if (targetVoxel == null || !targetVoxel.Active)
				return;

			// Disable the cell-emptied notification.
			// This way, we won't think that the base has been destroyed by the player.
			// We are not in fact dying, we're just destroying the base so we can fall over.
			this.Base.EnableCellEmptyBinding = false;

			Voxel m = targetVoxel.Get<Voxel>();

			m.Empty(this.Base.BaseBoxes.SelectMany(x => x.GetCoords()));

			m.Regenerate(delegate(List<DynamicVoxel> spawnedMaps)
			{
				if (spawnedMaps.Count == 0)
					this.Delete.Execute();
				else
				{
					Vector3 playerPos = PlayerFactory.Instance.Get<Transform>().Position;
					playerPos += PlayerFactory.Instance.Get<Player>().Character.LinearVelocity.Value * 0.65f;
					foreach (DynamicVoxel newMap in spawnedMaps)
					{
						newMap.Dangerous.Value = true;
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
						this.DynamicVoxels.Add(newMap.Entity);
					}
				}
			});

			this.TimeUntilRebuild.Value = RebuildDelay;
		}

		public override void Start()
		{
			foreach (Entity.Handle map in this.DynamicVoxels)
			{
				if (map.Target != null)
				{
					BEPUphysics.Entities.MorphableEntity e = map.Target.Get<DynamicVoxel>().PhysicsEntity;
					e.Material.KineticFriction = 1.0f;
					e.Material.StaticFriction = 1.0f;
				}
			}
		}

		public void Update(float dt)
		{
			if (this.TimeUntilRebuild > 0)
			{
				if (this.TargetVoxel.Value.Target == null || !this.TargetVoxel.Value.Target.Active)
				{
					this.Delete.Execute();
					return;
				}

				float newValue = Math.Max(0.0f, this.TimeUntilRebuild.Value - dt);
				this.TimeUntilRebuild.Value = newValue;
				if (newValue == 0.0f)
				{
					// Rebuild
					Entity targetMap = this.TargetVoxel.Value.Target;

					Voxel m = targetMap.Get<Voxel>();

					EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();

					int startIndex = this.Base.BaseBoxes.Sum(x => x.Volume);
					int index = startIndex;

					foreach (Entity.Handle e in this.DynamicVoxels)
					{
						Entity dynamicMap = e.Target;
						Voxel dynamicMapComponent = dynamicMap != null && dynamicMap.Active ? dynamicMap.Get<Voxel>() : null;

						if (dynamicMap == null || !dynamicMap.Active)
							continue;

						Quaternion orientation = Quaternion.CreateFromRotationMatrix(dynamicMapComponent.Transform.Value);

						List<Voxel.Coord> coords = new List<Voxel.Coord>();

						foreach (Voxel.Coord c in dynamicMapComponent.Chunks.SelectMany(x => x.Boxes).SelectMany(x => x.GetCoords()))
						{
							if (m[c].ID == 0)
								coords.Add(c);
						}

						foreach (Voxel.Coord c in coords.OrderBy(x => (new Vector3(x.X, x.Y, x.Z) - this.Base.Position).LengthSquared()))
						{
							Entity blockEntity = factory.CreateAndBind(main);
							c.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
							EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
							effectBlock.Offset.Value = m.GetRelativePosition(c);
							effectBlock.DoScale = dynamicMapComponent == null;
							if (dynamicMapComponent != null && dynamicMapComponent[c].ID == c.Data.ID)
							{
								effectBlock.StartPosition = dynamicMapComponent.GetAbsolutePosition(c);
								effectBlock.StartOrientation = orientation;
							}
							else
							{
								effectBlock.StartPosition = m.GetAbsolutePosition(c) + new Vector3(0.25f, 0.5f, 0.25f) * index;
								effectBlock.StartOrientation = Quaternion.CreateFromYawPitchRoll(0.15f * index, 0.15f * index, 0);
							}
							effectBlock.TotalLifetime = 0.05f + (index * rebuildTimeMultiplier * RebuildTime);
							effectBlock.Setup(targetMap, c, c.Data.ID);
							main.Add(blockEntity);
							index++;
						}
						dynamicMap.Delete.Execute();
					}
					this.DynamicVoxels.Clear();

					if (index > startIndex) // We built some stuff. Build the base.
					{
						int baseIndex = 0;
						foreach (Voxel.Coord c in this.Base.BaseBoxes.SelectMany(x => x.GetCoords()))
						{
							if (m[c].ID == 0)
							{
								Entity blockEntity = factory.CreateAndBind(main);
								EffectBlock effectBlock = blockEntity.Get<EffectBlock>();
								c.Data.ApplyToEffectBlock(blockEntity.Get<ModelInstance>());
								effectBlock.Offset.Value = m.GetRelativePosition(c);
								effectBlock.StartPosition = m.GetAbsolutePosition(c) + new Vector3(0.25f, 0.5f, 0.25f) * baseIndex;
								effectBlock.StartOrientation = Quaternion.CreateFromYawPitchRoll(0.15f * baseIndex, 0.15f * baseIndex, 0);
								effectBlock.TotalLifetime = 0.05f + (baseIndex * rebuildTimeMultiplier * RebuildTime);
								effectBlock.Setup(targetMap, c, c.Data.ID);
								main.Add(blockEntity);
								baseIndex++;
							}
						}
						this.TimeUntilRebuildComplete.Value = 0.05f + (index * rebuildTimeMultiplier * RebuildTime);
					}
					else // We didn't build anything. Delete ourselves.
						this.Delete.Execute();
				}
			}
			else if (this.TimeUntilRebuildComplete > 0)
			{
				// Rebuilding
				float newValue = Math.Max(0.0f, this.TimeUntilRebuildComplete.Value - dt);
				this.TimeUntilRebuildComplete.Value = newValue;
				if (newValue == 0.0f)
				{
					// Done rebuilding
					if (!this.Base.IsValid)
						this.Delete.Execute();
					else
					{
						this.Base.EnableCellEmptyBinding = !main.EditorEnabled;
						if (this.IsTriggered)
							fall();
					}
				}
			}
		}
	}
}