using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Factories
{
	public class PhoneFactory : Factory
	{
		public PhoneFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Phone");
			result.Add("Transform", new Transform());
			result.Add("Model", new Model());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();

			UIRenderer ui = result.GetOrCreate<UIRenderer>();
			ui.RenderTargetSize.Value = new Point(256, 512);

			Container msgBackground = new Container();

			ui.Root.Children.Add(msgBackground);
			msgBackground.Add(new Binding<Vector2, Point>(msgBackground.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), ui.RenderTargetSize));
			msgBackground.AnchorPoint.Value = new Vector2(0.5f);
			msgBackground.Add(new Binding<float, bool>(msgBackground.Opacity, x => x ? 1.0f : 0.5f, msgBackground.Highlighted));

			msgBackground.Tint.Value = Microsoft.Xna.Framework.Color.Red;
			msgBackground.Opacity.Value = 0.5f;

			TextElement msg = new TextElement();
			msg.FontFile.Value = "Font";
			msg.Opacity.Value = 1.0f;
			msg.WrapWidth.Value = 250.0f;
			msgBackground.Children.Add(msg);

			Model model = result.Get<Model>("Model");
			model.Filename.Value = "Models\\plane";
			model.Add(new Binding<Microsoft.Xna.Framework.Graphics.RenderTarget2D>(model.GetRenderTarget2DParameter("Diffuse" + Model.SamplerPostfix), ui.RenderTarget));
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			const float scale = 0.01f;
			model.Scale.Value = new Vector3(ui.RenderTargetSize.Value.X * scale, ui.RenderTargetSize.Value.Y * 0.5f * scale, 1.0f);

			ui.MouseFilter = delegate(MouseState input)
			{
				Microsoft.Xna.Framework.Graphics.Viewport viewport = main.GraphicsDevice.Viewport;

				Matrix inverseTransform = Matrix.Invert(Matrix.CreateScale(model.Scale) * transform.Matrix);
				Vector3 ray = Vector3.Normalize(viewport.Unproject(new Vector3(input.X, input.Y, 1), main.Camera.Projection, main.Camera.View, Matrix.Identity) - viewport.Unproject(new Vector3(input.X, input.Y, 0), main.Camera.Projection, main.Camera.View, Matrix.Identity));
				Vector3 rayStart = main.Camera.Position;

				ray = Vector3.TransformNormal(ray, inverseTransform);
				rayStart = Vector3.Transform(rayStart, inverseTransform);

				Point output;

				float? intersection = new Ray(rayStart, ray).Intersects(new Plane(Vector3.Right, 0.0f));
				if (intersection.HasValue)
				{
					Vector3 intersectionPoint = rayStart + ray * intersection.Value;
					Point size = ui.RenderTargetSize;
					Vector2 sizeF = new Vector2(size.X, size.Y);
					output = new Point((int)((0.5f - intersectionPoint.Z) * sizeF.X), (int)((0.5f + intersectionPoint.Y) * sizeF.Y));
				}
				else
					output = new Point(-1, -1);

				msg.Text.Value = "[" + output.X.ToString() + ", " + output.Y.ToString() + "]";

				return new MouseState
				(
					output.X,
					output.Y,
					input.ScrollWheelValue,
					input.LeftButton,
					input.MiddleButton,
					input.RightButton,
					input.XButton1,
					input.XButton2
				);
			};
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			Model model = result.Get<Model>("Model");
			Model editorModel = result.Get<Model>("EditorModel");
			Property<bool> editorSelected = result.GetOrMakeProperty<bool>("EditorSelected", false);
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !editorSelected || !model.IsValid, editorSelected, model.IsValid));
		}
	}
}
