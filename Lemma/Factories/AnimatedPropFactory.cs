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
			Entity result = new Entity(main, "AnimatedProp");
			result.Add("Transform", new Transform());
			AnimatedModel model = new AnimatedModel();
			result.Add("Model", model);

			result.Add("Animation", new Property<string> { Editable = true });
			result.Add("Loop", new Property<bool> { Editable = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			AnimatedModel model = result.Get<AnimatedModel>("Model");
			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			Property<string> animation = result.GetProperty<string>("Animation");
			Property<bool> loop = result.GetProperty<bool>("Loop");

			model.Add(new NotifyBinding(delegate()
			{
				foreach (SkinnedModel.Clip clip in model.CurrentClips)
					model.Stop.Execute(clip.Name);
				if (!string.IsNullOrEmpty(animation))
					model.StartClip.Execute(animation, 0, loop, AnimatedModel.DefaultBlendTime);
			}, animation, loop));
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
