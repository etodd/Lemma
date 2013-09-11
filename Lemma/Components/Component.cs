using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Lemma.Components
{
	public interface IComponent
	{
		Entity Entity { get; }
		bool NeedsAdded { get; }
		bool Active { get; }
		void SetMain(Main main);
		Property<bool> Enabled { get; }
		Property<bool> EnabledInEditMode { get; }
		Property<bool> EnabledWhenPaused { get; }
		Property<bool> Suspended { get; }
		void LoadContent(bool reload);
	}

	public interface IUpdateableComponent : IComponent
	{
		void Update(float dt);
	}

	public interface IEditorUIComponent : IComponent
	{
		void AddEditorElements(UIComponent propertyList, EditorUI ui);
	}

	public interface IDrawableComponent : IComponent
	{
		void Draw(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawableAlphaComponent : IComponent
	{
		void DrawAlpha(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public interface IDrawablePreFrameComponent : IComponent
	{
		void DrawPreFrame(GameTime time, RenderParameters parameters);
	}

	public interface INonPostProcessedDrawableComponent : IComponent
	{
		void DrawNonPostProcessed(GameTime time, RenderParameters parameters);
		Property<int> DrawOrder { get; }
	}

	public class Component : IComponent
	{
		[XmlIgnore]
		public bool Serialize = true;

		[XmlAttribute]
		[DefaultValue(true)]
		public bool Editable = true;

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

		[XmlIgnore]
		public Command Delete = new Command();

		[XmlIgnore]
		public Property<bool> EnabledInEditMode { get; set; }
		[XmlIgnore]
		public Property<bool> EnabledWhenPaused { get; set; }

		private List<IBinding> bindings = new List<IBinding>();

		protected Main main;

		[XmlIgnore]
		public virtual Entity Entity { get; set; }

		[XmlIgnore]
		public bool Active { get; private set; }

		public Component()
		{
			this.Active = true;
			this.Enabled = new Property<bool> { Value = true, Editable = false };
			this.Suspended = new Property<bool> { Value = false, Editable = false };
			this.EnabledInEditMode = new Property<bool> { Value = true, Editable = false };
			this.EnabledWhenPaused = new Property<bool> { Value = true, Editable = false };
			this.Delete.Action = (Action)this.delete;
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

		public virtual void SetMain(Main _main)
		{
			this.main = _main;
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
