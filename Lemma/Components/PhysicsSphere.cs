using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics;
using Microsoft.Xna.Framework;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionTests;
using System.Xml.Serialization;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using ComponentBind;

namespace Lemma.Components
{
	public class PhysicsSphere : Component<Main>, IUpdateableComponent
	{
		public Property<Matrix> Transform = new Property<Matrix>();
		public Property<float> Mass = new Property<float> { Value = 0.25f };
		public Property<float> Radius = new Property<float> { Value = 0.5f };
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector3> AngularVelocity = new Property<Vector3>();

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();
		[XmlIgnore]
		public Sphere Sphere;

		public override void Awake()
		{
			base.Awake();
			if (this.Mass == 0.0f)
				this.Sphere = new Sphere(Vector3.Zero, this.Radius);
			else
				this.Sphere = new Sphere(Vector3.Zero, this.Radius, this.Mass);
			this.Sphere.CollisionInformation.Events.ContactCreated += new BEPUphysics.BroadPhaseEntries.Events.ContactCreatedEventHandler<EntityCollidable>(Events_ContactCreated);
			this.Sphere.CollisionInformation.CollisionRules.Group = Util.Character.NoCollideGroup;
			this.Add(new SetBinding<Matrix>(this.Transform, delegate(Matrix value)
			{
				this.Sphere.WorldTransform = value;
			}));

			this.Add(new SetBinding<float>(this.Mass, delegate(float value)
			{
				this.Sphere.Mass = value;
			}));

			this.Add(new SetBinding<Vector3>(this.LinearVelocity, delegate(Vector3 value)
			{
				this.Sphere.LinearVelocity = value;
			}));

			this.Add(new SetBinding<Vector3>(this.AngularVelocity, delegate(Vector3 value)
			{
				this.Sphere.AngularVelocity = value;
			}));

			this.Add(new SetBinding<float>(this.Radius, delegate(float s)
			{
				this.Sphere.Radius = s;
				if (s < 0.5f)
					this.Sphere.PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Continuous;
				else
					this.Sphere.PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Discrete;
			}));

			Action remove = delegate()
			{
				if (this.Sphere.Space != null)
					this.main.Space.Remove(this.Sphere);
			};
			this.Add(new CommandBinding(this.Disable, remove));
			this.Add(new CommandBinding(this.OnSuspended, remove));

			Action add = delegate()
			{
				this.Sphere.LinearVelocity = Vector3.Zero;
				if (this.Sphere.Space == null && this.Enabled && !this.Suspended)
					this.main.Space.Add(this.Sphere);
			};
			this.Add(new CommandBinding(this.Enable, add));
			this.Add(new CommandBinding(this.OnResumed, add));

			this.main.Space.Add(this.Sphere);
		}

		void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		void IUpdateableComponent.Update(float dt)
		{
			this.Transform.Value = this.Sphere.WorldTransform;
			this.LinearVelocity.Value = this.Sphere.LinearVelocity;
			this.AngularVelocity.Value = this.Sphere.AngularVelocity;
		}

		public override void delete()
		{
			base.delete();
			if (this.Sphere.Space != null)
				this.main.Space.Remove(this.Sphere);
		}
	}
}
