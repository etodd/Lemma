using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class AnimatedPropFactory : Factory<Main>
	{
		public AnimatedPropFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "AnimatedProp");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			AnimatedModel model = entity.GetOrCreate<AnimatedModel>("Model");
			model.MapContent.Value = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			this.SetMain(entity, main);
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			Property<string> animation = entity.GetOrMakeProperty<string>("Animation", true);
			Property<bool> loop = entity.GetOrMakeProperty<bool>("Loop", true);

			model.Add(new NotifyBinding(delegate()
			{
				foreach (SkinnedModel.Clip clip in model.CurrentClips)
					model.Stop(clip.Name);
				if (!string.IsNullOrEmpty(animation))
					model.StartClip(animation, 0, loop, AnimatedModel.DefaultBlendTime);
			}, animation, loop));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			Model model = entity.Get<Model>("Model");
			editorModel.Add(new Binding<bool>(editorModel.Enabled, x => !x, model.IsValid));
		}
	}
}
