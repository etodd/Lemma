using System;
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
using ComponentBind;

namespace Lemma.Factories
{
	public class BouncerFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Bouncer");
			entity.Add("MapTransform", new Transform());
			entity.Add("Transform", new Transform());
			entity.Add("Voxel", new DynamicVoxel(0, 0, 0));
			return entity;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Bouncer bouncer = entity.GetOrCreate<Bouncer>("Bouncer");
			JointFactory.Bind(entity, main, bouncer.CreateJoint, false, creating, false);

			Components.Joint joint = entity.GetOrCreate<Components.Joint>("Joint");

			bouncer.Add(new Binding<Entity.Handle>(bouncer.Parent, joint.Parent));
			bouncer.Add(new Binding<Voxel.Coord>(bouncer.Coord, joint.Coord));

			DynamicVoxel voxel = entity.Get<DynamicVoxel>();
			voxel.KineticFriction.Value = voxel.StaticFriction.Value = 0;

			bouncer.Add(new CommandBinding(voxel.PhysicsUpdated, delegate()
			{
				bouncer.PhysicsUpdated.Execute(voxel.PhysicsEntity.Mass, voxel.PhysicsEntity.Volume);
			}));

			entity.Add("UVRotation", voxel.UVRotation);
			entity.Add("UVOffset", voxel.UVOffset);
			entity.Add("CannotSuspendByDistance", voxel.CannotSuspendByDistance);
		}
	}
}