using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class RaycastAIMovement : Component<Main>
	{
		public Property<int> OperationalRadius = new Property<int> { Value = 100 };
		public Property<Vector3> LastPosition = new Property<Vector3>();
		public Property<Vector3> NextPosition = new Property<Vector3>();
		public Property<float> PositionBlend = new Property<float>();
	}
}