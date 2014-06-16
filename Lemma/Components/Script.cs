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

		[XmlIgnore]
		public EditorProperty<string> Errors = new EditorProperty<string>();

		public EditorProperty<string> Name = new EditorProperty<string>();

		[XmlIgnore]
		public Command Execute = new Command();

		public EditorProperty<bool> ExecuteOnLoad = new EditorProperty<bool> { Value = true };

		public EditorProperty<bool> DeleteOnExecute = new EditorProperty<bool>();

		public ListProperty<Entity.Handle> ConnectedEntities = new ListProperty<Entity.Handle>();

		private MethodInfo scriptMethod;

		/// <summary>
		/// Tries to use reflection to load 
		/// </summary>
		/// <param name="main"></param>
		/// <param name="name"></param>
		/// <param name="scriptEntity"></param>
		/// <param name="errors"></param>
		/// <returns></returns>
		private static MethodInfo GetInternalScriptRunMethod(Main main, string name, Entity scriptEntity, out string errors)
		{
			string neededNameSpace = "Lemma.GameScripts";
			errors = null;
			Assembly assembly = Assembly.GetExecutingAssembly();
			foreach (var type in assembly.GetTypes())
			{
				if (type.Namespace == neededNameSpace)
				{
					if (type.IsClass && type.Name == name)
					{
						MethodInfo ret = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
						if (ret == null)
						{
							errors = "Could not find public static method Run in " + name;
							return null;
						}

						type.GetField("script", BindingFlags.Static | BindingFlags.Public).SetValue(null, scriptEntity);
						return ret;
					}
				}
			}
			errors = "Could not find class " + name;
			return null;

		}

		public static MethodInfo GetScriptRunMethod(Main main, string name, Entity scriptEntity, out string errors)
		{
			Assembly assembly = null;

			errors = null;

			string scriptPath = Path.Combine(IO.MapLoader.MapDirectory, name + "." + Script.ScriptExtension);
			string binaryPath = Path.Combine(IO.MapLoader.MapDirectory, name + "." + Script.BinaryExtension);

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
				return t.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
			}
			else
				return GetInternalScriptRunMethod(main, name, scriptEntity, out errors);
		}

		public override void Awake()
		{
			base.Awake();
			this.Errors.Value = null;
			this.Name.Set = delegate(string value)
			{
				this.Name.InternalValue = value;
				this.scriptMethod = null;
				this.Errors.Value = null;
				try
				{
					string errors;
					this.scriptMethod = GetScriptRunMethod(this.main, this.Name, this.Entity, out errors);
					this.Errors.Value = errors;
				}
				catch (Exception e)
				{
					this.Errors.Value = e.ToString();
				}
			};

			this.Execute.Action = delegate()
			{
				if (Lemma.GameScripts.ScriptBase.main == null)
				{
					Lemma.GameScripts.ScriptBase.main = main;
					Lemma.GameScripts.ScriptBase.renderer = main.Renderer;
				}

				if (this.scriptMethod != null)
					this.scriptMethod.Invoke(null, null);

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
