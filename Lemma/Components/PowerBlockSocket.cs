using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class PowerBlockSocket : Component<Main>
	{
		public Property<bool> Powered = new Property<bool>();

		[XmlIgnore]
		public Command OnPowerOn = new Command();

		[XmlIgnore]
		public Command OnPowerOff = new Command();

		[XmlIgnore]
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		[XmlIgnore]
		public Property<Vector3> Position = new Property<Vector3>();

		public Property<Voxel.t> Type = new Property<Voxel.t>();

		public override void Awake()
		{
			base.Awake();

			this.Add(new ChangeBinding<bool>(this.Powered, delegate(bool old, bool value)
			{
				if (value && !old)
					this.OnPowerOn.Execute();
				else if (!value && old)
					this.OnPowerOff.Execute();
			}));
		}

		public override void Start()
		{
			Entity v = this.AttachedVoxel.Value.Target;
			this.Powered.Value = v != null ? v.Get<Voxel>()[this.Position].ID == this.Type : false;
		}
	}
}
