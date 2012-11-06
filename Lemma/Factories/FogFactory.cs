using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class FogFactory : Factory
	{
		public FogFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Fog");

			Transform transform = new Transform();
			result.Add("Transform", transform);
			Fog fog = new Fog();
			result.Add("Fog", fog);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			result.CannotSuspendByDistance = true;
		}
	}
}
