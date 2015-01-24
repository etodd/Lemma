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
	public class Switch : Component<Main>
	{
		private static List<Switch> all = new List<Switch>();

		// Output properties
		public Property<bool> On = new Property<bool>();

		// Input properties
		[XmlIgnore]
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();

		[XmlIgnore]
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		[XmlIgnore]
		public Command OnPowerOn = new Command();

		[XmlIgnore]
		public Command OnPowerOff = new Command();

		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

		private static bool isPowered(Voxel.State state)
		{
			return state == Voxel.States.Powered || state == Voxel.States.PoweredSwitch || state == Voxel.States.HardPowered;
		}

		public override void Awake()
		{
			base.Awake();

			Switch.all.Add(this);

			this.Add(new NotifyBinding(delegate()
			{
				if (this.On)
					this.OnPowerOn.Execute();
				else
					this.OnPowerOff.Execute();
			}, this.On));

			this.Add(new CommandBinding(this.OnPowerOff, delegate()
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SWITCH_OFF, this.Entity);
			}));

			this.Add(new CommandBinding(this.OnPowerOn, delegate()
			{
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SWITCH_ON, this.Entity);
				Voxel map = this.AttachedVoxel.Value.Target.Get<Voxel>();
				bool regenerate = false;
				Stack<Voxel.Box> path = new Stack<Voxel.Box>();
				foreach (Switch s in Switch.all)
				{
					if (s.On && s != this && s.AttachedVoxel.Value.Target == this.AttachedVoxel.Value.Target
						&& VoxelAStar.Broadphase(map, map.GetBox(this.Coord), s.Coord, isPowered, path))
					{
						Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
						Voxel.Coord start = s.Coord;
						start.Data = map[start];
						queue.Enqueue(start);
						while (queue.Count > 0)
						{
							Voxel.Coord c = queue.Dequeue();
							map.Empty(c, true, true, map);
							if (c.Data == Voxel.States.PoweredSwitch)
								map.Fill(c, Voxel.States.Switch);
							else
								map.Fill(c, Voxel.States.Hard);
							regenerate = true;
							c.Data = null; // Ensure the visited dictionary works correctly
							Voxel.CoordDictionaryCache[c] = true;
							foreach (Direction adjacentDirection in DirectionExtensions.Directions)
							{
								Voxel.Coord adjacentCoord = c.Move(adjacentDirection);
								if (!Voxel.CoordDictionaryCache.ContainsKey(adjacentCoord))
								{
									adjacentCoord.Data = map[adjacentCoord];
									if (adjacentCoord.Data == Voxel.States.PoweredSwitch)
										queue.Enqueue(adjacentCoord);
									else if (adjacentCoord.Data == Voxel.States.Blue
										|| adjacentCoord.Data == Voxel.States.Powered
										|| adjacentCoord.Data == Voxel.States.Infected)
									{
										map.Empty(adjacentCoord, true, true, map);
										map.Fill(adjacentCoord, Voxel.States.Neutral);
										regenerate = true;
									}
								}
							}
						}
					}
					path.Clear();
				}
				Voxel.CoordDictionaryCache.Clear();
				if (regenerate)
					map.Regenerate();
			}));
		}

		public override void delete()
		{
			base.delete();
			Switch.all.Remove(this);
		}
	}
}
