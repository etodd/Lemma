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
	public class Abilities : ScriptBase
	{
		public static Entity script;

		public static void Run()
		{
			PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();

			playerData.EnableRoll.Value = property<bool>(script, "Roll");
			playerData.EnableCrouch.Value = property<bool>(script, "Crouch");
			playerData.EnableKick.Value = property<bool>(script, "Kick");
			playerData.EnableWallRun.Value = property<bool>(script, "WallRun");
			playerData.EnableWallRunHorizontal.Value = property<bool>(script, "WallRunHorizontal");
			playerData.EnableEnhancedWallRun.Value = property<bool>(script, "EnhancedWallRun");
			playerData.EnableSlowMotion.Value = property<bool>(script, "SlowMotion");
			playerData.EnableMoves.Value = property<bool>(script, "Moves");
			playerData.EnablePhone.Value = property<bool>(script, "Phone");

			script.Delete.Execute();
		}

		public static IEnumerable<string> EditorProperties()
		{
			script.Add("Roll", property<bool>(script, "Roll"));
			script.Add("Crouch", property<bool>(script, "Crouch"));
			script.Add("Kick", property<bool>(script, "Kick"));
			script.Add("WallRun", property<bool>(script, "WallRun"));
			script.Add("WallRunHorizontal", property<bool>(script, "WallRunHorizontal"));
			script.Add("EnhancedWallRun", property<bool>(script, "EnhancedWallRun"));
			script.Add("SlowMotion", property<bool>(script, "SlowMotion"));
			script.Add("Moves", property<bool>(script, "Moves"));
			script.Add("Phone", property<bool>(script, "Phone"));
			return new string[]
			{
				"Roll",
				"Crouch",
				"Kick",
				"WallRun",
				"WallRunHorizontal",
				"EnhancedWallRun",
				"SlowMotion",
				"Moves",
				"Phone",
			};
		}
	}
}