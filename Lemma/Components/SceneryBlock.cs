using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class SceneryBlock : Component<Main>
	{
		[XmlIgnore]
		public Property<bool> Valid = new Property<bool>();

		public Property<Voxel.t> Type = new Property<Voxel.t>();

		public void EditorProperties()
		{
			this.Entity.Add("Type", this.Type);
		}

		public override void Awake()
		{
			base.Awake();

			this.Add(new SetBinding<Voxel.t>(this.Type, delegate(Voxel.t value)
			{
				if (value == Voxel.t.Empty)
					this.Valid.Value = false;
				else
				{
					Voxel.States[value].ApplyToBlock(this.Entity);
					this.Valid.Value = true;
				}
			}));
		}
	}
}
