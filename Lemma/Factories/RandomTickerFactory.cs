using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class RandomTickerFactory : Factory<Main>
	{
		public RandomTickerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "RandomTicker");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			RandomTicker ticker = entity.GetOrCreate<RandomTicker>("Ticker");
			this.SetMain(entity, main);

			entity.Add("MaxInterval", ticker.MaxInterval);
			entity.Add("MinInterval", ticker.MinInterval);
			entity.Add("NumToFire", ticker.NumToFire);
			entity.Add("Enabled", ticker.Enabled);

			entity.Add("Disable", ticker.Disable);
			entity.Add("Enable", ticker.Enable);
			entity.Add("Disable", ticker.Disable);
			entity.Add("OnFire", ticker.OnFire);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}
