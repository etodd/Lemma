using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ComponentBind;
using Newtonsoft.Json.Converters;

namespace Lemma.Components
{
	public class DialogueForest
	{
		public interface IListener
		{
			void Text(string text, int level);
			void Choice(string parent, IEnumerable<string> choices);
			void Set(string key, string value);
			string Get(string key);
		}

		public class Node
		{
			public enum Type
			{
				Node, Text, Choice, Branch, Set
			}
			public string next;
			public List<string> choices;
			public Dictionary<string, string> branches;
			public string id;
			public string name;
			public string variable;
			public string value;
			public Type type;
		}

		static DialogueForest()
		{
			JsonConvert.DefaultSettings = delegate()
			{
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Converters.Add(new StringEnumConverter());
				return settings;
			};
		}

		private Dictionary<string, Node> nodes = new Dictionary<string, Node>();
		private Dictionary<string, Node> nodesByName = new Dictionary<string, Node>();

		public DialogueForest(string data)
		{
			List<Node> nodes = JsonConvert.DeserializeObject<List<Node>>(data);

			foreach (Node node in nodes)
			{
				this.nodes[node.id] = node;
				if (node.name != null)
					this.nodesByName[node.name] = node;
			}
		}

		public Node this[string id]
		{
			get
			{
				Node result;
				this.nodes.TryGetValue(id, out result);
				return result;
			}
		}

		public Node GetByName(string name)
		{
			Node result;
			this.nodesByName.TryGetValue(name, out result);
			return result;
		}

		public IEnumerable<Node> Nodes
		{
			get
			{
				return this.nodes.Values;
			}
		}

		public void Execute(Node node, IListener listener, int textLevel = 1)
		{
			string next = null;
			switch (node.type)
			{
				case DialogueForest.Node.Type.Node:
					if (node.choices != null && node.choices.Count > 0)
						listener.Choice(node.name, node.choices.Select(x => this[x].name));
					next = node.next;
					break;
				case DialogueForest.Node.Type.Text:
					listener.Text(node.name, textLevel);
					if (node.choices != null && node.choices.Count > 0)
						listener.Choice(node.name, node.choices.Select(x => this[x].name));
					next = node.next;
					textLevel++;
					break;
				case DialogueForest.Node.Type.Set:
					listener.Set(node.variable, node.value);
					next = node.next;
					break;
				case DialogueForest.Node.Type.Branch:
					string key = listener.Get(node.variable);
					if (key == null || !node.branches.TryGetValue(key, out next))
						node.branches.TryGetValue("_default", out next);
					break;
				default:
					break;
			}
			if (next != null)
				this.Execute(this[next], listener, textLevel);
		}
	}
}
