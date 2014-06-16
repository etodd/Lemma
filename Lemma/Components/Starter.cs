using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Starter : Component<Main>
	{
		[XmlIgnore]
		public Command Command = new Command();

		public override void Start()
		{
			if (!this.main.EditorEnabled)
				this.Command.Execute();
		}
	}
}
