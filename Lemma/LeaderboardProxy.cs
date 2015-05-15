using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Steamworks;

namespace Lemma
{
	public class LeaderboardProxy
	{
		public Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t> OnLeaderboardSync = new Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t>();

		public Command OnLeaderboardError = new Command();

#if STEAMWORKS
		private Callback<PersonaStateChange_t> personaCallback;
		private CallResult<LeaderboardFindResult_t> leaderboardFindCall;
		private CallResult<LeaderboardScoreUploaded_t> leaderboardUploadCall;
		private CallResult<LeaderboardScoresDownloaded_t> globalLeaderboardDownloadCall;
		private bool globalScoresDownloaded;
		private LeaderboardScoresDownloaded_t globalScores;
		private CallResult<LeaderboardScoresDownloaded_t> friendLeaderboardDownloadCall;
		private bool friendScoresDownloaded;
		private LeaderboardScoresDownloaded_t friendScores;
#endif

		public Property<bool> PersonaNotification = new Property<bool>();

		private int before, after, friendsBefore, friendsAfter;

		public LeaderboardProxy(int before = 5, int after = 5, int friendsBefore = 5, int friendsAfter = 5)
		{
#if STEAMWORKS
			this.personaCallback = Callback<PersonaStateChange_t>.Create(this.onPersonaStateChange);
#endif
			this.before = before;
			this.after = after;
			this.friendsBefore = friendsBefore;
			this.friendsAfter = friendsAfter;
		}

		public void Unregister()
		{
			this.CancelCallbacks();
#if STEAMWORKS
			if (this.personaCallback != null)
			{
				this.personaCallback.Unregister();
				this.personaCallback = null;
			}
#endif
		}

		public void CancelCallbacks()
		{
#if STEAMWORKS
			if (this.leaderboardFindCall != null)
				this.leaderboardFindCall.Cancel();
			this.leaderboardFindCall = null;
			if (this.leaderboardUploadCall != null)
				this.leaderboardUploadCall.Cancel();
			this.leaderboardUploadCall = null;
			if (this.globalLeaderboardDownloadCall != null)
				this.globalLeaderboardDownloadCall.Cancel();
			this.globalLeaderboardDownloadCall = null;
			if (this.friendLeaderboardDownloadCall != null)
				this.friendLeaderboardDownloadCall.Cancel();
			this.friendLeaderboardDownloadCall = null;
			this.friendScoresDownloaded = false;
			this.globalScoresDownloaded = false;
#endif
		}

		private void onPersonaStateChange(PersonaStateChange_t persona)
		{
			this.PersonaNotification.Changed();
		}

		private void checkLeaderboardsDownloaded()
		{
#if STEAMWORKS
			if (this.globalScoresDownloaded && this.friendScoresDownloaded)
				this.OnLeaderboardSync.Execute(this.globalScores, this.friendScores);
#endif
		}

		public void Sync(string uuid, int score = 0)
		{
#if STEAMWORKS
			this.CancelCallbacks();

			if (!SteamWorker.SteamInitialized)
			{
				this.OnLeaderboardError.Execute();
				return;
			}

			this.leaderboardFindCall = new CallResult<LeaderboardFindResult_t>((found, foundFailure) =>
			{
				this.leaderboardFindCall = null;
				if (foundFailure)
					this.OnLeaderboardError.Execute();
				else
				{
					if (score > 0)
					{
						this.leaderboardUploadCall = new CallResult<LeaderboardScoreUploaded_t>(delegate(LeaderboardScoreUploaded_t uploaded, bool uploadedFailure)
						{
							this.leaderboardUploadCall = null;
							if (uploadedFailure)
								this.OnLeaderboardError.Execute();
							else
								this.download(found.m_hSteamLeaderboard);
						});
						this.leaderboardUploadCall.Set(SteamUserStats.UploadLeaderboardScore(found.m_hSteamLeaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, new int[] {}, 0));
					}
					else
						this.download(found.m_hSteamLeaderboard);
				}
			});
			this.leaderboardFindCall.Set(SteamUserStats.FindOrCreateLeaderboard(uuid, ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds));
#endif
		}

		private void download(SteamLeaderboard_t leaderboard)
		{
#if STEAMWORKS
			this.globalLeaderboardDownloadCall = new CallResult<LeaderboardScoresDownloaded_t>((downloaded, downloadedFailure) =>
			{
				this.globalLeaderboardDownloadCall = null;
				if (downloadedFailure)
					this.OnLeaderboardError.Execute();
				else
				{
					if (downloaded.m_cEntryCount == 0)
					{
						// We're not ranked
						// Get the top global list
						this.globalLeaderboardDownloadCall = new CallResult<LeaderboardScoresDownloaded_t>((downloaded2, downloadedFailure2) =>
						{
							if (downloadedFailure2)
								this.OnLeaderboardError.Execute();
							else
							{
								this.globalScoresDownloaded = true;
								this.globalScores = downloaded2;
								this.checkLeaderboardsDownloaded();
							}
						});
						this.globalLeaderboardDownloadCall.Set(SteamUserStats.DownloadLeaderboardEntries(leaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, this.before + this.after));
					}
					else
					{
						this.globalScoresDownloaded = true;
						this.globalScores = downloaded;
						this.checkLeaderboardsDownloaded();
					}
				}
			});
			this.globalLeaderboardDownloadCall.Set(SteamUserStats.DownloadLeaderboardEntries(leaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -this.before, this.after));
			this.friendLeaderboardDownloadCall = new CallResult<LeaderboardScoresDownloaded_t>((downloaded, downloadedFailure) =>
			{
				this.friendLeaderboardDownloadCall = null;
				if (downloadedFailure)
					this.OnLeaderboardError.Execute();
				else
				{
					this.friendScoresDownloaded = true;
					this.friendScores = downloaded;
					this.checkLeaderboardsDownloaded();
				}
			});
			this.friendLeaderboardDownloadCall.Set(SteamUserStats.DownloadLeaderboardEntries(leaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends, -this.friendsBefore, this.friendsAfter));
#endif
		}
	}
}