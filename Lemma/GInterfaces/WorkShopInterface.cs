using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using ComponentBind;
using GeeUI.Views;
using ICSharpCode.SharpZipLib.Tar;
using Lemma.Components;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Point = Microsoft.Xna.Framework.Point;
using View = GeeUI.Views.View;

namespace Lemma.GInterfaces
{
	public class WorkShopInterface : Component<Main>
	{
		private View EncompassingView;
		private PanelView MainView;
		private TextFieldView NameView;
		private TextFieldView DescriptionView;
		
		private DropDownView SelectFile;

		private SteamUGCDetails_t currentPublishedFile;

		private ButtonView UploadButton;
		private ButtonView CloseButton;

		private Property<string> StatusString = new Property<string>() { Value = "" };

		private CallResult<RemoteStorageFileShareResult_t> fileShareResult;
		private CallResult<RemoteStoragePublishFileResult_t> filePublishResult;
		private CallResult<RemoteStorageUpdatePublishedFileResult_t> fileUpdateResult;
		private CallResult<SteamUGCQueryCompleted_t> queryResult;

		public override void Awake()
		{
			// This is to make it so nothing else can be interacted with.
			this.EncompassingView = new View(main.GeeUI, main.GeeUI.RootView);
			this.MainView = new PanelView(main.GeeUI, EncompassingView, Vector2.Zero);
			MainView.Resizeable = false;
			MainView.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			MainView.Width.Value = 400;
			MainView.Height.Value = 400;

			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Height, (p) => p.Y, main.ScreenSize));
			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Width, (p) => p.X, main.ScreenSize));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(i / 2f, MainView.Y), EncompassingView.Width));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(MainView.X, i / 2f), EncompassingView.Height));

			new TextView(main.GeeUI, MainView, "Workshop entry:", new Vector2(10, 8));
			this.SelectFile = new DropDownView(main.GeeUI, MainView, new Vector2(10, 35));
			SelectFile.AddOption("[Fetching...]", null);
			this.SelectFile.Position.Value = new Vector2(10, 30);

			this.queryResult = SteamWorker.GetCreatedWorkShopEntries((entries) =>
			{
				SelectFile.RemoveAllOptions();
				SelectFile.AddOption("[new]", delegate()
				{
					this.currentPublishedFile = default(SteamUGCDetails_t);
					this.UploadButton.Text = "Publish";
					this.UploadButton.AllowMouseEvents.Value = true;
					this.CloseButton.AllowMouseEvents.Value = true;
				});
				var listEntries = entries as List<SteamUGCDetails_t>;
				if (listEntries == null)
				{
					SelectFile.AddOption("[Error fetching entries]", null);
					return;
				}
				foreach (var entry in listEntries)
				{
					SteamUGCDetails_t entry1 = entry;
					SelectFile.AddOption(entry.m_rgchTitle, () =>
					{
						this.currentPublishedFile = entry1;
						this.NameView.Text = entry1.m_rgchTitle;
						this.DescriptionView.Text = entry1.m_rgchDescription;
						this.UploadButton.Text = "Update";
						this.UploadButton.AllowMouseEvents.Value = true;
						this.CloseButton.AllowMouseEvents.Value = true;
					}, related:entry);
				}
			});

			new TextView(main.GeeUI, MainView, "Name:", new Vector2(10, 68));
			this.NameView = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 85)) { MultiLine = false };
			new TextView(main.GeeUI, MainView, "Description:", new Vector2(10, 118));
			this.DescriptionView = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 135));

			this.UploadButton = new ButtonView(main.GeeUI, MainView, "Publish", new Vector2(50, 360));
			this.CloseButton = new ButtonView(main.GeeUI, MainView, "Close", new Vector2(300, 360));

			var statusString = new TextView(main.GeeUI, MainView, "Waiting", new Vector2(110, 365))
			{
				TextJustification = TextJustification.Center,
			};

			statusString.AutoSize.Value = false;
			statusString.Width.Value = 190;

			ConfigureTextField(NameView);
			ConfigureTextField(DescriptionView);

			UploadButton.OnMouseClick += (sender, args) =>
			{
				DoUpload();
			};

			CloseButton.OnMouseClick += (sender, args) =>
			{
				this.Delete.Execute();
			};

			this.Add(new Binding<string>(statusString.Text, StatusString));

			base.Awake();
		}

		public void DoUpload()
		{
			if (NameView.Text.Trim().Length == 0 || DescriptionView.Text.Trim().Length == 0)
				return;

			CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = false;

			string filePath = this.main.GetFullMapPath();
			string imagePath = string.Format("{0}.png", filePath.Substring(0, filePath.LastIndexOf('.')));
			string description = DescriptionView.Text;
			string name = NameView.Text;

			string steamFilePath = string.Format("workshop/maps/{0}.map", MD5(filePath));
			string steamImagePath = string.Format("workshop/maps/{0}.png", MD5(imagePath));

			StatusString.Value = "Storing map...";
			if (SteamWorker.WriteFileUGC(filePath, steamFilePath))
			{
				StatusString.Value = "Storing image...";
				if (!SteamWorker.WriteFileUGC(imagePath, steamImagePath))
				{
					StatusString.Value = "Failed to store image.";
					CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
					return;
				}
			}
			else
			{
				CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
				StatusString.Value = "Failed to store map.";
				return;
			}

			StatusString.Value = "Uploading map...";
			this.fileShareResult = SteamWorker.ShareFileUGC(steamFilePath, (b, t) =>
			{
				if (b)
				{
					StatusString.Value = "Uploading image...";
					this.fileShareResult = SteamWorker.ShareFileUGC(steamImagePath, (b1, handleT) =>
					{
						if (b1)
						{
							StatusString.Value = "Finalizing Entry...";
							if (string.IsNullOrEmpty(this.currentPublishedFile.m_pchFileName))
							{
								// Upload new
								this.filePublishResult = SteamWorker.UploadWorkShop(steamFilePath, steamImagePath, name, description, (publishSuccess, needsAcceptEULA, publishedFile) =>
								{
									if (publishSuccess)
									{
										StatusString.Value = "Entry created!";
										DescriptionView.ClearText();
										NameView.ClearText();
										CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
										SteamWorker.SetAchievement("level_editor");
									}
									else
									{
										StatusString.Value = "Publish failed.";
										CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
									}
								});
							}
							else
							{
								// Update existing
								this.fileUpdateResult = SteamWorker.UpdateWorkshopMap(currentPublishedFile.m_nPublishedFileId, steamFilePath, steamImagePath, name, description, publishSuccess =>
								{
									if (publishSuccess)
									{
										StatusString.Value = "Entry updated!";
										CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
										SteamRemoteStorage.FileDelete(this.currentPublishedFile.m_pchFileName);
									}
									else
									{
										StatusString.Value = "Update failed.";
										CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
									}
								});
							}
						}
						else
						{
							StatusString.Value = "Failed to upload image.";
							CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
						}
					});
				}
				else
				{
					StatusString.Value = "Failed to upload map.";
					CloseButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
				}
			});
		}

		private void ConfigureTextField(TextFieldView view)
		{
			bool multiLine = view.MultiLine;
			view.Width.Value = multiLine ? 380 : 340;
			view.Height.Value = multiLine ? 200 : 20;
		}

		public override void delete()
		{
			EncompassingView.RemoveFromParent();
			base.delete();
		}

		public string MD5(string filePath)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = File.ReadAllBytes(filePath);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
				sb.Append(hash[i].ToString("X2"));
			return sb.ToString();
		}
	}
}