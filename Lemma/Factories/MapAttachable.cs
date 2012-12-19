using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public static class MapAttachable
	{
		public static void MakeAttachable(Entity entity, Main main)
		{
			Transform transform = entity.Get<Transform>();
			Property<Entity.Handle> map = entity.GetOrMakeProperty<Entity.Handle>("AttachedMap");
			Property<Map.Coordinate> coord = entity.GetOrMakeProperty<Map.Coordinate>("AttachedCoordinate");

			if (main.EditorEnabled)
				return;

			Binding<Matrix> attachmentBinding = null;
			CommandBinding deleteBinding = null;
			CommandBinding<IEnumerable<Map.Coordinate>, Map> cellEmptiedBinding = null;

			entity.Add(new NotifyBinding(delegate()
			{
				if (attachmentBinding != null)
				{
					entity.Remove(attachmentBinding);
					entity.Remove(deleteBinding);
					entity.Remove(cellEmptiedBinding);
				}

				Map m = map.Value.Target.Get<Map>();
				coord.Value = m.GetCoordinate(transform.Position);

				Matrix offset = transform.Matrix * Matrix.Invert(Matrix.CreateTranslation(m.Offset) * m.Transform);

				attachmentBinding = new Binding<Matrix>(transform.Matrix, () => offset * Matrix.CreateTranslation(m.Offset) * m.Transform, m.Transform, m.Offset);
				entity.Add(attachmentBinding);

				deleteBinding = new CommandBinding(m.Delete, entity.Delete);
				entity.Add(deleteBinding);

				cellEmptiedBinding = new CommandBinding<IEnumerable<Map.Coordinate>, Map>(m.CellsEmptied, delegate(IEnumerable<Map.Coordinate> coords, Map newMap)
				{
					foreach (Map.Coordinate c in coords)
					{
						if (c.Equivalent(coord))
						{
							if (newMap == null)
								entity.Delete.Execute();
							else
								map.Value = newMap.Entity;
							break;
						}
					}
				});
				entity.Add(cellEmptiedBinding);
			}, map));

			entity.Add(new PostInitialization
			{
				delegate()
				{
					if (map.Value.Target == null)
					{
						bool found = false;
						foreach (Map m in Map.Maps)
						{
							Map.Coordinate c = m.GetCoordinate(transform.Position);
							Map.Chunk chunk = m.GetChunk(c, false);
							if (chunk != null)
							{
								if (chunk.Instantiated)
								{
									if (m[c].ID != 0)
									{
										map.Value = m.Entity;
										found = true;
										break;
									}
								}
								else
								{
									foreach (Map.Box b in chunk.DataBoxes)
									{
										if (b.Contains(c))
										{
											map.Value = m.Entity;
											found = true;
											break;
										}
									}
									if (found)
										break;
								}
							}
						}
						if (!found)
							entity.Delete.Execute();
					}
					else
						map.Reset();
				}
			});
		}
	}
}
