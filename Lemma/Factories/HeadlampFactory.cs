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
	public class HeadlampFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Headlamp");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PhysicsBlock physics = new PhysicsBlock();
			physics.Size.Value = new Vector3(0.5f, 0.5f, 0.5f);
			physics.Mass.Value = 0.1f;
			result.Add("Physics", physics);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\headlamp";
			model.Editable = false;

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			SpotLight light = new SpotLight();
			light.Color.Value = new Vector3(1.0f, 1.0f, 1.0f);
			light.Attenuation.Value = 20.0f;
			light.CookieTextureFile.Value = "Images\\headlamp-cookie";
			light.Shadowed.Value = true;
			result.Add("Light", light);

			result.Add("Attached", new Property<bool> { Value = false, Editable = false });
			result.Add("Active", new Property<bool> { Value = false, Editable = false });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			SpotLight light = result.Get<SpotLight>();
			Property<bool> attached = result.GetProperty<bool>("Attached");
			Property<bool> active = result.GetProperty<bool>("Active");

			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Quaternion>(light.Orientation, transform.Quaternion));
			light.Add(new Binding<bool>(light.Enabled, () => (!attached) || active, attached, active));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Add(new Binding<bool>(model.Enabled, x => !x, attached));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new Binding<bool>(trigger.Enabled, x => !x, attached));
			physics.Add(new Binding<bool>(physics.Enabled, x => !x, attached));

			TwoWayBinding<Matrix> physicsBinding = new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform);
			physics.Add(physicsBinding);

			Binding<Matrix> attachBinding = null;

			Command<Property<Matrix>> attach = new Command<Property<Matrix>>
			{
				Action = delegate(Property<Matrix> parent)
				{
					attached.Value = true;

					Matrix rotation = Matrix.CreateRotationX((float)Math.PI * 1.0f);

					attachBinding = new Binding<Matrix>(transform.Matrix, x => rotation * parent, parent);
					result.Add(attachBinding);

					physicsBinding.Enabled = false;
				}
			};
			result.Add("Attach", attach);

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				Property<Entity.Handle> headlampProperty = player.GetProperty<Entity.Handle>("Headlamp");
				if (headlampProperty.Value.Target != null)
				{
					// Player already has a headlamp.
					result.Delete.Execute();
				}
				else
				{
					headlampProperty.Value = result;
					active.Value = true;
				}
			}));

			result.Add("Detach", new Command
			{
				Action = delegate()
				{
					active.Value = false;
					attached.Value = false;

					physicsBinding.Enabled = true;

					if (attachBinding != null)
					{
						result.Remove(attachBinding);
						attachBinding = null;
					}
				}
			});

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
