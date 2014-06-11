using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class TickerFactory : Factory<Main>
	{
		public TickerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Ticker");
			Ticker ticker = entity.GetOrCreate<Ticker>("Ticker");
			ticker.Interval.Value = 1;
			ticker.NumToFire.Value = 0;
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Position");
			Ticker ticker = entity.GetOrCreate<Ticker>("Ticker");
			this.SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
