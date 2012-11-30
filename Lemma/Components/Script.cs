using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class Script : Component
	{
		public const string ScriptExtension = "cs";
		public const string BinaryExtension = "dll";

		private const string scriptPrefix =
@"
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Lemma.Components;
using Lemma.Factories;

namespace Lemma.Scripts
{
	public class Script : ScriptBase
	{
		public static void Run()
		{
";

		private const string scriptPostfix =
@"
		}
	}
}
";

		[XmlIgnore]
		public Property<string> Errors = new Property<string> { Editable = true };

		public Property<string> Name = new Property<string> { Editable = true };

		[XmlIgnore]
		public Command Execute = new Command { ShowInEditor = true };

		private MethodInfo scriptMethod;

		public static MethodInfo GetScriptRunMethod(Main main, string name, Entity scriptEntity, out string errors)
		{
			Assembly assembly = null;

			errors = null;

			string scriptPath = Path.Combine(main.Content.RootDirectory, name + "." + Script.ScriptExtension);
			string binaryPath = Path.Combine(main.Content.RootDirectory, name + "." + Script.BinaryExtension);

			DateTime scriptTime = File.GetLastWriteTime(scriptPath);
			DateTime binaryTime = File.GetLastWriteTime(binaryPath);
			if (!File.Exists(binaryPath) || scriptTime > binaryTime)
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
					CompilerResults cr = provider.CompileAssemblyFromSource(cp, Script.scriptPrefix + reader.ReadToEnd() + Script.scriptPostfix);

					if (cr.Errors.Count > 0)
					{
						// Display compilation errors.
						StringBuilder builder = new StringBuilder();
						foreach (CompilerError ce in cr.Errors)
						{
							builder.Append(ce.ToString());
							builder.Append("\n");
						}
						errors = builder.ToString();
					}
					else
					{
						assembly = cr.CompiledAssembly;
						// TODO: Hack for the alpha
						//File.Copy(cp.OutputAssembly, binaryPath, true);
					}
				}
			}
			else // Load the precompiled script binary
				assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), binaryPath));

			if (assembly != null)
			{
				Type t = assembly.GetType("Lemma.Scripts.Script");
				t.GetField("main", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).SetValue(null, main);
				t.GetField("renderer", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).SetValue(null, main.Renderer);
				t.GetField("script", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).SetValue(null, scriptEntity);
				return t.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
			}
			return null;
		}

		public override void InitializeProperties()
		{
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
				if (this.scriptMethod != null)
					this.scriptMethod.Invoke(null, null);
			};
		}
	}
}
