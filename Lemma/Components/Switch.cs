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

		private static bool canConnect(Voxel.State state)
		{
			return state == Voxel.States.Powered
				|| state == Voxel.States.HardPowered
				|| state == Voxel.States.PoweredSwitch;
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
				if (this.main.TotalTime > 0.1f)
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SWITCH_OFF, this.Entity);
			}));

			this.Add(new CommandBinding(this.OnPowerOn, delegate()
			{
				if (this.main.TotalTime > 0.1f)
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SWITCH_ON, this.Entity);
				Voxel map = this.AttachedVoxel.Value.Target.Get<Voxel>();

				Switch closestConnectedSwitch = null;
				Stack<Voxel.Box> closestConnectedPath = null;

				foreach (Switch s in Switch.all)
				{
					Stack<Voxel.Box> path = new Stack<Voxel.Box>();
					if (s.On && s != this && s.AttachedVoxel.Value.Target == this.AttachedVoxel.Value.Target
						&& VoxelAStar.Broadphase(map, map.GetBox(this.Coord), s.Coord, canConnect, path, 2000))
					{
						if (closestConnectedPath == null || path.Count < closestConnectedPath.Count)
						{
							closestConnectedSwitch = s;
							closestConnectedPath = path;
						}
					}
				}
				if (closestConnectedSwitch != null)
				{
					List<Voxel.Coord> changes = new List<Voxel.Coord>();
					Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
					Voxel.Coord start = closestConnectedSwitch.Coord;
					start.Data = null;
					queue.Enqueue(start);
					Voxel.CoordSetCache.Add(start);

					start.Data = Voxel.States.Switch;
					changes.Add(start);
					while (queue.Count > 0)
					{
						Voxel.Coord c = queue.Dequeue();

						foreach (Direction adjacentDirection in DirectionExtensions.Directions)
						{
							Voxel.Coord adjacentCoord = c.Move(adjacentDirection);
							if (!Voxel.CoordSetCache.Contains(adjacentCoord))
							{
								Voxel.CoordSetCache.Add(adjacentCoord);
								Voxel.State adjacentState = map[adjacentCoord];
								if (adjacentState == Voxel.States.PoweredSwitch)
								{
									queue.Enqueue(adjacentCoord);

									adjacentCoord.Data = Voxel.States.Switch;
									changes.Add(adjacentCoord);
								}
								else if (adjacentState == Voxel.States.Hard)
									queue.Enqueue(adjacentCoord);
								else if (adjacentState == Voxel.States.HardPowered)
								{
									queue.Enqueue(adjacentCoord);

									adjacentCoord.Data = Voxel.States.Hard;
									changes.Add(adjacentCoord);
								}
								else if ((adjacentState == Voxel.States.Blue || adjacentState == Voxel.States.Powered)
									&& closestConnectedPath.Contains(map.GetBox(adjacentCoord)))
								{
									adjacentCoord.Data = Voxel.States.Neutral;
									changes.Add(adjacentCoord);
									adjacentCoord.Data = null;
								}
							}
						}
					}
					Voxel.CoordSetCache.Clear();
					if (changes.Count > 0)
					{
						lock (map.MutationLock)
						{
							map.Empty(changes, true, true, map);
							map.Fill(changes);
						}
						map.Regenerate();
					}
				}
			}));
		}

		public override void delete()
		{
			base.delete();
			Switch.all.Remove(this);
		}
	}
}