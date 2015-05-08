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
		public static List<Collectible> Collectibles = new List<Collectible>();

		const int totalCollectibles = 49;

		[XmlIgnore]
		public Command PlayerTouched = new Command();

		public Property<bool> PickedUp = new Property<bool>(); 

		public static int ActiveCount
		{
			get
			{
				return Collectible.Collectibles.Count(x => !x.PickedUp);
			}
		}

		public override void Awake()
		{
			base.Awake();
			Collectible.Collectibles.Add(this);

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

					PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();
					playerData.Collectibles.Value++;
					SteamWorker.SetStat("stat_orbs_collected", playerData.Collectibles);
					if (SteamWorker.GetStat("stat_orbs_collected") == playerData.Collectibles)
						SteamWorker.IndicateAchievementProgress("cheevo_orbs", (uint)playerData.Collectibles.Value, (uint)totalCollectibles);

					int collected = Collectible.Collectibles.Count(x => x.PickedUp);
					int total = Collectible.Collectibles.Count;

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
			Collectible.Collectibles.Remove(this);
		}
	}
}