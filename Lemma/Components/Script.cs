using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Console;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Script : Component<Main>
	{
		public const string ScriptExtension = "cs";
		public const string BinaryExtension = "dll";
		public const string ScriptNamespace = "Lemma.GameScripts";

		[XmlIgnore]
		public Property<string> Errors = new Property<string>();

		public Property<string> Name = new Property<string>();

		[XmlIgnore]
		public Command Execute = new Command();

		public Property<bool> ExecuteOnLoad = new Property<bool> { Value = true };

		public Property<bool> DeleteOnExecute = new Property<bool>();

		private struct ScriptMethods
		{
			public MethodInfo Run;
			public MethodInfo EditorProperties;
			public MethodInfo Commands;
		}

		private ScriptMethods methods;

		/// <summary>
		/// Tries to use reflection to load 
		/// </summary>
		/// <param name="main"></param>
		/// <param name="name"></param>
		/// <param name="scriptEntity"></param>
		/// <param name="errors"></param>
		/// <returns></returns>
		private static ScriptMethods GetInternalScriptMethods(Main main, string name, Entity scriptEntity, out string errors)
		{
			errors = null;
			Assembly assembly = Assembly.GetExecutingAssembly();
			foreach (var type in assembly.GetTypes())
			{
				if (type.Namespace == Script.ScriptNamespace)
				{
					if (type.IsClass && type.BaseType == typeof(GameScripts.ScriptBase) && type.Name == name)
					{
						MethodInfo run = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
						if (run == null)
							errors = "Could not find public static method Run in " + name;

						return new ScriptMethods
						{
							Run = run,
							EditorProperties = type.GetMethod("EditorProperties", BindingFlags.Static | BindingFlags.Public),
							Commands = type.GetMethod("Commands", BindingFlags.Static | BindingFlags.Public),
						};
					}
				}
			}
			errors = "Could not find class " + name;
			return new ScriptMethods();
		}

		private bool loadedPreviously;
		private IEnumerable<string> editorProperties;
		private IEnumerable<string> commands;

		private void load(string name)
		{
			if (this.loadedPreviously)
			{
				if (this.editorProperties != null)
				{
					foreach (string prop in this.editorProperties)
						this.Entity.RemoveProperty(prop);
					this.editorProperties = null;
				}
				if (this.commands != null)
				{
					foreach (string cmd in this.commands)
						this.Entity.RemoveCommand(cmd);
					this.commands = null;
				}
			}

			this.methods = new ScriptMethods();
			this.Errors.Value = null;
			if (!string.IsNullOrEmpty(name))
			{
				try
				{
					string errors;
					this.methods = GetInternalScriptMethods(this.main, name, this.Entity, out errors);
					this.Errors.Value = errors;
					if (this.methods.EditorProperties != null)
					{
						object result = this.methods.EditorProperties.Invoke(null, this.parameters);
						this.editorProperties = result as IEnumerable<string>;
					}
					if (this.methods.Commands != null)
					{
						object result = this.methods.Commands.Invoke(null, this.parameters);
						this.commands = result as IEnumerable<string>;
					}
					this.loadedPreviously = true;
				}
				catch (Exception e)
				{
					this.Errors.Value = e.ToString();
					Log.d(this.Errors);
				}
			}
		}

		private object[] parameters;

		public override void Awake()
		{
			base.Awake();
			if (Lemma.GameScripts.ScriptBase.main == null)
			{
				Lemma.GameScripts.ScriptBase.main = main;
				Lemma.GameScripts.ScriptBase.renderer = main.Renderer;
			}
			this.parameters = new object[] { this.Entity };
			this.Errors.Value = null;
			this.Add(new ChangeBinding<string>(this.Name, delegate(string old, string value)
			{
				if (value != old)
					this.load(value);
			}));

			this.Execute.Action = delegate()
			{
				if (this.methods.Run != null)
					this.methods.Run.Invoke(null, this.parameters);

				if (this.DeleteOnExecute)
				{
					if (this.Entity != null)
						this.Entity.Add(new Animation(new Animation.Execute(this.Delete)));
					else
						this.main.AddComponent(new Animation(new Animation.Execute(this.Delete)));
				}
			};
		}

		public override void Start()
		{
			if (!this.main.EditorEnabled && this.ExecuteOnLoad)
				this.Execute.Execute();
		}

		public static void ConsoleInit()
		{
			Lemma.Console.Console.AddConVar(new ConVar("prefer_local_scripts", "If true, local scripts will be loaded instead of internal ones.", "true") { TypeConstraint = typeof(bool) });
		}
	}
}