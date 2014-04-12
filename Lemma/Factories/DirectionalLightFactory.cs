using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class DirectionalLightFactory : Factory<Main>
	{
		public DirectionalLightFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "DirectionalLight");

			Transform transform = new Transform();
			result.Add("Transform", transform);
			DirectionalLight directionalLight = new DirectionalLight();
			result.Add("DirectionalLight", directionalLight);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;
			Transform transform = result.Get<Transform>();
			DirectionalLight directionalLight = result.Get<DirectionalLight>();

			this.SetMain(result, main);

			directionalLight.Add(new TwoWayBinding<Vector3, Matrix>
			(
				directionalLight.Direction,
				delegate(Matrix x)
				{
					Vector3 y = Vector3.Normalize(-x.Forward);
					if (Vector3.Dot(y, directionalLight.Direction) > 0.0f)
						return y;
					return -y;
				},
				transform.Orientation,
				delegate(Vector3 x)
				{
					Matrix matrix = Matrix.Identity;
					matrix.Forward = Vector3.Normalize(-x);
					matrix.Left = x.Equals(Vector3.Up) ? Vector3.Left : Vector3.Normalize(Vector3.Cross(x, Vector3.Up));
					matrix.Up = Vector3.Normalize(Vector3.Cross(matrix.Left, matrix.Forward));
					return matrix;
				}
			));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{			
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Add(new Binding<Vector3>(model.Color, result.Get<DirectionalLight>().Color));
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));
		}
	}
}
