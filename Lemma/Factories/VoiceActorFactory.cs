using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class VoiceActorFactory : Factory<Main>
	{
		public VoiceActorFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "VoiceActor");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.GetOrCreate<Transform>("Transform");

			VoiceActor actor = result.GetOrCreate<VoiceActor>("VoiceActor");
			actor.Add(new Binding<Vector3>(actor.Position, transform.Position));

			AnimatedModel model = result.GetOrCreate<AnimatedModel>("Model");
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			Model editorModel = result.Get<Model>("EditorModel");
			Model model = result.Get<Model>("Model");
			editorModel.Add(new Binding<bool>(editorModel.Enabled, x => !x, model.IsValid));
		}
	}
}
