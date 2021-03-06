﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;

namespace ComponentBind
{
	public class Factory
	{
		protected static Dictionary<string, Factory> factories = new Dictionary<string, Factory>();
		protected static Dictionary<Type, Factory> factoriesByType = new Dictionary<Type, Factory>();

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

		public Vector3 Color = Vector3.One;

		public bool EditorCanSpawn = true;
		public bool AvailableInRelease = true;
	}

	public class Factory<MainClass> : Factory
		where MainClass : BaseMain
	{
		static Factory()
		{
			Factory<MainClass>.Initialize();
		}

		public static void Initialize()
		{
			Factory.factories.Clear();
			Factory.factoriesByType.Clear();
			Type baseType = typeof(Factory<MainClass>);
			foreach (Type type in Assembly.GetEntryAssembly().GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t)))
			{
				if (!type.ContainsGenericParameters)
				{
					Factory factory = (Factory)type.GetConstructor(new Type[] { }).Invoke(new object[] { });
					Factory.factories.Add(type.Name.Substring(0, type.Name.Length - "Factory".Length), factory);
					Factory.factoriesByType.Add(type, factory);
				}
			}
		}

		public static new Factory<MainClass> Get(string type)
		{
			Factory result = null;
			Factory.factories.TryGetValue(type, out result);
			return (Factory<MainClass>)result;
		}

		public virtual Entity Create(MainClass main)
		{
			return null;
		}

		public Entity CreateAndBind(MainClass main)
		{
			Entity entity = this.Create(main);
			this.Bind(entity, main, true);
			return entity;
		}

		public void SetMain(Entity entity, MainClass main)
		{
			entity.SetMain(main);
			if (main.EditorEnabled)
			{
				Factory<MainClass>.GlobalEditorComponents(entity, main);
				this.AttachEditorComponents(entity, main);
			}
		}

		public virtual void Bind(Entity entity, MainClass main, bool creating = false)
		{
			this.SetMain(entity, main);
		}

		public static Action<Factory<MainClass>, Entity, MainClass> DefaultEditorComponents;
		public static Action<Entity, MainClass> GlobalEditorComponents;

		public virtual void AttachEditorComponents(Entity entity, MainClass main)
		{
			Factory<MainClass>.DefaultEditorComponents(this, entity, main);
		}
	}
}
