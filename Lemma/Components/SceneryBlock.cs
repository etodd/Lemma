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
		public static List<SceneryBlock> All = new List<SceneryBlock>();

		[XmlIgnore]
		public Property<bool> Valid = new Property<bool>();

		public Property<Voxel.t> Type = new Property<Voxel.t>();

		public Property<float> Scale = new Property<float> { Value = 1.0f };

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
					Voxel.States.All[value].ApplyToBlock(this.Entity);
					this.Valid.Value = true;
				}
			}));

			SceneryBlock.All.Add(this);
		}

		public override void delete()
		{
			SceneryBlock.All.Remove(this);
			base.delete();
		}
	}
}
