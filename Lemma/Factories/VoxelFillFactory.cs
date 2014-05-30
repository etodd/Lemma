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
	public class VoxelFillFactory : VoxelFactory
	{
		public class CoordinateEntry
		{
			public Voxel.Coord Coord;
			public Vector3 Position;
			public float Distance;
		}

		public override Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity entity = base.Create(main, offsetX, offsetY, offsetZ);
			entity.Type = "VoxelFill";
			entity.ID = Entity.GenerateID(entity, main);
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.InternalBind(entity, main, creating, null, true);
			if (entity.GetOrMakeProperty<bool>("Attached", true))
				VoxelAttachable.MakeAttachable(entity, main);

			Property<Entity.Handle> target = entity.GetOrMakeProperty<Entity.Handle>("Target");

			Voxel map = entity.Get<Voxel>();

			Property<float> intervalMultiplier = entity.GetOrMakeProperty<float>("IntervalMultiplier", true, 1.0f);

			ListProperty<CoordinateEntry> coords = entity.GetOrMakeListProperty<CoordinateEntry>("Coordinates");

			Property<int> index = entity.GetOrMakeProperty<int>("FillIndex");

			Action populateCoords = delegate()
			{
				if (coords.Count == 0)
				{
					Entity targetEntity = target.Value.Target;
					if (targetEntity != null && targetEntity.Active)
					{
						Voxel m = targetEntity.Get<Voxel>();
						foreach (CoordinateEntry e in map.Chunks.SelectMany(c => c.Boxes.SelectMany(x => x.GetCoords())).Select(delegate(Voxel.Coord y)
						{
							Voxel.Coord z = m.GetCoordinate(map.GetAbsolutePosition(y));
							z.Data = y.Data;
							return new CoordinateEntry { Coord = z, };
						}))
							coords.Add(e);
					}
				}
			};

			if (main.EditorEnabled)
				coords.Clear();
			else
				entity.Add(new PostInitialization { populateCoords });

			Property<float> blockLifetime = entity.GetOrMakeProperty<float>("BlockLifetime", true, 0.25f);

			float intervalTimer = 0.0f;
			Updater update = new Updater
			{
				delegate(float dt)
				{
					intervalTimer += dt;
					Entity targetEntity = target.Value.Target;
					if (targetEntity != null && targetEntity.Active && index < coords.Count)
					{
						float interval = 0.03f * intervalMultiplier;
						while (intervalTimer > interval && index < coords.Count)
						{
							EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
							Voxel m = targetEntity.Get<Voxel>();
							
							CoordinateEntry entry = coords[index];
							Entity block = factory.CreateAndBind(main);
							entry.Coord.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
							block.GetProperty<bool>("CheckAdjacent").Value = false;
							block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(entry.Coord);
							block.GetProperty<bool>("Scale").Value = true;

							block.GetProperty<Vector3>("StartPosition").Value = entry.Position + new Vector3(8.0f, 20.0f, 8.0f) * blockLifetime.Value;
							block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * index) * Matrix.CreateRotationY(0.15f * index);

							block.GetProperty<float>("TotalLifetime").Value = blockLifetime;
							factory.Setup(block, targetEntity, entry.Coord, entry.Coord.Data.ID);
							main.Add(block);

							index.Value++;
							intervalTimer -= interval;
						}
					}
					else
						entity.Delete.Execute();
				}
			};
			update.Enabled.Value = index > 0;
			entity.Add("Update", update);

			Action fill = delegate()
			{
				if (index > 0 || update.Enabled)
					return; // We're already filling

				Entity targetEntity = target.Value.Target;
				if (targetEntity != null && targetEntity.Active)
				{
					populateCoords();
					Voxel m = targetEntity.Get<Voxel>();
					Vector3 focusPoint = main.Camera.Position;
					foreach (CoordinateEntry entry in coords)
					{
						entry.Position = m.GetAbsolutePosition(entry.Coord);
						entry.Distance = (focusPoint - entry.Position).LengthSquared();
					}

					List<CoordinateEntry> coordList = coords.ToList();
					coords.Clear();
					coordList.Sort(new LambdaComparer<CoordinateEntry>((x, y) => x.Distance.CompareTo(y.Distance)));
					foreach (CoordinateEntry e in coordList)
						coords.Add(e);

					update.Enabled.Value = true;
				}
			};

			entity.Add("Fill", new Command
			{
				Action = fill
			});

			entity.Add("Trigger", new Command
			{
				Action = delegate()
				{
					fill();
				}
			});
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);

			EntityConnectable.AttachEditorComponents(entity, entity.GetOrMakeProperty<Entity.Handle>("Target"));
		}
	}
}
