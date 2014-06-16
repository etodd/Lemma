using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Turret : Component<Main>
	{
		public Property<Vector3> Reticle = new Property<Vector3>();
		public Property<Entity.Handle> TargetAgent = new Property<Entity.Handle>();
	}
}
