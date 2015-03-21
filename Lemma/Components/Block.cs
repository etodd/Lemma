using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Block : Component<Main>
	{
		public Property<Voxel.t> StateId = new Property<Voxel.t>();
	}
}
