using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Sequence : Component<Main>
	{
		[XmlIgnore]
		public Command Commands = new Command();

		[XmlIgnore]
		public Command Advance = new Command();

		[XmlIgnore]
		public Command Done = new Command();

		public Property<int> Index = new Property<int>();

		public Sequence()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Advance.Action = this.advance;
		}
		
		private void advance()
		{
			if (this.Index < this.Commands.Bindings.Count)
				((CommandBinding)this.Commands.Bindings[this.Index]).Execute();
			else if (this.Index == this.Commands.Bindings.Count - 1)
				this.Done.Execute();
			this.Index.Value++;
		}
	}
}