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

		private static ScriptMethods GetScriptMethods(Main main, string name, Entity scriptEntity, out string errors)
		{
			Assembly assembly = null;

			errors = null;

			string scriptPath = Path.Combine(main.MapDirectory, name + "." + Script.ScriptExtension);
			string binaryPath = Path.Combine(main.MapDirectory, name + "." + Script.BinaryExtension);

			bool preferLocalScripts = (bool)Console.Console.GetConVar("prefer_local_scripts").GetCastedValue();

			DateTime scriptTime = File.GetLastWriteTime(scriptPath);
			DateTime binaryTime = File.GetLastWriteTime(binaryPath);

			bool loadOnlyLocal = preferLocalScripts && (File.Exists(scriptPath) || File.Exists(binaryPath));
			bool loadAssemblyVsScript = !(!File.Exists(binaryPath) || scriptTime > binaryTime) && File.Exists(scriptPath);

			if (loadOnlyLocal && !loadAssemblyVsScript)
			{
				// Recompile the script
				using (Stream stream = TitleContainer.OpenStream(scriptPath))
				using (TextReader reader = new StreamReader(stream))
				{
					CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

					CompilerParameters cp = new CompilerParameters
					{
						GenerateExecutable = false,
						GenerateInMemory = false,
						TreatWarningsAsErrors = false,
					};

					// Add references to all the assemblies we might need.
					Assembly executingAssembly = Assembly.GetExecutingAssembly();
					cp.ReferencedAssemblies.Add(executingAssembly.Location);
					foreach (AssemblyName assemblyName in executingAssembly.GetReferencedAssemblies())
						cp.ReferencedAssemblies.Add(Assembly.Load(assemblyName).Location);

					// Invoke compilation of the source file.
					CompilerResults cr = provider.CompileAssemblyFromSource(cp, reader.ReadToEnd());

					if (cr.Errors.Count > 0)
					{
						// Display compilation errors.
						StringBuilder builder = new StringBuilder();
						foreach (CompilerError ce in cr.Errors)
						{
							builder.Append("Line ");
							builder.Append(ce.Line.ToString());
							builder.Append(": ");
							builder.Append(ce.ErrorNumber);
							builder.Append(": ");
							builder.Append(ce.ErrorText);
							builder.Append("\n");
						}
						errors = builder.ToString();
					}
					else
						assembly = cr.CompiledAssembly;
				}
			}
			else if (loadOnlyLocal && File.Exists(binaryPath)) // Load the precompiled script binary
				assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), binaryPath));

			if (assembly != null)
			{
				Type t = assembly.GetType("Lemma.GameScripts.Script");
				t.GetField("script", BindingFlags.Static | BindingFlags.Public).SetValue(null, scriptEntity);
				return new ScriptMethods
				{
					Run = t.GetMethod("Run", BindingFlags.Static | BindingFlags.Public),
					EditorProperties = t.GetMethod("EditorProperties", BindingFlags.Static | BindingFlags.Public),
					Commands = t.GetMethod("Commands", BindingFlags.Static | BindingFlags.Public),
				};
			}
			else
				return GetInternalScriptMethods(main, name, scriptEntity, out errors);
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
					this.methods = GetScriptMethods(this.main, name, this.Entity, out errors);
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
