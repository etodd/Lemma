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
		public Property<int> IncrementBy = new Property<int> { Value = 1 };
		public Property<int> StartingValue = new Property<int>();

		public Property<int> Target = new Property<int> { Value = 4 };

		public Property<int> Count = new Property<int>();

		[XmlIgnore]
		public Command Increment = new Command();

		[XmlIgnore]
		public Command Reset = new Command();

		[XmlIgnore]
		public Command OnTargetHit = new Command();

		public override void Awake()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Reset.Action = () =>
			{
				this.Count.Value = this.StartingValue.Value;
			};
			this.Increment.Action = () =>
			{
				int oldCount = this.Count;
				this.Count.Value += this.IncrementBy.Value;
				bool execute = false;
				if (this.IncrementBy.Value < 0)
				{
					if (this.Count <= this.Target && oldCount > this.Target)
						execute = true;
				}
				if (this.IncrementBy.Value > 0)
				{
					if (this.Count >= this.Target && oldCount < this.Target)
						execute = true;
				}

				if (execute)
					this.OnTargetHit.Execute();
			};
			if (this.main.EditorEnabled)
				this.Add(new Binding<int>(this.Count, this.StartingValue));
		}
	}
}
