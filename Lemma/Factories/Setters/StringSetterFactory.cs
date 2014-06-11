using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class StringSetterFactory : Factory<Main>
	{
		public StringSetterFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Setter");
			Setter<string> setter = entity.GetOrCreate<Setter<string>>("Setter");
			Transform transform = entity.GetOrCreate<Transform>("Position");
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			base.Bind(entity, main, creating);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
			EntityConnectable.AttachEditorComponents(entity, entity.Get<Setter<string>>().ConnectedEntities);
		}
	}
}
