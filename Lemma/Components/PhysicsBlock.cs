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
	public class PhysicsBlock : Component<Main>, IUpdateableComponent
	{
		public Property<Matrix> Transform = new Property<Matrix>();
		public Property<float> Mass = new Property<float> { Value = 0.25f };
		public Property<Vector3> Size = new Property<Vector3> { Value = new Vector3(0.5f) };
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector3> AngularVelocity = new Property<Vector3>();

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();
		[XmlIgnore]
		public Box Box;

		public static void CancelPlayerCollisions(PhysicsBlock block)
		{
			block.Box.CollisionInformation.CollisionRules.Group = Util.Character.NoCollideGroup;
		}

		public override void Awake()
		{
			base.Awake();
			if (this.Mass == 0.0f)
				this.Box = new Box(Vector3.Zero, this.Size.Value.X, this.Size.Value.Y, this.Size.Value.Z);
			else
				this.Box = new Box(Vector3.Zero, this.Size.Value.X, this.Size.Value.Y, this.Size.Value.Z, this.Mass);
			this.Box.CollisionInformation.Events.ContactCreated += new BEPUphysics.BroadPhaseEntries.Events.ContactCreatedEventHandler<EntityCollidable>(Events_ContactCreated);
			this.Transform.Set = delegate(Matrix matrix)
			{
				this.Box.WorldTransform = matrix;
			};

			this.Transform.Get = delegate()
			{
				return this.Box.WorldTransform;
			};

			this.Mass.Set = delegate(float m)
			{
				this.Box.Mass = m;
			};

			this.Mass.Get = delegate()
			{
				return this.Box.Mass;
			};

			this.LinearVelocity.Get = delegate()
			{
				return this.Box.LinearVelocity;
			};

			this.LinearVelocity.Set = delegate(Vector3 value)
			{
				this.Box.LinearVelocity = value;
			};

			this.AngularVelocity.Get = delegate()
			{
				return this.Box.AngularVelocity;
			};

			this.AngularVelocity.Set = delegate(Vector3 value)
			{
				this.Box.AngularVelocity = value;
			};

			this.Size.Set = delegate(Vector3 s)
			{
				this.Box.Width = s.X;
				this.Box.Height = s.Y;
				this.Box.Length = s.Z;
			};

			this.Size.Get = delegate()
			{
				return new Vector3(this.Box.Width, this.Box.Height, this.Box.Length);
			};

			Action remove = delegate()
			{
				if (this.Box.Space != null)
					this.main.Space.Remove(this.Box);
			};
			this.Add(new CommandBinding(this.Disable, remove));
			this.Add(new CommandBinding(this.OnSuspended, remove));

			if (!this.Enabled)
				remove();

			Action add = delegate()
			{
				this.Box.LinearVelocity = Vector3.Zero;
				if (this.Box.Space == null && this.Enabled && !this.Suspended)
					this.main.Space.Add(this.Box);
			};
			this.Add(new CommandBinding(this.Enable, add));
			this.Add(new CommandBinding(this.OnResumed, add));

			this.main.Space.Add(this.Box);
		}

		void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		void IUpdateableComponent.Update(float dt)
		{
			this.Transform.Changed();
		}

		public override void delete()
		{
			base.delete();
			if (this.Box.Space != null)
				this.main.Space.Remove(this.Box);
		}
	}
}
