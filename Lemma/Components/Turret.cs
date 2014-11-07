using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Turret : Component<Main>
	{
		public Property<Vector3> Reticle = new Property<Vector3>();
		public Property<Entity.Handle> TargetAgent = new Property<Entity.Handle>();
		public Property<bool> On = new Property<bool> { Value = true };
		[XmlIgnore]
		public Command PowerOn = new Command();
		[XmlIgnore]
		public Command PowerOff = new Command();

		private bool suppressCommandNotification;

		public override void Awake()
		{
			base.Awake();

			this.Add(new ChangeBinding<bool>(this.On, delegate(bool old, bool value)
			{
				if (!this.suppressCommandNotification)
				{
					if (value)
						this.PowerOn.Execute();
					else
						this.PowerOff.Execute();
				}
			}));

			this.Add(new CommandBinding(this.PowerOn, delegate()
			{
				this.suppressCommandNotification = true;
				this.On.Value = true;
				this.suppressCommandNotification = false;
			}));

			this.Add(new CommandBinding(this.PowerOff, delegate()
			{
				this.suppressCommandNotification = true;
				this.On.Value = false;
				this.suppressCommandNotification = false;
			}));
		}
	}
}
