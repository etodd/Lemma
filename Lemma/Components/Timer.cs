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
		public EditorProperty<bool> Repeat = new EditorProperty<bool>();
		public EditorProperty<float> Interval = new EditorProperty<float> { Value = 1.0f };
		public EditorProperty<bool> ResetOnEnable = new EditorProperty<bool>();
		[XmlIgnore]
		public Command Command = new Command();
		[XmlIgnore]
		public Command Reset = new Command();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;

			this.Add(new CommandBinding(this.Enable, delegate()
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
