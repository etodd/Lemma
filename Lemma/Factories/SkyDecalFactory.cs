using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace Lemma.Factories
{
	public class SkyDecalFactory : Factory<Main>
	{
		public SkyDecalFactory()
		{
			this.Color = new Vector3(0.8f, 0.6f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "SkyDecal");

			ModelAlpha skybox = new ModelAlpha();
			skybox.DiffuseTexture.Value = "Images\\circle";
			entity.Add("Model", skybox);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			ModelAlpha model = entity.Get<ModelAlpha>("Model");
			model.Filename.Value = "Models\\plane";
			model.EffectFile.Value = "Effects\\SkyDecal";
			base.Bind(entity, main, creating);
			entity.CannotSuspendByDistance = true;

			model.DisableCulling.Value = true;
			model.CullBoundingBox.Value = false;
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.DrawOrder.Value = -9;

			model.Add(new Binding<Vector3>(model.GetVector3Parameter("CameraPosition"), main.Camera.Position));

			entity.Add("Scale", model.Scale);
			entity.Add("Alpha", model.Alpha);
			entity.Add("Color", model.Color, new PropertyEntry.EditorData { FChangeBy = 0.1f });
			entity.Add("Image", model.DiffuseTexture, new PropertyEntry.EditorData
			{
				Options = FileFilter.Get(main, main.Content.RootDirectory, new[] { "Images", "Game\\Images" }),
			});
		}
	}
}
