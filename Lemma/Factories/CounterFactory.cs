using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class CounterFactory : Factory<Main>
	{
		public CounterFactory()
		{
			this.Color = new Vector3(0.0f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Counter");
			Counter counter = entity.GetOrCreate<Counter>("Counter");
			Transform transform = entity.GetOrCreate<Transform>("Position");
			counter.StartingValue.Value = 0f;
			counter.IncrementBy.Value = 1f;
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			base.Bind(entity, main, creating);
			Counter c = entity.GetOrCreate<Counter>("Counter");
			BindCommand(entity, c.OnTargetHit, "OnTargetHit");
			BindCommand(entity, c.Increment, "Increment");
			BindCommand(entity, c.Reset, "Reset");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
