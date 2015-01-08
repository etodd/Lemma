using System;
using ComponentBind;
using Lemma.GameScripts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Lemma.Components;
using Lemma.Factories;

namespace Lemma.GameScripts
{
	public class DialogueLink : ScriptBase
	{
		public static new bool AvailableInReleaseEditor = true;
		public static void Run(Entity script)
		{
			Property<string> lastNode = property<string>(script, "LastNode");
			Property<string> nextNode = property<string>(script, "NextNode");

			Phone phone = PlayerDataFactory.Instance.Get<Phone>();
			script.Add(new CommandBinding(phone.OnVisit(lastNode), new Command
			{
				Action = delegate()
				{
					DialogueForest forest = WorldFactory.Instance.Get<World>().DialogueForest;
					DialogueForest.Node n = forest.GetByName(nextNode);
					phone.Execute(n);
				}
			}));
		}

		public static IEnumerable<string> EditorProperties(Entity script)
		{
			script.Add("LastNode", property<string>(script, "LastNode"));
			script.Add("NextNode", property<string>(script, "NextNode"));
			return new string[]
			{
				"LastNode",
				"NextNode",
			};
		}
	}
}