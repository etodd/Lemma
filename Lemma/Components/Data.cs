using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Data : Component<Main>
	{
		private Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>();
		private Dictionary<string, Command> commands = new Dictionary<string, Command>();

		[XmlArray("Properties")]
		[XmlArrayItem("Property", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] Properties
		{
			get
			{
				return this.properties.Select(x => new DictionaryEntry(x.Key, x.Value)).ToArray();
			}
			set
			{
				this.properties.Clear();
				for (int i = 0; i < value.Length; i++)
					this.properties.Add((string)value[i].Key, (IProperty)value[i].Value);
			}
		}

		public Property<T> Property<T>(string name, T defaultValue = default(T))
		{
			IProperty result;
			if (!this.properties.TryGetValue(name, out result))
				this.properties[name] = result = new Property<T> { Value = defaultValue };
			return (Property<T>)result;
		}

		public ListProperty<T> ListProperty<T>(string name)
		{
			IProperty result;
			if (!this.properties.TryGetValue(name, out result))
				this.properties[name] = result = new ListProperty<T>();
			return (ListProperty<T>)result;
		}

		public Command Command(string name)
		{
			Command result;
			if (!this.commands.TryGetValue(name, out result))
				this.commands[name] = result = new Command();
			return result;
		}
	}
}
