using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lemma.Console;
using Steamworks;

namespace Lemma.Util
{
	public static class SteamWorker
	{
		private static Dictionary<string, bool> _achievementDictionary;
		private static Dictionary<string, int> _statDictionary;

		private static DateTime _statsLastUploaded = DateTime.Now;

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
			_statDictionary = new Dictionary<string, int>();

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
			catch (DllNotFoundException)
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
			if (SteamInitialized)
				SteamAPI.RunCallbacks();
#endif
		}

		public static bool WriteFileUGC(string path, string steamPath)
		{
			if (!SteamInitialized) return false;
			if (!File.Exists(path)) return false;
			byte[] data = File.ReadAllBytes(path);
			return SteamRemoteStorage.FileWrite(steamPath, data, data.Length);
		}

		public static void ShareFileUGC(string path, Action<bool, UGCHandle_t> onShare = null)
		{
			new CallResult<RemoteStorageFileShareResult_t>((result, failure) =>
			{
				if (result.m_eResult == EResult.k_EResultOK)
				{
					if (onShare != null)
						onShare(true, result.m_hFile);
				}
				else
				{
					if (onShare != null)
						onShare(false, result.m_hFile);
				}
			}, SteamRemoteStorage.FileShare(path));

		}

		public static void UploadWorkShop(string mapFile, string imageFile, string title, string description, Action<bool, bool, PublishedFileId_t> onDone)
		{
			var call = SteamRemoteStorage.PublishWorkshopFile(mapFile, imageFile, new AppId_t(300340), title, description,
				ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic, new List<string>(),
				EWorkshopFileType.k_EWorkshopFileTypeCommunity);

			new CallResult<RemoteStoragePublishFileResult_t>((result, failure) =>
			{
				if (result.m_eResult == EResult.k_EResultOK)
				{
					onDone(true, result.m_bUserNeedsToAcceptWorkshopLegalAgreement, result.m_nPublishedFileId);
				}
				else
				{
					onDone(false, result.m_bUserNeedsToAcceptWorkshopLegalAgreement, result.m_nPublishedFileId);
				}
			}, call);

		}

		[AutoConCommand("set_stat", "Set a Steam stat")]
		public static void SetStat(string name, int newVal)
		{
			if (!Initialized) return;
			if (!_statDictionary.ContainsKey(name)) return;

			var curVal = _statDictionary[name];
			if (curVal == newVal) return;
			_statDictionary[name] = newVal;
			SteamUserStats.SetStat(name, newVal);
			_anythingChanged = true;

			if ((DateTime.Now - _statsLastUploaded).TotalSeconds >= 5)
				UploadStats();
		}

		public static void IncrementStat(string name, int increment)
		{
			if (!Initialized) return;
			if (!_statDictionary.ContainsKey(name)) return;
			SetStat(name, GetStat(name) + increment);
		}

		public static bool IsStat(string name)
		{
			if (!Initialized) return false;
			return _statDictionary.ContainsKey(name);
		}

		public static int GetStat(string name)
		{
			if (!Initialized) return 0;
			if (!_statDictionary.ContainsKey(name)) return 0;
			return _statDictionary[name];
		}

		public static void SetAchievement(string name, bool forceUpload = true)
		{
			if (!Initialized) return;
			if (!_achievementDictionary.ContainsKey(name)) return;
			if (_achievementDictionary[name]) return; //No use setting an already-unlocked cheevo.
			_achievementDictionary[name] = true;
			SteamUserStats.SetAchievement(name);
			_anythingChanged = true;
			if (forceUpload)
				UploadStats();
		}

		[AutoConCommand("upload_stats", "Upload stats to Steam")]
		public static void UploadStats(bool force = false)
		{
			if (!Initialized) return;
			if (!_anythingChanged && !force) return;
			SteamUserStats.StoreStats();
			_anythingChanged = false;
			_statsLastUploaded = DateTime.Now;
		}

		[AutoConCommand("reset_stats", "Reset all stats.")]
		public static void ResetAllStats(bool andCheevos = true)
		{
			if (!SteamInitialized) return;
			SteamUserStats.ResetAllStats(andCheevos);
			SteamUserStats.RequestCurrentStats();

			_achievementDictionary = new Dictionary<string, bool>();
			_statDictionary = new Dictionary<string, int>();
			StatsInitialized = false;
		}

		#region Callbacks

		private static void OnPublishedFile(RemoteStoragePublishFileResult_t result)
		{

		}

		private static void OnSharedFile(RemoteStorageFileShareResult_t result)
		{

		}

		private static void OnUserStatsReceived(UserStatsReceived_t pCallback)
		{
			if (!SteamInitialized) return;

			if (pCallback.m_nGameID != 300340 || pCallback.m_eResult != EResult.k_EResultOK) return;

			//I'm sorry, Evan. We'll need to find somewhere nice to put this part.
			string[] cheevoNames = new string[] { "perverse", "win_the_game", "100_blocks", "250_blocks",
				"500_blocks", "1000_blocks", "10000_blocks", "cheating_jerk", "5_minutes_played", 
				"30_minutes_played", "120_minutes_played", "300_minutes_played", "600_minutes_played" };
			string[] statNames = new string[] { "time_played", "blocks_created", "distance_traveled", "times_died", "time_perverse", "orbs_collected" };

			foreach (var cheevo in cheevoNames)
			{
				bool value;
				bool success = SteamUserStats.GetAchievement("cheevo_" + cheevo, out value);
				if (success)
					_achievementDictionary.Add("cheevo_" + cheevo, value);
			}

			foreach (var stat in statNames)
			{
				int value;
				bool success = SteamUserStats.GetStat("stat_" + stat, out value);
				if (success)
				{
					_statDictionary.Add("stat_" + stat, value);
					if (stat == "time_played")
					{
						Main.TotalGameTime.Value = value;
					}
				}
			}
			StatsInitialized = true;
		}
		#endregion

	}
}
