using System;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class AmbientLightFactory : Factory<Main>
	{
		public AmbientLightFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "AmbientLight");

			entity.Add("Transform", new Transform());

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			AmbientLight ambientLight = entity.GetOrCreate<AmbientLight>("AmbientLight");
			entity.CannotSuspendByDistance = true;

			this.SetMain(entity, main);

			entity.Add("Enable", ambientLight.Enable);
			entity.Add("Disable", ambientLight.Disable);
			entity.Add("Color", ambientLight.Color, new PropertyEntry.EditorData() { FChangeBy = 0.1f });
			entity.Add("Enabled", ambientLight.Enabled);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Add(new Binding<Vector3>(model.Color, entity.Get<AmbientLight>().Color));
			model.Scale.Value = new Vector3(0.5f);
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
