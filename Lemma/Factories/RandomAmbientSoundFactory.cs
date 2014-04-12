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
			result.Add("Sound", new Sound());
			result.Add("MinimumInterval", new Property<float> { Value = 10.0f, Editable = true });
			result.Add("MaximumInterval", new Property<float> { Value = 20.0f, Editable = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);

			Transform transform = result.Get<Transform>();
			Sound sound = result.Get<Sound>("Sound");

			sound.Add(new Binding<Vector3>(sound.Position, transform.Position));

			result.CannotSuspendByDistance = !sound.Is3D;
			result.Add(new NotifyBinding(delegate()
			{
				result.CannotSuspendByDistance = !sound.Is3D;
			}, sound.Is3D));

			Property<float> min = result.GetProperty<float>("MinimumInterval");
			Property<float> max = result.GetProperty<float>("MaximumInterval");

			Random random = new Random();
			float interval = min + ((float)random.NextDouble() * (max - min));
			Updater updater = new Updater
			{
				delegate(float dt)
				{
					if (!sound.IsPlaying)
						interval -= dt;
					if (interval <= 0)
					{
						sound.Play.Execute();
						interval = min + ((float)random.NextDouble() * (max - min));
					}
				}
			};
			updater.EnabledWhenPaused.Value = true;
			result.Add(updater);
		}
	}
}
