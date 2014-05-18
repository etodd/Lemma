using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace Lemma.Util
{
	public static class SteamWorker
	{
		private static Dictionary<string, bool> _achievementDictionary;
		private static Dictionary<string, float> _statDictionary;

		private static bool _anythingChanged = false;



		public static bool SteamInitialized { get; private set; }

		public static bool StatsInitialized { get; private set; }

		public static bool Initialized
		{
			get
			{
				return SteamInitialized && StatsInitialized;
			}
		}

		private static bool Init_SteamGame()
		{
			//Getting here means Steamworks MUST have initialized successfully. Oh, && operator!

			new Callback<UserStatsReceived_t>(OnUserStatsReceived);
			if (!SteamUserStats.RequestCurrentStats()) return false;
			_achievementDictionary = new Dictionary<string, bool>();
			_statDictionary = new Dictionary<string, float>();

			return true;
		}

		public static bool Init()
		{
			try
			{
#if STEAMWORKS
				return SteamInitialized = (SteamAPI.Init() && Init_SteamGame());
#else
				return (SteamInitialized = false) && false;
#endif
			}
			catch (DllNotFoundException exception)
			{
				//Required DLLs ain't there
				SteamInitialized = false;
				return false;
			}
		}

		public static void Shutdown()
		{
#if STEAMWORKS
			SteamAPI.Shutdown();
#endif
		}

		public static void Update()
		{
#if STEAMWORKS
			SteamAPI.RunCallbacks();
#endif
		}

		public static void SetStat(string name, float newVal)
		{
			if (!Initialized) return;
			if (!_statDictionary.ContainsKey(name)) return;
			_statDictionary[name] = newVal;
			SteamUserStats.SetStat(name, newVal);
			_anythingChanged = true;
		}

		public static void IncrementStat(string name, float increment)
		{
			if (!Initialized) return;
			if (!_statDictionary.ContainsKey(name)) return;
			SetStat(name, GetStat(name) + increment);
		}

		public static float GetStat(string name)
		{
			if (!Initialized) return 0;
			if (!_statDictionary.ContainsKey(name)) return 0;
			return _statDictionary[name];
		}

		public static void SetAchievement(string name, bool forceUpdate = true)
		{
			if (!Initialized) return;
			if (!_achievementDictionary.ContainsKey(name)) return;
			_achievementDictionary[name] = true;
			SteamUserStats.SetAchievement(name);
			_anythingChanged = true;
			if (forceUpdate)
				UploadStats();
		}

		public static void UploadStats(bool force = false)
		{
			if (!Initialized) return;
			if (!_anythingChanged && !force) return;
			SteamUserStats.StoreStats();
			_anythingChanged = false;
		}

		#region Callbacks
		private static void OnUserStatsReceived(UserStatsReceived_t pCallback)
		{
			if (!SteamInitialized) return;

			if (pCallback.m_nGameID != 30034 || pCallback.m_eResult != EResult.k_EResultOK) return;

			//I'm sorry, Evan. We'll need to find somewhere nice to put this part.
			string[] cheevoNames = new string[] { "perverse", "win_the_game", "100_blocks", "250_blocks",
				"500_blocks", "1000_blocks", "10000_blocks", "cheating_jerk", "5_minutes_played", 
				"30_minutes_played", "120_minutes_played", "300_minutes_played", "600_minutes_played" };
			string[] statNames = new string[] { "time_played", "blocks_created", "distance_traveled", "times_died", "time_perverse" };

			foreach (var cheevo in cheevoNames)
			{
				bool value;
				bool success = SteamUserStats.GetAchievement("cheevo_" + cheevo, out value);
				if (success)
					_achievementDictionary.Add("cheevo_" + cheevo, value);
			}

			foreach (var stat in statNames)
			{
				float value;
				bool success = SteamUserStats.GetStat("stat_" + stat, out value);
				if (success)
					_statDictionary.Add("stat_" + stat, value);
			}
			StatsInitialized = true;
		}
		#endregion

	}
}
