using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;

namespace Lemma.Components
{
	public class Joint : Component<Main>
	{
		public Property<Entity.Handle> Parent = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();
		public Property<Direction> Direction = new Property<Direction>();
	}
}
