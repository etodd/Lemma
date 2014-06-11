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
		public EditorProperty<float> IncrementBy = new EditorProperty<float>() { Description = "Will increment by this when fired" };
		public EditorProperty<float> StartingValue = new EditorProperty<float>() { Description = "On map load and on reset, will set internal counter to this value" };

		public EditorProperty<float> ExecuteAt = new EditorProperty<float>()
		{
			Description = "Will fire an event when the counter goes past this number."
		};

		public EditorProperty<bool> ResetOnExecute = new EditorProperty<bool>() { Description = "Will reset the counter when exceeding a threshhold" };

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
			this.Enabled.Editable = true;
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
			this.Reset.ShowInEditor = true;
		}
	}
}
