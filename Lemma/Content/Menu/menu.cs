const float fadeTime = 1.0f;

if (((GameMain)main).StartSpawnPoint.Value == "end")
{
	// End game
	Sound.PlayCue(((GameMain)main).MusicBank, "Theme");
	main.Renderer.InternalGamma.Value = 0.0f;
	main.Renderer.Brightness.Value = 0.0f;
	main.Renderer.BackgroundColor.Value = Renderer.DefaultBackgroundColor;
	main.IsMouseVisible.Value = true;

	ListContainer list = new ListContainer();
	list.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
	list.Add(new Binding<Vector2, Point>(list.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
	list.Add(new Binding<float, Point>(list.Spacing, x => x.Y * 0.025f, main.ScreenSize));
	list.Alignment.Value = ListContainer.ListAlignment.Middle;
	((GameMain)main).UI.Root.Children.Add(list);
	script.Add(new CommandBinding(script.Delete, list.Delete));

	Sprite logo = new Sprite();
	logo.Image.Value = "Images\\logo";
	logo.Opacity.Value = 0.0f;
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
		TextElement element = ((GameMain)main).CreateLink(text, url);
		element.Add(new Binding<float, Vector2>(element.WrapWidth, x => x.X, logo.ScaledSize));
		element.Opacity.Value = 0.0f;
		script.Add(new Animation
		(
			new Animation.FloatMoveTo(element.Opacity, 1.0f, fadeTime)
		));
		list.Children.Add(element);
	};

	addLink("Back the Kickstarter", "https://www.kickstarter.com/projects/869028656/lemma-first-person-parkour");
	addLink("Vote for Lemma on Greenlight", "http://steamcommunity.com/sharedfiles/filedetails/?id=105075009");
	addLink("Follow @et1337", "http://twitter.com/et1337");

	UIComponent creditsScroll = new UIComponent();
	creditsScroll.EnableScissor.Value = true;
	creditsScroll.Add(new Binding<Vector2, Point>(creditsScroll.Size, x => new Vector2(x.X * 0.4f, x.Y * 0.2f), main.ScreenSize));
	list.Children.Add(creditsScroll);

	TextElement credits = new TextElement();
	credits.FontFile.Value = "Font";
	credits.Text.Value = ((GameMain)main).Credits;
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
	((GameMain)main).UI.Root.Children.Add(logo);

	ListContainer corner = new ListContainer();
	corner.AnchorPoint.Value = new Vector2(1, 1);
	corner.Orientation.Value = ListContainer.ListOrientation.Vertical;
	corner.Alignment.Value = ListContainer.ListAlignment.Max;
	corner.Add(new Binding<Vector2, Point>(corner.Position, x => new Vector2(x.X - 10.0f, x.Y - 10.0f), main.ScreenSize));
	((GameMain)main).UI.Root.Children.Add(corner);

	TextElement webLink = ((GameMain)main).CreateLink("et1337.com", "http://et1337.com");
	corner.Children.Add(webLink);
		
	TextElement version = new TextElement();
	version.FontFile.Value = "Font";
	version.Text.Value = "Build " + GameMain.Build.ToString();
	corner.Children.Add(version);

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

((GameMain)main).CanSpawn = false;

main.Renderer.BlurAmount.Value = 1.0f;
main.Renderer.InternalGamma.Value = 0.0f;
main.Renderer.Brightness.Value = 0.0f;
main.Renderer.Tint.Value = new Vector3(0.0f);

script.Add(new Animation
(
	new Animation.Vector3MoveTo(main.Renderer.Tint, new Vector3(1.0f), 0.2f)
));

Transform camera = get("camera").Get<Transform>();
main.Camera.Position.Value = camera.Position;
main.Camera.RotationMatrix.Value = camera.Orientation;

get("fillMap1").GetCommand("Fill").Execute();