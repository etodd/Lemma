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
				messageContainer = main.Menu.ShowMessage(Entity, () => Message.Value, Message);
				if (DisplayLength.Value > 0)
				{
					main.Menu.HideMessage(Entity, messageContainer, DisplayLength.Value);
				}
			};

			this.Hide.Action = () =>
			{
				if (messageContainer == null) return;
				main.Menu.HideMessage(Entity, messageContainer);
			};
		}
	}
}
