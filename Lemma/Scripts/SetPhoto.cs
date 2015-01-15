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
using Lemma.GInterfaces;

namespace Lemma.GameScripts
{
	public class SetPhoto : ScriptBase
	{
		public static new bool AvailableInReleaseEditor = true;
		public static void Run(Entity script)
		{
			Phone phone = PlayerDataFactory.Instance.Get<Phone>();
			phone.Photo.Value = property<string>(script, "Photo");
			phone.CurrentMode.Value = Phone.Mode.Photos;
			phone.Show.Execute();
			AkSoundEngine.PostEvent(AK.EVENTS.PLAY_CAMERA_SHUTTER, PlayerFactory.Instance);
		}

		public static IEnumerable<string> EditorProperties(Entity script)
		{
			script.Add("Photo", property<string>(script, "Photo"), new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "Images", "Game\\Images" }),
			});
			return new[] { "Photo" };
		}
	}
}