using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Components
{
	public class Command
	{
		public bool HasBindings
		{
			get
			{
				return this.bindings.Count > 0;
			}
		}

		public Action Action;
		protected List<BaseCommandBinding> bindings = new List<BaseCommandBinding>();

		private List<BaseCommandBinding> bindingRemovals = new List<BaseCommandBinding>();
		private List<BaseCommandBinding> bindingAdditions = new List<BaseCommandBinding>();

		protected int executeLevel;
		protected bool applyChanges;

		protected void preNotification()
		{
			this.executeLevel++;
		}

		protected void postNotification()
		{
			this.executeLevel--;
			if (this.executeLevel == 0)
			{
				this.bindings.AddRange(this.bindingAdditions);
				this.bindingAdditions.Clear();
				foreach (BaseCommandBinding binding in this.bindingRemovals)
					this.bindings.Remove(binding);
				this.bindingRemovals.Clear();
			}
		}

		public bool ShowInEditor = false;

		public void AddBinding(BaseCommandBinding binding)
		{
			if (this.executeLevel == 0)
				this.bindings.Add(binding);
			else
				this.bindingAdditions.Add(binding);
		}

		public void RemoveBinding(BaseCommandBinding binding)
		{
			if (this.executeLevel == 0)
				this.bindings.Remove(binding);
			else
				this.bindingRemovals.Add(binding);
		}

		public virtual void Execute()
		{
			this.preNotification();
			foreach (CommandBinding binding in this.bindings)
				binding.Execute();
			this.postNotification();
			if (this.Action != null)
				this.Action();
		}
	}

	public class Command<Type> : Command
	{
		public new Action<Type> Action;

		public override void Execute()
		{
			throw new Exception("Incorrect number of command parameters.");
		}

		public void Execute(Type parameter)
		{
			this.preNotification();
			foreach (CommandBinding<Type> binding in this.bindings)
				binding.Execute(parameter);
			this.postNotification();
			if (this.Action != null)
				this.Action(parameter);
		}
	}

	public class Command<Type, Type2> : Command
	{
		public new Action<Type, Type2> Action;

		public override void Execute()
		{
			throw new Exception("Incorrect number of command parameters.");
		}

		public void Execute(Type parameter1, Type2 parameter2)
		{
			this.preNotification();
			foreach (CommandBinding<Type, Type2> binding in this.bindings)
				binding.Execute(parameter1, parameter2);
			this.postNotification();
			if (this.Action != null)
				this.Action(parameter1, parameter2);
		}
	}

	public class Command<Type, Type2, Type3> : Command
	{
		public new Action<Type, Type2, Type3> Action;

		public override void Execute()
		{
			throw new Exception("Incorrect number of command parameters.");
		}

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3)
		{
			this.preNotification();
			foreach (CommandBinding<Type, Type2, Type3> binding in this.bindings)
				binding.Execute(parameter1, parameter2, parameter3);
			this.postNotification();
			if (this.Action != null)
				this.Action(parameter1, parameter2, parameter3);
		}
	}
}
