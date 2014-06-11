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
		public EditorProperty<float> Interval = new EditorProperty<float>() {Description = "Will fire every X seconds."};
		public EditorProperty<float> NumToFire = new EditorProperty<float>() {Description = "Fires X times. -1 is infinite."};

		[XmlIgnore]
		public Command OnFire = new Command();

		private Property<float>  _internalTimer = new Property<float>(){Value = 0};

		public Ticker()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Enabled.Editable = true;
		}

		public void Update(float dt)
		{
			_internalTimer.Value += dt;
			if (Interval.Value < 0) Interval.Value = 0;
			if (_internalTimer.Value >= Interval.Value && Interval.Value > 0)
			{
				_internalTimer.Value -= Interval.Value;
				OnFire.Execute();
			}
		}
	}
}
