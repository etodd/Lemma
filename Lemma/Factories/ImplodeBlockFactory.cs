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
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");
			ImplodeBlock implodeBlock = entity.GetOrCreate<ImplodeBlock>("ImplodeBlock");

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Scale.Value = Vector3.Zero;
			model.Add(new Binding<Vector3>(model.Scale, implodeBlock.Scale));
			model.Add(new Binding<Vector3>(transform.Position, implodeBlock.Position));
			model.Add(new Binding<Quaternion>(transform.Quaternion, implodeBlock.Orientation));

			implodeBlock.Add(new CommandBinding(implodeBlock.Delete, entity.Delete));

			this.SetMain(entity, main);

			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), implodeBlock.Offset);
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
		}

		public void Implode(Main main, Voxel v, Voxel.Coord coord, Voxel.State state, Vector3 target)
		{
			Entity block = this.CreateAndBind(main);
			ImplodeBlock implodeBlock = block.Get<ImplodeBlock>();
			state.ApplyToEffectBlock(block.Get<ModelInstance>());
			implodeBlock.Offset.Value = v.GetRelativePosition(coord);

			implodeBlock.StartPosition.Value = v.GetAbsolutePosition(coord);
			Matrix orientation = v.Transform;
			orientation.Translation = Vector3.Zero;
			implodeBlock.Type.Value = Rift.Style.In;
			implodeBlock.StartOrientation.Value = orientation;
			implodeBlock.EndOrientation.Value = Matrix.CreateRotationX((float)this.random.NextDouble() * (float)Math.PI * 2.0f) * Matrix.CreateRotationY((float)this.random.NextDouble() * (float)Math.PI * 2.0f);
			implodeBlock.EndPosition.Value = target;
			main.Add(block);
		}

		public void BlowAway(Main main, Voxel v, Voxel.Coord coord, Voxel.State state)
		{
			Entity block = this.CreateAndBind(main);
			ImplodeBlock implodeBlock = block.Get<ImplodeBlock>();
			state.ApplyToEffectBlock(block.Get<ModelInstance>());
			implodeBlock.Offset.Value = v.GetRelativePosition(coord);

			Vector3 start = v.GetAbsolutePosition(coord);
			implodeBlock.StartPosition.Value = start;
			Matrix orientation = v.Transform;
			orientation.Translation = Vector3.Zero;
			implodeBlock.Type.Value = Rift.Style.Up;
			implodeBlock.StartOrientation.Value = orientation;
			implodeBlock.EndOrientation.Value = Matrix.CreateRotationX((float)this.random.NextDouble() * (float)Math.PI * 2.0f) * Matrix.CreateRotationY((float)this.random.NextDouble() * (float)Math.PI * 2.0f);
			implodeBlock.EndPosition.Value = start + new Vector3(10, 20, 10);
			main.Add(block);
		}
	}
}
