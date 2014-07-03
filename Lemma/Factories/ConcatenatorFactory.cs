using System;
using System.Security.Cryptography;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class ConcatenatorFactory : Factory<Main>
	{
		public ConcatenatorFactory()
		{
			this.Color = new Vector3(0.25f, 0.91f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Concatenator");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, "PrefixEntity", entity.Get<Concatenator>().PrefixEntity);
			EntityConnectable.AttachEditorComponents(entity, "SuffixEntity", entity.Get<Concatenator>().SuffixEntity);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Concatenator concatenator = entity.GetOrCreate<Concatenator>("Concatenator");

			base.Bind(entity, main, creating);

			if (main.EditorEnabled)
			{
				ListProperty<string> inTargetOptions = new ListProperty<string>();
				ListProperty<string> outTargetOptions = new ListProperty<string>();
				Action populateInOptions = delegate()
				{
					inTargetOptions.Clear();
					Entity e = concatenator.PrefixEntity.Value.Target;
					if (e != null && e.Active)
					{
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
							inTargetOptions.Add(p.Key);
					}
				};

				Action populateOutOptions = delegate()
				{
					outTargetOptions.Clear();
					Entity e = concatenator.SuffixEntity.Value.Target;
					if (e != null && e.Active)
					{
						foreach (KeyValuePair<string, PropertyEntry> p in e.Properties)
							outTargetOptions.Add(p.Key);
					}
				};

				Action populateOptions = delegate()
				{
					populateInOptions();
					populateOutOptions();
				};

				entity.Add(new NotifyBinding(populateOptions, concatenator.PrefixEntity, concatenator.SuffixEntity));
				entity.Add(new PostInitialization {populateOptions});
				entity.Add("PrefixProperty", concatenator.PrefixProperty, new PropertyEntry.EditorData
				{
					Options = inTargetOptions,
				});
				entity.Add("SuffixProperty", concatenator.SuffixProperty, new PropertyEntry.EditorData
				{
					Options = outTargetOptions,
				});
			}
			entity.Add("Concatenated", concatenator.Concatenated, "The concatenated string");
		}
	}
}
