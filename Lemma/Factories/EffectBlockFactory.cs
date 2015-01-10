using System; using ComponentBind;
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
	public class EffectBlockFactory : Factory<Main>
	{
		public struct BlockBuildOrder
		{
			public Voxel Voxel;
			public Voxel.Coord Coordinate;
			public Voxel.State State;
		}

		private Random random = new Random();

		public EffectBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "EffectBlock");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");

			EffectBlock effectBlock = entity.GetOrCreate<EffectBlock>("EffectBlock");

			model.Add(new Binding<Matrix>(model.Transform, effectBlock.Transform));

			entity.Add(new CommandBinding(effectBlock.Delete, entity.Delete));

			this.SetMain(entity, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), effectBlock.Offset);
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
			if (effectBlock.StateId != Voxel.t.Empty)
				Voxel.States.All[effectBlock.StateId].ApplyToEffectBlock(model);
		}

		public void Build(Main main, IEnumerable<BlockBuildOrder> blocks, Vector3 center, float delayMultiplier = 0.05f)
		{
			int index = 0;
			int playerCreatedBlocks = 0;
			foreach (BlockBuildOrder entry in blocks)
			{
				if (EffectBlock.IsAnimating(new EffectBlock.Entry { Voxel = entry.Voxel, Coordinate = entry.Coordinate }))
					continue;

				Entity entity = this.CreateAndBind(main);
				EffectBlock effectBlock = entity.Get<EffectBlock>();
				entry.State.ApplyToEffectBlock(entity.Get<ModelInstance>());
				effectBlock.Offset.Value = entry.Voxel.GetRelativePosition(entry.Coordinate);

				Vector3 absolutePos = entry.Voxel.GetAbsolutePosition(entry.Coordinate);

				float distance = (absolutePos - center).Length();
				effectBlock.StartPosition = absolutePos + new Vector3(0.05f, 0.1f, 0.05f) * distance;
				effectBlock.StartOrientation = Quaternion.CreateFromYawPitchRoll(0.15f * (distance + index), 0.15f * (distance + index), 0);
				effectBlock.TotalLifetime = Math.Max(delayMultiplier, distance * delayMultiplier);
				effectBlock.CheckAdjacent = true;
				effectBlock.Setup(entry.Voxel.Entity, entry.Coordinate, entry.State.ID);
				main.Add(entity);
				index++;
				if (entry.State.ID == Voxel.t.Blue)
					playerCreatedBlocks++;
			}
			SteamWorker.IncrementStat("stat_blocks_created", playerCreatedBlocks);
		}
	}
}
