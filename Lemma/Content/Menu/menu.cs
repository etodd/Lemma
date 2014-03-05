
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
	list.Add(new Binding<float, Point>(list.Spacing, x => x.Y * 0.025f, main.ScreenSize));
	list.Alignment.Value = ListContainer.ListAlignment.Middle;
	list.Add(new Binding<Vector2, Point>(list.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
	((GameMain)main).UI.Root.Children.Add(list);

	const float fadeTime = 1.0f;

	Sprite logo = new Sprite();
	logo.Image.Value = "Images\\logo";
	logo.Add(new Binding<Vector2>(logo.Scale, () => new Vector2((main.ScreenSize.Value.X * 0.25f) / logo.Size.Value.X), main.ScreenSize, logo.Size));
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

	addLink("Back the Kickstarter", "http://lemmagame.com");
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
		new Animation.Vector2MoveToSpeed(credits.Position, new Vector2(0, -credits.ScaledSize.Value.Y - creditsScroll.ScaledSize.Value.Y), 20.0f)
	));
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