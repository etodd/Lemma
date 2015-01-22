using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SequenceFactory : Factory<Main>
	{
		public SequenceFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Sequence");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Ticker ticker = entity.GetOrCreate<Ticker>("Ticker");
			Sequence sequence = entity.GetOrCreate<Sequence>("Sequence");
			ticker.Add(new CommandBinding(ticker.OnFire, sequence.Advance));
			ticker.Add(new CommandBinding(sequence.Done, ticker.Disable));
			this.SetMain(entity, main);

			entity.Add("Commands", sequence.Commands);
			entity.Add("Advance", sequence.Advance);
			entity.Add("Done", sequence.Done);
			entity.Add("Index", sequence.Index);
			entity.Add("Disable", ticker.Disable);
			entity.Add("Enable", ticker.Enable);
			entity.Add("Enabled", ticker.Enabled);
			entity.Add("Interval", ticker.Interval);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}
	}
}