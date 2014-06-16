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
			return new Entity(main, "DirectionalLight");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			DirectionalLight directionalLight = entity.GetOrCreate<DirectionalLight>("DirectionalLight");

			this.SetMain(entity, main);

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

			entity.Add("Enable", directionalLight.Enable);
			entity.Add("Disable", directionalLight.Disable);
			entity.Add("Enabled", directionalLight.Enabled);
			entity.Add("Color", directionalLight.Color);
			entity.Add("Shadowed", directionalLight.Shadowed);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{			
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Add(new Binding<Vector3>(model.Color, entity.Get<DirectionalLight>().Color));
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
