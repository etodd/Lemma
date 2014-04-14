using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class AmbientSoundFactory : Factory<Main>
	{
		public AmbientSoundFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "AmbientSound");

			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);

			Transform transform = result.Get<Transform>();

			Property<bool> is3D = result.GetOrMakeProperty<bool>("Is3D", true);

			Property<string> cue = result.GetOrMakeProperty<string>("Cue", true);

			result.CannotSuspendByDistance = !is3D;
			result.Add(new NotifyBinding(delegate()
			{
				result.CannotSuspendByDistance = !is3D;
			}, is3D));

			if (result.GetOrMakeProperty<bool>("Attachable", true))
				MapAttachable.MakeAttachable(result, main);

			if (!main.EditorEnabled)
				AkSoundEngine.PostEvent(cue, result);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);
		}
	}
}
