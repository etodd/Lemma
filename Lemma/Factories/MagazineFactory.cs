using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class MagazineFactory : Factory
	{
		public MagazineFactory()
		{
			
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Magazine");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PhysicsBlock physics = new PhysicsBlock();
			physics.Size.Value = new Vector3(0.25f, 0.4f, 0.15f);
			physics.Mass.Value = 0.03f;
			result.Add("Physics", physics);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\pistol-mag";
			model.Editable = false;

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			PointLight light = new PointLight();
			light.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
			light.Attenuation.Value = 6.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			PointLight light = result.Get<PointLight>();

			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				Entity pistol = player.GetProperty<Entity.Handle>("Pistol").Value.Target;
				if (pistol != null)
				{
					pistol.GetProperty<int>("Magazines").Value++;
					result.Delete.Execute();
					Sound.PlayCue(main, "Pick Up Mag", transform.Position);
				}
			}));

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
