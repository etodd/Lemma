using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class LogicGate : Component<Main>
	{
		public enum LogicMode { And, Nand, Or, Nor, ExclusiveOr, ExclusiveNor }

		public Property<LogicMode> Mode = new Property<LogicMode>();

		public Property<bool> Input1 = new Property<bool>();

		public Property<Entity.Handle> Input1Target = new Property<Entity.Handle>();

		public Property<string> Input1TargetProperty = new Property<string>();

		public Property<bool> Input2 = new Property<bool>();

		public Property<Entity.Handle> Input2Target = new Property<Entity.Handle>();

		public Property<string> Input2TargetProperty = new Property<string>();

		public Property<bool> Output = new Property<bool>();

		[XmlIgnore]
		public Command Input1On = new Command();

		[XmlIgnore]
		public Command Input1Off = new Command();

		[XmlIgnore]
		public Command Input2On = new Command();

		[XmlIgnore]
		public Command Input2Off = new Command();

		[XmlIgnore]
		public Command OutputOn = new Command();

		[XmlIgnore]
		public Command OutputOff = new Command();

		protected Binding<bool> input1Binding;
		protected Binding<bool> input2Binding;

		public override void Awake()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Add(new CommandBinding(this.Input1On, () => !this.Input1, delegate()
			{
				this.Input1.Value = true;
			}));
			this.Add(new CommandBinding(this.Input1Off, () => this.Input1, delegate()
			{
				this.Input1.Value = false;
			}));
			this.Add(new CommandBinding(this.Input2On, () => !this.Input2, delegate()
			{
				this.Input2.Value = true;
			}));
			this.Add(new CommandBinding(this.Input2Off, () => this.Input2, delegate()
			{
				this.Input2.Value = false;
			}));
			this.Add(new ChangeBinding<bool>(this.Input1, delegate(bool old, bool value)
			{
				if (!old && value)
					this.Input1On.Execute();
				else if (old && !value)
					this.Input1Off.Execute();
			}));
			this.Add(new ChangeBinding<bool>(this.Input2, delegate(bool old, bool value)
			{
				if (!old && value)
					this.Input2On.Execute();
				else if (old && !value)
					this.Input2Off.Execute();
			}));
			this.Add(new ChangeBinding<bool>(this.Output, delegate(bool old, bool value)
			{
				if (!old && value)
					this.OutputOn.Execute();
				else if (old && !value)
					this.OutputOff.Execute();
			}));
			this.Add(new NotifyBinding(this.evaluate, this.Input1, this.Input2, this.Mode));
		}

		private void evaluate()
		{
			switch (this.Mode.Value)
			{
				case LogicMode.And:
					this.Output.Value = this.Input1 && this.Input2;
					break;
				case LogicMode.Nand:
					this.Output.Value = !(this.Input1 && this.Input2);
					break;
				case LogicMode.Or:
					this.Output.Value = this.Input1 || this.Input2;
					break;
				case LogicMode.Nor:
					this.Output.Value = !(this.Input1 || this.Input2);
					break;
				case LogicMode.ExclusiveOr:
					this.Output.Value = this.Input1 ^ this.Input2;
					break;
				case LogicMode.ExclusiveNor:
					this.Output.Value = !(this.Input1 ^ this.Input2);
					break;
			}
		}

		public override void Start()
		{
			this.FindProperties();
		}

		public void FindProperties()
		{
			if (this.input1Binding == null)
			{
				Entity entity = this.Input1Target.Value.Target;
				if (entity != null && entity.Active)
				{
					Property<bool> targetProperty = entity.GetProperty<bool>(this.Input1TargetProperty);
					if (targetProperty != null)
					{
						this.input1Binding = new Binding<bool>(this.Input1, targetProperty);
						entity.Add(this.input1Binding);
						entity.Add(new CommandBinding(entity.Delete, delegate() { this.input1Binding = null; }));
					}
				}
			}

			if (this.input2Binding == null)
			{
				Entity entity = this.Input2Target.Value.Target;
				if (entity != null && entity.Active)
				{
					Property<bool> targetProperty = entity.GetProperty<bool>(this.Input2TargetProperty);
					if (targetProperty != null)
					{
						this.input2Binding = new Binding<bool>(this.Input2, targetProperty);
						entity.Add(this.input2Binding);
						entity.Add(new CommandBinding(entity.Delete, delegate() { this.input2Binding = null; }));
					}
				}
			}
		}

		public override void delete()
		{
			if (this.input1Binding != null)
			{
				Entity entity = this.Input1Target.Value.Target;
				if (entity != null && entity.Active)
					entity.Remove(this.input1Binding);
				this.input1Binding = null;
			}
			if (this.input2Binding != null)
			{
				Entity entity = this.Input2Target.Value.Target;
				if (entity != null && entity.Active)
					entity.Remove(this.input2Binding);
				this.input2Binding = null;
			}
			base.delete();
		}
	}
}