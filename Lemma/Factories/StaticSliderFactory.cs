using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;

namespace Lemma.Factories
{
	public class StaticSliderFactory : VoxelFactory
	{
		public override Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity entity = new Entity(main, "StaticSlider");

			// The transform has to come before the map component
			// So that its properties get bound correctly
			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			
			Voxel map = this.newVoxelComponent(offsetX, offsetY, offsetZ);
			entity.Add("Voxel", map);

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			Transform mapTransform = entity.GetOrCreate<Transform>("MapTransform");
			mapTransform.Selectable.Value = false;
			StaticSlider slider = entity.GetOrCreate<StaticSlider>("StaticSlider");
			Factory.Get<VoxelFactory>().InternalBind(entity, main, creating, mapTransform);
			slider.Add(new TwoWayBinding<Matrix>(slider.Transform, mapTransform.Matrix));
			slider.Add(new Binding<Matrix>(slider.EditorTransform, transform.Matrix));

			Voxel voxel = entity.Get<Voxel>();
			slider.Add(new Binding<Vector3>(voxel.LinearVelocity, slider.LinearVelocity));

			AkGameObjectTracker.Attach(entity, voxel.Transform);
			SoundKiller.Add(entity, AK.EVENTS.STOP_ALL_OBJECT);

			if (main.EditorEnabled)
				entity.Add(new Binding<Matrix>(entity.GetOrCreate<SliderCommon>("SliderCommon").OriginalTransform, voxel.Transform));

			entity.Add("Forward", slider.Forward);
			entity.Add("Backward", slider.Backward);
			entity.Add("OnHitMax", slider.OnHitMax);
			entity.Add("OnHitMin", slider.OnHitMin);

			entity.Add("Direction", slider.Direction);
			entity.Add("Minimum", slider.Minimum);
			entity.Add("Maximum", slider.Maximum);
			entity.Add("Speed", slider.Speed);
			entity.Add("Goal", slider.Goal);
			entity.Add("StartAtMinimum", slider.StartAtMinimum);
			entity.Add("EnablePhysics", voxel.EnablePhysics);
			entity.Add("Position", slider.Position, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("MovementLoop", slider.MovementLoop, new PropertyEntry.EditorData { Options = WwisePicker.Get(main) });
			entity.Add("MovementStop", slider.MovementStop, new PropertyEntry.EditorData { Options = WwisePicker.Get(main) });

			entity.Add("UVRotation", voxel.UVRotation);
			entity.Add("UVOffset", voxel.UVOffset);

			if (main.EditorEnabled)
				this.attachEditorComponents(entity, main);
		}

		private void attachEditorComponents(Entity entity, Main main)
		{
			StaticSlider slider = entity.Get<StaticSlider>();
			EntityConnectable.AttachEditorComponents(entity, "Parent", slider.Parent);

			Transform transform = entity.Get<Transform>("Transform");
			Transform mapTransform = entity.Get<Transform>("MapTransform");
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\cone";
			model.Serialize = false;
			model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));
			entity.Add("DirectionModel", model);

			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				Matrix m = Matrix.Identity;
				m.Translation = transform.Position;

				if (slider.Direction == Direction.None)
					m.Forward = m.Right = m.Up = Vector3.Zero;
				else
				{
					Vector3 normal = Vector3.TransformNormal(slider.Direction.Value.GetVector(), mapTransform.Matrix);

					m.Forward = -normal;
					if (normal.Equals(Vector3.Up))
						m.Right = Vector3.Left;
					else if (normal.Equals(Vector3.Down))
						m.Right = Vector3.Right;
					else
						m.Right = Vector3.Normalize(Vector3.Cross(normal, Vector3.Down));
					m.Up = -Vector3.Cross(normal, m.Left);
				}
				return m;
			}, transform.Position, mapTransform.Matrix, slider.Direction));
		}
	}
}