using System;
using ComponentBind;
using Lemma.GameScripts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Lemma.Components;
using Lemma.Factories;

namespace Lemma.GameScripts
{
	public class test : ScriptBase
	{
		public static Entity script;
		public static void Run()
		{
			GameMain.Config settings = ((GameMain)main).Settings;

			Entity playerData = Factory.Get<PlayerDataFactory>().Instance(main);

			Entity player = null;
			script.Add(new CommandBinding<Entity>(((GameMain)main).PlayerSpawned, delegate(Entity p)
			{
				player = p;
			}));

			Updater playerDamager = new Updater
			{
				delegate(float dt)
				{
					if (player != null && player.Active)
					{
						Player p = player.Get<Player>();
						float y = p.Transform.Value.Translation.Y;
						if (y < -40.0f || (y < -20.0f && p.LinearVelocity.Value.Y < -40.0f))
							player.Delete.Execute();
					}
					else
						player = null;
				}
			};
			script.Add(playerDamager);
		}
	}
}