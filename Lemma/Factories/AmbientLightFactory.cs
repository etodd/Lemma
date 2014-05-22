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
			AmbientLight ambientLight = new AmbientLight();
			ambientLight.Color.Value = Vector3.One;
			entity.Add("AmbientLight", ambientLight);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);
			entity.CannotSuspendByDistance = true;
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Add(new Binding<Vector3>(model.Color, entity.Get<AmbientLight>().Color));
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
