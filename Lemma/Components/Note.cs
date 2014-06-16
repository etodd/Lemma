using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Components
{
	public class Note : Component<Main>
	{
		public Property<string> Text = new Property<string>();
		public Property<string> Image = new Property<string>();
		public Property<bool> Collected = new Property<bool>();
	}
}
