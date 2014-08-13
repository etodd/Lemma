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
			directionalLight.Add(new Binding<Quaternion>(directionalLight.Quaternion, transform.Quaternion));

			this.SetMain(entity, main);

			entity.Add("Enable", directionalLight.Enable);
			entity.Add("Disable", directionalLight.Disable);
			entity.Add("Enabled", directionalLight.Enabled);
			entity.Add("Color", directionalLight.Color);
			entity.Add("Shadowed", directionalLight.Shadowed);
			entity.Add("Clouds", directionalLight.CloudShadow);
			entity.Add("CloudVelocity", directionalLight.CloudVelocity);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{			
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\light";
			model.Add(new Binding<Vector3>(model.Color, entity.Get<DirectionalLight>().Color));
			model.Serialize = false;

			entity.Add("EditorModel", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
