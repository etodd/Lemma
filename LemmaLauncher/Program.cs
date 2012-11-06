using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace LemmaLauncher
{
	class Program
	{
		public static void Main(string[] args)
		{
			string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			AppDomainSetup setup = new AppDomainSetup();
			setup.ShadowCopyFiles = "true";
			setup.ApplicationBase = baseDirectory;
			setup.PrivateBinPath = baseDirectory;

			AppDomain domain = AppDomain.CreateDomain("", AppDomain.CurrentDomain.Evidence, baseDirectory, baseDirectory, true, null, null);
			domain.ExecuteAssembly(Path.Combine(baseDirectory, "Lemma.exe"), args);
		}
	}
}
