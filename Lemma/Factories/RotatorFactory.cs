using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class RotatorFactory : Factory<Main>
	{
		public RotatorFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Rotator");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.GetOrCreate<Transform>("Transform");
			this.SetMain(entity, main);

			Property<Vector3> velocity = entity.GetOrMakeProperty<Vector3>("Velocity", true);

			ListProperty<Entity.Handle> targets = entity.GetOrMakeListProperty<Entity.Handle>("Targets");

			List<Transform> transforms = new List<Transform>();
			entity.Add(new PostInitialization
			{
				delegate()
				{
					foreach (Entity.Handle handle in targets)
					{
						Entity e = handle.Target;
						if (e != null && e.Active)
							transforms.Add(e.Get<Transform>());
					}
				}
			});

			entity.Add(new Updater
			{
				delegate(float dt)
				{
					if (transforms.Count == 0)
						entity.Delete.Execute();
					else
					{
						Vector3 v = velocity.Value * dt;
						Quaternion diff = Microsoft.Xna.Framework.Quaternion.CreateFromYawPitchRoll(v.X, v.Y, v.Z);
						for (int i = 0; i < transforms.Count; i++)
						{
							Transform t = transforms[i];
							if (t.Active)
								t.Quaternion.Value *= diff;
							else
							{
								transforms.RemoveAt(i);
								i--;
							}
						}
					}
				}
			});
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			EntityConnectable.AttachEditorComponents(entity, entity.GetOrMakeListProperty<Entity.Handle>("Targets"));
		}
	}
}
