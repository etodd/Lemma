using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PointLightFactory : Factory<Main>
	{
		public PointLightFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PointLight");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;

			PointLight light = entity.GetOrCreate<PointLight>("PointLight");
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			light.Add(new TwoWayBinding<Vector3>(light.Position, transform.Position));

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main).EditorProperties();

			entity.Add("Enable", light.Enable);
			entity.Add("Disable", light.Disable);
			entity.Add("Enabled", light.Enabled);
			entity.Add("Color", light.Color);
			entity.Add("Attenuation", light.Attenuation);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model model = entity.Get<Model>("EditorModel");

			Property<Vector3> color = entity.Get<PointLight>().Color;
			model.Add(new Binding<Vector3>(model.Color, color));
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));

			VoxelAttachable.AttachEditorComponents(entity, main, color);
		}
	}
}
