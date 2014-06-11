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

		public EditorProperty<Voxel.t> Type = new EditorProperty<Voxel.t>();

		public override void Awake()
		{
			base.Awake();

			this.Type.Set = delegate(Voxel.t value)
			{
				if (value == Voxel.t.Empty)
					this.Valid.Value = false;
				else
				{
					Voxel.States[value].ApplyToBlock(this.Entity);
					this.Valid.Value = true;
				}
				this.Type.InternalValue = value;
			};
		}
	}
}
