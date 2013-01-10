using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class BlockFactory : Factory
	{
		public BlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Block");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PhysicsBlock physics = new PhysicsBlock();
			result.Add("Physics", physics);

			Property<string> cue = new Property<string> { Value = "ConcreteRubble" };
			result.Add("CollisionSoundCue", cue);

			ModelInstance model = new ModelInstance();
			result.Add("Model", model);
			model.Scale.Value = new Vector3(0.5f);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;

			Transform transform = result.Get<Transform>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			ModelInstance model = result.Get<ModelInstance>();

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			Property<string> soundCue = result.GetProperty<string>("CollisionSoundCue");

			Property<Vector3> scale = new Property<Vector3> { Value = Vector3.One };

			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateScale(scale) * transform.Matrix, scale, transform.Matrix));

			const float volumeMultiplier = 0.05f;

			physics.Add(new CommandBinding<Collidable, ContactCollection>(physics.Collided, delegate(Collidable collidable, ContactCollection contacts)
			{
				float volume = contacts[contacts.Count - 1].NormalImpulse * volumeMultiplier;
				if (volume > 0.1f && soundCue.Value != null)
				{
					Sound sound = Sound.PlayCue(main, soundCue, transform.Position, volume, 0.05f);
					if (sound != null)
						sound.GetProperty("Pitch").Value = 1.0f;
				}
			}));

			result.Add("Fade", new Animation
			(
				new Animation.Delay(5.0f),
				new Animation.Vector3MoveTo(scale, Vector3.Zero, 1.0f),
				new Animation.Execute(delegate() { result.Delete.Execute(); })
			));

			this.SetMain(result, main);
			PhysicsBlock.CancelPlayerCollisions(physics);
		}
	}
}
