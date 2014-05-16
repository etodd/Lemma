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
			Entity result = new Entity(main, "RandomAmbientSound");

			result.Add("Transform", new Transform());
			result.Add("MinimumInterval", new Property<float> { Value = 10.0f, Editable = true });
			result.Add("MaximumInterval", new Property<float> { Value = 20.0f, Editable = true });

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

			Property<float> min = result.GetProperty<float>("MinimumInterval");
			Property<float> max = result.GetProperty<float>("MaximumInterval");

			Random random = new Random();
			float interval = min + ((float)random.NextDouble() * (max - min));
			Updater updater = new Updater
			{
				delegate(float dt)
				{
					if (interval <= 0)
					{
						AkSoundEngine.PostEvent(cue, result);
						interval = min + ((float)random.NextDouble() * (max - min));
					}
				}
			};
			updater.EnabledWhenPaused = true;
			result.Add(updater);
		}
	}
}
