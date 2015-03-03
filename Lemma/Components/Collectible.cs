using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Collectible : Component<Main>
	{
		private static List<Collectible> collectibles = new List<Collectible>();

		[XmlIgnore]
		public Command PlayerTouched = new Command();

		public Property<bool> PickedUp = new Property<bool>(); 

		public static int ActiveCount
		{
			get
			{
				return Collectible.collectibles.Count(x => !x.PickedUp);
			}
		}

		public override void Awake()
		{
			base.Awake();
			Collectible.collectibles.Add(this);

			this.PlayerTouched.Action = delegate
			{
				if (!this.PickedUp)
				{
					this.PickedUp.Value = true;
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_COLLECTIBLE, this.Entity);
					float originalGamma = main.Renderer.InternalGamma.Value;
					float originalBrightness = main.Renderer.Brightness.Value;
					this.Entity.Add
					(
						new Animation
						(
							new Animation.FloatMoveTo(main.Renderer.InternalGamma, 10.0f, 0.2f),
							new Animation.FloatMoveTo(main.Renderer.InternalGamma, originalGamma, 0.4f)
						)
					);

					PlayerDataFactory.Instance.Get<PlayerData>().Collectibles.Value++;

					int collected = Collectible.collectibles.Count(x => x.PickedUp);
					int total = Collectible.collectibles.Count;

					SteamWorker.IncrementStat("orbs_collected", 1);

					this.main.Menu.HideMessage
					(
						WorldFactory.Instance,
						this.main.Menu.ShowMessageFormat(WorldFactory.Instance, "\\orbs collected", collected, total),
						4.0f
					);
				}
			};
		}

		public override void delete()
		{
			base.delete();
			Collectible.collectibles.Remove(this);
		}
	}
}