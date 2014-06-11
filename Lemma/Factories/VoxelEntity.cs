using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public static class VoxelEntity
	{
		public static void Attach(Entity entity, Main main)
		{
			Transform transform = entity.Get<Transform>();
			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			Property<Entity.Handle> map = entity.GetOrMakeProperty<Entity.Handle>("AttachedVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("AttachedCoordinate");

			if (main.EditorEnabled)
				return;

			entity.Add(new PostInitialization
			{
				delegate()
				{
					if (map.Value.Target == null)
					{
						Voxel closestMap = null;
						Voxel.Coord? closestCoord = null;
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
									closestCoord = c;
								}
							}
						}
						if (closestMap == null)
							entity.Delete.Execute();
						else
						{
							map.Value = closestMap.Entity;
							coord.Value = closestCoord.Value;
						}
					}
					else
					{
						map.Reset();
						coord.Reset();
					}
				}
			});
		}

		public static void AttachEditorComponents(Entity entity, Main main, Property<Vector3> color = null)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			if (color != null)
				model.Add(new Binding<Vector3>(model.Color, color));
			model.Add(new Binding<bool>(model.Enabled, entity.EditorSelected));
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(1.0f, 1.0f, x), entity.GetOrMakeProperty<float>("AttachmentOffset", true)));
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
