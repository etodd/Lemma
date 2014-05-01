using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Reflection;

namespace ComponentBind
{
	[XmlInclude(typeof(Entity.Handle))]
	[XmlInclude(typeof(Property<Entity.Handle>))]
	[XmlInclude(typeof(ListProperty<Entity.Handle>))]
	[XmlInclude(typeof(ListProperty<string>))]
	[XmlInclude(typeof(Transform))]
	public class Entity
	{
		public struct Handle
		{
			private string id;
			[XmlAttribute]
			public string ID
			{
				get
				{
					return this.target != null ? this.target.ID : this.id;
				}
				set
				{
					this.id = value;
				}
			}

			private Entity target;

			[XmlIgnore]
			public Entity Target
			{
				get
				{
					if (string.IsNullOrEmpty(this.ID))
						this.target = null;
					else if (this.target == null || !this.target.Active || this.target.ID != this.ID)
					{
						if (!Entity.entities.TryGetValue(this.ID, out this.target))
							this.target = null;
					}
					return this.target;
				}
				set
				{
					this.target = value;
					this.ID = this.target == null ? null : this.target.ID;
				}
			}

			public static implicit operator Entity(Handle obj)
			{
				return obj.Target;
			}

			public static implicit operator Handle(Entity obj)
			{
				return new Handle { Target = obj, ID = obj == null ? null : obj.ID };
			}

			public override bool Equals(object obj)
			{
				if (obj is Handle)
					return ((Handle)obj).ID == this.ID;
				else
					return false;
			}

			public override int GetHashCode()
			{
				if (this.ID == null)
					return 0;
				byte[] hashValue = new MD5CryptoServiceProvider().ComputeHash(new UnicodeEncoding().GetBytes(this.ID));
				return (int)hashValue[0] | ((int)hashValue[1] >> 8) | ((int)hashValue[2] >> 16) | ((int)hashValue[3] >> 24);
			}
		}

		private static Dictionary<string, Entity> entities = new Dictionary<string, Entity>();

		public static string GenerateID(Entity entity, BaseMain main)
		{
			string baseId = char.ToLower(entity.Type[0]) + entity.Type.Substring(1);
			Factory factory = Factory.Get(entity.Type);
			for (int i = factory.SpawnIndex; ; i++)
			{
				string id = baseId + i.ToString();
				if (main.GetByID(id) == null)
				{
					factory.SpawnIndex = i;
					return id;
				}
			}
		}

		[XmlIgnore]
		public bool Active = true;
		
		[XmlAttribute]
		public string Type;

		[XmlIgnore]
		public string ID
		{
			get
			{
				return this.idProperty.Value;
			}
			set
			{
				this.idProperty.Value = value;
			}
		}

		public override string ToString()
		{
			return this.Type + " " + this.ID;
		}

		[XmlIgnore]
		public bool Serialize = true;

		[XmlIgnore]
		public bool CannotSuspend;

		[XmlIgnore]
		public bool CannotSuspendByDistance;
		
		private BaseMain main;
		private Dictionary<string, IComponent> components = new Dictionary<string, IComponent>();
		private Dictionary<Type, IComponent> componentsByType = new Dictionary<Type, IComponent>();
		private Dictionary<string, IProperty> properties = new Dictionary<string, IProperty>();
		private List<IBinding> bindings = new List<IBinding>();

		private Property<string> _idProperty;

		public static uint CurrentID;

		[XmlIgnore]
		public uint InternalID;

		private void createIdProperty()
		{
			this._idProperty = this.GetProperty<string>("ID");
			if (this._idProperty == null)
			{
				this._idProperty = new Property<string> { Editable = true, Value = Entity.GenerateID(this, this.main) };
				this.Add("ID", this._idProperty);
			}
			this._idProperty.Set = delegate(string value)
			{
				try
				{
					Entity.entities.Remove(this._idProperty.InternalValue);
				}
				catch (KeyNotFoundException)
				{

				}
				this._idProperty.InternalValue = value;
				Entity.entities.Add(value, this);
			};
		}

		private Property<string> idProperty
		{
			get
			{
				if (this._idProperty == null)
					this.createIdProperty();
				return this._idProperty;
			}
		}

		[XmlIgnore]
		private Dictionary<string, Command> commands = new Dictionary<string, Command>();

		public IEnumerable<IComponent> ComponentList
		{
			get
			{
				return this.components.Values;
			}
		}

		public IEnumerable<IProperty> PropertyList
		{
			get
			{
				return this.properties.Values;
			}
		}

		[XmlIgnore]
		public Command Delete = new Command();

		[XmlArray("Components")]
		[XmlArrayItem("Component", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] Components
		{
			get
			{
				// Make an array of DictionaryEntries to return
				IEnumerable<KeyValuePair<string, IComponent>> serializableComponents = this.components.Where(x => x.Value.Serialize);
				DictionaryEntry[] ret = new DictionaryEntry[serializableComponents.Count()];
				int i = 0;
				DictionaryEntry de;
				// Iterate through properties to load items into the array.
				foreach (KeyValuePair<string, IComponent> component in serializableComponents)
				{
					de = new DictionaryEntry();
					de.Key = component.Key;
					de.Value = component.Value;
					component.Value.OnSave();
					ret[i] = de;
					i++;
				}
				if (this.OnSave != null)
					this.OnSave.Execute();
				return ret;
			}
			set
			{
				this.components.Clear();
				for (int i = 0; i < value.Length; i++)
				{
					IComponent c = (IComponent)value[i].Value;
					this.components.Add((string)value[i].Key, c);
					Type t = c.GetType();
					do
					{
						this.componentsByType[t] = c;
						t = t.BaseType;
					}
					while (t.Assembly != Entity.componentBindAssembly);
				}
			}
		}

		[XmlIgnore]
		public Dictionary<string, IComponent> ComponentDictionary
		{
			get
			{
				return this.components;
			}
		}

		[XmlArray("Properties")]
		[XmlArrayItem("Property", Type = typeof(DictionaryEntry))]
		public DictionaryEntry[] Properties
		{
			get
			{
				return this.properties.Where(x => x.Value.Serialize).Select(x => new DictionaryEntry(x.Key, x.Value)).ToArray();
			}
			set
			{
				for (int i = 0; i < value.Length; i++)
					this.properties.Add((string)value[i].Key, (IProperty)value[i].Value);
			}
		}

		[XmlIgnore]
		public DictionaryEntry[] Commands
		{
			get
			{
				return this.commands.Select(x => new DictionaryEntry(x.Key, x.Value)).ToArray();
			}
		}

		[XmlIgnore]
		public Dictionary<string, IProperty> PropertyDictionary
		{
			get
			{
				return this.properties;
			}
		}

		public Entity()
		{
			// Called by XmlSerializer
			this.Delete.Action = (Action)this.delete;
			this.InternalID = Entity.CurrentID;
			Entity.CurrentID++;
		}

		[XmlIgnore]
		public Command OnSave;

		private static Assembly componentBindAssembly;

		static Entity()
		{
			Entity.componentBindAssembly = Assembly.GetExecutingAssembly();
		}

		public Entity(BaseMain _main, string _type)
			: this()
		{
			// Called by a Factory
			this.Serialize = true;
			this.Type = _type;
			this.Add("ID", new Property<string> { Editable = true, Value = Entity.GenerateID(this, _main) });
		}

		public void SetMain(BaseMain _main)
		{
			this.main = _main;
			if (_main.EditorEnabled)
				this.OnSave = new Command();

			if (this._idProperty == null)
				this.createIdProperty();
			foreach (IComponent c in this.components.Values.ToList())
			{
				c.Entity = this;
				this.main.AddComponent(c);
			}
		}

		public void SetSuspended(bool suspended)
		{
			foreach (IComponent c in this.components.Values.ToList())
			{
				if (c.Suspended.Value != suspended)
					c.Suspended.Value = suspended;
			}
		}

		public void Add(string name, Command cmd)
		{
			this.commands.Add(name, cmd);
		}

		public void Add(string name, IComponent component)
		{
			this.components.Add(name, component);
			Type t = component.GetType();
			do
			{
				this.componentsByType[t] = component;
				t = t.BaseType;
			}
			while (t.Assembly != Entity.componentBindAssembly);
			if (this.main != null)
			{
				component.Entity = this;
				this.main.AddComponent(component);
			}
		}

		public void AddWithoutOverwriting(string name, IComponent component)
		{
			this.components.Add(name, component);
			Type t = component.GetType();
			do
			{
				if (!this.componentsByType.ContainsKey(t))
					this.componentsByType[t] = component;
				t = t.BaseType;
			}
			while (t.Assembly != Entity.componentBindAssembly);
			if (this.main != null)
			{
				component.Entity = this;
				this.main.AddComponent(component);
			}
		}

		public void Add(IComponent component)
		{
			component.Serialize = false;
			this.Add(Guid.NewGuid().ToString(), component);
		}

		public void AddWithoutOverwriting(IComponent component)
		{
			this.AddWithoutOverwriting(Guid.NewGuid().ToString(), component);
		}

		public void Add(IBinding binding)
		{
			this.bindings.Add(binding);
		}

		public void Add(string name, IProperty property)
		{
			this.properties.Add(name, property);
		}

		public void RemoveProperty(string name)
		{
			this.properties.Remove(name);
		}

		public void RemoveComponent(string name)
		{
			IComponent c = null;
			this.components.TryGetValue(name, out c);
			if (c != null)
			{
				this.components.Remove(name);
				this.removeComponentTypeMapping(c);
			}
		}

		public void Remove(IBinding b)
		{
			b.Delete();
			this.bindings.Remove(b);
		}

		public void Remove(IComponent c)
		{
			foreach (KeyValuePair<string, IComponent> pair in this.components)
			{
				if (pair.Value == c)
				{
					this.components.Remove(pair.Key);
					break;
				}
			}

			this.removeComponentTypeMapping(c);
		}

		private void removeComponentTypeMapping(IComponent c)
		{
			Type type = c.GetType();
			do
			{
				IComponent typeComponent = null;
				this.componentsByType.TryGetValue(type, out typeComponent);
				if (typeComponent == c)
				{
					bool foundReplacement = false;
					foreach (IComponent c2 in this.components.Values)
					{
						if (c2.GetType().Equals(type))
						{
							this.componentsByType[type] = c2;
							foundReplacement = true;
							break;
						}
					}
					if (!foundReplacement)
						this.componentsByType.Remove(type);
				}
				type = type.BaseType;
			}
			while (type.Assembly != Entity.componentBindAssembly);
		}

		public Property<T> GetProperty<T>()
		{
			return this.properties.Values.OfType<Property<T>>().FirstOrDefault();
		}

		public Property<T> GetProperty<T>(string name)
		{
			IProperty result = null;
			this.properties.TryGetValue(name, out result);
			return (Property<T>)result;
		}

		public Property<T> GetOrMakeProperty<T>(string name, bool editable = false, T value = default(T))
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				result = new Property<T> { Value = value, Editable = editable };
				this.Add(name, result);
			}
			return (Property<T>)result;
		}

		public ListProperty<T> GetOrMakeListProperty<T>(string name)
		{
			IProperty result = null;
			if (!this.properties.TryGetValue(name, out result))
			{
				result = new ListProperty<T>();
				this.Add(name, result);
			}
			return (ListProperty<T>)result;
		}

		public ListProperty<T> GetListProperty<T>()
		{
			return this.properties.Values.OfType<ListProperty<T>>().FirstOrDefault();
		}

		public ListProperty<T> GetListProperty<T>(string name)
		{
			IProperty result = null;
			this.properties.TryGetValue(name, out result);
			return (ListProperty<T>)result;
		}

		public T Get<T>() where T : IComponent
		{
			IComponent result = null;
			this.componentsByType.TryGetValue(typeof(T), out result);
			return (T)result;
		}

		public T GetOrCreate<T>() where T : IComponent, new()
		{
			IComponent result = null;
			this.componentsByType.TryGetValue(typeof(T), out result);
			if (result == null)
			{
				result = new T();
				this.Add(result);
			}
			return (T)result;
		}

		public IEnumerable<T> GetAll<T>() where T : IComponent
		{
			return this.components.Values.OfType<T>();
		}

		public T Get<T>(string name) where T : IComponent
		{
			IComponent result = null;
			this.components.TryGetValue(name, out result);
			return (T)result;
		}

		public T GetOrCreate<T>(string name) where T : IComponent, new()
		{
			IComponent result = null;
			this.components.TryGetValue(name, out result);
			if (result == null)
			{
				result = new T();
				this.Add(name, result);
			}
			return (T)result;
		}

		public Command GetCommand(string name)
		{
			Command result = null;
			this.commands.TryGetValue(name, out result);
			return result;
		}

		public Command<T> GetCommand<T>(string name)
		{
			Command result = null;
			this.commands.TryGetValue(name, out result);
			return (Command<T>)result;
		}

		public Command<T, T2> GetCommand<T, T2>(string name)
		{
			Command result = null;
			this.commands.TryGetValue(name, out result);
			return (Command<T, T2>)result;
		}

		public Command<T, T2, T3> GetCommand<T, T2, T3>(string name)
		{
			Command result = null;
			this.commands.TryGetValue(name, out result);
			return (Command<T, T2, T3>)result;
		}

		protected void delete()
		{
			if (this.Active)
			{
				this.Active = false;
				IEnumerable<IComponent> components = this.components.Values.ToList();
				this.components.Clear();
				this.componentsByType.Clear();
				foreach (IComponent c in components)
					c.Delete.Execute();
				foreach (IBinding b in this.bindings)
					b.Delete();
				this.bindings.Clear();
				this.commands.Clear();
				this.main.Remove(this);
				Entity.entities.Remove(this.ID);
			}
		}
	}
}
