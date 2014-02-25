
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