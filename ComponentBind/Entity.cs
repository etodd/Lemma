using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Collections;
using System.ComponentModel;
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
		//This is a class because PASS-BY-REFERENCE
		public class CommandLink
		{
			public Handle TargetEntity;

			[XmlAttribute]
			[DefaultValue("")]
			public string TargetCommand;

			[XmlAttribute]
			[DefaultValue("")]
			public string SourceCommand;

			[XmlIgnore]
			public Command LinkedTargetCmd;

			[XmlIgnore]
			public Command LinkedSourceCmd;
		}

		public struct Handle
		{
			private ulong guid;

			[XmlAttribute]
			[DefaultValue(0)]
			public ulong GUID;

			private Entity target;

			[XmlIgnore]
			public Entity Target
			{
				get
				{
					if (this.target == null || this.target.GUID != this.GUID)
						Entity.guidTable.TryGetValue(this.GUID, out this.target);
					return this.target;
				}
				set
				{
					this.target = value;
					this.GUID = this.target == null ? 0 : this.target.GUID;
				}
			}

			public static implicit operator Entity(Handle obj)
			{
				return obj.Target;
			}

			public static implicit operator Handle(Entity obj)
			{
				return new Handle { Target = obj, GUID = obj == null ? 0 : obj.GUID };
			}

			public override bool Equals(object obj)
			{
				if (obj is Handle)
					return ((Handle)obj).GUID == this.GUID;
				else if (obj is Entity)
					return ((Entity)obj).GUID == this.GUID;
				else
					return false;
			}

			public override int GetHashCode()
			{
				return (int)(this.GUID & 0xffffffff);
			}
		}

		private static Dictionary<ulong, Entity> guidTable = new Dictionary<ulong, Entity>();
		private static Dictionary<string, Entity> idTable = new Dictionary<string, Entity>();

		public static Entity GetByID(string id)
		{
			Entity result;
			Entity.idTable.TryGetValue(id, out result);
			return result;
		}

		public static Entity GetByGUID(ulong id)
		{
			Entity result;
			Entity.guidTable.TryGetValue(id, out result);
			return result;
		}

		[XmlIgnore]
		public bool Active = true;

		[XmlAttribute]
		public string Type;

		[XmlIgnore]
		public bool EditorCanDelete = true;

		public Property<string> ID = new Property<string>();

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(this.ID))
				return this.ID.Value;
			else
				return string.Format("{0} {1}", this.Type, this.GUID);
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
		private List<IBinding> bindings = new List<IBinding>();

		public static ulong CurrentGUID = 1;

		[XmlAttribute]
		public ulong GUID;

		private Dictionary<string, Command.Entry> commands = new Dictionary<string, Command.Entry>();

		private readonly Dictionary<string, PropertyEntry> properties = new Dictionary<string, PropertyEntry>();

		public IEnumerable<IComponent> ComponentList
		{
			get
			{
				return this.components.Values;
			}
		}

		[XmlIgnore]
		public Command Delete = new Command();

		[XmlArray("LinkedCommands")]
		[XmlArrayItem("CommandLink", typeof(CommandLink))]
		public ListProperty<CommandLink> LinkedCommands = new ListProperty<CommandLink>();

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

		[XmlIgnore]
		public IEnumerable<KeyValuePair<string, Command.Entry>> Commands
		{
			get
			{
				return this.commands;
			}
		}

		[XmlIgnore]
		public IEnumerable<KeyValuePair<string, PropertyEntry>> Properties
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
		}

		[XmlIgnore]
		public Command OnSave;

		[XmlIgnore]
		public Property<bool> EditorSelected;

		private static Assembly componentBindAssembly;

		static Entity()
		{
			Entity.componentBindAssembly = Assembly.GetExecutingAssembly();
		}

		public Entity(BaseMain _main, string _type)
			: this()
		{
			// Called by a Factory
			this.Type = _type;
		}

		public void ClearGUID()
		{
			if (this.GUID != 0)
				Entity.guidTable.Remove(this.GUID);
		}

		public void NewGUID()
		{
			this.ClearGUID();
			this.GUID = Entity.CurrentGUID;
			Entity.CurrentGUID = Math.Max(Entity.CurrentGUID, this.GUID + 1);
			Entity.guidTable.Add(this.GUID, this);
		}

		public void SetMain(BaseMain _main)
		{
			if (this.GUID == 0)
				this.GUID = Entity.CurrentGUID;

			Entity.CurrentGUID = Math.Max(Entity.CurrentGUID, this.GUID + 1);
			Entity.guidTable.Add(this.GUID, this);

			this.main = _main;

			if (!string.IsNullOrEmpty(this.ID))
				Entity.idTable.Add(this.ID, this);

			if (_main.EditorEnabled)
			{
				this.OnSave = new Command();
				this.EditorSelected = new Property<bool>();
				string oldId = this.ID;
				this.Add(new NotifyBinding(delegate()
				{
					if (!string.IsNullOrEmpty(oldId))
						Entity.idTable.Remove(oldId);
					if (!string.IsNullOrEmpty(this.ID))
						Entity.idTable.Add(this.ID, this);
					oldId = this.ID;
				}, this.ID));
			}

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

		public void LinkedCommandCall(CommandLink link)
		{
			if (link.LinkedTargetCmd != null)
				link.LinkedTargetCmd.Execute();
			else if (link.TargetEntity.Target != null)
			{
				Command destCommand = link.TargetEntity.Target.getCommand(link.TargetCommand);
				if (destCommand != null)
				{
					link.LinkedTargetCmd = destCommand;
					destCommand.Execute();
				}
			}
		}

		public void Add(string name, Command cmd, Command.Perms perms = Command.Perms.Linkable, string description = null)
		{
			Command.Entry entry = new Command.Entry { Command = cmd, Permissions = perms, Key = name };
			if (this.main.EditorEnabled)
				entry.Description = description;
			this.commands.Add(name, entry);
			foreach (var link in LinkedCommands)
			{
				CommandLink link1 = link;
				if (link.LinkedSourceCmd == null && name == link.SourceCommand)
				{
					link.LinkedSourceCmd = cmd;
					this.Add(new CommandBinding(link.LinkedSourceCmd, () => LinkedCommandCall(link1)));
				}
			}
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

		public void Add(string name, IProperty prop, string description = null, Property<bool> visible = null, IListProperty options = null)
		{
			PropertyEntry entry = new PropertyEntry(prop, description);
			if (this.main.EditorEnabled)
			{
				entry.Visible = visible;
				entry.Options = options;
			}
			this.properties.Add(name, entry);
		}

		public Property<T> GetProperty<T>(string name)
		{
			PropertyEntry result;
			this.properties.TryGetValue(name, out result);
			if (result == null)
				return null;
			else
				return (Property<T>)result.Property;
		}

		public void AddWithoutOverwriting(IComponent component)
		{
			this.AddWithoutOverwriting(Guid.NewGuid().ToString(), component);
		}

		public void Add(IBinding binding)
		{
			this.bindings.Add(binding);
		}

		public void RemoveComponent(string name)
		{
			IComponent c;
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

		public T GetOrCreateWithoutOverwriting<T>(string name) where T : IComponent, new()
		{
			IComponent result = null;
			this.components.TryGetValue(name, out result);
			if (result == null)
			{
				result = new T();
				this.AddWithoutOverwriting(name, result);
			}
			return (T)result;
		}

		public T Create<T>(string name = null) where T : IComponent, new()
		{
			T result = new T();
			if (name == null)
				this.Add(result);
			else
				this.Add(name, result);
			return (T)result;
		}

		public T GetOrCreate<T>(string name, out bool created) where T : IComponent, new()
		{
			IComponent result = null;
			this.components.TryGetValue(name, out result);
			created = false;
			if (result == null)
			{
				created = true;
				result = new T();
				this.Add(name, result);
			}
			return (T)result;
		}

		private Command getCommand(string name)
		{
			Command.Entry result;
			this.commands.TryGetValue(name, out result);
			if (result != null)
				return result.Command;
			else
				return null;
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
				this.ClearGUID();
				if (!string.IsNullOrEmpty(this.ID))
					Entity.idTable.Remove(this.ID);
			}
		}
	}
}
