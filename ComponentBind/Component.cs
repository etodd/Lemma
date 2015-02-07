using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.ComponentModel;

namespace ComponentBind
{
	public interface IBindable
	{
		void delete();
	}

	public interface IComponent : IBindable
	{
		Entity Entity { get; set; }
		bool Active { get; }
		bool Serialize { get; set; }
		void SetMain(BaseMain main);
		Property<bool> Enabled { get; }
		bool EnabledInEditMode { get; }
		bool EnabledWhenPaused { get; }
		Property<bool> Suspended { get; }
		Command Delete { get; }
		void OnSave();
		void Start();
		void Awake();
	}

	public interface IGraphicsComponent : IComponent
	{
		void LoadContent(bool reload);
	}

	public interface IUpdateableComponent : IComponent
	{
		void Update(float dt);
	}

	public interface IEarlyUpdateableComponent : IComponent
	{
		void Update(float dt);
		Property<int> UpdateOrder { get; }
	}

	public class Bindable : IBindable
	{
		private List<IBinding> bindings = new List<IBinding>();

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
			for (int i = 0; i < this.bindings.Count; i++)
				this.bindings[i].Delete();
			this.bindings.Clear();
		}

		public virtual void delete()
		{
			for (int i = 0; i < this.bindings.Count; i++)
				this.bindings[i].Delete();
			this.bindings.Clear();
		}
	}

	public class Component<MainClass> : Bindable, IComponent
		where MainClass : BaseMain
	{
		[XmlIgnore]
		public bool Serialize { get; set; }

		public Property<bool> Enabled { get; set; }

		[XmlIgnore]
		public Property<bool> Suspended { get; set; }

		[XmlIgnore]
		public Command Enable = new Command();

		[XmlIgnore]
		public Command Disable = new Command();

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
		public bool EnabledInEditMode { get; set; }
		[XmlIgnore]
		public bool EnabledWhenPaused { get; set; }

		protected MainClass main;

		[XmlIgnore]
		public virtual Entity Entity { get; set; }

		[XmlIgnore]
		public bool Active { get; private set; }

		public Component()
		{
			this.Serialize = true;
			this.Enabled = new Property<bool> { Value = true };
			this.Suspended = new Property<bool> { Value = false };
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = true;
			this.Delete.Action = delegate()
			{
				if (this.Active)
				{
					this.Active = false;
					this.main.RemoveComponent(this);
				}
			};

			this.Add(new CommandBinding(Enable, () => !Enabled, delegate()
			{
				Enabled.Value = true;
			}));

			this.Add(new CommandBinding(Disable, () => Enabled, delegate()
			{
				Enabled.Value = false;
			}));
		}

		public virtual void OnSave()
		{

		}

		public virtual void Start()
		{

		}

		public virtual void Awake()
		{
			this.Add(new ChangeBinding<bool>(this.Enabled, delegate(bool old, bool value)
			{
				if (!old && value)
					this.Enable.Execute();
				else if (old && !value)
					this.Disable.Execute();
			}));
			this.Add(new ChangeBinding<bool>(this.Suspended, delegate(bool old, bool value)
			{
				if (!old && value)
					this.OnSuspended.Execute();
				else if (old && !value)
					this.OnResumed.Execute();
			}));
		}

		public void SetMain(BaseMain _main)
		{
			this.Active = true;
			this.main = (MainClass)_main;
		}

		public override void delete()
		{
			if (this.Entity != null)
				this.Entity.Remove(this);
			base.delete();
		}
	}
}
