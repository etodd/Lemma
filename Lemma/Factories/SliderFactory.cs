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
	public class SliderFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Slider");
			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			entity.Add("Voxel", new DynamicVoxel(0, 0, 0));
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Slider slider = entity.GetOrCreate<Slider>("Slider");

			JointFactory.Bind(entity, main, slider.CreateJoint, false, creating);

			Components.Joint joint = entity.GetOrCreate<Components.Joint>("Joint");
			slider.Add(new Binding<Direction>(slider.Direction, joint.Direction));

			entity.Add("Forward", slider.Forward);
			entity.Add("Backward", slider.Backward);
			entity.Add("OnHitMax", slider.OnHitMax);
			entity.Add("OnHitMin", slider.OnHitMin);

			entity.Add("Direction", joint.Direction);
			entity.Add("Minimum", slider.Minimum);
			entity.Add("Maximum", slider.Maximum);
			entity.Add("Locked", slider.Locked);
			entity.Add("Speed", slider.Speed);
			entity.Add("Goal", slider.Goal);
			entity.Add("Servo", slider.Servo);
			entity.Add("StartAtMinimum", slider.StartAtMinimum);

			DynamicVoxel voxel = entity.Get<DynamicVoxel>();
			entity.Add("UVRotation", voxel.UVRotation);
			entity.Add("UVOffset", voxel.UVOffset);
			entity.Add("CannotSuspendByDistance", voxel.CannotSuspendByDistance);
		}
	}
}
