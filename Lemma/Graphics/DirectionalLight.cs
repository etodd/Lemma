using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class DirectionalLight : Component
	{
		public static readonly List<DirectionalLight> All = new List<DirectionalLight>();

		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One, Editable = true };
		public Property<Vector3> Direction = new Property<Vector3> { Value = Vector3.Left, Editable = true };
		public Property<bool> Shadowed = new Property<bool> { Editable = true };

		public DirectionalLight()
		{
			this.Enabled.Editable = true;
		}

		public override void SetMain(Main _main)
		{
			base.SetMain(_main);
			DirectionalLight.All.Add(this);
		}

		protected override void delete()
		{
			base.delete();
			DirectionalLight.All.Remove(this);
		}
	}
}
