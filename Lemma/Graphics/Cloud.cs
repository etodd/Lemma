using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Cloud : Component<Main>
	{
		public Property<float> Height = new Property<float> { Value = 1.0f };

		public Property<Vector2> Velocity = new Property<Vector2> { Value = Vector2.One };

		public Property<float> StartDistance = new Property<float> { Value = 50 };

		public Property<bool> Infinite = new Property<bool> { Value = true };
	}
}
