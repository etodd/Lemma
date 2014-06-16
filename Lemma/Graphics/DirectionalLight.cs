using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class DirectionalLight : Component<Main>
	{
		public static readonly List<DirectionalLight> All = new List<DirectionalLight>();

		public Property<Vector3> Color = new Property<Vector3> { Value = Vector3.One };
		public Property<Vector3> Direction = new Property<Vector3> { Value = Vector3.Left };
		public Property<bool> Shadowed = new Property<bool>();

		public override void Awake()
		{
			base.Awake();
			DirectionalLight.All.Add(this);
		}

		public override void delete()
		{
			base.delete();
			DirectionalLight.All.Remove(this);
		}
	}
}
