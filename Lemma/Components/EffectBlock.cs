using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class EffectBlock : Component<Main>, IUpdateableComponent
	{
		public struct Entry
		{
			public Voxel Voxel;
			public Voxel.Coord Coordinate;
		}

		private static Dictionary<Entry, bool> animatingBlocks = new Dictionary<Entry, bool>();

		public static bool IsAnimating(Entry block)
		{
			return EffectBlock.animatingBlocks.ContainsKey(block);
		}

		public Property<bool> DoScale = new Property<bool> { Value = true };
		public Property<Vector3> StartPosition = new Property<Vector3>();
		public Property<Quaternion> StartOrientation = new Property<Quaternion>();
		public Property<Entity.Handle> TargetVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<Voxel.t> StateId = new Property<Voxel.t>();
		public Property<float> Delay = new Property<float>();

		public Property<Vector3> Offset = new Property<Vector3>();
		public Property<Vector3> Scale = new Property<Vector3>();
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<Vector3> Position = new Property<Vector3>();

		public Property<float> TotalLifetime = new Property<float>();
		public Property<float> Lifetime = new Property<float>();

		public Property<bool> CheckAdjacent = new Property<bool>();

		private Quaternion startQuat = Quaternion.Identity;

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;
			this.Add(new SetBinding<Quaternion>(this.StartOrientation, delegate(Quaternion value)
			{
				this.startQuat = value;
			}));

			Entry entry = new Entry();
			this.Add(new NotifyBinding(delegate()
			{
				if (entry.Voxel != null)
					EffectBlock.animatingBlocks.Remove(entry);

				Entity m = this.TargetVoxel.Value.Target;
				entry.Voxel = m != null ? m.Get<Voxel>() : null;
				
				if (entry.Voxel != null)
				{
					entry.Coordinate = this.Coord;
					EffectBlock.animatingBlocks[entry] = true;
				}
			}, this.TargetVoxel, this.Coord, this.StateId));

			this.Add(new CommandBinding(this.Delete, delegate()
			{
				if (entry.Voxel != null)
					EffectBlock.animatingBlocks.Remove(entry);
				entry.Voxel = null;
			}));
		}

		public void Setup(Entity map, Voxel.Coord c, Voxel.t s)
		{
			this.TargetVoxel.SetStealthy(map);
			this.Coord.SetStealthy(c);
			this.StateId.Value = s;
		}

		private static Random random = new Random();

		public void Update(float dt)
		{
			if (this.TargetVoxel.Value.Target == null || !this.TargetVoxel.Value.Target.Active)
			{
				this.Delete.Execute();
				return;
			}

			this.Lifetime.Value += dt;

			if (this.Lifetime < this.Delay)
			{
				this.Scale.Value = Vector3.Zero;
				return;
			}

			float blend = (this.Lifetime - this.Delay) / this.TotalLifetime;

			Voxel m = this.TargetVoxel.Value.Target.Get<Voxel>();

			if (blend > 1.0f)
			{
				if (this.StateId != Voxel.t.Empty)
				{
					Voxel.Coord c = this.Coord;

					bool foundAdjacentCell = false;
					if (this.CheckAdjacent)
					{
						bool blue = this.StateId == Voxel.t.Blue;
						foreach (Direction dir in DirectionExtensions.Directions)
						{
							Voxel.Coord adjacent = c.Move(dir);
							Voxel.t adjacentID = m[adjacent].ID;
							if (adjacentID != Voxel.t.Empty && (!blue || (adjacentID != Voxel.t.Infected && adjacentID != Voxel.t.HardInfected && adjacentID != Voxel.t.Slider && adjacentID != Voxel.t.SliderPowered)))
							{
								foundAdjacentCell = true;
								if (blue)
								{
									if (adjacentID == Voxel.t.Reset)
									{
										this.StateId.Value = Voxel.t.Neutral;
										break;
									}
								}
								else
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
							Voxel.State state = m[this.Coord];
							if (state.Permanent)
								foundConflict = true;
							else
							{
								if (state.ID != 0)
									m.Empty(this.Coord);
								m.Fill(this.Coord, Voxel.States.All[this.StateId]);
								m.Regenerate();
								if (EffectBlock.random.Next(0, 4) == 0)
									AkSoundEngine.PostEvent(AK.EVENTS.PLAY_BLOCK_BUILD, this.Entity);
								this.Entity.Delete.Execute();
								return;
							}
						}
					}

					// For one reason or another, we can't fill the cell
					// Animate nicely into oblivion
					this.StateId.Value = Voxel.t.Empty;
				}
				else
				{
					// For one reason or another, we can't fill the cell
					// Animate nicely into oblivion
					if (blend > 2.0f)
						this.Delete.Execute();
					else
						this.Scale.Value = new Vector3(2.0f - blend);
				}
			}
			else
			{
				if (this.DoScale)
					this.Scale.Value = new Vector3(blend);
				else
					this.Scale.Value = new Vector3(1.0f);
				Matrix finalOrientation = m.Transform;
				finalOrientation.Translation = Vector3.Zero;
				Quaternion finalQuat = Quaternion.CreateFromRotationMatrix(finalOrientation);
				finalQuat = Quaternion.Lerp(this.startQuat, finalQuat, blend);
				this.Orientation.Value = finalQuat;

				Vector3 finalPosition = m.GetAbsolutePosition(this.Coord);
				float distance = (finalPosition - this.StartPosition).Length() * 0.1f * Math.Max(0.0f, 0.5f - Math.Abs(blend - 0.5f));

				this.Position.Value = Vector3.Lerp(this.StartPosition, finalPosition, blend) + new Vector3((float)Math.Sin(blend * Math.PI) * distance);
			}
		}
	}
}
