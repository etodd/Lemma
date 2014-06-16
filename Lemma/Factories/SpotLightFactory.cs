using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SpotLightFactory : Factory<Main>
	{
		public SpotLightFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "SpotLight");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			SpotLight spotLight = entity.GetOrCreate<SpotLight>("SpotLight");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			spotLight.Add(new TwoWayBinding<Vector3>(spotLight.Position, transform.Position));
			spotLight.Add(new TwoWayBinding<Quaternion>(spotLight.Orientation, transform.Quaternion));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			Property<Vector3> color = entity.Get<SpotLight>().Color;
			model.Add(new Binding<Vector3>(model.Color, color));
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, delegate(Matrix x)
			{
				x.Forward *= -1;
				return x;
			}, entity.Get<Transform>().Matrix));

			VoxelAttachable.AttachEditorComponents(entity, main, color);
		}
	}
}
