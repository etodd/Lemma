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
	public class ImplodeBlockFactory : Factory<Main>
	{
		private Random random = new Random();

		public ImplodeBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "ImplodeBlock");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");
			ImplodeBlock implodeBlock = entity.GetOrCreate<ImplodeBlock>("ImplodeBlock");

			model.Add(new Binding<Matrix>(model.Transform, implodeBlock.Transform));

			implodeBlock.Add(new CommandBinding(implodeBlock.Delete, entity.Delete));

			this.SetMain(entity, main);

			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.Param, implodeBlock.Offset);
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
			if (implodeBlock.StateId != Voxel.t.Empty)
				Voxel.States.All[implodeBlock.StateId].ApplyToEffectBlock(model);
		}

		public void Implode(Main main, Voxel v, Voxel.Coord coord, Voxel.State state, Vector3 target)
		{
			Entity block = this.CreateAndBind(main);
			ImplodeBlock implodeBlock = block.Get<ImplodeBlock>();
			state.ApplyToEffectBlock(block.Get<ModelInstance>());
			implodeBlock.Offset.Value = v.GetRelativePosition(coord);

			implodeBlock.StateId = state.ID;
			implodeBlock.StartPosition = v.GetAbsolutePosition(coord);
			implodeBlock.Type = Rift.Style.In;
			implodeBlock.StartOrientation = Quaternion.CreateFromRotationMatrix(v.Transform);
			implodeBlock.EndOrientation = Quaternion.CreateFromYawPitchRoll((float)this.random.NextDouble() * (float)Math.PI * 2.0f, (float)this.random.NextDouble() * (float)Math.PI * 2.0f, 0.0f);
			implodeBlock.EndPosition = target;
			main.Add(block);
		}

		public void BlowAway(Main main, Voxel v, Voxel.Coord coord, Voxel.State state)
		{
			Entity block = this.CreateAndBind(main);
			ImplodeBlock implodeBlock = block.Get<ImplodeBlock>();
			state.ApplyToEffectBlock(block.Get<ModelInstance>());
			implodeBlock.Offset.Value = v.GetRelativePosition(coord);

			Vector3 start = v.GetAbsolutePosition(coord);
			implodeBlock.StateId = state.ID;
			implodeBlock.StartPosition = start;
			implodeBlock.Type = Rift.Style.Up;
			implodeBlock.StartOrientation = Quaternion.CreateFromRotationMatrix(v.Transform);
			implodeBlock.EndOrientation = Quaternion.CreateFromYawPitchRoll((float)this.random.NextDouble() * (float)Math.PI * 2.0f, (float)this.random.NextDouble() * (float)Math.PI * 2.0f, 0.0f);
			implodeBlock.EndPosition = start + new Vector3(10, 20, 10);
			main.Add(block);
		}
	}
}