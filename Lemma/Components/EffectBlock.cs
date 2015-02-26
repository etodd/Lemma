using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
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

		public bool DoScale = true;
		public Vector3 StartPosition;
		public Quaternion StartOrientation = Quaternion.Identity;
		public Entity.Handle TargetVoxel;
		public Voxel.Coord Coord;
		public Voxel.t StateId;
		public float Delay;

		// IO properties
		public Property<Vector3> Offset = new Property<Vector3>();
		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix>();

		public float TotalLifetime;
		public float Lifetime;

		public bool CheckAdjacent;

		private static float lastSound;
		private static bool soundTimerSetup;

		private Entry entry = new Entry();
		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;

			this.addEntry();
			this.Add(new CommandBinding(this.Delete, delegate()
			{
				if (this.entry.Voxel != null)
					EffectBlock.animatingBlocks.Remove(this.entry);
				this.entry.Voxel = null;
			}));
			
			EffectBlock.setupSoundTimer(main);
		}

		private static void setupSoundTimer(Main main)
		{
			if (!EffectBlock.soundTimerSetup)
			{
				EffectBlock.soundTimerSetup = true;
				new CommandBinding<string>(main.LoadingMap, delegate(string map)
				{
					EffectBlock.lastSound = 0;
				});
			}
		}

		private void addEntry()
		{
			Entity m = this.TargetVoxel.Target;
			this.entry.Voxel = m != null ? m.Get<Voxel>() : null;
			
			if (this.entry.Voxel != null)
			{
				this.entry.Coordinate = this.Coord;
				EffectBlock.animatingBlocks[this.entry] = true;
			}
		}

		public void Setup(Entity map, Voxel.Coord c, Voxel.t s)
		{
			this.TargetVoxel = map;
			this.Coord = c;
			this.StateId = s;
			this.addEntry();
		}

		public void Update(float dt)
		{
			if (this.TargetVoxel.Target == null || !this.TargetVoxel.Target.Active)
			{
				this.Delete.Execute();
				return;
			}

			this.Lifetime += dt;

			if (this.Lifetime < this.Delay)
				return;

			float blend = (this.Lifetime - this.Delay) / this.TotalLifetime;

			Voxel m = this.TargetVoxel.Target.Get<Voxel>();

			Matrix finalOrientation = m.Transform;
			finalOrientation.Translation = Vector3.Zero;
			Quaternion finalQuat = Quaternion.CreateFromRotationMatrix(finalOrientation);

			Vector3 finalPosition = m.GetAbsolutePosition(this.Coord);

			if (blend > 1.0f)
			{
				if (this.StateId != Voxel.t.Empty)
				{
					Voxel.Coord c = this.Coord;

					bool blue = this.StateId == Voxel.t.Blue;
					bool foundAdjacentCell = false;
					bool foundConflict = false;
					if (this.CheckAdjacent)
					{
						foreach (Direction dir in DirectionExtensions.Directions)
						{
							Voxel.Coord adjacent = c.Move(dir);
							Voxel.t adjacentID = m[adjacent].ID;
							if (adjacentID != Voxel.t.Empty)
							{
								if (blue && (adjacentID == Voxel.t.Infected || adjacentID == Voxel.t.HardInfected || adjacentID == Voxel.t.Slider || adjacentID == Voxel.t.SliderPowered || adjacentID == Voxel.t.SocketWhite || adjacentID == Voxel.t.SocketBlue || adjacentID == Voxel.t.SocketYellow))
									foundConflict = true;
								else
								{
									foundAdjacentCell = true;
									if (blue)
									{
										if (adjacentID == Voxel.t.Reset)
										{
											this.StateId = Voxel.t.Neutral;
											break;
										}
										else if (adjacentID == Voxel.t.Powered || adjacentID == Voxel.t.PermanentPowered || adjacentID == Voxel.t.PoweredSwitch || adjacentID == Voxel.t.HardPowered)
										{
											this.StateId = Voxel.t.Powered;
											break;
										}
									}
									else
										break;
								}
							}
						}
					}
					else
						foundAdjacentCell = true;

					if (foundAdjacentCell)
					{
						Vector3 absolutePos = m.GetAbsolutePosition(c);

						if (blue && !Zone.CanBuild(absolutePos))
							foundConflict = true;

						if (!foundConflict)
						{
							bool isDynamic = m.GetType() == typeof(DynamicVoxel);
							foreach (Voxel m2 in Voxel.ActivePhysicsVoxels)
							{
								bool atLeastOneDynamic = isDynamic || m2.GetType() == typeof(DynamicVoxel);
								if (m2 != m && atLeastOneDynamic && m2[absolutePos].ID != 0)
								{
									foundConflict = true;
									break;
								}
							}
						}

						if (!foundConflict)
						{
							Voxel.State state = m[this.Coord];
							if (state.Permanent || state.Hard || state.ID == this.StateId || (blue && state == Voxel.States.Powered))
								foundConflict = true;
							else
							{
								if (state.ID != 0)
									m.Empty(this.Coord);
								m.Fill(this.Coord, Voxel.States.All[this.StateId]);
								m.Regenerate();
								if (this.main.TotalTime - EffectBlock.lastSound > 0.15f)
								{
									EffectBlock.lastSound = this.main.TotalTime;
									AkSoundEngine.PostEvent(AK.EVENTS.PLAY_BLOCK_BUILD, absolutePos);
								}
								this.Entity.Delete.Execute();
								return;
							}
						}
					}

					// For one reason or another, we can't fill the cell
					// Animate nicely into oblivion
					this.StateId = Voxel.t.Empty;
				}
				else
				{
					// For one reason or another, we can't fill the cell
					// Animate nicely into oblivion
					if (blend > 2.0f)
						this.Delete.Execute();
					else
					{
						Matrix result = Matrix.CreateFromQuaternion(finalQuat);
						float scale = 2.0f - blend;
						result.Right *= scale;
						result.Up *= scale;
						result.Forward *= scale;
						result.Translation = finalPosition;
						this.Transform.Value = result;
					}
				}
			}
			else
			{
				float scale;
				if (this.DoScale)
					scale = blend;
				else
					scale = 1.0f;

				float distance = (finalPosition - this.StartPosition).Length() * 0.1f * Math.Max(0.0f, 0.5f - Math.Abs(blend - 0.5f));

				Matrix result = Matrix.CreateFromQuaternion(Quaternion.Lerp(this.StartOrientation, finalQuat, blend));
				result.Right *= scale;
				result.Up *= scale;
				result.Forward *= scale;
				result.Translation = Vector3.Lerp(this.StartPosition, finalPosition, blend) + new Vector3((float)Math.Sin(blend * Math.PI) * distance);
				this.Transform.Value = result;
			}
		}
	}
}