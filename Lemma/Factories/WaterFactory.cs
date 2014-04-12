using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class WaterFactory : Factory<Main>
	{
		public WaterFactory()
		{
			this.Color = new Vector3(0.8f, 0.8f, 0.8f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Water");

			Transform transform = new Transform();
			result.Add("Transform", transform);
			Water water = new Water();
			result.Add("Water", water);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Property<bool> cannotSuspendByDistance = result.GetOrMakeProperty<bool>("CannotSuspendByDistance", true, true);
			cannotSuspendByDistance.Set = delegate(bool value)
			{
				cannotSuspendByDistance.InternalValue = value;
				result.CannotSuspendByDistance = value;
			};
			Transform transform = result.Get<Transform>();
			Water water = result.Get<Water>();

			Sound sound = result.GetOrCreate<Sound>("SoundLoop");
			sound.Cue.Value = "Water Loop";
			sound.IsPlaying.Value = true;
			sound.Is3D.Value = true;
			sound.Serialize = false;
			sound.Editable = false;

			result.Add(new Updater
			{
				delegate(float dt)
				{
					Vector3 pos = main.Camera.Position;
					BoundingBox box = water.Fluid.BoundingBox;
					pos.X = Math.Max(box.Min.X, Math.Min(pos.X, box.Max.X));
					pos.Y = transform.Position.Value.Y;
					pos.Z = Math.Max(box.Min.Z, Math.Min(pos.Z, box.Max.Z));
					sound.Position.Value = pos;
				}
			});

			this.SetMain(result, main);

			water.Add(new TwoWayBinding<Vector3>(water.Position, transform.Position));
		}
	}
}
