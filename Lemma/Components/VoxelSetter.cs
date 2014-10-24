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

		[XmlIgnore]
		public Command Set = new Command();

		public override void Awake()
		{
			base.Awake();

			this.Add(new CommandBinding(this.Set, delegate()
			{
				Voxel map = this.AttachedVoxel.Value.Target.Get<Voxel>();
				map.Empty(this.Coord, true, true, map);
				map.Fill(this.Coord, Voxel.States.All[this.State]);
				map.Regenerate();
			}));
		}
	}
}
