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

			slider.Add(new Binding<Direction>(slider.Direction, entity.GetOrCreate<Components.Joint>("Joint").Direction));

			BindCommand(entity, slider.Forward, "Forward");
			BindCommand(entity, slider.Backward, "Backward");
			BindCommand(entity, slider.HitMax, "HitMax");
			BindCommand(entity, slider.HitMin, "HitMin");
		}
	}
}
