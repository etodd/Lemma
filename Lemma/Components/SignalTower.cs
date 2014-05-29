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
		public Property<string> Initial = new Property<string> { Editable = true };
		public Property<Entity.Handle> Player = new Property<Entity.Handle> { Editable = false };

		private const float messageDelay = 2.0f;

		[XmlIgnore]
		public Command<Entity> PlayerEnteredRange = new Command<Entity>();
		[XmlIgnore]
		public Command<Entity> PlayerExitedRange = new Command<Entity>();

		public override void Awake()
		{
			base.Awake();
			this.PlayerEnteredRange.Action = delegate(Entity p)
			{
				Phone phone = Factory<Main>.Get<PlayerDataFactory>().Instance.Get<Phone>();

				if (!string.IsNullOrEmpty(this.Initial))
				{
					DialogueForest forest = WorldFactory.Instance.GetProperty<DialogueForest>();
					DialogueForest.Node n = forest.GetByName(this.Initial);
					if (n == null)
						Log.d(string.Format("Could not find dialogue node {0}", this.Initial));
					else
					{
						if (n.type == DialogueForest.Node.Type.Choice)
							throw new Exception("Cannot start dialogue tree with a choice");
						phone.Execute(forest, n);
						if (phone.Schedules.Count == 0)
						{
							// If there are choices available, they will initiate a conversation.
							// The player should be able to pull up the phone, see the choices, and walk away without picking any of them.
							// Normally, you can't put the phone down until you've picked an answer.
							phone.WaitForAnswer.Value = false;
						}
					}

					if (phone.Schedules.Count > 0) // We sent a message. That means this signal tower cannot execute again.
						this.Initial.Value = null;
					
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIGNAL_TOWER_ACTIVATE, this.Entity);
				}

				p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = this.Entity;
			};

			this.PlayerExitedRange.Action = delegate(Entity p)
			{
				Phone phone = Factory<Main>.Get<PlayerDataFactory>().Instance.Get<Phone>();

				phone.ActiveAnswers.Clear();

				if (p != null)
					p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = null;
			};

			if (!this.main.EditorEnabled)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIGNAL_TOWER_LOOP, this.Entity);
		}

		public override void delete()
		{
			AkSoundEngine.PostEvent(AK.EVENTS.STOP_SIGNAL_TOWER_LOOP, this.Entity);
			Entity player = this.Player.Value.Target;
			if (player != null && player.Active)
			{
				Property<Entity.Handle> signalTowerHandle = player.GetOrMakeProperty<Entity.Handle>("SignalTower");
				if (signalTowerHandle.Value.Target == this.Entity)
					signalTowerHandle.Value = null;
			}
			base.delete();
		}
	}
}
