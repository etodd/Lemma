using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Concatenator : Component<Main>
	{
		public Property<Entity.Handle> PrefixEntity = new Property<Entity.Handle>();
		public Property<Entity.Handle> SuffixEntity = new Property<Entity.Handle>();

		public Property<string> PrefixProperty = new Property<string>();
		public Property<string> SuffixProperty = new Property<string>();

		public Property<string> Concatenated = new Property<string>();

		private IProperty targetPrefixProp;
		private IProperty targetSuffixProp;

		public override void Awake()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;

			Concatenated.Get = GetConcat;

			this.Add(new NotifyBinding(() =>
			{
				this.targetPrefixProp = null;
				this.targetSuffixProp = null;
				FindProperties();
			}, PrefixEntity, SuffixEntity, PrefixProperty, SuffixProperty));
			
			base.Awake();
		}

		public string GetConcat()
		{
			FindProperties();
			return GetPropertyValue(targetPrefixProp) + GetPropertyValue(targetSuffixProp);
		}

		private string GetPropertyValue(IProperty prop)
		{
			if (prop == null) return null;
			return prop.GetType().GetProperty("Value").GetValue(prop, null).ToString();
		}

		public override void Start()
		{
			this.FindProperties();
		}

		private void FindProperty(ref IProperty prop, Entity entity, string name)
		{
			if (entity != null && entity.Active)
			{
				prop = entity.GetProperty(name);
			}
		}

		public void FindProperties()
		{
			if (this.targetPrefixProp == null)
			{
				Entity entity = this.PrefixEntity.Value.Target;
				FindProperty(ref targetPrefixProp, entity, PrefixProperty);
			}
			if (this.targetSuffixProp == null)
			{
				Entity entity = this.SuffixEntity.Value.Target;
				FindProperty(ref targetSuffixProp, entity, SuffixProperty);
			}
		}
	}
}
