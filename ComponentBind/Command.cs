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
		public void AddBinding(ICommandBinding binding)
		{
			this.bindings.Add(binding);
		}

		public void RemoveBinding(ICommandBinding binding)
		{
			this.bindings.Remove(binding);
		}
	}

	public class Command : BaseCommand
	{
		public enum Perms { Linkable, Executable, LinkableAndExecutable }

		public class Entry
		{
			public Command Command;
			public Perms Permissions;
			public string Description;
			public string Key;
		}

		public Action Action;

		public void Execute()
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				((CommandBinding)this.bindings[j]).Execute();
			if (this.Action != null)
				this.Action();
		}
	}

	public class Command<Type> : BaseCommand
	{
		public Action<Type> Action;

		public void Execute(Type parameter)
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				((CommandBinding<Type>)this.bindings[j]).Execute(parameter);
			if (this.Action != null)
				this.Action(parameter);
		}
	}

	public class Command<Type, Type2> : BaseCommand
	{
		public Action<Type, Type2> Action;

		public void Execute(Type parameter1, Type2 parameter2)
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				((CommandBinding<Type, Type2>)this.bindings[j]).Execute(parameter1, parameter2);
			if (this.Action != null)
				this.Action(parameter1, parameter2);
		}
	}

	public class Command<Type, Type2, Type3> : BaseCommand
	{
		public Action<Type, Type2, Type3> Action;

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3)
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				((CommandBinding<Type, Type2, Type3>)this.bindings[j]).Execute(parameter1, parameter2, parameter3);
			if (this.Action != null)
				this.Action(parameter1, parameter2, parameter3);
		}
	}

	public class Command<Type, Type2, Type3, Type4> : BaseCommand
	{
		public Action<Type, Type2, Type3, Type4> Action;

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3, Type4 parameter4)
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				((CommandBinding<Type, Type2, Type3, Type4>)this.bindings[j]).Execute(parameter1, parameter2, parameter3, parameter4);
			if (this.Action != null)
				this.Action(parameter1, parameter2, parameter3, parameter4);
		}
	}
}
