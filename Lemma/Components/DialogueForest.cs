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
		public class Node
		{
			public enum Type
			{
				Text, Choice, Branch, Set
			}
			public string next;
			public List<string> choices;
			public Dictionary<string, string> branches;
			public string id;
			public string name;
			public string variable;
			public string value;
			public Type type;
			public Node Parent;
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

			foreach (Node node in nodes)
			{
				if (node.choices != null)
				{
					foreach (string c in node.choices)
						this[c].Parent = node;
				}

				if (node.next != null)
					this[((string)node.next)].Parent = node;

				if (node.branches != null)
				{
					foreach (Node child in node.branches.Values.Select(x => this[x]))
						child.Parent = node;
				}
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
	}
}
