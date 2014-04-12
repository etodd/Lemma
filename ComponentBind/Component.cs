using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.ComponentModel;

namespace ComponentBind
{
	public interface IComponent
	{
		Entity Entity { get; set; }
		bool NeedsAdded { get; }
		bool Active { get; }
		bool Serialize { get; set; }
		bool Editable { get; set; }
		void SetMain(BaseMain main);
		Property<bool> Enabled { get; }
		Property<bool> EnabledInEditMode { get; }
		Property<bool> EnabledWhenPaused { get; }
		Property<bool> Suspended { get; }
		Command Delete { get; }
		void LoadContent(bool reload);
		void OnSave();
	}

	public interface IUpdateableComponent : IComponent
	{
		void Update(float dt);
	}

	public class Component<MainClass> : IComponent
		where MainClass : BaseMain
	{
		[XmlIgnore]
		public bool Serialize { get; set; }

		[XmlAttribute]
		[DefaultValue(true)]
		public bool Editable { get; set; }

		public Property<bool> Enabled { get; set; }

		[XmlIgnore]
		public Property<bool> Suspended { get; set; }

		[XmlIgnore]
		public bool NeedsAdded
		{
			get
			{
				return this.main == null;
			}
		}

		[XmlIgnore]
		public Command OnEnabled = new Command();

		[XmlIgnore]
		public Command OnDisabled = new Command();

		[XmlIgnore]
		public Command OnSuspended = new Command();

		[XmlIgnore]
		public Command OnResumed = new Command();

		private Command del = new Command();
		[XmlIgnore]
		public Command Delete
		{
			get
			{
				return this.del;
			}
		}

		[XmlIgnore]
		public Property<bool> EnabledInEditMode { get; set; }
		[XmlIgnore]
		public Property<bool> EnabledWhenPaused { get; set; }

		private List<IBinding> bindings = new List<IBinding>();

		protected MainClass main;

		[XmlIgnore]
		public virtual Entity Entity { get; set; }

		[XmlIgnore]
		public bool Active { get; private set; }

		public Component()
		{
			this.Serialize = true;
			this.Active = true;
			this.Editable = true;
			this.Enabled = new Property<bool> { Value = true, Editable = false };
			this.Suspended = new Property<bool> { Value = false, Editable = false };
			this.EnabledInEditMode = new Property<bool> { Value = true, Editable = false };
			this.EnabledWhenPaused = new Property<bool> { Value = true, Editable = false };
			this.Delete.Action = (Action)this.delete;
		}

		public virtual void OnSave()
		{

		}

		public virtual void InitializeProperties()
		{

		}

		public virtual void LoadContent(bool reload)
		{

		}

		public void Add(IBinding binding)
		{
			this.bindings.Add(binding);
		}

		public void Remove(IBinding binding)
		{
			binding.Delete();
			this.bindings.Remove(binding);
		}

		public void RemoveAllBindings()
		{
			foreach (IBinding binding in this.bindings)
				binding.Delete();
			this.bindings.Clear();
		}

		public virtual void SetMain(BaseMain _main)
		{
			this.main = (MainClass)_main;
			this.LoadContent(false);
			this.InitializeProperties();
			this.Enabled.Set = delegate(bool value)
			{
				bool oldValue = this.Enabled.InternalValue;
				this.Enabled.InternalValue = value;
				if (!oldValue && value)
					this.OnEnabled.Execute();
				else if (oldValue && !value)
					this.OnDisabled.Execute();
			};
			this.Suspended.Set = delegate(bool value)
			{
				bool oldValue = this.Suspended.InternalValue;
				this.Suspended.InternalValue = value;
				if (!oldValue && value)
					this.OnSuspended.Execute();
				else if (oldValue && !value)
					this.OnResumed.Execute();
			};
		}

		protected virtual void delete()
		{
			if (this.Active)
			{
				this.Active = false;
				if (this.Entity != null)
					this.Entity.Remove(this);
				foreach (IBinding binding in this.bindings)
					binding.Delete();
				this.bindings.Clear();
				this.main.RemoveComponent(this);
			}
		}
	}
}
