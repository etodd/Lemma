using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
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
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		public Property<Voxel.t> Type = new Property<Voxel.t>();

		public Property<bool> PowerOnOnly = new Property<bool>();

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

			this.Add(new NotifyBinding(this.updatePower, this.Coord));
		}

		private void updatePower()
		{
			Entity v = this.AttachedVoxel.Value.Target;
			bool powered = false;
			if (v != null)
			{
				Voxel voxel = v.Get<Voxel>();
				Voxel.Coord coord = this.Coord;
				for (int i = 0; i < 6; i++)
				{
					Direction dir = DirectionExtensions.Directions[i];
					if (voxel[coord.Move(dir)].ID == this.Type)
					{
						powered = true;
						break;
					}
				}
			}
			this.Powered.Value = powered;
		}
	}
}