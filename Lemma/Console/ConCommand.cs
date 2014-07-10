using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms.VisualStyles;
using ComponentBind;

namespace Lemma.Console
{
	public class ConCommand
	{
		public Property<string> Name = new Property<string>();
		public Property<string> Description = new Property<string>();
		public Property<Action<ArgCollection>> OnCalled = new Property<Action<ArgCollection>>();
		public Property<CommandArgument[]> Arguments = new Property<CommandArgument[]>();


		public int NumArgs
		{
			get { return Arguments.Value.Length; }
		}

		public int NumRequiredArgs
		{
			get
			{
				int ret = 0;
				foreach (var arg in Arguments.Value)
					if (!arg.Optional) ret++;
				return ret;
			}
		}

		public int NumOptionalArgs
		{
			get
			{
				int ret = 0;
				foreach (var arg in Arguments.Value)
					if (arg.Optional) ret++;
				return ret;
			}

		}

		//Only one constructor; OnCalled needs to be set. It's a command.
		public ConCommand(string name, string description, Action<ArgCollection> onCalled, params CommandArgument[] args)
		{
			this.Name.Value = name;
			this.Description.Value = description;
			this.OnCalled.Value = onCalled;
			this.Arguments.Value = args;
		}

		public void SetArgs(CommandArgument[] args)
		{
			bool pastOptional = false;
			foreach (var arg in args)
			{
				if (arg.Optional) pastOptional = true;
				if (!arg.Optional && pastOptional) throw new Exception("Cannot define a non-optional argument after an optional argument has been defined.");
				if (string.IsNullOrEmpty(arg.Name)) throw new Exception("Empty or null name for command argument");
				if (arg.CommandType == null) throw new Exception("Invalid type for command argument");
			}
			this.Arguments.Value = args;
		}

		public ParsedArgument[] ParseArguments(ConsoleParser.ParseResult.ParseToken[] tokens)
		{
			if (tokens == null) return null;
			List<ParsedArgument> ret = new List<ParsedArgument>();

			int curArg = 0;
			foreach (var token in tokens)
			{
				if (token.Type == ConsoleParser.ParseResult.ParseToken.TokenType.CmdOrVar) continue;
				CommandArgument arg = Arguments.Value[curArg];
				if (!arg.IsGoodValue(token.Value)) return null;

				ParsedArgument newArg = new ParsedArgument()
				{
					Name = arg.Name,
					CommandType = arg.CommandType,
					StrValue = token.Value,
					Value = arg.GetConvertedValue(token.Value)
				};
				curArg++;
				ret.Add(newArg);
			}

			//Required argument(s) not passed.
			if (curArg < NumRequiredArgs) return null;

			//Have to populate the list with any optional arguments not passed in.
			for (int i = curArg; i < NumArgs; i++)
			{
				CommandArgument arg = Arguments.Value[i];
				ParsedArgument newArg = new ParsedArgument()
				{
					Name = arg.Name,
					CommandType = arg.CommandType,
					StrValue = arg.DefaultVal == null ? null : arg.DefaultVal.ToString(),
					Value = arg.DefaultVal
				};
				curArg++;
				ret.Add(newArg);
			}

			return ret.ToArray();
		}

		public CommandArgument GetArgument(string name)
		{
			foreach (var arg in Arguments.Value)
				if (arg.Name == name) return arg;
			return null;
		}

		public class CommandArgument
		{
			public Type CommandType = typeof(string);
			public string Name;
			public bool Optional = false;
			public object DefaultVal = null;
			public Func<object, bool> Validate = o => true;

			public object GetConvertedValue(string input, bool ignore = false)
			{
				if (!ignore && !IsGoodValue(input)) return null;
				var typeConverter = TypeDescriptor.GetConverter(CommandType);
				return typeConverter.ConvertFromString(input);
			}

			public bool IsGoodValue(string input)
			{
				Type T = CommandType;
				if (T == null) return true;
				var typeConverter = TypeDescriptor.GetConverter(T);
				return typeConverter.CanConvertFrom(typeof(string)) && IsGood(T, input) && Validate(GetConvertedValue(input, true));
			}

			public bool IsGood(Type T, string input)
			{
				if (T == typeof(bool))
				{
					string[] good = new string[] { "true", "false" };
					return good.Contains(input.ToLower());
				}
				return true;
			}
		}

		public class ParsedArgument
		{
			public string Name;
			public Type CommandType;
			public string StrValue;
			public object Value;
		}

		public class ArgCollection
		{
			public ParsedArgument[] ParsedArgs;

			public object Get(int i)
			{
				if (ParsedArgs == null || ParsedArgs.Length <= i) return null;
				return ParsedArgs[i].Value;
			}

			public object Get(string name)
			{
				if (ParsedArgs == null) return null;
				foreach(var arg in ParsedArgs)
					if (arg.Name == name)
						return arg.Value;
				return null;
			}
		}
	}
}
