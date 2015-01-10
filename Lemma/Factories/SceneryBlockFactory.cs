using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class SceneryBlockFactory : Factory<Main>
	{
		public SceneryBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "SceneryBlock");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			PhysicsBlock physics = entity.GetOrCreate<PhysicsBlock>("Physics");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			SceneryBlock sceneryBlock = entity.GetOrCreate<SceneryBlock>("SceneryBlock");
			physics.Add(new Binding<Vector3, float>(physics.Size, x => new Vector3(x), sceneryBlock.Scale));
			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateScale(sceneryBlock.Scale) * transform.Matrix, transform.Matrix, sceneryBlock.Scale));

			this.SetMain(entity, main);

			entity.Add("IsAffectedByGravity", physics.IsAffectedByGravity);
			sceneryBlock.EditorProperties();
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Property<bool> valid = entity.Get<SceneryBlock>().Valid;
			entity.Add(new Binding<bool>(entity.Get<Model>("EditorModel").Enabled, () => Editor.EditorModelsVisible && !valid, valid, Editor.EditorModelsVisible));
		}
	}
}