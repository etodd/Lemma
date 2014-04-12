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
			Entity result = new Entity(main, "SceneryBlock");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.GetOrCreate<Transform>("Transform");
			PhysicsBlock physics = result.GetOrCreate<PhysicsBlock>();
			physics.Serialize = true;
			physics.Size.Value = Vector3.One;
			physics.Editable = false;
			ModelInstance model = result.GetOrCreate<ModelInstance>();
			model.Editable = false;
			model.Serialize = true;

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			Property<string> soundCue = result.GetOrMakeProperty<string>("CollisionSoundCue", false);
			soundCue.Serialize = false;

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			const float volumeMultiplier = 0.1f;

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

			this.SetMain(result, main);

			Property<bool> valid = result.GetOrMakeProperty<bool>("Valid", false);
			valid.Serialize = false;

			Property<string> type = result.GetOrMakeProperty<string>("Type", true);
			type.Set = delegate(string value)
			{
				Map.CellState state;
				if (WorldFactory.StatesByName.TryGetValue(value, out state))
				{
					state.ApplyToBlock(result);
					valid.Value = true;
				}
					
				type.InternalValue = value;
			};
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			result.Add(new Binding<bool>(result.Get<Model>("EditorModel").Enabled, x => !x, result.GetOrMakeProperty<bool>("Valid")));
		}
	}
}
