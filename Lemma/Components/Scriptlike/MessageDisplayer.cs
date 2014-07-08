using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class MessageDisplayer : Component<Main>, IUpdateableComponent
	{
		public Property<string> Message = new Property<string>();
		public Property<float> DisplayLength = new Property<float>();
 
		[XmlIgnore]
		public Command Display = new Command();

		[XmlIgnore]
		public Command Hide = new Command();

		private Container messageContainer = null;

		private float _timer = 0;
		private bool _displaying = false;
		public MessageDisplayer()
		{
			this.Display.Action = () =>
			{
				if (_displaying) return;
				_displaying = true;
				messageContainer = main.Menu.ShowMessage(Entity, () => Message.Value, Message);
				if (DisplayLength.Value > 0)
				{
					_timer = DisplayLength;
				}
			};

			this.Hide.Action = () =>
			{
				if (messageContainer == null) return;
				main.Menu.HideMessage(Entity, messageContainer);
				_displaying = false;
			};
		}

		public void Update(float dt)
		{
			if (!_displaying) return;
			_timer -= dt;
			if (_timer <= 0)
			{
				_timer = 0;
				if (DisplayLength > 0)
				{
					this.Hide.Execute();
				}
			}
		}
	}
}
