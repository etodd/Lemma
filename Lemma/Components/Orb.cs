using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Orb : Component<Main>
	{
		public ListProperty<Voxel.Coord> CoordQueue = new ListProperty<Voxel.Coord>();

		public Property<Voxel.Coord> ExplosionOriginalCoord = new Property<Voxel.Coord>();

		public Property<bool> Exploded = new Property<bool>();
	}
}
