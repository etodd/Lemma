using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Constant : Component<Main>
	{
		public Property<float> FloatProperty = new Property<float>();
		public Property<int> IntProperty = new Property<int>();
		public Property<bool> BoolProperty = new Property<bool>();
		public Property<string> StringProperty = new Property<string>();
		public Property<Vector3> Vector3Property = new Property<Vector3>();
		public Property<Vector4> Vector4Property = new Property<Vector4>();
		public Property<Direction> DirectionProperty = new Property<Direction>();

		public Constant()
		{
		}
	}
}
