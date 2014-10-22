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
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		[XmlIgnore]
		public Command OnPowerOn = new Command();

		[XmlIgnore]
		public Command OnPowerOff = new Command();

		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

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
				AkSoundEngine.PostEvent(this.On ? AK.EVENTS.PLAY_SWITCH_ON : AK.EVENTS.PLAY_SWITCH_OFF, this.Position);
			}, this.On));

			this.Add(new CommandBinding(this.OnPowerOn, delegate()
			{
				Voxel map = this.AttachedVoxel.Value.Target.Get<Voxel>();
				bool regenerate = false;
				foreach (Switch s in Switch.all)
				{
					if (s != this && s.On && s.AttachedVoxel.Value.Target == this.AttachedVoxel.Value.Target)
					{
						// There can only be one active switch per map

						Dictionary<Voxel.Coord, bool> visited = new Dictionary<Voxel.Coord, bool>();
						Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
						queue.Enqueue(s.Coord);
						while (queue.Count > 0)
						{
							Voxel.Coord c = queue.Dequeue();
							map.Empty(c, true, true, map);
							map.Fill(c, Voxel.States.Switch);
							regenerate = true;
							visited[c] = true;
							foreach (Direction adjacentDirection in DirectionExtensions.Directions)
							{
								Voxel.Coord adjacentCoord = c.Move(adjacentDirection);
								if (!visited.ContainsKey(adjacentCoord))
								{
									Voxel.t adjacentID = map[adjacentCoord].ID;
									if (adjacentID == Voxel.t.PoweredSwitch)
										queue.Enqueue(adjacentCoord);
									else if (adjacentID == Voxel.t.Blue || adjacentID == Voxel.t.Powered)
									{
										map.Empty(adjacentCoord, true, true, map);
										map.Fill(adjacentCoord, Voxel.States.Neutral);
										regenerate = true;
									}
								}
							}
						}
					}
				}
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
