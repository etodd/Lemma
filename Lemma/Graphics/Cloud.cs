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
		public EditorProperty<float> Height = new EditorProperty<float> { Value = 1.0f };

		public EditorProperty<Vector2> Velocity = new EditorProperty<Vector2> { Value = Vector2.One };

		public EditorProperty<float> StartDistance = new EditorProperty<float> { Value = 50 };
	}
}
