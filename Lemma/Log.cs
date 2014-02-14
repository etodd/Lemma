using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Lemma
{
	public class Log
	{
#if DEBUG
		public static void d(string log)
		{
			StackTrace trace = new StackTrace();
			MethodBase method = trace.GetFrame(1).GetMethod();
			Console.WriteLine(string.Format("{0}.{1}: {2}", method.ReflectedType.Name, method.Name, log));
		}
#else
		public static void d(string log)
		{

		}
#endif
	}
}
