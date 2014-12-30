using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class BlockFactory : Factory<Main>
	{
		public BlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Block");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;

			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PhysicsBlock physics = entity.GetOrCreate<PhysicsBlock>("Physics");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");
			model.Scale.Value = new Vector3(0.5f);

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			Property<Vector3> scale = new Property<Vector3> { Value = Vector3.One };

			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateScale(scale) * transform.Matrix, scale, transform.Matrix));

			entity.Add("Fade", new Animation
			(
				new Animation.Delay(5.0f),
				new Animation.Vector3MoveTo(scale, Vector3.Zero, 1.0f),
				new Animation.Execute(entity.Delete)
			));

			this.SetMain(entity, main);
			PhysicsBlock.CancelPlayerCollisions(physics);
		}
	}
}