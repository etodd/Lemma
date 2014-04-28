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
	public class MapFactory : Factory<Main>
	{
		public MapFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return this.Create(main, 0, 0, 0);
		}

		public virtual Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity result = new Entity(main, "Map");

			result.Add("Transform", new Transform());
			
			Map map = this.newMapComponent(offsetX, offsetY, offsetZ);
			result.Add("Map", map);

			return result;
		}

		public Entity CreateAndBind(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity result = this.Create(main, offsetX, offsetY, offsetZ);
			this.InternalBind(result, main, true);
			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.InternalBind(result, main, creating);
		}

		public void InternalBind(Entity result, Main main, bool creating = false, Transform transform = null, bool dataOnly = false)
		{
			if (transform == null)
				transform = result.GetOrCreate<Transform>("Transform");

			result.CannotSuspend = false;

			Map map = result.Get<Map>();

			// Apply the position and orientation components to the map
			if (main.EditorEnabled || map.Scale.Value != 1.0f)
			{
				map.Add(new TwoWayBinding<Matrix, Matrix>
				(
					transform.Matrix,
					x => x * Matrix.CreateScale(1.0f / map.Scale),
					new IProperty[] { map.Scale },
					map.Transform,
					x => Matrix.CreateScale(map.Scale) * x,
					new IProperty[] { map.Scale },
					() => true
				));
			}
			else
				map.Add(new TwoWayBinding<Matrix>(transform.Matrix, map.Transform));

			map.Add(new CommandBinding(map.CompletelyEmptied, delegate()
			{
				if (!main.EditorEnabled)
					result.Delete.Execute();
			}));

			Entity world = main.Get("World").FirstOrDefault();

			if (dataOnly)
				map.EnablePhysics.Value = false;
			else
			{
				map.CreateModel = delegate(Vector3 min, Vector3 max, Map.CellState state)
				{
					if (state.Invisible && !main.EditorEnabled)
						return null;

					DynamicModel<Map.MapVertex> model = new DynamicModel<Map.MapVertex>(Map.MapVertex.VertexDeclaration);
					model.EffectFile.Value = "Effects\\Environment";
					model.Lock = new object();
					state.ApplyTo(model);

					/*
					ModelAlpha debug = new ModelAlpha { Serialize = false };
					debug.Alpha.Value = 0.01f;
					debug.DrawOrder.Value = 11; // In front of water
					debug.Color.Value = new Vector3(1.0f, 0.8f, 0.6f);
					debug.Filename.Value = "Models\\alpha-box";
					debug.CullBoundingBox.Value = false;
					debug.DisableCulling.Value = true;
					debug.Add(new Binding<Matrix>(debug.Transform, delegate()
					{
						BoundingBox box = model.BoundingBox;
						return Matrix.CreateScale(box.Max - box.Min) * Matrix.CreateTranslation((box.Max + box.Min) * 0.5f) * transform.Matrix;
					}, transform.Matrix, model.BoundingBox));
					result.Add(debug);
					*/

					if (main.EditorEnabled || map.Scale.Value != 1.0f)
						model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateScale(map.Scale) * transform.Matrix, transform.Matrix, map.Scale));
					else
						model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

					model.Add(new Binding<Vector3>(model.GetVector3Parameter("Offset"), map.Offset));

					Map.CellState s = state;

					if (!s.ShadowCast)
						model.UnsupportedTechniques.Add(Technique.Shadow);

					result.Add(model);

					// We have to create this binding after adding the model to the entity
					// Because when the model loads, it automatically calculates a bounding box for it.
					model.Add(new Binding<BoundingBox, Vector3>(model.BoundingBox, x => new BoundingBox(min - x, max - x), map.Offset));
					model.CullBoundingBox.Value = true;

					return model;
				};
			}

			this.SetMain(result, main);
			map.Offset.Changed();
		}

		protected virtual Map newMapComponent(int offsetX, int offsetY, int offsetZ)
		{
			return new Map(offsetX, offsetY, offsetZ);
		}
	}

	public class DynamicMapFactory : MapFactory
	{
		public override Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity result = base.Create(main, offsetX, offsetY, offsetZ);
			result.Type = "DynamicMap";
			result.ID = Entity.GenerateID(result, main);
			return result;
		}

		protected override Map newMapComponent(int offsetX, int offsetY, int offsetZ)
		{
			return new DynamicMap(offsetX, offsetY, offsetZ);
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main, creating);
			DynamicMap map = result.Get<DynamicMap>();

			const float volumeMultiplier = 0.002f;

			map.Add(new CommandBinding<Collidable, ContactCollection>(map.Collided, delegate(Collidable collidable, ContactCollection contacts)
			{
				ContactInformation contact = contacts[contacts.Count - 1];
				float volume = contact.NormalImpulse * volumeMultiplier;
				if (volume > 0.1f)
				{
					// TODO: figure out Wwise volume parameter
					uint cue = map[contact.Contact.Position - (contact.Contact.Normal * 0.25f)].RubbleEvent;
					AkSoundEngine.PostEvent(cue, result);
				}
			}));
		}
	}
}
