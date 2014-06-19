using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class RotatorFactory : Factory<Main>
	{
		public RotatorFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Rotator");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.GetOrCreate<Transform>("Transform");
			Rotator rotator = entity.GetOrCreate<Rotator>("Rotator");
			rotator.Add(new CommandBinding(rotator.Delete, entity.Delete));
			this.SetMain(entity, main);
			rotator.EditorProperties();
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			EntityConnectable.AttachEditorComponents(entity, "Targets", entity.Get<Rotator>().Targets);
		}
	}
}
