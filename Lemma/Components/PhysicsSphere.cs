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
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		public Property<float> Mass = new Property<float> { Editable = true, Value = 0.25f };
		public Property<float> Radius = new Property<float> { Editable = true, Value = 0.5f };
		public Property<Vector3> LinearVelocity = new Property<Vector3> { Editable = false };
		public Property<Vector3> AngularVelocity = new Property<Vector3> { Editable = false };

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();
		[XmlIgnore]
		public Sphere Sphere;

		public override void InitializeProperties()
		{
			if (this.Mass == 0.0f)
				this.Sphere = new Sphere(Vector3.Zero, this.Radius);
			else
				this.Sphere = new Sphere(Vector3.Zero, this.Radius, this.Mass);
			this.Sphere.CollisionInformation.Events.ContactCreated += new BEPUphysics.BroadPhaseEntries.Events.ContactCreatedEventHandler<EntityCollidable>(Events_ContactCreated);
			this.Sphere.CollisionInformation.CollisionRules.Group = Util.Character.NoCollideGroup;
			this.Transform.Set = delegate(Matrix matrix)
			{
				this.Sphere.WorldTransform = matrix;
			};

			this.Transform.Get = delegate()
			{
				return this.Sphere.BufferedStates.InterpolatedStates.WorldTransform;
			};

			this.Mass.Set = delegate(float m)
			{
				this.Sphere.Mass = m;
			};

			this.Mass.Get = delegate()
			{
				return this.Sphere.Mass;
			};

			this.LinearVelocity.Get = delegate()
			{
				return this.Sphere.LinearVelocity;
			};

			this.LinearVelocity.Set = delegate(Vector3 value)
			{
				this.Sphere.LinearVelocity = value;
			};

			this.AngularVelocity.Get = delegate()
			{
				return this.Sphere.AngularVelocity;
			};

			this.AngularVelocity.Set = delegate(Vector3 value)
			{
				this.Sphere.AngularVelocity = value;
			};

			this.Radius.Set = delegate(float s)
			{
				this.Sphere.Radius = s;
				if (s < 0.5f)
					this.Sphere.PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Continuous;
				else
					this.Sphere.PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Discrete;
			};

			this.Radius.Get = delegate()
			{
				return this.Sphere.Radius;
			};

			Action remove = delegate()
			{
				if (this.Sphere.Space != null)
					this.main.Space.Remove(this.Sphere);
			};
			this.Add(new CommandBinding(this.OnDisabled, remove));
			this.Add(new CommandBinding(this.OnSuspended, remove));

			Action add = delegate()
			{
				this.Sphere.LinearVelocity = Vector3.Zero;
				if (this.Sphere.Space == null && this.Enabled && !this.Suspended)
					this.main.Space.Add(this.Sphere);
			};
			this.Add(new CommandBinding(this.OnEnabled, add));
			this.Add(new CommandBinding(this.OnResumed, add));

			this.main.Space.Add(this.Sphere);
		}

		void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		void IUpdateableComponent.Update(float dt)
		{
			this.Transform.Changed();
		}

		protected override void delete()
		{
			base.delete();
			if (this.Sphere.Space != null)
				this.main.Space.Remove(this.Sphere);
		}
	}
}
