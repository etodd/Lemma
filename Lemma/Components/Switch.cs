using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Switch : Component<Main>
	{
		public Property<bool> On = new Property<bool>();

		[XmlIgnore]
		public Command OnPowerOn = new Command();

		[XmlIgnore]
		public Command OnPowerOff = new Command();

		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.Add(new NotifyBinding(delegate()
			{
				if (this.On)
					this.OnPowerOn.Execute();
				else
					this.OnPowerOff.Execute();
				AkSoundEngine.PostEvent(this.On ? AK.EVENTS.PLAY_SWITCH_ON : AK.EVENTS.PLAY_SWITCH_OFF, this.Position);
			}, this.On));
		}
	}
}
