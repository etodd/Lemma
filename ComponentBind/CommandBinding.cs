using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComponentBind
{
	public abstract class BaseCommandBinding : IBinding
	{
		protected Command source;
		protected Command[] destinations;
		protected Func<bool> enabled;

		public bool Enabled { get; set; }

		protected BaseCommandBinding()
		{
			this.Enabled = true;
		}

		public void Delete()
		{
			this.Enabled = false;
			this.source.RemoveBinding(this);
			this.source = null;
			this.destinations = null;
			this.enabled = null;
		}
	}

	public class CommandBinding : BaseCommandBinding
	{
		public CommandBinding(Command _source, params Command[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command _source, Func<bool> enabled, params Command[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations;
			this.enabled = enabled;
		}

		public CommandBinding(Command _source, params Action[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command _source, Func<bool> enabled, params Action[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations.Select(x => new Command { Action = x }).ToArray();
			this.enabled = enabled;
		}

		public void Execute()
		{
			if (this.Enabled && this.enabled())
			{
				foreach (Command cmd in this.destinations)
				{
					cmd.Execute();
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type> : BaseCommandBinding
	{
		public CommandBinding(Command<Type> _source, params Command<Type>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type> _source, params Action<Type>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type> _source, Func<bool> enabled, params Action<Type>[] _destinations)
			: this(_source, enabled, _destinations.Select(x => new Command<Type> { Action = x }).ToArray())
		{
		}

		public CommandBinding(Command<Type> _source, Func<bool> enabled, params Command<Type>[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations;
			this.enabled = enabled;
		}

		public void Execute(Type parameter1)
		{
			if (this.Enabled && this.enabled())
			{
				foreach (Command<Type> cmd in this.destinations)
				{
					cmd.Execute(parameter1);
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type, Type2> : BaseCommandBinding
	{
		public CommandBinding(Command<Type, Type2> _source, params Command<Type, Type2>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2> _source, params Action<Type, Type2>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2> _source, Func<bool> enabled, params Action<Type, Type2>[] _destinations)
			: this(_source, enabled, _destinations.Select(x => new Command<Type, Type2> { Action = x }).ToArray())
		{
		}

		public CommandBinding(Command<Type, Type2> _source, Func<bool> enabled, params Command<Type, Type2>[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations;
			this.enabled = enabled;
		}

		public void Execute(Type parameter1, Type2 parameter2)
		{
			if (this.Enabled && this.enabled())
			{
				foreach (Command<Type, Type2> cmd in this.destinations)
				{
					cmd.Execute(parameter1, parameter2);
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type, Type2, Type3> : BaseCommandBinding
	{
		public CommandBinding(Command<Type, Type2, Type3> _source, params Command<Type, Type2, Type3>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2, Type3> _source, params Action<Type, Type2, Type3>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2, Type3> _source, Func<bool> enabled, params Action<Type, Type2, Type3>[] _destinations)
			: this(_source, enabled, _destinations.Select(x => new Command<Type, Type2, Type3> { Action = x }).ToArray())
		{
		}

		public CommandBinding(Command<Type, Type2, Type3> _source, Func<bool> enabled, params Command<Type, Type2, Type3>[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations;
			this.enabled = enabled;
		}

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3)
		{
			if (this.Enabled && this.enabled())
			{
				foreach (Command<Type, Type2, Type3> cmd in this.destinations)
				{
					cmd.Execute(parameter1, parameter2, parameter3);
					if (this.destinations == null)
						break;
				}
			}
		}
	}
}
