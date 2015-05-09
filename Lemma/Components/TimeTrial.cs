using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.GInterfaces;
using Lemma.Util;
using Steamworks;

namespace Lemma.Components
{
	public class TimeTrial : Component<Main>, IUpdateableComponent
	{
		public Property<string> NextMap = new Property<string>();

		[XmlIgnore]
		public Property<float> ElapsedTime = new Property<float>();

		[XmlIgnore]
		public Command Retry = new Command();

		[XmlIgnore]
		public Property<float> BestTime = new Property<float>();

		[XmlIgnore]
		public Command ShowUI = new Command();

#if STEAMWORKS
		[XmlIgnore]
		public Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t> OnLeaderboardSync = new Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t>();

		[XmlIgnore]
		public Command OnLeaderboardError = new Command();

		private CallResult<LeaderboardFindResult_t> leaderboardFindCall;
		private CallResult<LeaderboardScoreUploaded_t> leaderboardUploadCall;
		private CallResult<LeaderboardScoresDownloaded_t> globalLeaderboardDownloadCall;
		private bool globalScoresDownloaded;
		private LeaderboardScoresDownloaded_t globalScores;
		private CallResult<LeaderboardScoresDownloaded_t> friendLeaderboardDownloadCall;
		private bool friendScoresDownloaded;
		private LeaderboardScoresDownloaded_t friendScores;
#endif

		public override void Awake()
		{
			base.Awake();

			this.EnabledWhenPaused = false;

			this.Retry.Action = this.retry;

			this.Add(new CommandBinding(this.main.Spawner.PlayerSpawned, delegate()
			{
				PlayerFactory.Instance.Add(new CommandBinding(PlayerFactory.Instance.Get<Player>().Die, (Action)this.retryDeath));
			}));

			this.BestTime.Value = this.main.GetMapTime(WorldFactory.Instance.Get<World>().UUID);

			this.Add(new CommandBinding(this.Disable, delegate()
			{
				this.main.BaseTimeMultiplier.Value = 0.0f;
				this.main.Menu.CanPause.Value = false;
				AkSoundEngine.PostEvent(AK.EVENTS.STOP_ALL, this.Entity);
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_MUSIC_STINGER, this.Entity);
				this.BestTime.Value = this.main.SaveMapTime(WorldFactory.Instance.Get<World>().UUID, this.ElapsedTime);
				PlayerFactory.Instance.Get<FPSInput>().Enabled.Value = false;
#if STEAMWORKS
				SteamWorker.IncrementStat("stat_challenge_levels_played", 1);
				this.ShowUI.Execute();
				this.syncLeaderboard();
#endif
			}));
		}

#if STEAMWORKS
		private void cancelCallbacks()
		{
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
		}

		private void syncLeaderboard()
		{
			this.cancelCallbacks();

			if (!SteamWorker.SteamInitialized)
			{
				this.OnLeaderboardError.Execute();
				return;
			}

			string uuid = WorldFactory.Instance.Get<World>().UUID;
			int score = (int)(this.BestTime.Value * 1000.0f);

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

		private void checkLeaderboardsDownloaded()
		{
			if (this.globalScoresDownloaded && this.friendScoresDownloaded)
				this.OnLeaderboardSync.Execute(this.globalScores, this.friendScores);
		}
#endif

		private void retryDeath()
		{
			this.main.Spawner.CanSpawn = false;
			this.Entity.Add(new Animation
			(
				new Animation.Delay(0.5f),
				new Animation.Execute(delegate()
				{
					this.main.CurrentSave.Value = null;
					this.main.EditorEnabled.Value = false;
					IO.MapLoader.Load(this.main, this.main.MapFile);
				})
			));
		}

		private void retry()
		{
			this.main.Spawner.CanSpawn = false;
			this.main.CurrentSave.Value = null;
			this.main.EditorEnabled.Value = false;
			IO.MapLoader.Load(this.main, this.main.MapFile);
		}

		public void Update(float dt)
		{
			this.ElapsedTime.Value += dt;
		}

		public override void delete()
		{
#if STEAMWORKS
			this.cancelCallbacks();
#endif
			base.delete();
			this.main.BaseTimeMultiplier.Value = 1.0f;
			this.main.Menu.CanPause.Value = true;
		}
	}
}