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
	public class VoxelSetter : Component<Main>
	{
		// Input properties
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<Voxel.t> State = new Property<Voxel.t>();
		public Property<bool> Contiguous = new Property<bool>();

		[XmlIgnore]
		public Command Set = new Command();

		public override void Awake()
		{
			base.Awake();

			this.Add(new CommandBinding(this.Set, delegate()
			{
				Voxel map = this.AttachedVoxel.Value.Target.Get<Voxel>();
				Voxel.State state = Voxel.States.All[this.State];
				if (this.Contiguous)
				{
					Voxel.Box b = map.GetBox(this.Coord);
					if (b != null && b.Type != state)
					{
						List<Voxel.Box> boxes = map.GetContiguousByType(new[] { b }).ToList();
						map.Empty(boxes.SelectMany(x => x.GetCoords()), true, true, map);
						foreach (Voxel.Coord c in boxes.SelectMany(x => x.GetCoords()))
							map.Fill(c, state);
						map.Regenerate();
					}
				}
				else
				{
					if (map[this.Coord] != state)
					{
						map.Empty(this.Coord, true, true, map);
						map.Fill(this.Coord, state);
						map.Regenerate();
					}
				}
			}));
		}
	}
}
