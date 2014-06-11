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
			physics.Size.Value = Vector3.One;
			physics.Editable = false;
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");
			model.Editable = false;

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			this.SetMain(entity, main);

			Property<bool> valid = entity.GetOrMakeProperty<bool>("Valid", false);
			valid.Serialize = false;

			Property<Voxel.t> type = entity.GetOrMakeProperty<Voxel.t>("Type", true);
			type.Set = delegate(Voxel.t value)
			{
				if (value == Voxel.t.Empty)
					valid.Value = false;
				else
				{
					Voxel.States[value].ApplyToBlock(entity);
					valid.Value = true;
				}
				type.InternalValue = value;
			};
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			entity.Add(new Binding<bool>(entity.Get<Model>("EditorModel").Enabled, x => !x, entity.GetOrMakeProperty<bool>("Valid")));
		}
	}
}
