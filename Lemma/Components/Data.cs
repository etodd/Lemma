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
				for (int i = 0; i < value.Length; i++)
				{
					this.properties.Add((string)value[i].Key, (IProperty)value[i].Value);
				}
			}
		}

		public Property<T> Property<T>(string name, T defaultValue)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				result = new Property<T> { Value = defaultValue };
				this.properties[name] = result;
			}
			return (Property<T>)result;
		}

		public ListProperty<T> ListProperty<T>(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				result = new ListProperty<T>();
				this.properties[name] = result;
			}
			return (ListProperty<T>)result;
		}
	}
}
