using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class WaterFactory : Factory
	{
		public WaterFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Water");

			Transform transform = new Transform();
			result.Add("Transform", transform);
			Water water = new Water();
			result.Add("Water", water);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;
			Transform transform = result.Get<Transform>();
			Water water = result.Get<Water>();

			this.SetMain(result, main);

			water.Add(new TwoWayBinding<Vector3>(water.Position, transform.Position));
		}
	}
}
