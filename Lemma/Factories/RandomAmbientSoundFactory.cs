using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Util;

namespace Lemma.Factories
{
	public class RandomAmbientSoundFactory : Factory<Main>
	{
		public RandomAmbientSoundFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "RandomAmbientSound");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.SetMain(entity, main);

			Property<bool> is3D = entity.GetOrMakeProperty<bool>("Is3D", true);

			Property<string> cue = entity.GetOrMakeProperty<string>("Play", true);
			Property<string> stop = entity.GetOrMakeProperty<string>("Stop", true);

			entity.CannotSuspendByDistance = !is3D;
			entity.Add(new NotifyBinding(delegate()
			{
				entity.CannotSuspendByDistance = !is3D;
			}, is3D));

			if (!main.EditorEnabled)
				SoundKiller.Add(entity, stop);

			Property<float> min = entity.GetOrMakeProperty<float>("MinimumInterval", true, 10.0f);
			Property<float> max = entity.GetOrMakeProperty<float>("MaximumInterval", true, 20.0f);

			Random random = new Random();
			float interval = min + ((float)random.NextDouble() * (max - min));
			Updater updater = new Updater
			{
				delegate(float dt)
				{
					interval -= dt;
					if (interval <= 0)
					{
						AkSoundEngine.PostEvent(cue, entity);
						interval = min + ((float)random.NextDouble() * (max - min));
					}
				}
			};
			updater.EnabledWhenPaused = true;
			entity.Add(updater);
		}
	}
}
