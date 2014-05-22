using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Console
{
	[AttributeUsage(AttributeTargets.Method)]
	public class AutoConCommand : System.Attribute
	{
		public readonly string ConVarName;
		public readonly string ConVarDesc;
		public AutoConCommand(string name, string description)
		{
			this.ConVarName = name;
			this.ConVarDesc = description;
		}
	}
}
