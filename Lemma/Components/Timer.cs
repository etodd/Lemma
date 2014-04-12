using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Timer : Component<Main>, IUpdateableComponent
	{
		protected float time = 0.0f;
		public Property<bool> Repeat = new Property<bool> { Editable = true };
		public Property<float> Interval = new Property<float> { Editable = true, Value = 1.0f };
		public Property<bool> ResetOnEnable = new Property<bool> { Editable = true };
		[XmlIgnore]
		public Command Command = new Command();
		[XmlIgnore]
		public Command Reset = new Command();

		public override void InitializeProperties()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;

			this.Add(new CommandBinding(this.OnEnabled, delegate()
			{
				if (this.ResetOnEnable)
					this.Reset.Execute();
			}));

			this.Reset.Action = delegate()
			{
				this.time = 0.0f;
			};
		}

		public void Update(float elapsedTime)
		{
			if (this.time < 0.0f)
				return;

			this.time += elapsedTime;
			if (this.time > this.Interval)
			{
				this.Command.Execute();
				if (this.Repeat)
					this.Reset.Execute();
				else
					this.time = -1.0f;
			}
		}
	}
}
