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

		private CallResult<LeaderboardFindResult_t> leaderboardFindCall;
		private CallResult<LeaderboardScoreUploaded_t> leaderboardUploadCall;
		private CallResult<LeaderboardScoresDownloaded_t> globalLeaderboardDownloadCall;
		private bool globalScoresDownloaded;
		private LeaderboardScoresDownloaded_t globalScores;
		private CallResult<LeaderboardScoresDownloaded_t> friendLeaderboardDownloadCall;
		private bool friendScoresDownloaded;
		private LeaderboardScoresDownloaded_t friendScores;

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
#endif
		}

		private void checkLeaderboardsDownloaded()
		{
			if (this.globalScoresDownloaded && this.friendScoresDownloaded)
				this.OnLeaderboardSync.Execute(this.globalScores, this.friendScores);
		}

		public void Sync(string uuid, int score = 0)
		{
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
					this.leaderboardUploadCall = new CallResult<LeaderboardScoreUploaded_t>(delegate(LeaderboardScoreUploaded_t uploaded, bool uploadedFailure)
					{
						this.leaderboardUploadCall = null;
						if (uploadedFailure)
							this.OnLeaderboardError.Execute();
						else
						{
							this.globalLeaderboardDownloadCall = new CallResult<LeaderboardScoresDownloaded_t>((downloaded, downloadedFailure) =>
							{
								this.globalLeaderboardDownloadCall = null;
								if (downloadedFailure)
									this.OnLeaderboardError.Execute();
								else
								{
									this.globalScoresDownloaded = true;
									this.globalScores = downloaded;
									this.checkLeaderboardsDownloaded();
								}
							});
							this.globalLeaderboardDownloadCall.Set(SteamUserStats.DownloadLeaderboardEntries(found.m_hSteamLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -5, 5));
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
							this.friendLeaderboardDownloadCall.Set(SteamUserStats.DownloadLeaderboardEntries(found.m_hSteamLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends, -5, 5));
						}
					});
					this.leaderboardUploadCall.Set(SteamUserStats.UploadLeaderboardScore(found.m_hSteamLeaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, new int[] {}, 0));
				}
			});
			this.leaderboardFindCall.Set(SteamUserStats.FindOrCreateLeaderboard(uuid, ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds));
		}
	}
}