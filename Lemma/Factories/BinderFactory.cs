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
	public class BinderFactory : Factory<Main>
	{
		public BinderFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Binder");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, "InTarget", entity.Get<Binder>().InTarget);
			EntityConnectable.AttachEditorComponents(entity, "OutTarget", entity.Get<Binder>().OutTarget);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Binder binder = entity.GetOrCreate<Binder>("Binder");

			base.Bind(entity, main, creating);

			entity.Add("Bind", binder.Bind);
			entity.Add("Unbind", binder.UnBind);
			entity.Add("Type", binder.PropertyType);

			if (main.EditorEnabled)
			{
				Func<Setter.PropType, Property<bool>> visible = delegate(Setter.PropType t)
				{
					Property<bool> result = new Property<bool>();
					entity.Add(new Binding<bool, Setter.PropType>(result, x => x == t, binder.PropertyType));
					return result;
				};

				ListProperty<string> inTargetOptions = new ListProperty<string>();
				ListProperty<string> outTargetOptions = new ListProperty<string>();
				Action populateInOptions = delegate()
				{
					inTargetOptions.Clear();
					Entity e = binder.InTarget.Value.Target;
					if (e != null && e.Active)
					{
						Type t = Setter.TypeMapping[binder.PropertyType];
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
						{
							if (p.Value.Property.GetType().GetGenericArguments().First() == t)
								inTargetOptions.Add(p.Key);
						}
					}
				};

				Action populateOutOptions = delegate()
				{
					outTargetOptions.Clear();
					Entity e = binder.OutTarget.Value.Target;
					if (e != null && e.Active)
					{
						Type t = Setter.TypeMapping[binder.PropertyType];
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
						{
							if (p.Value.Property.GetType().GetGenericArguments().First() == t)
								outTargetOptions.Add(p.Key);
						}
					}
				};

				Action populateOptions = delegate()
				{
					populateInOptions();
					populateOutOptions();
				};

				entity.Add(new NotifyBinding(populateOptions, binder.InTarget, binder.OutTarget, binder.PropertyType));
				entity.Add(new PostInitialization { populateOptions });
				entity.Add("TargetInProperty", binder.TargetInProperty, null, null, inTargetOptions);
				entity.Add("TargetOutProperty", binder.TargetOutProperty, null, null, outTargetOptions);
			}
		}
	}
}
