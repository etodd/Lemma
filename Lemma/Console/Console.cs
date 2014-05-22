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
						var collection = new ConCommand.ArgCollection() { ParsedArgs = parsedArgs };
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
						convar.OutCastConstraint.ToString().Replace("System.", "") + ")");
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
			return convar == null ? defaultVal : convar.Value.Value;
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

		public static void BindAllTypes()
		{
			Assembly a = Assembly.GetExecutingAssembly();
			foreach (Type t in a.GetTypes())
				BindType(t);
		}

		public static void BindType(Type t, object instance = null)
		{

			List<MemberInfo> members = new List<MemberInfo>();
			foreach (FieldInfo m in t.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				members.Add(m);
			}
			foreach (PropertyInfo m in t.GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				members.Add(m);
			}
			foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
				members.Add(m);
			foreach (MemberInfo m in members)
			{
				foreach (var attribute in m.GetCustomAttributes(false))
				{
					if (attribute is AutoConVar)
					{
						BindMember(m, (AutoConVar)attribute, instance);
					}
					else if (attribute is AutoConCommand)
					{
						BindMethod((MethodInfo)m, (AutoConCommand)attribute, instance);
					}
				}
			}
		}

		public static void BindMethod(MethodInfo member, AutoConCommand command, object instance = null)
		{
			List<Type> allowedTypes = new Type[] { typeof(bool), typeof(int), typeof(float), typeof(double), typeof(string) }.ToList();
			List<ConCommand.CommandArgument> args = new List<ConCommand.CommandArgument>();
			int numParams = member.GetParameters().Length;
			foreach (var param in member.GetParameters())
			{
				numParams++;
				var paramType = param.ParameterType;
				object defaultValue = param.DefaultValue;
				bool isOptional = param.IsOptional;
				string name = param.Name;
				args.Add(new ConCommand.CommandArgument()
				{
					CommandType = paramType,
					DefaultVal = defaultValue,
					Name = name,
					Optional = isOptional
				});
			}
			AddConCommand(new ConCommand(command.ConVarName, command.ConVarDesc, collection => CallMethod(member, collection), args.ToArray()));
		}

		private static void CallMethod(MethodInfo member, ConCommand.ArgCollection collection)
		{
			List<object> invokeParams = new List<object>();
			foreach(var o in collection.ParsedArgs)
				invokeParams.Add(o.Value);
			member.Invoke(null, invokeParams.ToArray());
		}

		public static void BindMember(MemberInfo member, AutoConVar convar, object instance = null)
		{
			List<Type> allowedTypes = new Type[] { typeof(bool), typeof(int), typeof(float), typeof(double), typeof(string) }.ToList();
			List<Type> generics = allowedTypes.Select(Type => typeof(Property<>).MakeGenericType(Type)).ToList();
			allowedTypes.AddRange(generics);

			Type curType = null;
			object value = "null";
			bool isProperty = true;

			string name = convar.ConVarName;
			string desc = convar.ConVarDesc;

			switch (member.MemberType)
			{
				case MemberTypes.Field:
					curType = ((FieldInfo)member).FieldType;
					value = ((FieldInfo)member).GetValue(instance);
					break;
				case MemberTypes.Property:
					curType = ((PropertyInfo)member).PropertyType;
					value = ((PropertyInfo)member).GetValue(instance, null);
					break;
			}
			if (curType == null) return;
			if (!allowedTypes.Contains(curType))
			{
				throw new Exception("Cannot auto-bind convar to type " + curType.ToString());
			}

			object propertyValue = value;
			//There is NO other way I'm SORRY
			if (curType == typeof(Property<bool>))
			{
				value = ((Property<bool>)value).Value.ToString();
				curType = typeof(bool);
				((Property<bool>)propertyValue).AddBinding(new NotifyBinding(() =>
				{
					Console.GetConVar(name).Value.InternalValue = ((Property<bool>)propertyValue).Value.ToString();
				}));
			}
			else if (curType == typeof(Property<int>))
			{
				value = ((Property<int>)value).Value.ToString();
				curType = typeof(int);
				((Property<int>)propertyValue).AddBinding(new NotifyBinding(() =>
				{
					Console.GetConVar(name).Value.InternalValue = ((Property<int>)propertyValue).Value.ToString();
				}));
			}
			else if (curType == typeof(Property<float>))
			{
				value = ((Property<float>)value).Value.ToString();
				curType = typeof(float);
				((Property<float>)propertyValue).AddBinding(new NotifyBinding(() =>
				{
					Console.GetConVar(name).Value.InternalValue = ((Property<float>)propertyValue).Value.ToString();
				}));
			}
			else if (curType == typeof(Property<double>))
			{
				value = ((Property<double>)value).Value.ToString();
				curType = typeof(double);
				((Property<double>)propertyValue).AddBinding(new NotifyBinding(() =>
				{
					Console.GetConVar(name).Value.InternalValue = ((Property<double>)propertyValue).Value.ToString();
				}));
			}
			else if (curType == typeof(Property<string>))
			{
				value = ((Property<string>)value).Value;
				curType = typeof(string);
				((Property<string>)propertyValue).AddBinding(new NotifyBinding(() =>
				{
					Console.GetConVar(name).Value.InternalValue = ((Property<string>)propertyValue).Value.ToString();
				}));
			}
			else
			{
				isProperty = false;
			}

			if (!isProperty)
			{
				AddConVar(new ConVar(name, desc, (string)value)
				{
					TypeConstraint = curType,
					Value = new Property<string>() { Value = (string)value },
					OnChanged = new Property<Action<string>>()
					{
						Value = (s) =>
						{
							switch (member.MemberType)
							{
								case MemberTypes.Field:
									((FieldInfo)member).SetValue(instance, GetConVar(name).GetCastedValue());
									break;
								case MemberTypes.Property:
									((PropertyInfo)member).SetValue(instance, GetConVar(name).GetCastedValue(), null);
									break;
							}
						}
					}
				});
			}
			else
			{
				AddConVar(new ConVar(name, desc, (string)value)
				{
					TypeConstraint = curType,
					Value = new Property<string>() { Value = (string)value },
					OnChanged = new Property<Action<string>>()
					{
						Value = (s) =>
						{
							object val = GetConVar(name).GetCastedValue();
							if (curType == typeof(bool))
							{
								((Property<bool>)propertyValue).Value = (bool)val;
							}
							else if (curType == typeof(int))
							{
								((Property<int>)propertyValue).Value = (int)val;
							}
							else if (curType == typeof(float))
							{
								((Property<float>)propertyValue).Value = (float)val;
							}
							else if (curType == typeof(double))
							{
								((Property<double>)propertyValue).Value = (double)val;
							}
							else if (curType == typeof(string))
							{
								((Property<string>)propertyValue).Value = (string)val;
							}
						}
					}
				});
			}
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
				BindType(t);
			}



			Console.Instance = this;
		}

		public void Update(float dt)
		{

		}
	}
}
