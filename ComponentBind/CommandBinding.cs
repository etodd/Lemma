using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComponentBind
{
	public interface ICommandBinding : IBinding
	{

	}

	public abstract class BaseCommandBinding<CommandType> : ICommandBinding
		where CommandType : BaseCommand
	{
		protected CommandType source;
		protected CommandType[] destinations;
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

	public class CommandBinding : BaseCommandBinding<Command>
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
				for (int i = 0; i < this.destinations.Length; i++)
				{
					this.destinations[i].Execute();
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type> : BaseCommandBinding<Command<Type>>
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
				for (int i = 0; i < this.destinations.Length; i++)
				{
					this.destinations[i].Execute(parameter1);
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type, Type2> : BaseCommandBinding<Command<Type, Type2>>
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
				for (int i = 0; i < this.destinations.Length; i++)
				{
					this.destinations[i].Execute(parameter1, parameter2);
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type, Type2, Type3> : BaseCommandBinding<Command<Type, Type2, Type3>>
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
				for (int i = 0; i < this.destinations.Length; i++)
				{
					this.destinations[i].Execute(parameter1, parameter2, parameter3);
					if (this.destinations == null)
						break;
				}
			}
		}
	}

	public class CommandBinding<Type, Type2, Type3, Type4> : BaseCommandBinding<Command<Type, Type2, Type3, Type4>>
	{
		public CommandBinding(Command<Type, Type2, Type3, Type4> _source, params Command<Type, Type2, Type3, Type4>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2, Type3, Type4> _source, params Action<Type, Type2, Type3, Type4>[] _destinations)
			: this(_source, () => true, _destinations)
		{
		}

		public CommandBinding(Command<Type, Type2, Type3, Type4> _source, Func<bool> enabled, params Action<Type, Type2, Type3, Type4>[] _destinations)
			: this(_source, enabled, _destinations.Select(x => new Command<Type, Type2, Type3, Type4> { Action = x }).ToArray())
		{
		}

		public CommandBinding(Command<Type, Type2, Type3, Type4> _source, Func<bool> enabled, params Command<Type, Type2, Type3, Type4>[] _destinations)
		{
			this.source = _source;
			this.source.AddBinding(this);
			this.destinations = _destinations;
			this.enabled = enabled;
		}

		public void Execute(Type parameter1, Type2 parameter2, Type3 parameter3, Type4 parameter4)
		{
			if (this.Enabled && this.enabled())
			{
				for (int i = 0; i < this.destinations.Length; i++)
				{
					this.destinations[i].Execute(parameter1, parameter2, parameter3, parameter4);
					if (this.destinations == null)
						break;
				}
			}
		}
	}
}
