using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Components
{
	public class Skybox : Component<Main>
	{
		public EditorProperty<bool> Vertical = new EditorProperty<bool>();
		public EditorProperty<float> GodRays = new EditorProperty<float> { Value = 0.25f };
		public EditorProperty<float> GodRayExtinction = new EditorProperty<float> { Value = 1.0f };
		public EditorProperty<float> VerticalSize = new EditorProperty<float> { Value = 10.0f };
		public EditorProperty<float> VerticalCenter = new EditorProperty<float>();
		public EditorProperty<float> StartDistance = new EditorProperty<float> { Value = 50.0f };
	}
}
