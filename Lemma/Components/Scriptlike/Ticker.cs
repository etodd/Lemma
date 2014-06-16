using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Ticker : Component<Main>, IUpdateableComponent
	{
		public Property<float> Interval = new Property<float> { Value = 1 };
		public Property<int> NumToFire = new Property<int> { Value = -1 };

		[XmlIgnore]
		public Command OnFire = new Command();

		public Property<float> Timer = new Property<float>();

		public Ticker()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
		}

		public void Update(float dt)
		{
			if (this.NumToFire != 0)
			{
				this.Timer.Value += dt;
				if (this.Interval.Value > 0 && this.Timer > this.Interval.Value)
				{
					this.Timer.Value -= this.Interval.Value;
					this.NumToFire.Value--;
					this.OnFire.Execute();
				}
			}
		}
	}
}
