using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Console
{
	[AttributeUsage(AttributeTargets.Field)]
	public class AutoConVar : System.Attribute
	{
		public readonly string ConVarName;
		public readonly string ConVarDesc;
		public AutoConVar(string name, string description)
		{
			this.ConVarName = name;
			this.ConVarDesc = description;
		}
	}
}
