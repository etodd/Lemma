using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class EffectBlockFactory : Factory
	{
		public struct BlockEntry
		{
			public Map Map;
			public Map.Coordinate Coordinate;
		}

		private Dictionary<BlockEntry, bool> animatingBlocks = new Dictionary<BlockEntry, bool>();

		public bool IsAnimating(BlockEntry block)
		{
			return this.animatingBlocks.ContainsKey(block);
		}

		public EffectBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "EffectBlock");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			ModelInstance model = new ModelInstance();
			result.Add("Model", model);

			result.Add("Offset", new Property<Vector3> { Editable = false });
			result.Add("Lifetime", new Property<float> { Editable = false });
			result.Add("TotalLifetime", new Property<float> { Editable = true });
			result.Add("StartPosition", new Property<Vector3> { Editable = true });
			result.Add("StartOrientation", new Property<Matrix> { Editable = false });
			result.Add("TargetMap", new Property<Entity.Handle> { Editable = true });
			result.Add("TargetCoord", new Property<Map.Coordinate> { Editable = false });
			result.Add("TargetCellStateID", new Property<int> { Editable = true });
			result.Add("Scale", new Property<bool> { Editable = true, Value = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			Transform transform = result.Get<Transform>();
			ModelInstance model = result.Get<ModelInstance>();

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Scale.Value = Vector3.Zero;

			Property<bool> scale = result.GetProperty<bool>("Scale");
			Property<Vector3> start = result.GetProperty<Vector3>("StartPosition");
			start.Set = delegate(Vector3 value)
			{
				start.InternalValue = value;
				transform.Position.Value = value;
			};
			Property<Matrix> startOrientation = result.GetProperty<Matrix>("StartOrientation");
			Vector3 startEuler = Vector3.Zero;
			startOrientation.Set = delegate(Matrix value)
			{
				startOrientation.InternalValue = value;
				startEuler = Quaternion.CreateFromRotationMatrix(startOrientation).ToEuler();
				transform.Orientation.Value = value;
			};

			Property<Entity.Handle> map = result.GetProperty<Entity.Handle>("TargetMap");
			Property<Map.Coordinate> coord = result.GetProperty<Map.Coordinate>("TargetCoord");
			Property<int> stateId = result.GetProperty<int>("TargetCellStateID");

			Property<float> totalLifetime = result.GetProperty<float>("TotalLifetime");
			Property<float> lifetime = result.GetProperty<float>("Lifetime");

			Property<bool> checkAdjacent = result.GetOrMakeProperty<bool>("CheckAdjacent");

			Updater update = null;
			update = new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;

					float blend = lifetime / totalLifetime;

					if (map.Value.Target == null || !map.Value.Target.Active)
					{
						result.Delete.Execute();
						return;
					}

					Map m = map.Value.Target.Get<Map>();

					if (blend > 1.0f)
					{
						if (stateId != 0)
						{
							Map.Coordinate c = coord;

							bool foundAdjacentCell = false;
							if (checkAdjacent)
							{
								bool avoid = stateId == WorldFactory.StatesByName["Temporary"].ID;
								int avoidID = WorldFactory.StatesByName["AvoidAI"].ID;
								foreach (Direction dir in DirectionExtensions.Directions)
								{
									Map.Coordinate adjacent = c.Move(dir);
									int adjacentID = m[adjacent].ID;
									if (adjacentID != 0 && (!avoid || adjacentID != avoidID))
									{
										foundAdjacentCell = true;
										break;
									}
								}
							}
							else
								foundAdjacentCell = true;

							if (foundAdjacentCell)
							{
								bool foundConflict = false;
								Vector3 absolutePosition = m.GetAbsolutePosition(c);
								foreach (Map m2 in Map.ActivePhysicsMaps)
								{
									if (m2 != m && m2[absolutePosition].ID != 0)
									{
										foundConflict = true;
										break;
									}
								}

								if (!foundConflict)
								{
									Map.CellState state = m[coord];
									if (state.Permanent)
										foundConflict = true;
									else
									{
										if (state.ID != 0)
											m.Empty(coord);
										m.Fill(coord, WorldFactory.States[stateId]);
										m.Regenerate();
										Sound.PlayCue(main, "BuildBlock", transform.Position, 1.0f, 0.06f);
										result.Delete.Execute();
										return;
									}
								}
							}

							// For one reason or another, we can't fill the cell
							// Animate nicely into oblivion
							update.Delete.Execute();
							result.Add(new Animation
							(
								new Animation.Vector3MoveTo(model.Scale, Vector3.Zero, 1.0f),
								new Animation.Execute(result.Delete)
							));
						}
					}
					else
					{
						if (scale)
							model.Scale.Value = new Vector3(blend);
						else
							model.Scale.Value = new Vector3(1.0f);
						Matrix finalOrientation = m.Transform;
						finalOrientation.Translation = Vector3.Zero;
						Vector3 finalEuler = Quaternion.CreateFromRotationMatrix(finalOrientation).ToEuler();
						finalEuler = Vector3.Lerp(startEuler, finalEuler, blend);
						transform.Orientation.Value = Matrix.CreateFromYawPitchRoll(finalEuler.X, finalEuler.Y, finalEuler.Z);

						Vector3 finalPosition = m.GetAbsolutePosition(coord);
						float distance = (finalPosition - start).Length() * 0.1f * Math.Max(0.0f, 0.5f - Math.Abs(blend - 0.5f));

						transform.Position.Value = Vector3.Lerp(start, finalPosition, blend) + new Vector3((float)Math.Sin(blend * Math.PI) * distance);
					}
				},
			};

			result.Add(update);

			BlockEntry entry = new BlockEntry();
			result.Add(new NotifyBinding(delegate()
			{
				if (entry.Map != null)
					this.animatingBlocks.Remove(entry);

				Entity m = map.Value.Target;
				entry.Map = m != null ? m.Get<Map>() : null;
				
				if (entry.Map != null)
				{
					entry.Coordinate = coord;
					this.animatingBlocks[entry] = true;
				}
			}, map, coord, stateId));

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				if (entry.Map != null)
					this.animatingBlocks.Remove(entry);
				entry.Map = null;
			}));

			this.SetMain(result, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), result.GetProperty<Vector3>("Offset"));
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
		}

		public void Setup(Entity result, Entity m, Map.Coordinate c, int s)
		{
			Property<Entity.Handle> map = result.GetProperty<Entity.Handle>("TargetMap");
			Property<Map.Coordinate> coord = result.GetProperty<Map.Coordinate>("TargetCoord");
			Property<int> stateId = result.GetProperty<int>("TargetCellStateID");
			map.InternalValue = m;
			coord.InternalValue = c;
			stateId.InternalValue = s;
			stateId.Changed();
		}
	}
}
