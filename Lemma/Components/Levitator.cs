using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Levitator : Component<Main>
	{
		public Property<Entity.Handle> LevitatingVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> GrabCoord = new Property<Voxel.Coord>();
	}
}
