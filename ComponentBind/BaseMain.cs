using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ComponentBind
{
	public class BaseMain : Microsoft.Xna.Framework.Game
	{
		public Command<Entity> EntityAdded = new Command<Entity>();
		public Command<Entity> EntityRemoved = new Command<Entity>();

		public Property<bool> Paused = new Property<bool>();

		public Property<float> ElapsedTime = new Property<float>();
		public Property<float> TotalTime = new Property<float>();

		public Property<bool> EditorEnabled = new Property<bool> { Value = false };

		public GameTime GameTime;

		public ListProperty<Entity> Entities;

		protected List<IComponent> componentsToRemove = new List<IComponent>();
		protected List<IComponent> componentsToAdd = new List<IComponent>();

		public void Add(Entity entity)
		{
			if (entity.Active && !entity.Added)
			{
				entity.Added = true;
				this.Entities.Add(entity);
				this.EntityAdded.Execute(entity);
			}
		}

		public void AddComponent(IComponent component)
		{
			if (this.EditorEnabled || component.Entity == null || component.Entity.CannotSuspend)
				component.Suspended.Value = false;
			if (component.NeedsAdded)
			{
				component.SetMain(this);
				if (typeof(IGraphicsComponent).IsAssignableFrom(component.GetType()))
					((IGraphicsComponent)component).LoadContent(false);
				component.Awake();
				this.componentsToAdd.Add(component);
			}
		}

		public void RemoveComponent(IComponent component)
		{
			this.componentsToRemove.Add(component);
		}

		public void Remove(Entity entity)
		{
			if (entity.Active)
			{
				// Trigger all delete commands
				// then call this Remove function again, but this time the entity will be inactive.
				entity.Delete.Execute();
			}
			else if (entity.Added)
			{
				entity.Added = false;
				this.Entities.Remove(entity);
				this.EntityRemoved.Execute(entity);
			}
		}

		public IEnumerable<Entity> Get(string type)
		{
			return this.Entities.Where(x => x.Type == type && x.Active);
		}

		public Entity GetByID(string id)
		{
			return Entity.GetByID(id);
		}

		public Entity GetByGUID(ulong id)
		{
			return Entity.GetByGUID(id);
		}
	}
}
