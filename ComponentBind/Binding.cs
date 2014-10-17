using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComponentBind
{
	public interface IBinding
	{
		void Delete();
	}

	public interface IPropertyBinding : IBinding
	{
		void OnChanged(IProperty changed);
	}

	public class Binding<Type, Type2> : IPropertyBinding
	{
		protected Property<Type> destination;
		protected Func<Type> get;
		protected IProperty[] sources;

		protected Binding()
		{

		}

		public Binding(Property<Type> _destination, Func<Type2, Type> transform, Property<Type2> _source)
		{
			this.destination = _destination;
			_source.AddBinding(this);
			this.get = () => transform(_source.Value);
			this.sources = new IProperty[] { _source };
			this.OnChanged(_source);
		}

		public virtual void OnChanged(IProperty changed)
		{
			this.destination.InternalSet(this.get(), this);
		}

		public virtual void Delete()
		{
			foreach (IProperty property in this.sources)
				property.RemoveBinding(this);
			this.sources = null;
			this.get = null;
			this.destination = null;
		}
	}

	public class SetBinding<Type> : IPropertyBinding
	{
		protected Action<Type> notify;
		protected Property<Type> source;

		protected SetBinding()
		{

		}

		public SetBinding(Property<Type> source, Action<Type> _notify)
		{
			this.notify = _notify;
			this.source = source;
			source.AddBinding(this);
			Type v = this.source.Value;
			if (v != null)
				this.notify(v);
		}

		public void OnChanged(IProperty changed)
		{
			this.notify(this.source.Value);
		}

		public void Delete()
		{
			this.source.RemoveBinding(this);
			this.notify = null;
			this.source = null;
		}
	}

	public class ChangeBinding<Type> : IPropertyBinding
	{
		protected Action<Type, Type> notify;
		protected Property<Type> source;
		protected Type value;

		protected ChangeBinding()
		{

		}

		public ChangeBinding(Property<Type> source, Action<Type, Type> _notify)
		{
			this.notify = _notify;
			this.source = source;
			this.value = source.Value;
			source.AddBinding(this);
			if (this.value != null && !this.value.Equals(default(Type)))
				this.notify(default(Type), this.value);
		}

		public void OnChanged(IProperty changed)
		{
			Type old = this.value;
			this.value = this.source.Value;
			this.notify(old, this.value);
		}

		public void Delete()
		{
			this.source.RemoveBinding(this);
			this.notify = null;
			this.source = null;
		}
	}

	public class NotifyBinding : IPropertyBinding
	{
		protected Action notify;
		protected Func<bool> enabled;
		protected IProperty[] sources;

		private bool _enabled = true;
		public bool Enabled
		{
			get
			{
				return this._enabled;
			}
			set
			{
				bool oldValue = this._enabled;
				this._enabled = value;
				if (value && !oldValue)
					this.OnChanged(null);
			}
		}

		protected NotifyBinding()
		{

		}

		public NotifyBinding(Action _notify, params IProperty[] _sources)
			: this(_notify, () => true, _sources)
		{
		}

		public NotifyBinding(Action _notify, Func<bool> enabled, params IProperty[] _sources)
		{
			this.notify = _notify;
			this.enabled = enabled;
			this.sources = _sources;
			foreach (IProperty property in this.sources)
				property.AddBinding(this);
		}

		public void OnChanged(IProperty changed)
		{
			if (this.Enabled && this.enabled())
				this.notify();
		}

		public void Delete()
		{
			foreach (IProperty property in this.sources)
				property.RemoveBinding(this);
			this.notify = null;
			this.sources = null;
			this.enabled = null;
		}
	}

	public class Binding<Type> : Binding<Type, Type>
	{
		protected Binding()
		{
		}

		public Binding(Property<Type> _destination, Property<Type> _source)
			: base(_destination, x => x, _source)
		{

		}

		public Binding(Property<Type> _destination, Func<Type, Type> transform, Property<Type> _source)
			: base(_destination, transform, _source)
		{

		}

		public Binding(Property<Type> _destination, Func<Type> _get, params IProperty[] _sources)
		{
			this.destination = _destination;
			this.get = _get;
			this.sources = _sources;
			foreach (IProperty property in this.sources)
				property.AddBinding(this);
			this.OnChanged(_sources.FirstOrDefault());
		}
	}

	/// <summary>
	/// Important: When initializing, the first given property takes precedence.
	/// </summary>
	/// <typeparam name="Type"></typeparam>
	/// <typeparam name="Type2"></typeparam>
	public class TwoWayBinding<Type, Type2> : Binding<Type, Type2>
	{
		protected Property<Type> property1;
		protected IProperty[] property1Sources;
		protected Property<Type2> property2;
		protected IProperty[] property2Sources;
		protected Func<Type2, Type> transform1;
		protected Func<Type, Type2> transform2;

		protected bool reevaluating = false;

		protected TwoWayBinding()
		{

		}

		public TwoWayBinding(
			Property<Type> _property1,
			Func<Type2, Type> _transform1,
			Property<Type2> _property2,
			Func<Type, Type2> _transform2)
			: this(_property1, _transform1, new IProperty[] { }, _property2, _transform2, new IProperty[] { })
		{
		}

		public TwoWayBinding(
			Property<Type> _property1,
			Func<Type2, Type> _transform1,
			IEnumerable<IProperty> _property1Sources,
			Property<Type2> _property2,
			Func<Type, Type2> _transform2,
			IEnumerable<IProperty> _property2Sources)
		{
			this.property1 = _property1;
			this.property2 = _property2;
			this.property1Sources = _property1Sources.Union(new IProperty[] { this.property2 }).ToArray();
			this.property2Sources = _property2Sources.Union(new IProperty[] { this.property1 }).ToArray();
			this.transform1 = _transform1;
			this.transform2 = _transform2;
			foreach (IProperty property in this.property1Sources)
				property.AddBinding(this);
			foreach (IProperty property in this.property2Sources)
				property.AddBinding(this);
			this.OnChanged(this.property1);
		}

		public void Reevaluate(IProperty destination)
		{
			if (!this.reevaluating)
			{
				this.reevaluating = true;
				if (destination == this.property1)
					this.property1.InternalSet(this.transform1(this.property2.Value), this);
				else if (destination == this.property2)
					this.property2.InternalSet(this.transform2(this.property1.Value), this);
				else
					throw new ArgumentException("Binding received improper property change notification.");
				this.reevaluating = false;
			}
		}

		public override void OnChanged(IProperty changed)
		{
			if (this.property2Sources.Contains(changed))
				this.property2.InternalSet(this.transform2(this.property1.Value), this);
			else if (this.property1Sources.Contains(changed))
				this.property1.InternalSet(this.transform1(this.property2.Value), this);
			else
				throw new ArgumentException("Binding received improper property change notification.");
		}

		public override void Delete()
		{
			foreach (IProperty property in this.property1Sources)
				property.RemoveBinding(this);
			foreach (IProperty property in this.property2Sources)
				property.RemoveBinding(this);
			this.property1 = null;
			this.property1Sources = null;
			this.property2 = null;
			this.property2Sources = null;
			this.transform1 = null;
			this.transform2 = null;
		}
	}

	/// <summary>
	/// Important: When initializing, the first given property takes precedence.
	/// </summary>
	/// <typeparam name="Type"></typeparam>
	public class TwoWayBinding<Type> : TwoWayBinding<Type, Type>
	{
		protected TwoWayBinding()
		{
		}

		public TwoWayBinding(Property<Type> _property1, Property<Type> _property2)
			: base(_property1, x => x, _property2, x => x)
		{
		}
	}

	public interface IListBinding<Type> : IPropertyBinding
	{
		void Add(Type x, IProperty property);
		void Remove(Type x, IProperty property);
		void OnChanged(Type x, Type y, IProperty property);
		void Clear(IProperty property);
	}

	public class ListBinding<Type, Type2> : IListBinding<Type2>
	{
		protected class Entry
		{
			public int Index;
		}
		protected ListProperty<Type> destination;
		protected Func<Type2, Type> transform;
		protected Func<Type2, bool> filter;
		protected Dictionary<Type2, Entry> mapping = new Dictionary<Type2, Entry>();
		protected IProperty[] sources;

		public bool Enabled { get; set; }

		protected ListBinding()
		{
			this.Enabled = true;
		}

		public ListBinding(ListProperty<Type> _destination, ListProperty<Type2> _source, Func<Type2, Type> _transform, Func<Type2, bool> _filter)
		{
			this.Enabled = true;
			this.destination = _destination;
			this.transform = _transform;
			this.filter = _filter;
			_source.AddBinding(this);
			this.sources = new IProperty[] { _source };
			this.OnChanged(_source);
		}

		public ListBinding(ListProperty<Type> _destination, ListProperty<Type2> _source, Func<Type2, Type> _transform)
			: this(_destination, _source, _transform, (x) => true)
		{
		}

		public void OnChanged(IProperty property)
		{
			if (this.Enabled)
			{
				this.Clear(property);
				foreach (Type2 x in (ListProperty<Type2>)this.sources[0])
					this.Add(x, property);
			}
		}

		public void Add(Type2 x, IProperty property)
		{
			if (this.Enabled && this.filter((Type2)x))
			{
				Type y = this.transform(x);
				this.destination.Add(y);
				this.mapping.Add((Type2)x, new Entry { Index = this.destination.Length - 1 });
			}
		}

		public void Remove(Type2 x, IProperty property)
		{
			if (this.Enabled && this.filter((Type2)x))
			{
				int e = this.mapping[(Type2)x].Index;
				this.mapping.Remove((Type2)x);
				this.destination.RemoveAt(e);
				this.recalculate(e + 1, e);
			}
		}

		private void recalculate(int oldIndex, int newIndex)
		{
			if (newIndex != oldIndex)
			{
				int diff = newIndex - oldIndex;
				foreach (Entry entry in this.mapping.Values)
				{
					if (entry.Index >= oldIndex)
						entry.Index += diff;
				}
			}
		}

		public void OnChanged(Type2 from, Type2 to, IProperty property)
		{
			if (this.Enabled)
			{
				bool originallyIncluded = this.filter((Type2)from), nowIncluded = this.filter((Type2)to);
				if (originallyIncluded && nowIncluded)
				{
					Entry oldEntry = this.mapping[(Type2)from];
					Type newValue = this.transform((Type2)to);
					Entry newEntry = new Entry { Index = oldEntry.Index };

					this.destination.Changed(oldEntry.Index, newValue);

					this.mapping[(Type2)from] = newEntry;
				}
				else if (!originallyIncluded && nowIncluded)
				{
					Type newValue = this.transform((Type2)to);
					this.destination.Add(newValue);
					this.mapping.Add((Type2)to, new Entry { Index = this.destination.Length - 1 });
				}
				else if (originallyIncluded && !nowIncluded)
				{
					Entry entry = this.mapping[(Type2)from];
					this.destination.RemoveAt(entry.Index);
					this.mapping.Remove((Type2)from);
					this.recalculate(entry.Index + 1, entry.Index);
				}
			}
		}

		public void Clear(IProperty property)
		{
			this.mapping.Clear();
			if (this.Enabled)
				this.destination.Clear();
		}

		public void Delete()
		{
			foreach (IProperty source in this.sources)
				source.RemoveBinding(this);
			this.sources = null;
			this.destination = null;
			this.transform = null;
			this.filter = null;
			this.mapping.Clear();
		}
	}

	public class ListNotifyBinding<Type> : IListBinding<Type>
	{
		protected IProperty[] sources;
		private Action notify;

		public bool Enabled { get; set; }

		protected ListNotifyBinding()
		{
			this.Enabled = true;
		}

		public ListNotifyBinding(Action notify, ListProperty<Type> _source)
		{
			this.Enabled = true;
			this.notify = notify;
			_source.AddBinding(this);
			this.sources = new IProperty[] { _source };
		}

		public void OnChanged(IProperty property)
		{
			if (this.Enabled)
				this.notify();
		}

		public void Add(Type x, IProperty property)
		{
			if (this.Enabled)
				this.notify();
		}

		public void Remove(Type x, IProperty property)
		{
			if (this.Enabled)
				this.notify();
		}

		public void OnChanged(Type from, Type to, IProperty property)
		{
			if (this.Enabled)
				this.notify();
		}

		public void Clear(IProperty property)
		{
			if (this.Enabled)
				this.notify();
		}

		public void Delete()
		{
			foreach (IProperty source in this.sources)
				source.RemoveBinding(this);
			this.sources = null;
		}
	}

	public class ListBinding<Type> : ListBinding<Type, Type>
	{
		public ListBinding(ListProperty<Type> _destination, ListProperty<Type> _source)
			: base(_destination, _source, x => x, x => true)
		{
		}
	}
}
