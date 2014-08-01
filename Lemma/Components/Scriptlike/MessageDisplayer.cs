using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class MessageDisplayer : Component<Main>
	{
		public Property<string> Message = new Property<string>();
		public Property<float> DisplayLength = new Property<float>();
 
		[XmlIgnore]
		public Command Display = new Command();

		[XmlIgnore]
		public Command Hide = new Command();

		private Container messageContainer = null;

		public MessageDisplayer()
		{
			this.Display.Action = () =>
			{
				this.messageContainer = this.main.Menu.ShowMessage(Entity, () => Message.Value, Message);
				if (this.DisplayLength > 0)
					this.main.Menu.HideMessage(this.Entity, this.messageContainer, this.DisplayLength);
			};

			this.Hide.Action = () =>
			{
				this.main.Menu.HideMessage(Entity, this.messageContainer);
				this.messageContainer = null;
			};
		}
	}
}
