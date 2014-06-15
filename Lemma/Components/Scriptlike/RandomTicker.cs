using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class RandomTicker : Component<Main>, IUpdateableComponent
	{
		private static Random random = new Random();
		public EditorProperty<float> MaxInterval = new EditorProperty<float>() { Description = "Maximum time between firing (seconds).", Value = 2 };
		public EditorProperty<float> MinInterval = new EditorProperty<float>() { Description = "Minimum time between firing (seconds).", Value = 1 };
		public EditorProperty<int> NumToFire = new EditorProperty<int>() { Description = "Fires X times. -1 is infinite.", Value = -1 };

		[XmlIgnore]
		public Command OnFire = new Command();

		public Property<float> Timer = new Property<float>();

		public RandomTicker()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Enabled.Editable = true;
		}
		
		public override void Awake()
		{
			base.Awake();
			if (this.Timer == 0)
				this.Timer.Value = this.MinInterval + ((float)random.NextDouble() * (this.MaxInterval - this.MinInterval));
		}

		public void Update(float dt)
		{
			if (this.NumToFire != 0)
			{
				this.Timer.Value -= dt;
				if (this.Timer < 0)
				{
					this.Timer.Value = this.MinInterval + ((float)random.NextDouble() * (this.MaxInterval - this.MinInterval));
					this.NumToFire.Value--;
					this.OnFire.Execute();
				}
			}
		}
	}
}
