using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

namespace Lemma.Components
{
	public interface IProperty
	{
		bool Editable { get; set; }
		bool Serialize { get; set; }
		void AddBinding(IPropertyBinding binding);
		void RemoveBinding(IPropertyBinding binding);
		void Reset();
	}

	[DebuggerDisplay("Property {Value}")]
	public class Property<Type> : IProperty
	{
		[XmlIgnore]
		public Type InternalValue;

		[XmlIgnore]
		public bool IsInitializing;

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
				this.IsInitializing = true;
				this.set = value;
				if (this.InternalValue != null && !this.InternalValue.Equals(default(Type)))
					this.set(this.InternalValue);
				this.IsInitializing = false;
			}
		}
		protected List<IPropertyBinding> bindings = new List<IPropertyBinding>();

		protected bool editable = true;
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

		protected bool serialize = true;
		[XmlIgnore]
		[DefaultValue(true)]
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
			foreach (IPropertyBinding b in this.bindings)
				b.OnChanged(this);
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
			try
			{
				foreach (IPropertyBinding b in this.bindings)
				{
					if (b != binding)
						b.OnChanged(this);
				}
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
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
			try
			{
				foreach (IPropertyBinding b in this.bindings)
					b.OnChanged(this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.Add(t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.Add(t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.Remove(t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
		}

		public bool Remove(Type t)
		{
			int index = this.InternalList.IndexOf(t);
			this.InternalList.RemoveAt(index);
			this.Size.Value = this.InternalList.Count;
			if (this.ItemRemoved != null)
				this.ItemRemoved(index, t);
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.Remove(t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
			return true;
		}

		public bool RemoveWithoutNotifying(Type t)
		{
			int index = this.InternalList.IndexOf(t);
			this.InternalList.RemoveAt(index);
			this.Size.Value = this.InternalList.Count;
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.Remove(t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.OnChanged(from, to, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
		}

		public void Changed(int i, Type to)
		{
			Type from = this.InternalList[i];
			this.InternalList[i] = to;
			if (this.ItemChanged != null)
				this.ItemChanged(i, from, to);
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.OnChanged(from, to, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
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
				try
				{
					foreach (IListBinding<Type> b in this.bindings)
						b.Clear(this);
				}
				catch (InvalidOperationException)
				{
					// Bindings were modified while we were enumerating
				}
			}
		}

		public void Changed(Type t)
		{
			if (this.ItemChanged != null)
			{
				int i = this.InternalList.IndexOf(t);
				this.ItemChanged(i, t, t);
			}
			try
			{
				foreach (IListBinding<Type> b in this.bindings)
					b.OnChanged(t, t, this);
			}
			catch (InvalidOperationException)
			{
				// Bindings were modified while we were enumerating
			}
		}
	}
}
