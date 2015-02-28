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
	public class DialogueTrigger : ScriptBase
	{
		public static new bool AvailableInReleaseEditor = true;
		public static void Run(Entity script)
		{
			Property<string> node = property<string>(script, "Node");
			Command trigger = command(script, "Trigger");

			Phone phone = PlayerDataFactory.Instance.Get<Phone>();
			if (!string.IsNullOrEmpty(node))
				script.Add(new CommandBinding(phone.OnVisit(node), trigger));
		}

		public static IEnumerable<string> EditorProperties(Entity script)
		{
			script.Add("Node", property<string>(script, "Node"));
			return new string[]
			{
				"Node",
			};
		}

		public static IEnumerable<string> Commands(Entity script)
		{
			script.Add("Trigger", command(script, "Trigger"));
			return new string[]
			{
				"Trigger",
			};
		}
	}
}