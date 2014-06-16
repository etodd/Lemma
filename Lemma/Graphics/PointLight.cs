using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class PointLight : Component<Main>
	{
		public static readonly List<PointLight> All = new List<PointLight>();

		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One };
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Attenuation = new Property<float> { Value = 10.0f };

		[XmlIgnore]
		public Property<BoundingSphere> BoundingSphere = new Property<BoundingSphere>();

		public override void Awake()
		{
			base.Awake();
			this.Add(new Binding<BoundingSphere>(this.BoundingSphere, () => new BoundingSphere(this.Position, this.Attenuation), this.Position, this.Attenuation));
			PointLight.All.Add(this);
		}

		public override void delete()
		{
			base.delete();
			PointLight.All.Remove(this);
		}
	}
}
