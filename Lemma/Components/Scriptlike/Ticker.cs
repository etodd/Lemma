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
		public EditorProperty<float> Interval = new EditorProperty<float>() { Description = "Will fire every X seconds.", Value = 1 };
		public EditorProperty<int> NumToFire = new EditorProperty<int>() { Description = "Fires X times. -1 is infinite.", Value = -1 };

		[XmlIgnore]
		public Command OnFire = new Command();

		public Property<float> Timer = new Property<float>();

		public Ticker()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Enabled.Editable = true;
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
