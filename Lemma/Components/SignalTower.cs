using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using ComponentBind;

namespace Lemma.Components
{
	public class SignalTower : Component<Main>
	{
		public static List<SignalTower> All = new List<SignalTower>();

		public Property<string> Initial = new Property<string>();
		public Property<Entity.Handle> Player = new Property<Entity.Handle>();

		private const float messageDelay = 2.0f;

		[XmlIgnore]
		public Command PlayerEnteredRange = new Command();
		[XmlIgnore]
		public Command PlayerExitedRange = new Command();

		public override void Awake()
		{
			base.Awake();
			this.PlayerEnteredRange.Action = delegate()
			{
				Phone phone = PlayerDataFactory.Instance.Get<Phone>();

				if (!string.IsNullOrEmpty(this.Initial))
				{
					DialogueForest forest = WorldFactory.Instance.Get<World>().DialogueForest;
					DialogueForest.Node n = forest.GetByName(this.Initial);
					if (n == null)
						Log.d(string.Format("Could not find dialogue node {0}", this.Initial));
					else
					{
						if (n.type == DialogueForest.Node.Type.Choice)
							throw new Exception("Cannot start dialogue tree with a choice");
						phone.Execute(n);
					}

					if (phone.Schedules.Length > 0) // We sent a message. That means this signal tower cannot execute again.
						this.Initial.Value = null;
					
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIGNAL_TOWER_ACTIVATE, this.Entity);
				}

				PlayerFactory.Instance.Get<Player>().SignalTower.Value = this.Entity;
			};

			this.PlayerExitedRange.Action = delegate()
			{
				Phone phone = PlayerDataFactory.Instance.Get<Phone>();

				if (!string.IsNullOrEmpty(this.Initial)) // The player did not interact.
					phone.ActiveAnswers.Clear();

				if (PlayerFactory.Instance != null)
					PlayerFactory.Instance.Get<Player>().SignalTower.Value = null;
			};

			if (!this.main.EditorEnabled)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIGNAL_TOWER_LOOP, this.Entity);

			SignalTower.All.Add(this);
		}

		public override void delete()
		{
			SignalTower.All.Remove(this);
			AkSoundEngine.PostEvent(AK.EVENTS.STOP_SIGNAL_TOWER_LOOP, this.Entity);
			Entity player = this.Player.Value.Target;
			if (player != null && player.Active)
			{
				Property<Entity.Handle> signalTowerHandle = player.Get<Player>().SignalTower;
				if (signalTowerHandle.Value.Target == this.Entity)
					signalTowerHandle.Value = null;
			}
			base.delete();
		}
	}
}
