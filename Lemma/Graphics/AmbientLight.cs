using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AmbientLight : Component
	{
		public static readonly List<AmbientLight> All = new List<AmbientLight>();

		public Property<Vector3> Color = new Property<Vector3> { Editable = true };

		public AmbientLight()
		{
			this.Enabled.Editable = true;
		}

		public override void SetMain(Main _main)
		{
			base.SetMain(_main);
			AmbientLight.All.Add(this);
		}

		protected override void delete()
		{
			base.delete();
			AmbientLight.All.Remove(this);
		}
	}
}
