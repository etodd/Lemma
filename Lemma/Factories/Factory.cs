using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class Factory
	{
		static Factory()
		{
			Factory.Initialize();
		}

		public static void Initialize()
		{
			Factory.factories.Clear();
			Factory.factoriesByType.Clear();
			Type baseType = typeof(Factory);
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t)))
			{
				Factory factory = (Factory)type.GetConstructor(new Type[] { }).Invoke(new object[] { });
				Factory.factories.Add(type.Name.Replace("Factory", ""), factory);
				Factory.factoriesByType.Add(type, factory);
			}
		}

		protected Vector3 Color = Vector3.One;
		protected static Dictionary<string, Factory> factories = new Dictionary<string, Factory>();
		protected static Dictionary<Type, Factory> factoriesByType = new Dictionary<Type, Factory>();

		private int spawnIndex = 1;
		public int SpawnIndex
		{
			get
			{
				return this.spawnIndex;
			}
			set
			{
				this.spawnIndex = value;
			}
		}

		public static T Get<T>() where T : Factory
		{
			Factory result = null;
			Factory.factoriesByType.TryGetValue(typeof(T), out result);
			return (T)result;
		}

		public static Factory Get(string type)
		{
			Factory result = null;
			Factory.factories.TryGetValue(type, out result);
			return result;
		}

		public static Entity Create(Main main, string type)
		{
			return Factory.factories[type].Create(main);
		}

		public static Entity CreateAndBind(Main main, string type)
		{
			return Factory.factories[type].CreateAndBind(main);
		}

		public virtual Entity Create(Main main)
		{
			return null;
		}

		public Entity CreateAndBind(Main main)
		{
			Entity result = this.Create(main);
			this.Bind(result, main, true);
			return result;
		}

		public void SetMain(Entity result, Main main)
		{
			this.SpawnIndex++;
			result.SetMain(main);
			if (main.EditorEnabled)
				this.AttachEditorComponents(result, main);
		}

		public virtual void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
		}

		public virtual void AttachEditorComponents(Entity result, Main main)
		{
			Transform transform = result.Get<Transform>();
			if (transform == null)
				return;

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Color.Value = new Vector3(this.Color.X, this.Color.Y, this.Color.Z);
			model.IsInstanced.Value = false;
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
		}

		public static Entity Duplicate(Main main, Entity source)
		{
			Entity dest = Factory.factories[source.Type].CreateAndBind(main);

			Dictionary<string, IProperty> destProperties = dest.PropertyDictionary;
			foreach (KeyValuePair<string, IProperty> pair in source.PropertyDictionary.Where(x => x.Key != "ID"))
			{
				IProperty destProperty = destProperties[pair.Key];
				if (typeof(IListProperty).IsAssignableFrom(destProperty.GetType()))
					((IListProperty)pair.Value).CopyTo((IListProperty)destProperty);
				else
				{
					PropertyInfo prop = destProperty.GetType().GetProperty("Value");
					prop.SetValue(destProperty, prop.GetValue(pair.Value, null), null);
				}
			}

			Dictionary<string, Component> destComponents = dest.ComponentDictionary;
			foreach (KeyValuePair<string, Component> pair in source.ComponentDictionary)
			{
				if (pair.Value.Serialize)
				{
					Component destComponent = destComponents[pair.Key];
					foreach (FieldInfo field in pair.Value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
					{
						if (!typeof(IProperty).IsAssignableFrom(field.FieldType) || field.GetCustomAttributes(true).FirstOrDefault(x => typeof(XmlIgnoreAttribute).IsAssignableFrom(x.GetType())) != null)
							continue;
						IProperty destProperty = (IProperty)field.GetValue(destComponent);
						if (!destProperty.Serialize)
							continue;
						IProperty srcProperty = (IProperty)field.GetValue(pair.Value);
						if (typeof(IListProperty).IsAssignableFrom(destProperty.GetType()))
							((IListProperty)srcProperty).CopyTo((IListProperty)destProperty);
						else
						{
							PropertyInfo prop = destProperty.GetType().GetProperty("Value");
							prop.SetValue(destProperty, prop.GetValue(srcProperty, null), null);
						}
					}
				}
			}

			return dest;
		}
	}
}
