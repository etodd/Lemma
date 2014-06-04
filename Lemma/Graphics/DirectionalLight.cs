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

		public EditorProperty<Vector3> Color = new EditorProperty<Vector3> { Value = Vector3.One };
		public EditorProperty<Vector3> Direction = new EditorProperty<Vector3> { Value = Vector3.Left };
		public EditorProperty<bool> Shadowed = new EditorProperty<bool>();

		public DirectionalLight()
		{
			this.Enabled.Editable = true;
		}

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
