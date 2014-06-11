using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public static class VoxelAttachable
	{
		public static void MakeAttachable(Entity entity, Main main, bool deleteIfRemoved = true, bool deleteIfMoved = false, Command deleteCommand = null)
		{
			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			Property<Entity.Handle> voxel = entity.GetOrMakeProperty<Entity.Handle>("AttachedVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("AttachedCoordinate");

			if (main.EditorEnabled)
				return;

			Transform transform = entity.Get<Transform>();

			if (deleteCommand == null)
				deleteCommand = entity.Delete;

			Binding<Matrix> attachmentBinding = null;
			CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellEmptiedBinding = null;

			entity.Add(new NotifyBinding(delegate()
			{
				if (attachmentBinding != null)
				{
					entity.Remove(attachmentBinding);
					entity.Remove(cellEmptiedBinding);
				}

				Voxel m = voxel.Value.Target.Get<Voxel>();
				coord.Value = m.GetCoordinate(Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix));

				Matrix offset = transform.Matrix * Matrix.Invert(Matrix.CreateTranslation(m.Offset) * m.Transform);

				attachmentBinding = new Binding<Matrix>(transform.Matrix, () => offset * Matrix.CreateTranslation(m.Offset) * m.Transform, m.Transform, m.Offset);
				entity.Add(attachmentBinding);

				cellEmptiedBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsEmptied, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
				{
					foreach (Voxel.Coord c in coords)
					{
						if (c.Equivalent(coord))
						{
							if (newMap == null)
							{
								if (deleteIfRemoved)
									deleteCommand.Execute();
							}
							else
							{
								if (deleteIfMoved)
									deleteCommand.Execute();
								else
									voxel.Value = newMap.Entity;
							}
							break;
						}
					}
				});
				entity.Add(cellEmptiedBinding);
			}, voxel));

			entity.Add(new PostInitialization
			{
				delegate()
				{
					if (voxel.Value.Target == null)
					{
						Voxel closestMap = null;
						int closestDistance = 3;
						float closestFloatDistance = 3.0f;
						Vector3 target = Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix);
						foreach (Voxel m in Voxel.Voxels)
						{
							Voxel.Coord targetCoord = m.GetCoordinate(target);
							Voxel.Coord? c = m.FindClosestFilledCell(targetCoord, closestDistance);
							if (c.HasValue)
							{
								float distance = (m.GetRelativePosition(c.Value) - m.GetRelativePosition(targetCoord)).Length();
								if (distance < closestFloatDistance)
								{
									closestFloatDistance = distance;
									closestDistance = (int)Math.Floor(distance);
									closestMap = m;
								}
							}
						}
						if (closestMap == null)
							deleteCommand.Execute();
						else
							voxel.Value = closestMap.Entity;
					}
					else
						voxel.Reset();
				}
			});
		}

		public static void BindTarget(Entity entity, Property<Vector3> target)
		{
			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			Transform transform = entity.Get<Transform>();
			entity.Add(new Binding<Vector3>(target, () => Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix), attachOffset, transform.Matrix));
		}

		public static void BindTarget(Entity entity, Property<Matrix> target)
		{
			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			Transform transform = entity.Get<Transform>();
			entity.Add(new Binding<Matrix>(target, () => Matrix.CreateTranslation(0, 0, attachOffset) * transform.Matrix, attachOffset, transform.Matrix));
		}

		public static void AttachEditorComponents(Entity entity, Main main, Property<Vector3> color = null)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			if (color != null)
				model.Add(new Binding<Vector3>(model.Color, color));

			Property<float> attachmentOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);

			Model editorModel = entity.Get<Model>("EditorModel");
			model.Add(new Binding<bool>(model.Enabled, () => entity.EditorSelected && attachmentOffset > 0, entity.EditorSelected, attachmentOffset));
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(1.0f, 1.0f, x), attachmentOffset));
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
