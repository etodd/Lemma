using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace ComponentBind
{
	public class Log
	{
		public static Action<string> Handler = (Action<string>)Console.WriteLine;

		public static void d(string log)
		{
			if (Log.Handler != null)
			{
				StackTrace trace = new StackTrace();
				MethodBase method = trace.GetFrame(1).GetMethod();
				Log.Handler(string.Format("{0}.{1}: {2}", method.ReflectedType.Name, method.Name, log));
			}
		}
	}
}
