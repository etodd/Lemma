using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ComponentBind;
using Newtonsoft.Json.Converters;

namespace Lemma.Util
{
	public class DialogueForest
	{
		public interface IClient
		{
			void Visit(Node node);
			void Text(Node node, int level);
			void Choice(Node node, IEnumerable<Node> choices);
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

		private Dictionary<string, Node> nodes = new Dictionary<string, Node>();
		private Dictionary<string, Node> nodesByName = new Dictionary<string, Node>();

		public IEnumerable<Node> Load(string data)
		{
			List<Node> nodes = JsonConvert.DeserializeObject<List<Node>>(data);

			foreach (Node node in nodes)
			{
				this.nodes[node.id] = node;
				if (node.name != null)
					this.nodesByName[node.name] = node;
			}
			return nodes;
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

		public void Validate(Strings strings)
		{
#if DEBUG
			foreach (Node n in this.nodes.Values)
			{
				if ((n.type == Node.Type.Choice || n.type == Node.Type.Text) && !strings.HasKey(n.name))
					Log.d(string.Format("Invalid dialogue {0} \"{1}\"", n.type.ToString(), n.name));
			}
#endif
		}

		public void Execute(Node node, IClient client, int textLevel = 1)
		{
			client.Visit(node);
			string next = null;
			switch (node.type)
			{
				case Node.Type.Node:
					if (node.choices != null && node.choices.Count > 0)
						client.Choice(node, node.choices.Select(x => this[x]));
					next = node.next;
					break;
				case Node.Type.Text:
					client.Text(node, textLevel);
					if (node.choices != null && node.choices.Count > 0)
						client.Choice(node, node.choices.Select(x => this[x]));
					next = node.next;
					textLevel++;
					break;
				case Node.Type.Set:
					client.Set(node.variable, node.value);
					next = node.next;
					break;
				case Node.Type.Branch:
					string key = client.Get(node.variable);
					if (key == null || !node.branches.TryGetValue(key, out next))
						node.branches.TryGetValue("_default", out next);
					break;
				default:
					break;
			}
			if (next != null)
				this.Execute(this[next], client, textLevel);
		}
	}
}