using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using ComponentBind;
using GeeUI.Views;
using ICSharpCode.SharpZipLib.Tar;
using Lemma.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Point = Microsoft.Xna.Framework.Point;
using View = GeeUI.Views.View;

namespace Lemma.GInterfaces
{
	public class WorkShopInterface : Component<Main>
	{
		public SpriteFont MainFont;

		private View EncompassingView;
		private PanelView MainView;
		private TextFieldView MapFilePath;
		private ButtonView MapOpenPath;
		private TextFieldView MapImagePath;
		private ButtonView OpenImagePath;
		private TextFieldView NameView;
		private TextFieldView DescriptionView;

		private ButtonView UploadButton;
		private ButtonView CancelButton;

		private Property<string> StatusString = new Property<string>() { Value = "" };

		private string MapPath = "";
		public WorkShopInterface()
		{
			this.MapPath = "";
		}

		public WorkShopInterface(string path)
		{
			MapPath = path;
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
			MainView.Height.Value = 400;

			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Height, (p) => p.Y, main.ScreenSize));
			this.EncompassingView.Add(new Binding<int, Point>(EncompassingView.Width, (p) => p.X, main.ScreenSize));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(i / 2f, MainView.Y), EncompassingView.Width));
			this.MainView.Add(new Binding<Vector2, int>(MainView.Position, i => new Vector2(MainView.X, i / 2f), EncompassingView.Height));

			new TextView(main.GeeUI, MainView, "Map File:", new Vector2(10, 8), MainFont);
			this.MapFilePath = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 25), MainFont) { MultiLine = false, Editable = false };
			this.MapOpenPath = new ButtonView(main.GeeUI, MainView, "...", new Vector2(360, 25), MainFont);
			new TextView(main.GeeUI, MainView, "Thumbnail File:", new Vector2(10, 48), MainFont);
			this.MapImagePath = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 65), MainFont) { MultiLine = false, Editable = false };
			this.OpenImagePath = new ButtonView(main.GeeUI, MainView, "...", new Vector2(360, 65), MainFont);
			new TextView(main.GeeUI, MainView, "Name:", new Vector2(10, 88), MainFont);
			this.NameView = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 105), MainFont) { MultiLine = false };
			new TextView(main.GeeUI, MainView, "Description:", new Vector2(10, 128), MainFont);
			this.DescriptionView = new TextFieldView(main.GeeUI, MainView, new Vector2(10, 145), MainFont);

			this.UploadButton = new ButtonView(main.GeeUI, MainView, "Upload", new Vector2(50, 360), MainFont);
			this.CancelButton = new ButtonView(main.GeeUI, MainView, "Cancel", new Vector2(300, 360), MainFont);

			var statusString = new TextView(main.GeeUI, MainView, "Waiting", new Vector2(110, 365), MainFont)
			{
				TextJustification = TextJustification.Center,
			};

			statusString.AutoSize.Value = false;
			statusString.Width.Value = 190;

			ConfigureTextField(MapImagePath);
			ConfigureTextField(MapFilePath);
			ConfigureTextField(NameView);
			ConfigureTextField(DescriptionView);

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

			OpenImagePath.OnMouseClick += (sender, args) =>
			{
				var dialog = new System.Windows.Forms.OpenFileDialog();
				dialog.Filter = "Image Files |*.png";
				var result = dialog.ShowDialog();
				if (result == DialogResult.OK)
					this.MapImagePath.Text = dialog.FileName;
			};

			this.Add(new Binding<string>(statusString.Text, StatusString));

			MapFilePath.Text = MapPath;
			base.Awake();
		}

		public void DoUpload()
		{
			if (MapFilePath.Text.Length == 0 || MapImagePath.Text.Length == 0 || NameView.Text.Length == 0 ||
				DescriptionView.Text.Length == 0) return;

			CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = false;

			string filePath = MapFilePath.Text;
			string imagePath = MapImagePath.Text;
			string description = DescriptionView.Text;
			string name = NameView.Text;

			string steamFilePath = "workshop/maps/" + MD5(filePath) + ".map";
			string steamImagePath = "workshop/maps/" + MD5(imagePath) + ".png";

			StatusString.Value = "Storing map...";
			if (SteamWorker.WriteFileUGC(filePath, steamFilePath))
			{
				StatusString.Value = "Storing image...";
				if (SteamWorker.WriteFileUGC(imagePath, steamImagePath))
				{

				}
				else
				{
					StatusString.Value = "Failed to store image.";
					CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
					return;
				}
			}
			else
			{
				CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
				StatusString.Value = "Failed to store map.";
				return;
			}

			StatusString.Value = "Uploading map...";
			SteamWorker.ShareFileUGC(steamFilePath, (b, t) =>
			{
				if (b)
				{
					StatusString.Value = "Uploading image...";
					SteamWorker.ShareFileUGC(steamImagePath, (b1, handleT) =>
					{
						if (b1)
						{
							StatusString.Value = "Finalizing Entry...";
							SteamWorker.UploadWorkShop(steamFilePath, steamImagePath, name, description, (publishSuccess, needsAcceptEULA, publishedFile) =>
							{
								if (publishSuccess)
								{
									StatusString.Value = "Success!";
									MapImagePath.ClearText();
									MapFilePath.ClearText();
									DescriptionView.ClearText();
									NameView.ClearText();
									CancelButton.AllowMouseEvents.Value = UploadButton.AllowMouseEvents.Value = true;
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
							StatusString.Value = "Failed to upload image.";
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
			view.Height.Value = multiLine ? 200 : 16;
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
