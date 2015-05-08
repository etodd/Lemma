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
using Microsoft.Xna.Framework.Input;

namespace Lemma.GameScripts
{
	public class MainMenu : ScriptBase
	{
		public static new bool AvailableInReleaseEditor = false;

		private static PCInput.PCInputBinding[] konamiCode = new[]
		{
			new PCInput.PCInputBinding { Key = Keys.Up, GamePadButton = Buttons.DPadUp },
			new PCInput.PCInputBinding { Key = Keys.Up, GamePadButton = Buttons.DPadUp },
			new PCInput.PCInputBinding { Key = Keys.Down, GamePadButton = Buttons.DPadDown },
			new PCInput.PCInputBinding { Key = Keys.Down, GamePadButton = Buttons.DPadDown },
			new PCInput.PCInputBinding { Key = Keys.Left, GamePadButton = Buttons.DPadLeft },
			new PCInput.PCInputBinding { Key = Keys.Right, GamePadButton = Buttons.DPadRight },
			new PCInput.PCInputBinding { Key = Keys.Left, GamePadButton = Buttons.DPadLeft },
			new PCInput.PCInputBinding { Key = Keys.Right, GamePadButton = Buttons.DPadRight },
			new PCInput.PCInputBinding { Key = Keys.B, GamePadButton = Buttons.B },
			new PCInput.PCInputBinding { Key = Keys.A, GamePadButton = Buttons.A },
		};

		public static void Run(Entity script)
		{
			const float fadeTime = 1.0f;

			main.Spawner.CanSpawn = false;

			Sprite logo = new Sprite();
			logo.Image.Value = "Images\\logo";
			logo.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			logo.Add(new Binding<Vector2, Point>(logo.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
			main.UI.Root.Children.Insert(0, logo);

			Container cornerContainer = main.UIFactory.CreateContainer();
			cornerContainer.AnchorPoint.Value = new Vector2(1, 0);
			cornerContainer.PaddingLeft.Value = cornerContainer.PaddingRight.Value = 12.0f;
			#if VR
			if (main.VR)
				cornerContainer.Add(new Binding<Vector2, Point>(cornerContainer.Position, x => new Vector2(x.X * 0.75f, x.Y * 0.25f), main.ScreenSize));
			else
			#endif
				cornerContainer.Add(new Binding<Vector2, Point>(cornerContainer.Position, x => new Vector2(x.X - 10.0f, 10.0f), main.ScreenSize));
			main.UI.Root.Children.Add(cornerContainer);

			ListContainer corner = new ListContainer();
			corner.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			corner.Alignment.Value = ListContainer.ListAlignment.Middle;
			corner.Spacing.Value = 12.0f;
			cornerContainer.Children.Add(corner);

			TextElement version = new TextElement();
			version.FontFile.Value = main.Font;
			version.Add(new Binding<string>(version.Text, x => string.Format(main.Strings.Get("build number") ?? "Build {0}", Main.Build.ToString()), main.Strings.Language));
			corner.Children.Add(version);

			TextElement webLink = main.UIFactory.CreateLink("et1337.com", "http://et1337.com");
			corner.Children.Add(webLink);

			Container languageMenu = new Container();

			Container languageButton = main.UIFactory.CreateButton(delegate()
			{
				languageMenu.Visible.Value = !languageMenu.Visible;
			});
			corner.Children.Add(languageButton);

			Sprite currentLanguageIcon = new Sprite();
			currentLanguageIcon.Add(new Binding<string, Main.Config.Lang>(currentLanguageIcon.Image, x => "Images\\" + x.ToString(), main.Settings.Language));
			languageButton.Children.Add(currentLanguageIcon);

			languageMenu.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			languageMenu.Visible.Value = false;
			languageMenu.AnchorPoint.Value = new Vector2(1, 0);
			cornerContainer.CheckLayout();
			languageMenu.Add(new Binding<Vector2>(languageMenu.Position, () => languageButton.GetAbsolutePosition() + new Vector2(languageButton.ScaledSize.Value.X, languageButton.ScaledSize.Value.Y), languageButton.Position, languageButton.ScaledSize, cornerContainer.Position));
			main.UI.Root.Children.Add(languageMenu);
			
			ListContainer languages = new ListContainer();
			languages.Orientation.Value = ListContainer.ListOrientation.Vertical;
			languages.Alignment.Value = ListContainer.ListAlignment.Max;
			languages.Spacing.Value = 0.0f;
			languageMenu.Children.Add(languages);
			
			foreach (Main.Config.Lang language in Enum.GetValues(typeof(Main.Config.Lang)))
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
			cornerContainer.Opacity.Value = 0.0f;
			webLink.Opacity.Value = 0.0f;
			languageButton.Opacity.Value = 0.0f;
			currentLanguageIcon.Opacity.Value = 0.0f;

			script.Add(new Animation
			(
				new Animation.Delay(1.0f),
				new Animation.Parallel
				(
					new Animation.FloatMoveTo(logo.Opacity, 1.0f, fadeTime),
					new Animation.FloatMoveTo(version.Opacity, 1.0f, fadeTime),
					new Animation.FloatMoveTo(cornerContainer.Opacity, UIFactory.Opacity, fadeTime),
					new Animation.FloatMoveTo(webLink.Opacity, 1.0f, fadeTime),
					new Animation.FloatMoveTo(languageButton.Opacity, UIFactory.Opacity, fadeTime),
					new Animation.FloatMoveTo(currentLanguageIcon.Opacity, 1.0f, fadeTime)
				)
			));

			script.Add(new CommandBinding(script.Delete, logo.Delete, cornerContainer.Delete, languageMenu.Delete));

			main.Renderer.InternalGamma.Value = 0.0f;
			main.Renderer.Brightness.Value = 0.0f;
			main.Renderer.Tint.Value = new Vector3(0.0f);
			script.Add(new Animation
			(
				new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.0f), 0.3f)
			));

			if (main.Settings.GodModeProperty)
				SteamWorker.SetAchievement("cheevo_god_mode");
			else
			{
				int konamiIndex = 0;
				PCInput input = script.Create<PCInput>();
				input.Add(new CommandBinding<PCInput.PCInputBinding>(input.AnyInputDown, delegate(PCInput.PCInputBinding button)
				{
					if (!main.Settings.GodModeProperty)
					{
						if (button.Key == konamiCode[konamiIndex].Key || button.GamePadButton == konamiCode[konamiIndex].GamePadButton)
						{
							if (konamiIndex == konamiCode.Length - 1)
							{
								main.Settings.GodModeProperty.Value = true;
								main.SaveSettings();
								SteamWorker.SetAchievement("cheevo_god_mode");
								main.Menu.HideMessage(script, main.Menu.ShowMessage(script, "\\god mode"), 5.0f);
							}
							else
								konamiIndex++;
						}
						else
						{
							konamiIndex = 0;
							if (button.Key == konamiCode[konamiIndex].Key || button.GamePadButton == konamiCode[konamiIndex].GamePadButton)
								konamiIndex++;
						}
					}
				}));
			}
		}
	}
}