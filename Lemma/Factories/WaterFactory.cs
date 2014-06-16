using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class WaterFactory : Factory<Main>
	{
		public WaterFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Water");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Water water = entity.GetOrCreate<Water>("Water");

			this.SetMain(entity, main);

			water.Add(new TwoWayBinding<Vector3>(water.Position, transform.Position));
		}
	}
}
