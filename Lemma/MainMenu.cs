using System;
using ComponentBind;
using Lemma.GameScripts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Lemma.Components;
using Lemma.Factories;

namespace Lemma.GameScripts
{
	public class MainMenu : ScriptBase
	{
		public static void Run(Entity script)
		{
			const float fadeTime = 1.0f;
#if VR
			// The logo is too bright in VR mode because of the light ramp
			Color logoTint = new Color(0.55f, 0.55f, 0.55f);
#endif

			main.Spawner.CanSpawn = false;

			script.Add(new Animation
			(
				new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.0f), 0.2f)
			));

			if (main.Spawner.StartSpawnPoint.Value == "end")
			{
				// End game
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_MUSIC_THEME);
				main.Renderer.InternalGamma.Value = 0.0f;
				main.Renderer.Brightness.Value = 0.0f;
				main.LightingManager.BackgroundColor.Value = World.DefaultBackgroundColor;
				main.UI.IsMouseVisible.Value = true;

				ListContainer list = new ListContainer();
				list.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				list.Add(new Binding<Vector2, Point>(list.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
				list.Add(new Binding<float, Point>(list.Spacing, x => x.Y * 0.025f, main.ScreenSize));
				list.Alignment.Value = ListContainer.ListAlignment.Middle;
				main.UI.Root.Children.Add(list);
				script.Add(new CommandBinding(script.Delete, list.Delete));

				Sprite logo = new Sprite();
				logo.Image.Value = "Images\\logo";
				logo.Opacity.Value = 0.0f;
#if VR
				if (main.VR)
					logo.Tint.Value = logoTint;
#endif
				script.Add(new Animation
				(
					new Animation.FloatMoveTo(logo.Opacity, 1.0f, fadeTime)
				));
				list.Children.Add(logo);

				Action<string> addText = delegate(string text)
				{
					TextElement element = new TextElement();
					element.FontFile.Value = "Font";
					element.Text.Value = text;
					element.Add(new Binding<float, Vector2>(element.WrapWidth, x => x.X, logo.ScaledSize));
					element.Opacity.Value = 0.0f;
					script.Add(new Animation
					(
						new Animation.FloatMoveTo(element.Opacity, 1.0f, fadeTime)
					));
					list.Children.Add(element);
				};

				addText("Want more Lemma?");

				Action<string, string> addLink = delegate(string text, string url)
				{
					TextElement element = main.UIFactory.CreateLink(text, url);
					element.Add(new Binding<float, Vector2>(element.WrapWidth, x => x.X, logo.ScaledSize));
					element.Opacity.Value = 0.0f;
					script.Add(new Animation
					(
						new Animation.FloatMoveTo(element.Opacity, 1.0f, fadeTime)
					));
					list.Children.Add(element);
				};

				addLink("Check out Lemma on IndieDB", "http://indiedb.com/games/lemma");
				addLink("Follow @et1337", "http://twitter.com/et1337");

				UIComponent creditsScroll = new UIComponent();
				creditsScroll.EnableScissor.Value = true;
				creditsScroll.Add(new Binding<Vector2, Point>(creditsScroll.Size, x => new Vector2(x.X * 0.4f, x.Y * 0.2f), main.ScreenSize));
				list.Children.Add(creditsScroll);

				TextElement credits = new TextElement();
				credits.FontFile.Value = "Font";
				credits.Text.Value = main.Menu.Credits;
				credits.Add(new Binding<float, Vector2>(credits.WrapWidth, x => x.X, creditsScroll.Size));
				credits.Position.Value = new Vector2(0, creditsScroll.ScaledSize.Value.Y * 1.5f);
				creditsScroll.Children.Add(credits);

				script.Add(new Animation
				(
					new Animation.Vector2MoveToSpeed(credits.Position, new Vector2(0, -credits.ScaledSize.Value.Y - creditsScroll.ScaledSize.Value.Y), 30.0f)
				));
			}
			else
			{
				// Main menu

				Sprite logo = new Sprite();
				logo.Image.Value = "Images\\logo";
				logo.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				logo.Add(new Binding<Vector2, Point>(logo.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
#if VR
				if (main.VR)
					logo.Tint.Value = logoTint;
#endif
				main.UI.Root.Children.Add(logo);

				ListContainer corner = new ListContainer();
				corner.AnchorPoint.Value = new Vector2(1, 1);
				corner.Orientation.Value = ListContainer.ListOrientation.Vertical;
				corner.Reversed.Value = true;
				corner.Alignment.Value = ListContainer.ListAlignment.Max;
				corner.Add(new Binding<Vector2, Point>(corner.Position, x => new Vector2(x.X - 10.0f, x.Y - 10.0f), main.ScreenSize));
				main.UI.Root.Children.Add(corner);

				TextElement version = new TextElement();
				version.FontFile.Value = "Font";
				version.Text.Value = "Build " + Main.Build.ToString();
				corner.Children.Add(version);

				TextElement webLink = main.UIFactory.CreateLink("et1337.com", "http://et1337.com");
				corner.Children.Add(webLink);

				Container languageMenu = new Container();

				UIComponent languageButton = main.UIFactory.CreateButton(delegate()
				{
					languageMenu.Visible.Value = !languageMenu.Visible;
				});
				corner.Children.Add(languageButton);

				Sprite currentLanguageIcon = new Sprite();
				currentLanguageIcon.Add(new Binding<string, Main.Config.Lang>(currentLanguageIcon.Image, x => "Images\\" + x.ToString(), main.Settings.Language));
				languageButton.Children.Add(currentLanguageIcon);

				languageMenu.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				languageMenu.Visible.Value = false;
				corner.Children.Add(languageMenu);
				
				ListContainer languages = new ListContainer();
				languages.Orientation.Value = ListContainer.ListOrientation.Vertical;
				languages.Alignment.Value = ListContainer.ListAlignment.Max;
				languages.Spacing.Value = 0.0f;
				languageMenu.Children.Add(languages);
				
				foreach (Main.Config.Lang language in Main.Languages)
				{
					UIComponent button = main.UIFactory.CreateButton(delegate()
					{
						main.Settings.Language.Value = language;
						languageMenu.Visible.Value = false;
					});

					Sprite icon = new Sprite();
					icon.Image.Value = "Images\\" + language.ToString();
					button.Children.Add(icon);

					languages.Children.Add(button);
				}

				logo.Opacity.Value = 0.0f;
				version.Opacity.Value = 0.0f;
				webLink.Opacity.Value = 0.0f;

				script.Add(new Animation
				(
					new Animation.Delay(1.0f),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(logo.Opacity, 1.0f, fadeTime),
						new Animation.FloatMoveTo(version.Opacity, 1.0f, fadeTime),
						new Animation.FloatMoveTo(webLink.Opacity, 1.0f, fadeTime)
					)
				));

				script.Add(new CommandBinding(script.Delete, logo.Delete, corner.Delete));
			}

#if VR
			if (!main.VR)
#endif
				main.Renderer.BlurAmount.Value = 1.0f;

			main.Renderer.InternalGamma.Value = 0.0f;
			main.Renderer.Brightness.Value = 0.0f;
			main.Renderer.Tint.Value = new Vector3(0.0f);
		}
	}
}