using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class SetterFactory : Factory<Main>
	{
		public SetterFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Setter");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, "Target", entity.Get<Setter>().Target);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Setter setter = entity.GetOrCreate<Setter>("Setter");

			base.Bind(entity, main, creating);

			entity.Add("Set", setter.Set);
			entity.Add("Type", setter.PropertyType);

			if (main.EditorEnabled)
			{
				Func<Setter.PropType, Property<bool>> visible = delegate(Setter.PropType t)
				{
					Property<bool> result = new Property<bool>();
					entity.Add(new Binding<bool, Setter.PropType>(result, x => x == t, setter.PropertyType));
					return result;
				};

				ListProperty<string> targetOptions = new ListProperty<string>();
				Action populateOptions = delegate()
				{
					targetOptions.Clear();
					Entity e = setter.Target.Value.Target;
					if (e != null && e.Active)
					{
						Type t = Setter.TypeMapping[setter.PropertyType];
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
						{
							if (p.Value.Property.GetType().GetGenericArguments().First() == t)
								targetOptions.Add(p.Key);
						}
					}
				};
				entity.Add(new NotifyBinding(populateOptions, setter.Target, setter.PropertyType));
				entity.Add(new PostInitialization { populateOptions });
				entity.Add("TargetProperty", setter.TargetProperty, new PropertyEntry.EditorData
				{
					Options = targetOptions,
				});

				entity.Add("Bool", setter.Bool, null, visible(Setter.PropType.Bool));
				entity.Add("Int", setter.Int, null, visible(Setter.PropType.Int));
				entity.Add("Float", setter.Float, null, visible(Setter.PropType.Float));
				entity.Add("Direction", setter.Direction, null, visible(Setter.PropType.Direction));
				entity.Add("String", setter.String, null, visible(Setter.PropType.String));
				entity.Add("Vector2", setter.Vector2, null, visible(Setter.PropType.Vector2));
				entity.Add("Vector3", setter.Vector3, null, visible(Setter.PropType.Vector3));
				entity.Add("Vector4", setter.Vector4, null, visible(Setter.PropType.Vector4));
				entity.Add("Coord", setter.Coord, null, visible(Setter.PropType.Coord));
			}
			else
			{
				entity.Add("Bool", setter.Bool);
				entity.Add("Int", setter.Int);
				entity.Add("Float", setter.Float);
				entity.Add("Direction", setter.Direction);
				entity.Add("String", setter.String);
				entity.Add("Vector2", setter.Vector2);
				entity.Add("Vector3", setter.Vector3);
				entity.Add("Vector4", setter.Vector4);
				entity.Add("Coord", setter.Coord);
			}
		}
	}
}
