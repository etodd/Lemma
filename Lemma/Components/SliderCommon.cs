using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class SliderCommon : Component<Main>
	{
		// Original transform of the slider at spawn
		public Property<Matrix> OriginalTransform = new Property<Matrix>();
	}
}
