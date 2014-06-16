using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Components
{
	public class Skybox : Component<Main>
	{
		public Property<bool> Vertical = new Property<bool>();
		public Property<float> GodRays = new Property<float> { Value = 0.25f };
		public Property<float> GodRayExtinction = new Property<float> { Value = 1.0f };
		public Property<float> VerticalSize = new Property<float> { Value = 10.0f };
		public Property<float> VerticalCenter = new Property<float>();
		public Property<float> StartDistance = new Property<float> { Value = 50.0f };
	}
}
