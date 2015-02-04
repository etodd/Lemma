using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using BEPUphysics;
using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Paths.PathFollowing;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Bouncer : Component<Main>, IUpdateableComponent
	{
		[XmlIgnore]
		public Command<float, float> PhysicsUpdated = new Command<float, float>(); // mass and volume

		[XmlIgnore]
		public Property<Entity.Handle> Parent = new Property<Entity.Handle>();

		[XmlIgnore]
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		private NoRotationJoint joint = null;
		private EntityMover mover = null;

		private void physicsUpdated(float mass, float volume)
		{
			if (this.mover != null)
			{
				float density = mass / volume;
				this.mover.LinearMotor.Settings.Servo.SpringSettings.StiffnessConstant = 200.0f * density;
				this.mover.LinearMotor.Settings.Servo.SpringSettings.DampingConstant = 7.0f * density;
			}
		}

		public ISpaceObject CreateJoint(BEPUphysics.Entities.Entity entity1, BEPUphysics.Entities.Entity entity2, Vector3 pos, Vector3 direction, Vector3 anchor)
		{
			// entity1 is us
			// entity2 is the main map we are attaching to
			Vector3 originalPos = entity1.Position;
			entity1.Position = pos;
			this.joint = new NoRotationJoint(entity2, entity1);
			entity1.Position = originalPos;
			if (this.mover != null && this.mover.Space != null)
				this.main.Space.Remove(this.mover);
			this.mover = new EntityMover(entity1);
			this.main.Space.Add(this.mover);
			this.physicsUpdated(entity1.Mass, entity1.Volume);
			return this.joint;
		}

		public override void Awake()
		{
			base.Awake();
			this.Add(new CommandBinding<float, float>(this.PhysicsUpdated, this.physicsUpdated));
		}

		public void Update(float dt)
		{
			if (this.mover != null)
			{
				Entity parentEntity = this.Parent.Value.Target;
				if (parentEntity != null && parentEntity.Active)
				{
					Voxel parentMap = parentEntity.Get<Voxel>();
					Voxel.Coord coord = this.Coord;
					this.mover.TargetPosition = parentMap.GetAbsolutePosition(new Vector3(coord.X + 1.0f, coord.Y + 1.0f, coord.Z + 1.0f));
				}
				else
				{
					if (this.mover != null && this.mover.Space != null)
						this.main.Space.Remove(this.mover);
					this.mover = null;
				}
			}
		}
	}
}