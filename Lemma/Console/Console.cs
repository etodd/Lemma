using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Console
{
	public class Console : Component<Main>, IUpdateableComponent
	{
		public static List<ConVar> ConVars = new List<ConVar>();
		public static List<ConCommand> Commands = new List<ConCommand>();

		public static Console Instance;

		public void ConsoleUserInput(string input)
		{
			ConsoleParser.ParseResult parsed = ConsoleParser.Parse(input);
			Log(">" + input);
			if (parsed.ParsedResult.Length == 0 || !IsConVarOrCmd(parsed.ParsedResult[0].Value))
			{
				Log("Incorrect input");
			}
			string ConObject = parsed.ParsedResult[0].Value;
			if (IsConCommand(ConObject))
			{
				int NumArgs = parsed.ParsedResult.Length - 1;
				var command = GetConCommand(ConObject);
				if (NumArgs >= command.NumRequiredArgs && NumArgs <= command.NumArgs)
				{
					var parsedArgs = command.ParseArguments(parsed.ParsedResult);
					if (parsedArgs == null)
					{
						Log("ERROR: Bad arguments");
						PrintConCommandDescription(command);
					}
					else
					{
						var collection = new ConCommand.ArgCollection() {ParsedArgs = parsedArgs};
						command.OnCalled.Value(collection);
					}
				}
				else
				{
					Log("ERROR: Wrong number of arguments.");
					PrintConCommandDescription(command);
				}
			}
			else if (IsConVar(ConObject))
			{
				if (parsed.ParsedResult.Length == 1)
				{
					var convar = GetConVar(parsed.ParsedResult[0].Value);
					Log(convar.Name + ": " + convar.Description + " (Value: " + convar.GetCastedValue() + " " +
						convar.OutCastConstraint.ToString().Replace("System.","") + ")");
				}
				else
				{
					var argument = parsed.ParsedResult[1].Value;
					SetConVarValue(parsed.ParsedResult[0].Value, argument);
				}
			}

		}

		public void Log(string input)
		{
			main.ConsoleUI.LogText(input);
		}

		public void PrintConCommandDescription(string name)
		{
			PrintConCommandDescription(GetConCommand(name));
		}

		public void PrintConCommandDescription(ConCommand cmd)
		{
			if (cmd == null) return;
			string print = cmd.Name + ": " + cmd.Description;
			if (cmd.NumArgs > 0)
			{
				print += " ( ";
				int i = 0;
				foreach (var arg in cmd.Arguments.Value)
				{
					if (i++ > 0) print += ", ";
					string type = arg.CommandType.ToString().Replace("System.", "");
					if (arg.Optional)
					{
						print += " [" + type + " " + arg.Name + " = " + arg.DefaultVal + "]";
					}
					else
					{
						print += type + " " + arg.Name;
					}
				}
			}
			print += " )";
			Log(print);
		}



		public static void SetConVarValue(string name, string value)
		{
			var convar = GetConVar(name);
			if (convar == null || !convar.IsGoodValue(value)) return;

			convar.Value.Value = value;
			if (convar.OnChanged.Value != null)
				convar.OnChanged.Value(value);
		}

		public static string GetConVarValue(string name, string defaultVal = "")
		{
			var convar = GetConVar(name);
			return convar == null ? defaultVal : convar.Value;
		}

		public static ConCommand GetConCommand(string name)
		{
			if (!IsConCommand(name)) return null;
			return (from command in Commands where command.Name == name select command).First();
		}

		public static ConVar GetConVar(string name)
		{
			if (!IsConVar(name)) return null;
			return (from convar in ConVars where convar.Name == name select convar).First();
		}

		public static bool IsConVar(string name)
		{
			return (from convar in ConVars where convar.Name == name select convar).Any();
		}

		public static void AddConVar(ConVar c)
		{
			if (IsConVar(c.Name)) return;
			ConVars.Add(c);
		}

		public static void AddConCommand(ConCommand c)
		{
			if (IsConCommand(c.Name)) return;
			Commands.Add(c);
		}

		public static bool IsConCommand(string name)
		{
			return (from command in Commands where command.Name == name select command).Any();
		}

		public static bool IsConVarOrCmd(string name)
		{
			return IsConVar(name) || IsConCommand(name);
		}

		public override void Awake()
		{
			base.Awake();

			//Get all classes, and execute "ConsoleInit" if any class has it.
			Assembly a = Assembly.GetExecutingAssembly();
			foreach (Type t in a.GetTypes())
			{
				MethodInfo info = t.GetMethod("ConsoleInit");
				if (info != null)
					info.Invoke(t, new object[0]);
			}

			Console.Instance = this;
		}

		public void Update(float dt)
		{

		}
	}
}
