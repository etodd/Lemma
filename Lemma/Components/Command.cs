using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Components
{
	public class Command
	{
		public Action Action;
		protected List<BaseCommandBinding> bindings = new List<BaseCommandBinding>();

		public bool ShowInEditor = false;

		public void AddBinding(BaseCommandBinding binding)
		{
			this.bindings.Add(binding);
		}

		public void RemoveBinding(BaseCommandBinding binding)
		{
			this.bindings.Remove(binding);
		}

		public virtual void Execute()
		{
			try
			{
				foreach (CommandBinding binding in this.bindings)
					binding.Execute();
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (CommandBinding<Type> binding in this.bindings)
					binding.Execute(parameter);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (CommandBinding<Type, Type2> binding in this.bindings)
					binding.Execute(parameter1, parameter2);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (CommandBinding<Type, Type2, Type3> binding in this.bindings)
					binding.Execute(parameter1, parameter2, parameter3);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
			if (this.Action != null)
				this.Action(parameter1, parameter2, parameter3);
		}
	}
}
