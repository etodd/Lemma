using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

namespace ComponentBind
{
	public interface IProperty
	{
		void AddBinding(IPropertyBinding binding);
		void RemoveBinding(IPropertyBinding binding);
		void Reset();
	}

	public class PropertyEntry
	{
		public IProperty Property;
		public Property<bool> Visible;
		public string Description;
		public IListProperty Options;
		public bool Readonly;
		public EditorData Data = new EditorData();

		public PropertyEntry(IProperty property, string description = null)
		{
			this.Property = property;
			this.Description = description;
		}

		public class EditorData
		{
			public int IChangeBy = 1;
			public float FChangeBy = 1f;
			public byte BChangeBy = 1;
		}
	}

	[DebuggerDisplay("Property {Value}")]
	public class Property<Type> : IProperty
	{

		[XmlIgnore]
		public Type InternalValue;

		[XmlIgnore]
		public Func<Type> Get;

		protected Action<Type> set;
		[XmlIgnore]
		public Action<Type> Set
		{
			get
			{
				return this.set;
			}
			set
			{
				this.set = value;
				if (this.InternalValue != null && !this.InternalValue.Equals(default(Type)))
					this.set(this.InternalValue);
			}
		}
		protected List<IPropertyBinding> bindings = new List<IPropertyBinding>();

		public void AddBinding(IPropertyBinding binding)
		{
			if (!this.bindings.Contains(binding))
				this.bindings.Add(binding);
		}

		public void RemoveBinding(IPropertyBinding binding)
		{
			this.bindings.Remove(binding);
		}

		public void Changed()
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(this);
		}

		public Type Value
		{
			get
			{
				return this.InternalGet(null);
			}
			set
			{
				this.InternalSet(value, null);
			}
		}

		public void Reset()
		{
			this.InternalSet(this.InternalGet(null), null);
		}

		public void InternalSet(Type obj, IPropertyBinding binding)
		{
			if (this.Set != null)
				this.Set(obj);
			else
				this.InternalValue = obj;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
			{
				IPropertyBinding b = this.bindings[j];
				if (b != binding)
					b.OnChanged(this);
			}
		}

		public Type InternalGet(IPropertyBinding binding)
		{
			return this.Get != null ? this.Get() : this.InternalValue;
		}

		public static implicit operator Type(Property<Type> obj)
		{
			return obj.Value;
		}

		public override string ToString()
		{
			return this.Value.ToString();
		}
	}

	public interface IListProperty : IProperty
	{
		void CopyTo(IListProperty dest);
	}

	public class ListProperty<Type> : ICollection<Type>, IListProperty
	{
		public delegate void ItemAddedEventHandler(int index, Type t);
		public delegate void ItemRemovedEventHandler(int index, Type t);
		public delegate void ItemChangedEventHandler(int index, Type old, Type newValue);
		public delegate void ClearEventHandler();
		protected bool editable = false;
		[XmlAttribute]
		[DefaultValue(true)]
		public bool Editable
		{
			get
			{
				return this.editable;
			}
			set
			{
				this.editable = value;
			}
		}

		protected string description = "";
		[XmlIgnore]
		[DefaultValue("")]
		public string Description
		{
			get { return description; }
			set { description = value; }
		}

		protected bool serialize = true;
		[XmlIgnore]
		public bool Serialize
		{
			get
			{
				return this.serialize;
			}
			set
			{
				this.serialize = value;
			}
		}

		public int Count
		{
			get
			{
				return this.InternalList.Count;
			}
		}

		public Property<int> Size = new Property<int>();

		public event ItemAddedEventHandler ItemAdded;
		public event ItemRemovedEventHandler ItemRemoved;
		public event ItemChangedEventHandler ItemChanged;
		public event ClearEventHandler Cleared;
		public event ClearEventHandler Clearing;

		public void Reset()
		{

		}

		public List<Type> InternalList = new List<Type>();
		protected List<IListBinding<Type>> bindings = new List<IListBinding<Type>>();

		public void AddBinding(IPropertyBinding binding)
		{
			if (!this.bindings.Contains(binding))
				this.bindings.Add((IListBinding<Type>)binding);
		}

		public void RemoveBinding(IPropertyBinding binding)
		{
			this.bindings.Remove((IListBinding<Type>)binding);
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool Contains(Type t)
		{
			return this.InternalList.Contains(t);
		}

		public void CopyTo(Type[] array, int arrayIndex)
		{
			this.InternalList.CopyTo(array, arrayIndex);
		}

		IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
		{
			return this.InternalList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.InternalList.GetEnumerator();
		}

		public Type this[int i]
		{
			get
			{
				return this.InternalList[i];
			}
			set
			{
				this.Changed(i, value);
			}
		}

		public void Changed()
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(this);
		}

		public void CopyTo(IListProperty dest)
		{
			ListProperty<Type> list = (ListProperty<Type>)dest;
			list.Clear();
			foreach (Type t in this)
				list.Add(t);
		}

		public void Add(Type t)
		{
			this.InternalList.Add(t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Add(t, this);

			this.Size.Value = this.InternalList.Count;
			if (this.ItemAdded != null)
				this.ItemAdded(this.InternalList.Count - 1, t);
		}

		public void AddAll(IEnumerable<Type> items)
		{
			foreach (Type t in items)
				this.Add(t);
		}

		public void Insert(int index, Type t)
		{
			this.InternalList.Insert(index, t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Add(t, this);

			this.Size.Value = this.InternalList.Count;
			if (this.ItemAdded != null)
				this.ItemAdded(index, t);
		}

		public void RemoveAt(int index)
		{
			Type t = this.InternalList[index];
			this.InternalList.RemoveAt(index);
			this.Size.Value = this.InternalList.Count;
			if (this.ItemRemoved != null)
				this.ItemRemoved(index, t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);
		}

		public bool Remove(Type t)
		{
			int index = this.InternalList.IndexOf(t);
			this.InternalList.RemoveAt(index);
			this.Size.Value = this.InternalList.Count;
			if (this.ItemRemoved != null)
				this.ItemRemoved(index, t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);

			return true;
		}

		public bool RemoveWithoutNotifying(Type t)
		{
			int index = this.InternalList.IndexOf(t);
			this.InternalList.RemoveAt(index);
			this.Size.Value = this.InternalList.Count;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);

			return true;
		}

		public void Remove(IEnumerable<Type> items)
		{
			foreach (Type t in items)
				this.Remove(t);
		}

		public void Changed(Type from, Type to)
		{
			int i = this.InternalList.IndexOf(from);
			this.InternalList[i] = to;
			if (this.ItemChanged != null)
				this.ItemChanged(i, from, to);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(from, to, this);
		}

		public void Changed(int i, Type to)
		{
			Type from = this.InternalList[i];
			this.InternalList[i] = to;
			if (this.ItemChanged != null)
				this.ItemChanged(i, from, to);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(from, to, this);
		}

		public void Clear()
		{
			bool notify = this.InternalList.Count > 0;

			if (notify && this.Clearing != null)
				this.Clearing();

			this.InternalList.Clear();
			this.Size.Value = 0;

			if (notify)
			{
				if (this.Cleared != null)
					this.Cleared();

				for (int i = this.bindings.Count - 1; i >= 0; i = Math.Min(this.bindings.Count - 1, i - 1))
					this.bindings[i].Clear(this);
			}
		}

		public void Changed(Type t)
		{
			if (this.ItemChanged != null)
			{
				int i = this.InternalList.IndexOf(t);
				this.ItemChanged(i, t, t);
			}

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(t, t, this);
		}
	}
}
