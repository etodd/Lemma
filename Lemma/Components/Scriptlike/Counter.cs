using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Counter : Component<Main>
	{
		public Property<float> IncrementBy = new Property<float>();
		public Property<float> StartingValue = new Property<float>();

		public Property<float> ExecuteAt = new Property<float>();

		public Property<bool> ResetOnExecute = new Property<bool>();

		[XmlIgnore]
		public Command Increment = new Command();

		[XmlIgnore]
		public Command Reset = new Command();

		[XmlIgnore]
		public Command OnTargetHit = new Command();

		private Property<float> _internalCount = new Property<float>() { Value = 0 };

		public Counter()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Reset.Action = () =>
			{
				_internalCount.Value = StartingValue.Value;
			};
			this.Increment.Action = () =>
			{
				_internalCount.Value += IncrementBy.Value;
				bool execute = false;
				if (IncrementBy.Value < 0)
				{
					if (_internalCount.Value < ExecuteAt.Value)
					{
						execute = true;
					}
				}
				if (IncrementBy.Value > 0)
				{
					if (_internalCount.Value > ExecuteAt.Value)
					{
						execute = true;
					}
				}

				if (execute)
				{
					OnTargetHit.Execute();
					if (ResetOnExecute.Value)
					{
						this.Reset.Execute();
					}
				}
			};
		}
	}
}
