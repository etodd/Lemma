using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class AmbientSoundFactory : Factory
	{
		public AmbientSoundFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "AmbientSound");

			result.Add("Transform", new Transform());
			result.Add("Sound", new Sound());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);

			Transform transform = result.Get<Transform>();
			Sound sound = result.Get<Sound>("Sound");

			result.CannotSuspendByDistance = !sound.Is3D;
			result.Add(new NotifyBinding(delegate()
			{
				result.CannotSuspendByDistance = !sound.Is3D;
			}, sound.Is3D));

			if (result.GetOrMakeProperty<bool>("Attachable", true))
				MapAttachable.MakeAttachable(result, main);

			sound.Add(new Binding<Vector3>(sound.Position, transform.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);
		}
	}
}
