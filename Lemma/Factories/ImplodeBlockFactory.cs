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

		private const float totalLifetime = 0.4f;

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			ModelInstance model = entity.GetOrCreate<ModelInstance>("Model");

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));
			model.Scale.Value = Vector3.Zero;

			Property<bool> scale = entity.GetOrMakeProperty<bool>("Scale", false, true);
			Property<Vector3> start = entity.GetOrMakeProperty<Vector3>("StartPosition");
			start.Set = delegate(Vector3 value)
			{
				start.InternalValue = value;
				transform.Position.Value = value;
			};
			Property<Vector3> end = entity.GetOrMakeProperty<Vector3>("EndPosition");
			Property<Matrix> startOrientation = entity.GetOrMakeProperty<Matrix>("StartOrientation");
			Quaternion startQuat = Quaternion.Identity;
			startOrientation.Set = delegate(Matrix value)
			{
				startOrientation.InternalValue = value;
				startQuat = Quaternion.CreateFromRotationMatrix(startOrientation);
				transform.Orientation.Value = value;
			};

			Property<Matrix> endOrientation = entity.GetOrMakeProperty<Matrix>("EndOrientation");
			Quaternion endQuat = Quaternion.Identity;
			endOrientation.Set = delegate(Matrix value)
			{
				endOrientation.InternalValue = value;
				endQuat = Quaternion.CreateFromRotationMatrix(endOrientation);
				transform.Orientation.Value = value;
			};

			Property<float> lifetime = entity.GetOrMakeProperty<float>("Lifetime");

			Updater update = null;
			update = new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;

					float blend = lifetime / totalLifetime;

					if (blend > 1.0f)
					{
						entity.Delete.Execute();
					}
					else
					{
						model.Scale.Value = new Vector3(1.0f - blend);
						transform.Orientation.Value = Matrix.CreateFromQuaternion(Quaternion.Lerp(startQuat, endQuat, blend));
						transform.Position.Value = Vector3.Lerp(start, end, blend);
					}
				},
			};

			entity.Add(update);

			this.SetMain(entity, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), entity.GetOrMakeProperty<Vector3>("Offset"));
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
		}

		public void Implode(Main main, Voxel v, Voxel.Coord coord, Voxel.State state, Vector3 target)
		{
			Entity block = this.CreateAndBind(main);
			state.ApplyToEffectBlock(block.Get<ModelInstance>());
			block.GetOrMakeProperty<Vector3>("Offset").Value = v.GetRelativePosition(coord);

			block.GetOrMakeProperty<Vector3>("StartPosition").Value = v.GetAbsolutePosition(coord);
			Matrix orientation = v.Transform;
			orientation.Translation = Vector3.Zero;
			block.GetOrMakeProperty<Matrix>("StartOrientation").Value = orientation;
			block.GetOrMakeProperty<Matrix>("EndOrientation").Value = Matrix.CreateRotationX((float)this.random.NextDouble() * (float)Math.PI * 2.0f) * Matrix.CreateRotationY((float)this.random.NextDouble() * (float)Math.PI * 2.0f);
			block.GetOrMakeProperty<Vector3>("EndPosition").Value = target;
			main.Add(block);
		}
	}
}
