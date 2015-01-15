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
		public static void Run(Entity script)
		{
			PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();

			script.Add(new TwoWayBinding<bool>(property<bool>(script, "Roll"), playerData.EnableCrouch));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "Crouch"), playerData.EnableCrouch));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "Kick"), playerData.EnableKick));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "WallRun"), playerData.EnableWallRun));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "WallRunHorizontal"), playerData.EnableWallRunHorizontal));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "EnhancedWallRun"), playerData.EnableEnhancedWallRun));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "Moves"), playerData.EnableMoves));
			script.Add(new TwoWayBinding<bool>(property<bool>(script, "Phone"), PlayerDataFactory.Instance.Get<Phone>().Enabled));
			playerData.MaxSpeed.Value = Character.DefaultMaxSpeed;
		}

		public static IEnumerable<string> EditorProperties(Entity script)
		{
			script.Add("Roll", property<bool>(script, "Roll"));
			script.Add("Crouch", property<bool>(script, "Crouch"));
			script.Add("Kick", property<bool>(script, "Kick"));
			script.Add("WallRun", property<bool>(script, "WallRun"));
			script.Add("WallRunHorizontal", property<bool>(script, "WallRunHorizontal"));
			script.Add("EnhancedWallRun", property<bool>(script, "EnhancedWallRun"));
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
				"Moves",
				"Phone",
			};
		}
	}
}