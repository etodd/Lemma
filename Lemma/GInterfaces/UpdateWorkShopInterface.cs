using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using ComponentBind;
using GeeUI.Views;
using ICSharpCode.SharpZipLib.Tar;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Point = Microsoft.Xna.Framework.Point;
using View = GeeUI.Views.View;

namespace Lemma.GInterfaces
{
	public class UpdateWorkShopInterface : Component<Main>
	{
		public SpriteFont MainFont;

		private View EncompassingView;
		private PanelView MainView;
		private TextFieldView MapFilePath;
		private ButtonView MapOpenPath;
		private DropDownView SelectFile;

		private SteamUGCDetails_t currentPublishedFile;

		private ButtonView UploadButton;
		private ButtonView CancelButton;

		private Property<string> StatusString = new Property<string>() { Value = "" };

		private string MapPath = "";
		public UpdateWorkShopInterface()
		{
			this.MapPath = "";
		}

		public override void Awake()
		{
			MainFont = main.Content.Load<SpriteFont>("Font");

			//This is to make it so nothing else can be interacted with.
			this.EncompassingView = new View(main.GeeUI, main.GeeUI.RootView);
			this.MainView = new PanelView(main.GeeUI, EncompassingView, Vector2.Zero);
			MainView.Resizeable = false;
			MainView.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			MainView.Width.Value = 400;
			MainView.Height.Value = 155;

			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Height, (p) => p.Y, main.ScreenSize));
			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Width, (p) => p.X, main.ScreenSize));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(i / 2f, MainView.Y), EncompassingView.Width));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(MainView.X, i / 2f), EncompassingView.Height));

			new TextView(main.GeeUI, MainView, "Current Workshop Entry:", new Vector2(10, 8), MainFont);
			this.SelectFile = new DropDownView(main.GeeUI, MainView, new Vector2(10, 35), MainFont);
			SelectFile.AddOption("Fetching...", null);
			this.SelectFile.Position.Value = new Vector2(10, 30);

			new TextView(main.GeeUI, MainView, "New Map File:", new Vector2(10, 68), MainFont);
			this.MapFilePath = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 85), MainFont) { MultiLine = false, Editable = false };
			this.MapOpenPath = new ButtonView(main.GeeUI, MainView, "...", new Vector2(360, 85), MainFont);

			SteamWorker.GetCreatedWorkShopEntries((entries) =>
			{
				SelectFile.RemoveAllOptions();
				if (entries == null)
				{
					SelectFile.AddOption("Error fetching entries", null);
					return;
				}
				var listEntries = entries as List<SteamUGCDetails_t>;
				if (listEntries == null || listEntries.Count == 0)
				{
					SelectFile.AddOption("Error fetching entries", null);
					return;
				}
				foreach (var entry in listEntries)
				{
					SteamUGCDetails_t entry1 = entry;
					var button = SelectFile.AddOption(entry.m_rgchTitle, () =>
					{
						this.currentPublishedFile = entry1;
						this.UploadButton.AllowMouseEvents.Value = true;
						this.CancelButton.AllowMouseEvents.Value = true;
					}, related:entry);
				}
			});

			this.UploadButton = new ButtonView(main.GeeUI, MainView, "Upload", new Vector2(50, 125), MainFont);
			this.CancelButton = new ButtonView(main.GeeUI, MainView, "Cancel", new Vector2(300, 125), MainFont);

			var statusString = new TextView(main.GeeUI, MainView, "Waiting", new Vector2(110, 130), MainFont)
			{
				TextJustification = TextJustification.Center,
			};

			statusString.AutoSize.Value = false;
			statusString.Width.Value = 190;

			ConfigureTextField(MapFilePath);

			UploadButton.OnMouseClick += (sender, args) =>
			{
				DoUpload();
			};

			CancelButton.OnMouseClick += (sender, args) =>
			{
				this.Delete.Execute();
			};

			MapOpenPath.OnMouseClick += (sender, args) =>
			{
				var dialog = new System.Windows.Forms.OpenFileDialog();
				dialog.Filter = "Map Files|*.map";
				var result = dialog.ShowDialog();
				if (result == DialogResult.OK)
					this.MapFilePath.Text = dialog.FileName;
			};

			this.Add(new Binding<string>(statusString.Text, StatusString));

			MapFilePath.Text = MapPath;
			base.Awake();
		}

		public void DoUpload()
		{
			if (MapFilePath.Text.Length == 0) return;

			CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = false;

			string filePath = MapFilePath.Text;

			string steamFilePath = "workshop/maps/" + MD5(filePath) + ".map";

			StatusString.Value = "Storing map...";
			if (!SteamWorker.WriteFileUGC(filePath, steamFilePath))
			{
				CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
				StatusString.Value = "Failed to store map.";
				return;
			}

			StatusString.Value = "Uploading map...";
			SteamWorker.ShareFileUGC(steamFilePath, (mapSuccess, t) =>
			{
				if (mapSuccess)
				{
					StatusString.Value = "Finalizing Entry...";
					SteamWorker.UpdateWorkshopMap(currentPublishedFile.m_nPublishedFileId, steamFilePath, publishSuccess =>
					{
						if (publishSuccess)
						{
							StatusString.Value = "Success!";
							MapFilePath.ClearText();
							CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;

							//Delete the old map
							bool deleteGood = SteamRemoteStorage.FileDelete(currentPublishedFile.m_pchFileName);
							deleteGood = !deleteGood;
						}
						else
						{
							StatusString.Value = "Failed publishing";
							CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
						}
					});
				}
				else
				{
					StatusString.Value = "Failed to upload map.";
					CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
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
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}
	}
}
