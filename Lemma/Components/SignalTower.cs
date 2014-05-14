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
		public Property<bool> Send = new Property<bool> { Editable = true };
		public Property<Entity.Handle> Player = new Property<Entity.Handle> { Editable = false };

		private const float messageDelay = 2.0f;

		[XmlIgnore]
		public Command<Entity> PlayerEnteredRange = new Command<Entity>();
		[XmlIgnore]
		public Command<Entity> PlayerExitedRange = new Command<Entity>();

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			this.PlayerEnteredRange.Action = delegate(Entity p)
			{
				Phone phone = Factory<Main>.Get<PlayerDataFactory>().Instance(this.main).Get<Phone>();

				bool active = !string.IsNullOrEmpty(this.Initial);
				if (active && this.Send)
				{
					phone.Delay(SignalTower.messageDelay, this.Initial);
					this.Send.Value = false;
				}

				// Add answers to phone
				phone.ActiveAnswers.Clear();

				if (active)
				{
					IEnumerable<DialogueForest> forests = WorldFactory.Get().GetListProperty<DialogueForest>();
					foreach (DialogueForest forest in forests)
					{
						DialogueForest.Node n = forest.GetByName(this.Initial);
						if (n != null)
						{
							foreach (DialogueForest.Node choice in n.choices.Select(x => forest[x]))
								phone.ActiveAnswers.Add(new Phone.Ans(choice.name));
							break;
						}
					}
				}

				p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = this.Entity;
			};

			this.PlayerExitedRange.Action = delegate(Entity p)
			{
				Phone phone = Factory<Main>.Get<PlayerDataFactory>().Instance(this.main).Get<Phone>();
				if (phone.ActiveAnswers.Count > 0)
				{
					// The player didn't pick an answer
					phone.ActiveAnswers.Clear();
				}
				else
					this.Initial.Value = null; // The player picked an answer. This signal tower is done.

				p.GetOrMakeProperty<Entity.Handle>("SignalTower").Value = null;
			};
		}

		public override void delete()
		{
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
