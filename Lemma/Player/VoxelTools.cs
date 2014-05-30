using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class VoxelTools : Component<Main>
	{
		// Input properties
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Height = new Property<float>();
		public Property<float> SupportHeight = new Property<float>();

		private Random random = new Random();

		private Voxel.State temporary;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.temporary = Voxel.States[Voxel.t.Temporary];
		}

		public void BuildFloor(Voxel floorMap, Voxel.Coord floorCoordinate, Direction forwardDir, Direction rightDir)
		{
			List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();

			Voxel.Coord newFloorCoordinate = floorMap.GetCoordinate(this.Position);
			floorCoordinate.SetComponent(rightDir, newFloorCoordinate.GetComponent(rightDir));
			floorCoordinate.SetComponent(forwardDir, newFloorCoordinate.GetComponent(forwardDir));

			const int radius = 3;
			for (Voxel.Coord x = floorCoordinate.Move(rightDir, -radius); x.GetComponent(rightDir) < floorCoordinate.GetComponent(rightDir) + radius; x = x.Move(rightDir))
			{
				int dx = x.GetComponent(rightDir) - floorCoordinate.GetComponent(rightDir);
				for (Voxel.Coord y = x.Move(forwardDir, -radius); y.GetComponent(forwardDir) < floorCoordinate.GetComponent(forwardDir) + radius; y = y.Move(forwardDir))
				{
					int dy = y.GetComponent(forwardDir) - floorCoordinate.GetComponent(forwardDir);
					if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && floorMap[y].ID == 0)
					{
						buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
						{
							Voxel = floorMap,
							Coordinate = y,
							State = this.temporary,
						});
					}
				}
			}
			Factory.Get<EffectBlockFactory>().Build(this.main, buildCoords, false, this.Position);
		}

		public bool BreakWalls(Vector3 forward, Vector3 right, bool breakFloor)
		{
			BlockFactory blockFactory = Factory.Get<BlockFactory>();
			Vector3 pos = this.Position + new Vector3(0, 0.1f + (this.Height * -0.5f) - this.SupportHeight, 0) + forward * -1.0f;
			Vector3 basePos = pos;
			bool broke = false;
			foreach (Voxel map in Voxel.ActivePhysicsVoxels.ToList())
			{
				List<Voxel.Coord> removals = new List<Voxel.Coord>();
				Quaternion mapQuaternion = map.Entity.Get<Transform>().Quaternion;
				pos = basePos;
				for (int i = 0; i < 5; i++)
				{
					Voxel.Coord center = map.GetCoordinate(pos);
					Voxel.Coord top = map.GetCoordinate(basePos + new Vector3(0, this.Height + this.SupportHeight + 0.5f, 0));
					Direction upDir = map.GetRelativeDirection(Vector3.Up);
					Direction rightDir = map.GetRelativeDirection(right);
					for (Voxel.Coord y = center.Move(upDir.GetReverse(), breakFloor ? 2 : 0); y.GetComponent(upDir) <= top.GetComponent(upDir); y = y.Move(upDir))
					{
						for (Voxel.Coord z = y.Move(rightDir.GetReverse(), 1); z.GetComponent(rightDir) < center.GetComponent(rightDir) + 2; z = z.Move(rightDir))
						{
							Voxel.State state = map[z];
							if (state.ID != 0 && !state.Permanent && !state.Hard && !removals.Contains(z))
							{
								broke = true;
								removals.Add(z);
								Vector3 cellPos = map.GetAbsolutePosition(z);
								Vector3 toCell = cellPos - pos;
								Entity block = blockFactory.CreateAndBind(this.main);
								Transform blockTransform = block.Get<Transform>();
								blockTransform.Position.Value = cellPos;
								blockTransform.Quaternion.Value = mapQuaternion;
								state.ApplyToBlock(block);
								toCell += forward * 4.0f;
								toCell.Normalize();
								PhysicsBlock physicsBlock = block.Get<PhysicsBlock>();
								physicsBlock.LinearVelocity.Value = toCell * 15.0f;
								physicsBlock.AngularVelocity.Value = new Vector3(((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f, ((float)this.random.NextDouble() - 0.5f) * 2.0f);
								main.Add(block);
							}
						}
					}
					pos += forward * 0.5f;
				}
				if (removals.Count > 0)
				{
					map.Empty(removals);
					map.Regenerate();
				}
			}
			return broke;
		}
	}
}
