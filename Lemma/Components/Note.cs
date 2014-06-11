using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;

namespace Lemma.Components
{
	public class Note : Component<Main>
	{
		public EditorProperty<string> Text = new EditorProperty<string>();
		public EditorProperty<string> Image = new EditorProperty<string>();
		public Property<bool> Collected = new Property<bool>();
	}
}
