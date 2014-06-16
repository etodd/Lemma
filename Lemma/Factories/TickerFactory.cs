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
			return new Entity(main, "Ticker");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Position");
			Ticker ticker = entity.GetOrCreate<Ticker>("Ticker");
			this.SetMain(entity, main);

			entity.Add("OnFire", ticker.OnFire);
			entity.Add("Disable", ticker.Disable);
			entity.Add("Enable", ticker.Enable);
			entity.Add("Enabled", ticker.Enabled);
			entity.Add("Interval", ticker.Interval);
			entity.Add("NumToFire", ticker.NumToFire);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
