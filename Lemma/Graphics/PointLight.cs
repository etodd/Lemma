using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class PointLight : Component
	{
		public static readonly List<PointLight> All = new List<PointLight>();

		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One, Editable = true };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<float> Attenuation = new Property<float> { Value = 10.0f, Editable = true };
		public Property<bool> Shadowed = new Property<bool> { Value = false, Editable = true };
		public Property<bool> AlwaysShadow = new Property<bool> { Value = false, Editable = true };

		[XmlIgnore]
		public Property<BoundingSphere> BoundingSphere = new Property<BoundingSphere> { Editable = false };

		public override void InitializeProperties()
		{
			this.Add(new Binding<BoundingSphere>(this.BoundingSphere, () => new BoundingSphere(this.Position, this.Attenuation), this.Position, this.Attenuation));
			this.Enabled.Editable = true;
		}

		public override void SetMain(Main _main)
		{
			base.SetMain(_main);
			PointLight.All.Add(this);
		}

		protected override void delete()
		{
			base.delete();
			PointLight.All.Remove(this);
		}
	}
}
