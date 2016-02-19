using ComponentBind;
using System;
using Lemma.Factories;
using Microsoft.Xna.Framework;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Lemma.Components
{
	public class TimeTrialUI : Component<Main>, IUpdateableComponent
	{
		// Input properties
		public Property<float> ElapsedTime = new Property<float>();
		public Property<string> NextMap = new Property<string>();
		[XmlIgnore]
		public Property<float> BestTime = new Property<float>();

		private Property<float> verticalSpeed = new Property<float>();
		private Property<float> horizontalSpeed = new Property<float>();

		// Output commands
		[XmlIgnore]
		public Command Retry = new Command();

		[XmlIgnore]
		public Command MainMenu = new Command();

		[XmlIgnore]
		public Command Edit = new Command();

		[XmlIgnore]
		public Command LoadNextMap = new Command();

		// Input commands

		[XmlIgnore]
		public Command Show = new Command();

		[XmlIgnore]
		public Command ShowEnd = new Command();

		[XmlIgnore]
		public Command OnLeaderboardError = new Command();

		[XmlIgnore]
		public Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t> OnLeaderboardSync = new Command<LeaderboardScoresDownloaded_t, LeaderboardScoresDownloaded_t>();
		public Property<bool> PersonaNotification = new Property<bool>();

		private bool shown;

		private float width
		{
			get
			{
				return 400.0f * this.main.FontMultiplier;
			}
		}

		private float spacing
		{
			get
			{
				return 8.0f * this.main.FontMultiplier;
			}
		}

		public override void Awake()
		{
			this.Serialize = false;
			this.EnabledWhenPaused = false;

			{
				Container container = this.main.UIFactory.CreateContainer();
				container.Opacity.Value = UIFactory.Opacity;
				container.PaddingBottom.Value = container.PaddingLeft.Value = container.PaddingRight.Value = container.PaddingTop.Value = 16.0f * this.main.FontMultiplier;
				container.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
				bool vr = false;
#if VR
				vr = this.main.VR;
#endif
				container.Add(new Binding<Vector2, Point>(container.Position, x => new Vector2(x.X * 0.9f, x.Y * (vr ? 0.3f : 0.1f)), this.main.ScreenSize));
				container.Visible.Value = false;
				this.main.UI.Root.Children.Add(container);
				container.Add(new CommandBinding(this.Delete, container.Delete));
				container.Add(new CommandBinding(this.ShowEnd, container.Delete));
				container.Add(new CommandBinding(this.Show, delegate() { container.Visible.Value = true; }));

				ListContainer list = new ListContainer();
				list.Orientation.Value = ListContainer.ListOrientation.Vertical;
				list.Alignment.Value = ListContainer.ListAlignment.Max;
				container.Children.Add(list);

				TextElement elapsedTime = new TextElement();
				elapsedTime.FontFile.Value = this.main.FontLarge;
				elapsedTime.Add(new Binding<string, float>(elapsedTime.Text, SecondsToTimeString, this.ElapsedTime));
				list.Children.Add(elapsedTime);

				TextElement vertSpeed = this.main.UIFactory.CreateLabel();
				vertSpeed.Add(new Binding<string, float>(vertSpeed.Text, x => x.ToString("00.00"), this.verticalSpeed));
				list.Children.Add(vertSpeed);

				TextElement horizontalSpeed = main.UIFactory.CreateLabel();
				horizontalSpeed.Add(new Binding<string, float>(horizontalSpeed.Text, x => x.ToString("00.00"), this.horizontalSpeed));
				list.Children.Add(horizontalSpeed);

				TextElement bestTime = this.main.UIFactory.CreateLabel();
				bestTime.Add(new Binding<string, float>(bestTime.Text, SecondsToTimeString, this.BestTime));
				list.Children.Add(bestTime);
			}

			this.ShowEnd.Action = delegate()
			{
				if (this.shown)
					return;
				this.shown = true;

				Container container = this.main.UIFactory.CreateContainer();
				container.Opacity.Value = UIFactory.Opacity;
				container.PaddingBottom.Value = container.PaddingLeft.Value = container.PaddingRight.Value = container.PaddingTop.Value = 16.0f * this.main.FontMultiplier;
				container.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				container.Add(new Binding<Vector2, Point>(container.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), this.main.ScreenSize));
				this.main.UI.Root.Children.Add(container);
				container.Add(new CommandBinding(this.Delete, container.Delete));

				ListContainer list = new ListContainer();
				list.Orientation.Value = ListContainer.ListOrientation.Vertical;
				list.Alignment.Value = ListContainer.ListAlignment.Middle;
				list.Spacing.Value = this.spacing;
				container.Children.Add(list);

				TextElement elapsedTime = new TextElement();
				elapsedTime.FontFile.Value = this.main.FontLarge;
				elapsedTime.Add(new Binding<string, float>(elapsedTime.Text, SecondsToTimeString, this.ElapsedTime));
				list.Children.Add(elapsedTime);

				TextElement bestTime = this.main.UIFactory.CreateLabel();
				bestTime.Add(new Binding<string>(bestTime.Text, () => string.Format(main.Strings.Get("best time"), SecondsToTimeString(this.BestTime)), this.BestTime, main.Strings.Language));
				list.Children.Add(bestTime);

#if STEAMWORKS
				Container leaderboard = this.main.UIFactory.CreateContainer();
				this.resizeContainer(leaderboard);
				list.Children.Add(leaderboard);

				ListContainer leaderboardList = new ListContainer();
				leaderboardList.ResizePerpendicular.Value = false;
				leaderboardList.Size.Value = new Vector2(this.width - 8.0f, 0);
				leaderboardList.Orientation.Value = ListContainer.ListOrientation.Vertical;
				leaderboardList.Alignment.Value = ListContainer.ListAlignment.Middle;
				leaderboardList.Spacing.Value = 0;

				Scroller scroller = new Scroller();
				scroller.Children.Add(leaderboardList);
				scroller.Add(new Binding<Vector2>(scroller.Size, () => new Vector2(leaderboardList.Size.Value.X, this.main.ScreenSize.Value.Y * 0.5f), leaderboardList.Size, this.main.ScreenSize));
				leaderboard.Children.Add(scroller);

				{
					TextElement leaderboardLabel = this.main.UIFactory.CreateLabel();
					leaderboardLabel.Text.Value = "\\leaderboard";
					leaderboardList.Children.Add(leaderboardLabel);
					TextElement loading = this.main.UIFactory.CreateLabel();
					loading.Text.Value = "\\loading";
					leaderboardList.Children.Add(loading);
				}

				this.OnLeaderboardSync.Action = delegate(LeaderboardScoresDownloaded_t globalScores, LeaderboardScoresDownloaded_t friendScores)
				{
					leaderboardList.Children.Clear();

					{
						TextElement leaderboardLabel = this.main.UIFactory.CreateLabel("\\leaderboard");
						Container labelContainer = this.main.UIFactory.CreateContainer();
						leaderboardList.Children.Add(labelContainer);
						labelContainer.Children.Add(leaderboardLabel);
					}

					int[] details = new int[] {};
					for (int i = 0; i < globalScores.m_cEntryCount; i++)
					{
						LeaderboardEntry_t entry;
						SteamUserStats.GetDownloadedLeaderboardEntry(globalScores.m_hSteamLeaderboardEntries, i, out entry, details, 0);
						leaderboardList.Children.Add(this.leaderboardEntry(entry));
					}

					if (friendScores.m_cEntryCount > 1)
					{
						{
							TextElement friendsLabel = this.main.UIFactory.CreateLabel("\\friends");
							Container labelContainer = this.main.UIFactory.CreateContainer();
							leaderboardList.Children.Add(labelContainer);
							labelContainer.Children.Add(friendsLabel);
						}

						for (int i = 0; i < friendScores.m_cEntryCount; i++)
						{
							LeaderboardEntry_t entry;
							SteamUserStats.GetDownloadedLeaderboardEntry(friendScores.m_hSteamLeaderboardEntries, i, out entry, details, 0);
							leaderboardList.Children.Add(this.leaderboardEntry(entry));
						}
					}
				};
				
				this.OnLeaderboardError.Action = delegate()
				{
					leaderboardList.Children.Clear();
					TextElement error = this.main.UIFactory.CreateLabel();
					error.Text.Value = "\\leaderboard error";
					leaderboardList.Children.Add(error);
				};
#endif

				Container retry = this.main.UIFactory.CreateButton("\\retry", delegate()
				{
					this.Retry.Execute();
				});
				this.resizeButton(retry);
				list.Children.Add(retry);

				if (this.main.Settings.GodModeProperty || Path.GetDirectoryName(this.main.MapFile) == this.main.CustomMapDirectory)
				{
					Container edit = this.main.UIFactory.CreateButton("\\edit mode", delegate()
					{
						this.Edit.Execute();
					});
					this.resizeButton(edit);
					list.Children.Add(edit);
				}

				if (!string.IsNullOrEmpty(this.NextMap))
				{
					Container next = this.main.UIFactory.CreateButton("\\next level", delegate()
					{
						this.LoadNextMap.Execute();
					});
					this.resizeButton(next);
					list.Children.Add(next);
				}

				{
					Container challenge = this.main.UIFactory.CreateButton("\\challenge levels", delegate()
					{
						this.MainMenu.Execute();
						this.main.Menu.ShowChallengeMenu();
					});
					this.resizeButton(challenge);
					list.Children.Add(challenge);
				}

				Container mainMenu = this.main.UIFactory.CreateButton("\\main menu", delegate()
				{
					this.MainMenu.Execute();
				});
				this.resizeButton(mainMenu);
				list.Children.Add(mainMenu);

				this.main.UI.IsMouseVisible.Value = true;

				const float gamepadMoveInterval = 0.1f;
				float lastGamepadMove = 0.0f;

				Func<int, int, int> nextButton = delegate(int search, int dir)
				{
					int i = search;
					while (true)
					{
						i = i + dir;
						if (i < 0)
							i = list.Children.Count - 1;
						else if (i >= list.Children.Count)
							i = 0;
						UIComponent item = list.Children[i];
						if (item is Container)
							return i;
					}
				};

				int selected = nextButton(0, 1);
				if (main.GamePadConnected)
					list.Children[selected].Highlighted.Value = true;

				PCInput input = this.Entity.GetOrCreate<PCInput>();
				Action<int> moveSelection = delegate(int delta)
				{
					if (this.main.GameTime.TotalGameTime.TotalSeconds - lastGamepadMove > gamepadMoveInterval)
					{
						Container button;
						if (selected < list.Children.Length)
						{
							button = (Container)list.Children[selected];
							button.Highlighted.Value = false;
						}

						selected = nextButton(selected, delta);

						button = (Container)list.Children[selected];
						button.Highlighted.Value = true;
						lastGamepadMove = (float)this.main.GameTime.TotalGameTime.TotalSeconds;
					}
				};

				input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickUp), delegate()
				{
					moveSelection(-1);
				}));

				input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadUp), delegate()
				{
					moveSelection(-1);
				}));

				input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickDown), delegate()
				{
					moveSelection(1);
				}));

				input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadDown), delegate()
				{
					moveSelection(1);
				}));

				input.Add(new CommandBinding(input.GetButtonDown(Buttons.A), delegate()
				{
					if (selected < list.Children.Count)
					{
						UIComponent selectedItem = list.Children[selected];
						selectedItem.MouseLeftUp.Execute();
					}
				}));
			};
		}

		void IUpdateableComponent.Update(float dt)
		{
			Entity player = PlayerFactory.Instance;
			if (player != null)
			{
				Vector3 v = player.Get<Player>().Character.LinearVelocity.Value;
				this.verticalSpeed.Value = Math.Abs(v.Y);
				v.Y = 0.0f;
				this.horizontalSpeed.Value = v.Length();
			}
		}

#if STEAMWORKS
		private Container leaderboardEntry(LeaderboardEntry_t entry)
		{
			Container container = this.main.UIFactory.CreateButton(delegate()
			{
				if (SteamWorker.SteamInitialized)
					SteamFriends.ActivateGameOverlayToUser("steamid", entry.m_steamIDUser);
			});
			this.resizeContainer(container, 4.0f);

			TextElement rank = this.main.UIFactory.CreateLabel(entry.m_nGlobalRank.ToString());
			rank.AnchorPoint.Value = new Vector2(1, 0);
			rank.Position.Value = new Vector2(this.width * 0.15f, 0);
			container.Children.Add(rank);

			TextElement name = this.main.UIFactory.CreateLabel();
			name.FilterUnicode.Value = true;
			if (SteamFriends.RequestUserInformation(entry.m_steamIDUser, true))
			{
				// Need to wait for a callback before we know their username
				name.Add(new Binding<string>(name.Text, () => SteamFriends.GetFriendPersonaName(entry.m_steamIDUser), this.PersonaNotification));
			}
			else
			{
				// We already know the username
				name.Text.Value = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
			}
			name.Position.Value = new Vector2(this.width * 0.15f + this.spacing, 0);
			container.Children.Add(name);

			TextElement score = this.main.UIFactory.CreateLabel(SecondsToTimeString((float)entry.m_nScore / 1000.0f));
			score.AnchorPoint.Value = new Vector2(1, 0);
			score.Position.Value = new Vector2(this.width - 4.0f - this.spacing, 0);
			container.Children.Add(score);
			return container;
		}
#endif

		private void resizeContainer(Container container, float padding = 0.0f)
		{
			container.ResizeHorizontal.Value = false;
			container.Size.Value = new Vector2(this.width - padding * 2.0f, 0.0f);
		}

		private void resizeButton(Container button, float padding = 0.0f)
		{
			this.resizeContainer(button, padding);
			TextElement label = (TextElement)button.Children[0];
			label.WrapWidth.Value = this.width - padding * 2.0f;
			label.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			label.Add(new Binding<Vector2>(label.Position, x => x * new Vector2(0.5f, 0.5f), button.Size));
		}

		public static string SecondsToTimeString(float seconds)
		{
			if (seconds == 0.0f)
				return "--";

			TimeSpan t = TimeSpan.FromSeconds(seconds);
			return string.Format("{0:D2}:{1:D2}:{2:D3}", 
				t.Minutes, 
				t.Seconds, 
				t.Milliseconds);
		}
	}
}