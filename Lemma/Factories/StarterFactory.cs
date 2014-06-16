using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class StarterFactory : Factory<Main>
	{
		public StarterFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Starter");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.GetOrCreate<Transform>("Transform");
			this.SetMain(entity, main);
			Starter starter = entity.GetOrCreate<Starter>("Starter");
			entity.CannotSuspendByDistance = true;

			entity.Add("Command", starter.Command);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
