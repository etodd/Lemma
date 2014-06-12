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
	public class SpinnerFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Spinner");

			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			entity.Add("Voxel", new DynamicVoxel(0, 0, 0));

			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Components.Joint joint = entity.GetOrCreate<Components.Joint>("Joint");
			Spinner spinner = entity.GetOrCreate<Spinner>("Spinner");

			spinner.Add(new Binding<Direction>(spinner.Direction, joint.Direction));

			JointFactory.Bind(entity, main, spinner.CreateJoint, true, creating);

			entity.Add("On", new Command { Action = spinner.On });
			entity.Add("Off", new Command { Action = spinner.Off });
			entity.Add("Forward", new Command { Action = spinner.Forward });
			entity.Add("Backward", new Command { Action = spinner.Backward });
			entity.Add("HitMax", spinner.HitMax);
			entity.Add("HitMin", spinner.HitMin);
		}
	}
}
