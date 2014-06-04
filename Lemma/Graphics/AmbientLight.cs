using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AmbientLight : Component<Main>
	{
		public static readonly List<AmbientLight> All = new List<AmbientLight>();

		public EditorProperty<Vector3> Color = new EditorProperty<Vector3>();

		public AmbientLight()
		{
			this.Enabled.Editable = true;
		}

		public override void Awake()
		{
			base.Awake();
			AmbientLight.All.Add(this);
		}

		public override void delete()
		{
			base.delete();
			AmbientLight.All.Remove(this);
		}
	}
}
