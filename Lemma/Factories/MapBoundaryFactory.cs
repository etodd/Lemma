using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class MapBoundaryFactory : Factory
	{
		public MapBoundaryFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "MapBoundary");
			result.Add("Transform", new Transform());
			PhysicsBlock block = new PhysicsBlock();
			block.Size.Value = new Vector3(100.0f, 50.0f, 2.0f);
			result.Add("PhysicsBlock", block);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			result.CannotSuspendByDistance = true;

			Transform transform = result.Get<Transform>();

			ModelAlpha model = new ModelAlpha();
			model.Color.Value = new Vector3(1.2f, 1.0f, 0.8f);
			model.Editable = false;
			model.Serialize = false;
			model.Filename.Value = "Models\\electricity";
			model.DrawOrder.Value = 11;
			result.Add("Model", model);

			PhysicsBlock block = result.Get<PhysicsBlock>();
			block.Box.BecomeKinematic();
			block.Add(new TwoWayBinding<Matrix>(transform.Matrix, block.Transform));
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Add(new Binding<Vector3>(model.Scale, x => new Vector3(x.X * 0.5f, x.Y * 0.5f, 1.0f), block.Size));

			Property<Vector2> scaleParameter = model.GetVector2Parameter("Scale");
			model.Add(new Binding<Vector2, Vector3>(scaleParameter, x => new Vector2(x.Y, x.X), model.Scale));
			model.Add(new CommandBinding(main.ReloadedContent, delegate()
			{
				scaleParameter.Reset();
			}));
		}
	}
}
