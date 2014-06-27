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
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");

			EffectBlock effectBlock = entity.GetOrCreate<EffectBlock>("EffectBlock");

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Scale.Value = Vector3.Zero;

			entity.Add(new NotifyBinding(delegate()
			{
				transform.Position.Value = effectBlock.StartPosition;
			}, effectBlock.StartPosition));
			entity.Add(new CommandBinding(effectBlock.Delete, entity.Delete));

			entity.Add(new NotifyBinding(delegate()
			{
				transform.Quaternion.Value = effectBlock.StartOrientation;
			}, effectBlock.StartOrientation));

			model.Add(new Binding<Vector3>(model.Scale, effectBlock.Scale));
			transform.Add(new Binding<Vector3>(transform.Position, effectBlock.Position));
			transform.Add(new Binding<Quaternion>(transform.Quaternion, effectBlock.Orientation));

			this.SetMain(entity, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), effectBlock.Offset);
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
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
				effectBlock.StartPosition.Value = absolutePos + new Vector3(0.05f, 0.1f, 0.05f) * distance;
				effectBlock.StartOrientation.Value = Quaternion.CreateFromYawPitchRoll(0.15f * (distance + index), 0.15f * (distance + index), 0);
				effectBlock.TotalLifetime.Value = Math.Max(delayMultiplier, distance * delayMultiplier);
				effectBlock.CheckAdjacent.Value = true;
				effectBlock.Setup(entry.Voxel.Entity, entry.Coordinate, entry.State.ID);
				main.Add(entity);
				index++;
				if (entry.State.ID == Voxel.t.Temporary)
					playerCreatedBlocks++;
			}
			SteamWorker.IncrementStat("stat_blocks_created", playerCreatedBlocks);
		}
	}
}
