using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComponentBind
{
	public abstract class BaseCommand
	{
		public bool HasBindings
		{
			get
			{
				return this.bindings.Count > 0;
			}
		}

		protected List<ICommandBinding> bindings = new List<ICommandBinding>();

		private List<ICommandBinding> bindingRemovals = new List<ICommandBinding>();
		private List<ICommandBinding> bindingAdditions = new List<ICommandBinding>();

		protected int executeLevel;

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
				foreach (ICommandBinding binding in this.bindingRemovals)
					this.bindings.Remove(binding);
				this.bindingRemovals.Clear();
			}
		}

		public bool ShowInEditor = false;
		public bool AllowLinking = false;
		public bool AllowExecuting = false;

		public void AddBinding(ICommandBinding binding)
		{
			if (this.executeLevel == 0)
				this.bindings.Add(binding);
			else
				this.bindingAdditions.Add(binding);
		}

		public void RemoveBinding(ICommandBinding binding)
		{
			if (this.executeLevel == 0)
				this.bindings.Remove(binding);
			else
				this.bindingRemovals.Add(binding);
		}

	}

	public class Command : BaseCommand
	{
		public Action Action;

		public void Execute()
		{
			this.preNotification();
			foreach (CommandBinding binding in this.bindings)
				binding.Execute();
			this.postNotification();
			if (this.Action != null)
				this.Action();
		}
	}

	public class Command<Type> : BaseCommand
	{
		public Action<Type> Action;

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

	public class Command<Type, Type2> : BaseCommand
	{
		public Action<Type, Type2> Action;

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

	public class Command<Type, Type2, Type3> : BaseCommand
	{
		public Action<Type, Type2, Type3> Action;

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

	public class Command<Type, Type2, Type3, Type4> : BaseCommand
	{
		public Action<Type, Type2, Type3, Type4> Action;

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3, Type4 parameter4)
		{
			this.preNotification();
			foreach (CommandBinding<Type, Type2, Type3, Type4> binding in this.bindings)
				binding.Execute(parameter1, parameter2, parameter3, parameter4);
			this.postNotification();
			if (this.Action != null)
				this.Action(parameter1, parameter2, parameter3, parameter4);
		}
	}
}
