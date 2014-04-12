using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class PlayerSpawn : Component<Main>
	{
		public Property<bool> IsActivated = new Property<bool> { Editable = true };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<float> Rotation = new Property<float> { Editable = false };

		[XmlIgnore]
		public Command Activate = new Command();
		[XmlIgnore]
		public Command Deactivate = new Command();

		private static List<PlayerSpawn> spawns = new List<PlayerSpawn>();

		public static PlayerSpawn FirstActive()
		{
			return PlayerSpawn.spawns.FirstOrDefault(x => x.IsActivated);
		}

		public PlayerSpawn()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
		}

		public override void InitializeProperties()
		{
			PlayerSpawn.spawns.Add(this);

			this.Add(new CommandBinding(this.Delete, delegate() { PlayerSpawn.spawns.Remove(this); }));

			this.Activate.Action = delegate()
			{
				this.IsActivated.Value = true;
			};

			this.Deactivate.Action = delegate()
			{
				this.IsActivated.Value = false;
			};

			this.IsActivated.Set = delegate(bool value)
			{
				this.IsActivated.InternalValue = value;
				if (value)
				{
					foreach (PlayerSpawn spawn in PlayerSpawn.spawns)
					{
						if (spawn != this)
							spawn.Deactivate.Execute();
					}
				}
			};
		}
	}
}
