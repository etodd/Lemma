using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class ParticleEmitterFactory : Factory<Main>
	{
		public ParticleEmitterFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "ParticleEmitter");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			this.SetMain(entity, main);
			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("ParticleEmitter");
			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				VoxelAttachable.MakeAttachable(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			ParticleEmitter emitter = entity.Get<ParticleEmitter>();
			Property<bool> editorSelected = entity.GetOrMakeProperty<bool>("EditorSelected");
			editorSelected.Serialize = false;
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !editorSelected || emitter.ParticleType.Value == null, editorSelected, emitter.ParticleType));

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}