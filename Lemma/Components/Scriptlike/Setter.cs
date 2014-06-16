using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Setter<T> : Component<Main>
	{
		public EditorProperty<T> NewVal = new EditorProperty<T>() { Description = "New value of the property" };

		public ListProperty<Entity.Handle> ConnectedEntities = new ListProperty<Entity.Handle>();
		public EditorProperty<string> TargetProperty = new EditorProperty<string>();

		[XmlIgnore]
		public Command Set = new Command();

		private List<Property<T>> _allProperties = null;

		public Setter()
		{
		}

		public override void Awake()
		{
			this._allProperties = new List<Property<T>>();
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Enabled.Editable = true;
			this.Set.Action = () =>
			{
				string[] split = TargetProperty.Value.Split('.');
				if (split.Length != 2)
				{
					Log.d("Incorrect TargetProperty in Setter " + Entity.ID);
					return;
				}
				string targetComponent = split[0];
				string targetProperty = split[1];
				FindProperties(targetComponent, targetProperty);
				foreach (var prop in _allProperties)
				{
					if (prop == null) continue;
					prop.Value = NewVal.Value;
				}
			};
			base.Awake();
		}

		public void FindProperties(string component, string property)
		{
			if (_allProperties.Count != 0) return;
			foreach (var handle in ConnectedEntities)
			{
				Entity entity = handle.Target;
				if (entity != null && entity.Active)
				{
					var comp = entity.Get<ComponentBind.IComponent>(component);
					if (comp == null)
					{
						Log.d("Cannot find component " + component + " in Setter " + Entity.ID);
						continue;
					}
					var t = comp.GetType();
					foreach (FieldInfo p in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
					{
						if (p.Name == property)
						{
							if (p.FieldType == typeof(EditorProperty<T>) || (p.FieldType == typeof(Property<T>) && ((Property<T>)p.GetValue(comp)).Editable))
							{
								Property<T> prop = (Property<T>)p.GetValue(comp);
								if(prop != null)
									_allProperties.Add(prop);
								else
								{
									Log.d("Property is null in Setter " + Entity.ID);
								}
							}
						}
					}
				}
			}
		}
	}
}
