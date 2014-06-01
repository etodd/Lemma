using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class EffectBlockFactory : Factory<Main>
	{
		public struct BlockEntry
		{
			public Voxel Voxel;
			public Voxel.Coord Coordinate;
		}

		public struct BlockBuildOrder
		{
			public Voxel Voxel;
			public Voxel.Coord Coordinate;
			public Voxel.State State;
		}

		private Dictionary<BlockEntry, bool> animatingBlocks = new Dictionary<BlockEntry, bool>();

		private Random random = new Random();

		public bool IsAnimating(BlockEntry block)
		{
			return this.animatingBlocks.ContainsKey(block);
		}

		public EffectBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "EffectBlock");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Scale.Value = Vector3.Zero;

			Property<bool> scale = entity.GetOrMakeProperty<bool>("Scale", false, true);
			Property<Vector3> start = entity.GetOrMakeProperty<Vector3>("StartPosition");
			start.Set = delegate(Vector3 value)
			{
				start.InternalValue = value;
				transform.Position.Value = value;
			};
			Property<Matrix> startOrientation = entity.GetOrMakeProperty<Matrix>("StartOrientation");
			Vector3 startEuler = Vector3.Zero;
			startOrientation.Set = delegate(Matrix value)
			{
				startOrientation.InternalValue = value;
				startEuler = Quaternion.CreateFromRotationMatrix(startOrientation).ToEuler();
				transform.Orientation.Value = value;
			};

			Property<Entity.Handle> voxel = entity.GetOrMakeProperty<Entity.Handle>("TargetVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("TargetCoord");
			Property<Voxel.t> stateId = entity.GetOrMakeProperty<Voxel.t>("TargetVoxelState");

			Property<float> totalLifetime = entity.GetOrMakeProperty<float>("TotalLifetime");
			Property<float> lifetime = entity.GetOrMakeProperty<float>("Lifetime");

			Property<bool> checkAdjacent = entity.GetOrMakeProperty<bool>("CheckAdjacent");

			Updater update = null;
			update = new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;

					float blend = lifetime / totalLifetime;

					if (voxel.Value.Target == null || !voxel.Value.Target.Active)
					{
						entity.Delete.Execute();
						return;
					}

					Voxel m = voxel.Value.Target.Get<Voxel>();

					if (blend > 1.0f)
					{
						if (stateId != Voxel.t.Empty)
						{
							Voxel.Coord c = coord;

							bool foundAdjacentCell = false;
							if (checkAdjacent)
							{
								bool avoid = stateId == Voxel.t.Temporary;
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Voxel.Coord adjacent = c.Move(dir);
									Voxel.t adjacentID = m[adjacent].ID;
									if (adjacentID != 0 && (!avoid || (adjacentID != Voxel.t.Infected && adjacentID != Voxel.t.InfectedCritical)))
									{
										foundAdjacentCell = true;
										break;
									}
								}
							}
							else
								foundAdjacentCell = true;

							if (foundAdjacentCell)
							{
								Vector3 absolutePos = m.GetAbsolutePosition(c);

								bool foundConflict = false;
								if (!Zone.CanBuild(absolutePos))
									foundConflict = true;

								if (!foundConflict)
								{
									foreach (Voxel m2 in Voxel.ActivePhysicsVoxels)
									{
										if (m2 != m && m2[absolutePos].ID != 0)
										{
											foundConflict = true;
											break;
										}
									}
								}

								if (!foundConflict)
								{
									Voxel.State state = m[coord];
									if (state.Permanent)
										foundConflict = true;
									else
									{
										if (state.ID != 0)
											m.Empty(coord);
										m.Fill(coord, Voxel.States[stateId]);
										m.Regenerate();
										if (this.random.Next(0, 4) == 0)
											AkSoundEngine.PostEvent(AK.EVENTS.PLAY_BLOCK_BUILD, entity);
										entity.Delete.Execute();
										return;
									}
								}
							}

							// For one reason or another, we can't fill the cell
							// Animate nicely into oblivion
							update.Delete.Execute();
							entity.Add(new Animation
							(
								new Animation.Vector3MoveTo(model.Scale, Vector3.Zero, 1.0f),
								new Animation.Execute(entity.Delete)
							));
						}
					}
					else
					{
						if (scale)
							model.Scale.Value = new Vector3(blend);
						else
							model.Scale.Value = new Vector3(1.0f);
						Matrix finalOrientation = m.Transform;
						finalOrientation.Translation = Vector3.Zero;
						Vector3 finalEuler = Quaternion.CreateFromRotationMatrix(finalOrientation).ToEuler();
						finalEuler = Vector3.Lerp(startEuler, finalEuler, blend);
						transform.Orientation.Value = Matrix.CreateFromYawPitchRoll(finalEuler.X, finalEuler.Y, finalEuler.Z);

						Vector3 finalPosition = m.GetAbsolutePosition(coord);
						float distance = (finalPosition - start).Length() * 0.1f * Math.Max(0.0f, 0.5f - Math.Abs(blend - 0.5f));

						transform.Position.Value = Vector3.Lerp(start, finalPosition, blend) + new Vector3((float)Math.Sin(blend * Math.PI) * distance);
					}
				},
			};

			entity.Add(update);

			BlockEntry entry = new BlockEntry();
			entity.Add(new NotifyBinding(delegate()
			{
				if (entry.Voxel != null)
					this.animatingBlocks.Remove(entry);

				Entity m = voxel.Value.Target;
				entry.Voxel = m != null ? m.Get<Voxel>() : null;
				
				if (entry.Voxel != null)
				{
					entry.Coordinate = coord;
					this.animatingBlocks[entry] = true;
				}
			}, voxel, coord, stateId));

			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				if (entry.Voxel != null)
					this.animatingBlocks.Remove(entry);
				entry.Voxel = null;
			}));

			this.SetMain(entity, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), entity.GetOrMakeProperty<Vector3>("Offset"));
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
		}

		public void Build(Main main, IEnumerable<BlockBuildOrder> blocks, bool fake, Vector3 center, float delayMultiplier = 0.05f)
		{
			int index = 0;
			EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
			foreach (BlockBuildOrder entry in blocks)
			{
				if (factory.IsAnimating(new EffectBlockFactory.BlockEntry { Voxel = entry.Voxel, Coordinate = entry.Coordinate }))
					continue;

				Entity block = factory.CreateAndBind(main);
				entry.State.ApplyToEffectBlock(block.Get<ModelInstance>());
				block.GetOrMakeProperty<Vector3>("Offset").Value = entry.Voxel.GetRelativePosition(entry.Coordinate);

				Vector3 absolutePos = entry.Voxel.GetAbsolutePosition(entry.Coordinate);

				float distance = (absolutePos - center).Length();
				block.GetOrMakeProperty<Vector3>("StartPosition").Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f) * distance;
				block.GetOrMakeProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * (distance + index)) * Matrix.CreateRotationY(0.15f * (distance + index));
				block.GetOrMakeProperty<float>("TotalLifetime").Value = Math.Max(delayMultiplier, distance * delayMultiplier);
				block.GetOrMakeProperty<bool>("CheckAdjacent").Value = true;
				factory.Setup(block, entry.Voxel.Entity, entry.Coordinate, fake ? 0 : entry.State.ID);
				main.Add(block);
				index++;
			}
			SteamWorker.IncrementStat("stat_blocks_created", index);
		}

		public void Setup(Entity entity, Entity map, Voxel.Coord c, Voxel.t s)
		{
			Property<Entity.Handle> mapHandle = entity.GetOrMakeProperty<Entity.Handle>("TargetVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("TargetCoord");
			Property<Voxel.t> stateId = entity.GetOrMakeProperty<Voxel.t>("TargetVoxelState");
			mapHandle.InternalValue = map;
			coord.InternalValue = c;
			stateId.InternalValue = s;
			stateId.Changed();
		}
	}
}
