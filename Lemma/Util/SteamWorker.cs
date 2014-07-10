using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Lemma.Console;
using Steamworks;

namespace Lemma.Util
{
	public static class SteamWorker
	{
		public static DirectoryInfo DownloadedMaps
		{
			get
			{
				string completedDirectory = Directory.GetCurrentDirectory() + "\\WSFiles\\";
				if (!Directory.Exists(completedDirectory)) Directory.CreateDirectory(completedDirectory);
				return new DirectoryInfo(completedDirectory);
			}
		}

		private static uint ugcPage = 0;
		private static UGCHandle_t ugcHandle;

		private static Dictionary<string, bool> _achievementDictionary;
		private static Dictionary<string, int> _statDictionary;

		private static DateTime _statsLastUploaded = DateTime.Now;

		private static bool _anythingChanged = false;

		public static bool SteamInitialized { get; private set; }

		public static bool StatsInitialized { get; private set; }

		public static Property<bool> OverlayActive = new Property<bool>() { Value = false };

		public static bool OverlaySafelyGone
		{
			get { return _overlayTimer <= 0; }
		}

		private static float _overlayTimer = 0f;

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
			RemoveWSTemp();
			new Callback<SteamUGCQueryCompleted_t>(OnUGCQueryReturn);
			new Callback<UserStatsReceived_t>(OnUserStatsReceived);
			new Callback<RemoteStoragePublishedFileSubscribed_t>(OnSubscribed);
			new Callback<GameOverlayActivated_t>(OnOverlayActivated);

			QuerySubscribed();
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

		public static void Update(float dt)
		{
#if STEAMWORKS
			if (SteamInitialized)
				SteamAPI.RunCallbacks();
			if (_overlayTimer > 0)
				_overlayTimer -= dt;
#endif
		}


		public static void RemoveWSTemp()
		{
			string tempDirectory = Directory.GetCurrentDirectory() + "\\WSTemp\\";
			if (!Directory.Exists(tempDirectory))
			{
				Directory.CreateDirectory(tempDirectory);
			}
			foreach (var dir in Directory.GetDirectories(tempDirectory))
			{
				Directory.Delete(dir, true);
			}
			foreach (var file in Directory.GetFiles(tempDirectory))
			{
				File.Delete(file);
			}
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

		public static void UpdateWorkshopMap(PublishedFileId_t workshop, string pchFile, Action<bool> onDone)
		{
			var updateHandle = SteamRemoteStorage.CreatePublishedFileUpdateRequest(workshop);
			SteamRemoteStorage.UpdatePublishedFileFile(updateHandle, pchFile);
			var call = SteamRemoteStorage.CommitPublishedFileUpdate(updateHandle);
			new CallResult<RemoteStorageUpdatePublishedFileResult_t>((result, failure) =>
			{
				if (onDone == null) return;
				onDone(result.m_eResult == EResult.k_EResultOK && !failure);
			}, call);
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


		public static Property<int> Downloading = new Property<int>();
		public static Command OnLevelDownloaded = new Command();
		private static void DownloadLevel(PublishedFileId_t file)
		{
			var publishedCall = SteamRemoteStorage.GetPublishedFileDetails(file, 0);

			string tempDirectory = Directory.GetCurrentDirectory() + "\\WSTemp\\";
			string completedDirectory = Directory.GetCurrentDirectory() + "\\WSFiles\\";
			if (!Directory.Exists(tempDirectory)) Directory.CreateDirectory(tempDirectory);
			if (!Directory.Exists(completedDirectory)) Directory.CreateDirectory(completedDirectory);

			string tempDirectoryNew = tempDirectory + file.m_PublishedFileId + "\\";
			string completedDirectoryNew = completedDirectory + file.m_PublishedFileId + "\\";

			MapManifest checkAgainst = null;
			if (Directory.Exists(completedDirectoryNew))
			{
				string mapPath = completedDirectoryNew + file.m_PublishedFileId + ".map";
				checkAgainst = MapManifest.FromAbsolutePath(mapPath);
				if (checkAgainst == null)
				{
					Directory.Delete(completedDirectoryNew, true);
				}
			}

			bool doImg = checkAgainst == null;
			if (!Directory.Exists(tempDirectoryNew)) Directory.CreateDirectory(tempDirectoryNew);
			if (!Directory.Exists(completedDirectoryNew)) Directory.CreateDirectory(completedDirectoryNew);

			new CallResult<RemoteStorageGetPublishedFileDetailsResult_t>((t, failure) =>
			{
				if (!failure && t.m_eResult == EResult.k_EResultOK)
				{
					if (checkAgainst != null)
					{
						if (!String.Equals(checkAgainst.MapPchName, t.m_pchFileName))
						{
							File.Delete(completedDirectoryNew + file.m_PublishedFileId + ".map");
						}
						else
						{
							return;
						}
					}
					Downloading.Value++;

					string mapFileTemp = tempDirectoryNew + file.m_PublishedFileId.ToString() + ".map";
					string imageFileTemp = tempDirectoryNew + file.m_PublishedFileId.ToString() + ".png";
					var downloadMapCall = SteamRemoteStorage.UGCDownloadToLocation(t.m_hFile, mapFileTemp, 1);
					new CallResult<RemoteStorageDownloadUGCResult_t>((resultT, ioFailure) =>
					{
						if (ioFailure)
						{
							Downloading.Value = Math.Max(0, Downloading - 1);
						}
						else
						{
							Action<RemoteStorageDownloadUGCResult_t, bool> onImgDownloaded = (resultT2, ioFailure2) =>
							{
								Downloading.Value = Math.Max(0, Downloading - 1);
								if (!ioFailure2)
								{
									string mapFileNew = mapFileTemp.Replace(tempDirectoryNew, completedDirectoryNew);
									File.Move(mapFileTemp, mapFileNew);
									if (doImg)
									{
										string imageFileNew = imageFileTemp.Replace(tempDirectoryNew, completedDirectoryNew);
										File.Move(imageFileTemp, imageFileNew);
									}
									MapManifest manifest = MapManifest.FromAbsolutePath(mapFileNew);
									manifest.MapName = t.m_rgchTitle;
									manifest.MapPchName = resultT.m_pchFileName;
									manifest.Save();
									OnLevelDownloaded.Execute();
								}
							};
							if (doImg)
							{
								var downloadImageCall = SteamRemoteStorage.UGCDownloadToLocation(t.m_hPreviewFile, imageFileTemp, 1);
								new CallResult<RemoteStorageDownloadUGCResult_t>(
									(ugcResultT, bIoFailure) => onImgDownloaded(ugcResultT, bIoFailure), downloadImageCall);
							}
							else
							{
								onImgDownloaded(new RemoteStorageDownloadUGCResult_t(), false);
							}
						}
					}, downloadMapCall);
				}
			}, publishedCall);
		}

		private static void QuerySubscribed()
		{
			ugcPage++;

			var query = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
			   EUserUGCList.k_EUserUGCList_Subscribed, EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame,
			   EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc, new AppId_t(300340), new AppId_t(300340),
			   ugcPage);

			var call = SteamUGC.SendQueryUGCRequest(query);

			new CallResult<SteamUGCQueryCompleted_t>((t, failure) =>
			{
				OnUGCQueryReturn(t);
			}, call);

		}

		public static void GetCreatedWorkShopEntries(Action<IEnumerable<SteamUGCDetails_t>> onResult)
		{
			var query = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
			   EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame,
			   EUserUGCListSortOrder.k_EUserUGCListSortOrder_LastUpdatedDesc, new AppId_t(300340), new AppId_t(300340),
			   1);

			var call = SteamUGC.SendQueryUGCRequest(query);

			new CallResult<SteamUGCQueryCompleted_t>((t, failure) =>
			{
				if (onResult == null)
					return;

				List<SteamUGCDetails_t> ret = new List<SteamUGCDetails_t>();

				for (uint i = 0; i < t.m_unNumResultsReturned; i++)
				{
					var deets = new SteamUGCDetails_t();
					if (SteamUGC.GetQueryUGCResult(t.m_handle, i, ref deets))
					{
						if (deets.m_nConsumerAppID.m_AppId == 300340 && deets.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCommunity)
						{
							ret.Add(deets);
						}
					}
				}

				onResult(ret);
			}, call);
		}

		#region Callbacks

		private static void OnOverlayActivated(GameOverlayActivated_t callback)
		{
			OverlayActive.Value = callback.m_bActive != 0;
			if (!OverlayActive) _overlayTimer = 0.2f;
		}

		private static List<string> directories = new List<string>();
		private static void OnUGCQueryReturn(SteamUGCQueryCompleted_t handle)
		{
			if (ugcPage == 1)
			{
				directories.Clear();
				foreach (var s in SteamWorker.DownloadedMaps.GetDirectories())
				{
					directories.Add(s.Name);
				}
			}

			int blah = 0;
			for (uint i = 0; i < handle.m_unNumResultsReturned; i++)
			{
				var deets = new SteamUGCDetails_t();
				if (SteamUGC.GetQueryUGCResult(handle.m_handle, i, ref deets))
				{
					directories.Remove(deets.m_nPublishedFileId.m_PublishedFileId.ToString());
					if (deets.m_nConsumerAppID.m_AppId == 300340 && deets.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCommunity)
					{
						DownloadLevel(deets.m_nPublishedFileId);
					}
				}
			}
			if (handle.m_unTotalMatchingResults > handle.m_unNumResultsReturned && handle.m_unNumResultsReturned != 0)
			{
				QuerySubscribed();
			}
			else
			{
				//This whole ordeal deletes folders in here that are not currently-subscribed workshop maps.
				foreach (var dir in directories)
				{
					Directory.Delete(DownloadedMaps.FullName + dir, true);
				}
			}

		}

		private static void OnSubscribed(RemoteStoragePublishedFileSubscribed_t subscribed)
		{
			if (!SteamInitialized) return;

			//Because Steam can be an idiot sometimes!
			if (subscribed.m_nAppID.m_AppId != 300340) return;

			DownloadLevel(subscribed.m_nPublishedFileId);
		}

		private static void OnUserStatsReceived(UserStatsReceived_t pCallback)
		{
			if (!SteamInitialized) return;

			if (pCallback.m_nGameID != 300340 || pCallback.m_eResult != EResult.k_EResultOK) return;

			//I'm sorry, Evan. We'll need to find somewhere nice to put this part.
			string[] cheevoNames = new string[] { "perverse", "win_the_game", "100_blocks", "250_blocks",
				"500_blocks", "1000_blocks", "10000_blocks", "cheating_jerk", "5_minutes_played", 
				"30_minutes_played", "120_minutes_played", "300_minutes_played", "600_minutes_played", "pillar_crushed" };
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
