using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.UpdateableSystems;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.MathExtensions;
using BEPUphysics.Entities;
using BEPUphysics;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using Lemma.Components;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using System.Xml.Serialization;

namespace Lemma.Util
{
	public class Character : Updateable, IEndOfTimeStepUpdateable
	{
		/// <summary>
		/// A box positioned relative to the character's body used to identify collision pairs with nearby objects that could be possibly stood upon.
		/// </summary>
		private Box collisionPairCollector;

		/// <summary>
		/// The distance above the ground that the bottom of the character's body floats.
		/// </summary>
		public Property<float> SupportHeight = new Property<float>();

		/// <summary>
		/// Rate of increase in the character's speed in the movementDirection.
		/// </summary>
		public Property<float> Acceleration = new Property<float> { Value = 20.0f };

		/// <summary>
		/// The character's physical representation that handles iteractions with the environment.
		/// </summary>
		[XmlIgnore]
		public Cylinder Body;

		/// <summary>
		/// Whether or not the character is currently standing on anything that can be walked upon.
		/// False if there exists no support or the support is too heavily sloped, otherwise true.
		/// </summary>
		public Property<bool> HasTraction = new Property<bool>();

		/// <summary>
		/// Whether or not the character is currently standing on anything.
		/// </summary>
		public Property<bool> IsSupported = new Property<bool>();

		public Property<bool> IsSwimming = new Property<bool>();

		/// <summary>
		/// Initial vertical speed when jumping.
		/// </summary>
		public Property<float> JumpSpeed = new Property<float> { Value = 9.0f };

		public Property<Player.WallRun> WallRunState = new Property<Player.WallRun> { Value = Player.WallRun.None };

		/// <summary>
		/// The maximum slope under which walking forces can be applied.
		/// </summary>
		public Property<float> MaxSlope = new Property<float> { Value = (float)Math.PI * 0.3f };

		/// <summary>
		/// Maximum speed in the movementDirection that can be attained.
		/// </summary>
		public Property<float> MaxSpeed = new Property<float> { Value = 8 };

		/// <summary>
		/// Normalized direction which the character tries to move.
		/// </summary>
		public Property<Vector2> MovementDirection = new Property<Vector2> { Value = Vector2.Zero };

		/// <summary>
		/// Deceleration applied to oppose horizontal movement when the character does not have a steady foothold on the ground (hasTraction == false).
		/// </summary>
		public Property<float> SlidingDeceleration = new Property<float> { Value = 0.3f };

		/// <summary>
		/// Deceleration applied to oppose uncontrolled horizontal movement when the character has a steady foothold on the ground (hasTraction == true).
		/// </summary>
		public Property<float> TractionDeceleration = new Property<float> { Value = 100.0f };

		public Property<bool> EnableWalking = new Property<bool> { Value = true };

		/// <summary>
		/// The location of the player's feet.
		/// </summary>
		public Property<Vector3> SupportLocation = new Property<Vector3>();

		/// <summary>
		/// The physics entity the player is currently standing on.
		/// </summary>
		[XmlIgnore]
		public Property<BEPUphysics.Entities.Entity> SupportEntity = new Property<BEPUphysics.Entities.Entity>();

		[XmlIgnore]
		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();

		private float defaultCharacterHeight;
		private float defaultSupportHeight;

		public Property<bool> Crouched = new Property<bool>();
		public Property<bool> AllowUncrouch = new Property<bool>();

		private Vector3[] rayOffsets;

		public static readonly CollisionGroup NoCollideGroup = new CollisionGroup();
		public static readonly CollisionGroup CharacterGroup = new CollisionGroup();

		static Character()
		{
			CollisionRules.CollisionGroupRules.Add(new CollisionGroupPair(Character.NoCollideGroup, Character.CharacterGroup), CollisionRule.NoBroadPhase);
		}

		private Main main;

		/// <summary>
		/// Constructs a simple character controller.
		/// </summary>
		/// <param name="position">Location to initially place the character.</param>
		/// <param name="characterHeight">The height of the character.</param>
		/// <param name="characterWidth">The diameter of the character.</param>
		/// <param name="supportHeight">The distance above the ground that the bottom of the character's body floats.</param>
		/// <param name="mass">Total mass of the character.</param>
		public Character(Main main, Vector3 position, float characterHeight, float characterWidth, float supportHeight, float mass)
		{
			this.main = main;
			this.Body = new Cylinder(position, characterHeight, characterWidth / 2, mass);
			this.Body.IgnoreShapeChanges = true;
			this.Body.LinearDamping = 0.0f;
			this.Body.CollisionInformation.CollisionRules.Group = Character.CharacterGroup;
			this.defaultCharacterHeight = characterHeight;
			this.Body.CollisionInformation.Events.ContactCreated += new BEPUphysics.Collidables.Events.ContactCreatedEventHandler<EntityCollidable>(Events_ContactCreated);
			this.collisionPairCollector = new Box(position + new Vector3(0, (characterHeight * -0.5f) - supportHeight, 0), characterWidth, supportHeight * 2, characterWidth, 1);
			this.collisionPairCollector.CollisionInformation.CollisionRules.Personal = CollisionRule.NoNarrowPhaseUpdate; //Prevents collision detection/contact generation from being run.
			this.collisionPairCollector.IsAffectedByGravity = false;
			this.collisionPairCollector.CollisionInformation.CollisionRules.Group = Character.CharacterGroup;
			CollisionRules.AddRule(this.collisionPairCollector, this.Body, CollisionRule.NoBroadPhase); //Prevents the creation of any collision pairs between the body and the collector.
			this.SupportHeight.Value = supportHeight;
			this.defaultSupportHeight = supportHeight;

			this.Body.LocalInertiaTensorInverse = new Matrix3X3();
			this.collisionPairCollector.LocalInertiaTensorInverse = new Matrix3X3();

			//Make the body slippery.
			//Note that this will not make all collisions have zero friction;
			//the friction coefficient between a pair of objects is based
			//on a blending of the two objects' materials.
			this.Body.Material.KineticFriction = 0.0f;
			this.Body.Material.StaticFriction = 1.0f;
			this.Body.Material.Bounciness = 0.0f;

			this.Crouched.Set = delegate(bool value)
			{
				bool oldValue = this.Crouched.InternalValue;
				if (value && !oldValue)
				{
					this.Body.Position += new Vector3(0, (this.defaultSupportHeight * -0.5f) + (this.defaultCharacterHeight * -0.25f), 0);
					this.Body.Height = this.defaultCharacterHeight * 0.5f;
					this.SupportHeight.Value = this.defaultSupportHeight * 0.5f;
					this.collisionPairCollector.Height = this.SupportHeight * 2;
				}
				else if (!value && oldValue)
				{
					this.Body.Height = this.defaultCharacterHeight;
					this.Body.Position += new Vector3(0, (this.defaultSupportHeight * 0.5f) + (this.defaultCharacterHeight * 0.25f), 0);
					this.SupportHeight.Value = this.defaultSupportHeight;
					this.collisionPairCollector.Height = this.SupportHeight * 2;
				}
				this.Crouched.InternalValue = value;
			};

			const int rayChecks = 4;
			float radius = this.Body.Radius - 0.1f;
			this.rayOffsets = new[] { Vector3.Zero }.Concat(Enumerable.Range(0, rayChecks).Select(
			delegate(int x)
			{
				float angle = x * ((2.0f * (float)Math.PI) / (float)rayChecks);
				return new Vector3((float)Math.Cos(angle) * radius, 0, (float)Math.Sin(angle) * radius);
			})).ToArray();
		}

		private void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, BEPUphysics.CollisionTests.ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		/// <summary>
		/// Handles the updating of the character.  Called by the owning space object when necessary.
		/// </summary>
		/// <param name="dt">Simulation seconds since the last update.</param>
		void IEndOfTimeStepUpdateable.Update(float dt)
		{
			BEPUphysics.Entities.Entity supportEntity;
			object supportEntityTag;
			Vector3 supportLocation, supportNormal;
			float supportDistance;

			if (this.findSupport(out supportEntityTag, out supportEntity, out supportLocation, out supportNormal, out supportDistance))
			{
				this.SupportEntity.Value = supportEntity;
				this.SupportLocation.Value = supportLocation;
				this.IsSupported.Value = true;
				// Support location only has velocity if we're actually sitting on an entity, as opposed to some static geometry.
				Vector3 supportLocationVelocity;
				if (supportEntity != null)
				{
					supportLocationVelocity = supportEntity.LinearVelocity + //linear component
											  Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
					supportEntity.ActivityInformation.Activate();
				}
				else
					supportLocationVelocity = new Vector3();
				// linear velocity of point on body relative to center

				this.support(supportLocationVelocity, supportNormal, supportDistance);
				this.HasTraction.Value = this.isSupportSlopeWalkable(supportNormal);
				this.handleHorizontalMotion(supportLocationVelocity, supportNormal, dt);
			}
			else
			{
				this.SupportEntity.Value = null;
				this.IsSupported.Value = false;
				this.HasTraction.Value = false;
				if (this.EnableWalking)
				{
					if (this.IsSwimming)
						this.handleNoTraction(dt, 1.0f, 4.0f, 0.5f);
					else
						this.handleNoTraction(dt, 0.0f, 0.1f, 2.0f);
				}
				
			}

			if (this.Crouched && this.AllowUncrouch)
			{
				// Try to uncrouch

				Vector3 rayOrigin = this.Body.Position;
				rayOrigin.Y += this.Body.Height * 0.5f;

				bool foundCeiling = false;

				foreach (Vector3 rayStart in this.rayOffsets.Select(x => x + rayOrigin))
				{
					RayCastResult rayHit;
					//Fire a ray at the candidate and determine some details! 
					if (this.main.Space.RayCast(new Ray(rayStart, Vector3.Up), this.defaultCharacterHeight - this.Body.Height, out rayHit))
					{
						foundCeiling = true;
						break;
					}
				}

				if (!foundCeiling)
					this.Crouched.Value = false;
			}

			this.collisionPairCollector.LinearVelocity = this.Body.LinearVelocity;
			this.collisionPairCollector.Position = this.Body.Position + new Vector3(0, (this.Body.Height * -0.5f) - this.SupportHeight, 0);
		}

		/// <summary>
		/// Locates the closest support entity by performing a raycast at collected candidates.
		/// </summary>
		/// <param name="supportEntity">The closest supporting entity.</param>
		/// <param name="supportLocation">The support location where the ray hit the entity.</param>
		/// <param name="supportNormal">The normal at the surface where the ray hit the entity.</param>
		/// <param name="supportDistance">Distance from the character to the support location.</param>
		/// <returns>Whether or not a support was located.</returns>
		private bool findSupport(out object supportEntityTag, out BEPUphysics.Entities.Entity supportEntity, out Vector3 supportLocation, out Vector3 supportNormal, out float supportDistance)
		{
			supportEntity = null;
			supportEntityTag = null;
			supportLocation = Toolbox.NoVector;
			supportNormal = Toolbox.NoVector;
			supportDistance = float.MaxValue;

			Vector3 rayOrigin = this.Body.Position;
			rayOrigin.Y += this.Body.Height * -0.5f;

			for (int i = 0; i < this.collisionPairCollector.CollisionInformation.Pairs.Count; i++)
			{
				var pair = this.collisionPairCollector.CollisionInformation.Pairs[i];
				//Determine which member of the collision pair is the possible support.
				Collidable candidate = (pair.BroadPhaseOverlap.EntryA == collisionPairCollector.CollisionInformation ? pair.BroadPhaseOverlap.EntryB : pair.BroadPhaseOverlap.EntryA) as Collidable;
				//Ensure that the candidate is a valid supporting entity.
				if (candidate.CollisionRules.Personal >= CollisionRule.NoSolver)
					continue; //It is invalid!

				if (candidate.CollisionRules.Group == Character.NoCollideGroup)
					continue;

				//The maximum length is supportHeight * 2 instead of supportHeight alone because the character should be able to step downwards.
				//This acts like a sort of 'glue' to help the character stick on the ground in general.
				float maximumDistance;
				//The 'glue' effect should only occur if the character has a solid hold on the ground though.
				//Otherwise, the character is falling or sliding around uncontrollably.
				if (this.HasTraction && !this.IsSwimming)
					maximumDistance = (this.SupportHeight * 2.0f);
				else
					maximumDistance = this.SupportHeight;

				foreach (Vector3 rayStart in this.rayOffsets.Select(x => x + rayOrigin))
				{
					RayHit rayHit;
					//Fire a ray at the candidate and determine some details! 
					if (candidate.RayCast(new Ray(rayStart, Vector3.Down), maximumDistance, out rayHit))
					{
						//We want to find the closest support, so compare it against the last closest support.
						if (rayHit.T < supportDistance)
						{
							supportDistance = rayHit.T;
							supportLocation = rayHit.Location;
							supportNormal = rayHit.T > 0 ? rayHit.Normal : Vector3.Up;

							var entityInfo = candidate as EntityCollidable;
							if (entityInfo != null)
							{
								supportEntity = entityInfo.Entity;
								supportEntityTag = supportEntity != null ? supportEntity.Tag : candidate.Tag;
							}
							else
								supportEntityTag = candidate.Tag;
						}
					}
				}
			}

			bool isSupported = supportDistance < float.MaxValue;

			if (!isSupported && this.WallRunState.Value == Player.WallRun.None)
			{
				foreach (Contact contact in this.Body.CollisionInformation.Pairs.SelectMany(x => x.Contacts.Select(y => y.Contact)))
				{
					Vector3 normal = (contact.Position - this.Body.Position).SetComponent(Direction.PositiveY, 0);
					float length = normal.Length();
					if (length > 0.0f)
						this.Body.LinearVelocity += -0.1f * (normal / length);
				}
			}

			supportNormal.Normalize();
			return isSupported;
		}

		/// <summary>
		/// Determines if the ground supporting the character is sloped gently enough to allow for normal walking.
		/// </summary>
		/// <param name="supportNormal">Normal of the surface being stood upon.</param>
		/// <returns>Whether or not the slope is walkable.</returns>
		private bool isSupportSlopeWalkable(Vector3 supportNormal)
		{
			//The following operation is equivalent to performing a dot product between the support normal and Vector3.Down and finding the angle it represents using Acos.
			return Math.Acos(Math.Abs(Math.Min(supportNormal.Y, 1))) <= MaxSlope;
		}

		/// <summary>
		/// Maintains the position of the character's body above the ground.
		/// </summary>
		/// <param name="supportLocationVelocity">Velocity of the support point connected to the supportEntity.</param>
		/// <param name="supportNormal">The normal at the surface where the ray hit the entity.</param>
		/// <param name="supportDistance">Distance from the character to the support location.</param>
		private void support(Vector3 supportLocationVelocity, Vector3 supportNormal, float supportDistance)
		{
			//Put the character at the right distance from the ground.
			float heightDifference = this.SupportHeight - supportDistance;
			this.Body.Position += (new Vector3(0, heightDifference, 0));

			//Remove from the character velocity which would push it toward or away from the surface.
			//This is a relative velocity, so the velocity of the body and the velocity of a point on the support entity must be found.
			float bodyNormalVelocity = Vector3.Dot(this.Body.LinearVelocity, supportNormal);
			float supportEntityNormalVelocity = Vector3.Dot(supportLocationVelocity, supportNormal);
			this.Body.LinearVelocity -= (bodyNormalVelocity - supportEntityNormalVelocity) * supportNormal;
		}

		/// <summary>
		/// Manages movement acceleration, deceleration, and sliding.
		/// </summary>
		/// <param name="supportLocationVelocity">Velocity of the support point connected to the supportEntity.</param>
		/// <param name="supportNormal">The normal at the surface where the ray hit the entity.</param>
		/// <param name="dt">Timestep of the simulation.</param>
		private void handleHorizontalMotion(Vector3 supportLocationVelocity, Vector3 supportNormal, float dt)
		{
			if (this.HasTraction && this.MovementDirection != Vector2.Zero && this.EnableWalking)
			{
				// Identify a coordinate system that uses the support normal as Y.
				// X is the axis point along the left (negative) and right (positive) relative to the movement direction.
				// Z points forward (positive) and backward (negative) in the movement direction modified to be parallel to the surface.
				Vector3 horizontal = new Vector3(this.MovementDirection.Value.X, 0, this.MovementDirection.Value.Y);
				horizontal.Normalize();
				Vector3 x = Vector3.Cross(horizontal, supportNormal);
				Vector3 z = Vector3.Cross(supportNormal, x);

				// Remove from the character a portion of velocity which pushes it horizontally off the desired movement track defined by the movementDirection.
				float bodyXVelocity = Vector3.Dot(this.Body.LinearVelocity, x);
				float supportEntityXVelocity = Vector3.Dot(supportLocationVelocity, x);
				float velocityChange = MathHelper.Clamp(bodyXVelocity - supportEntityXVelocity, -dt * this.TractionDeceleration, dt * this.TractionDeceleration);
				this.Body.LinearVelocity -= velocityChange * x;

				float bodyZVelocity = Vector3.Dot(Body.LinearVelocity, z);
				float supportEntityZVelocity = Vector3.Dot(supportLocationVelocity, z);
				float netZVelocity = bodyZVelocity - supportEntityZVelocity;
				// The velocity difference along the Z axis should accelerate/decelerate to match the goal velocity (max speed).
				float speed = this.Crouched ? this.MaxSpeed * 0.5f : this.MaxSpeed;
				if (netZVelocity > speed)
				{
					// Decelerate
					velocityChange = Math.Min(dt * this.TractionDeceleration, netZVelocity - speed);
					this.Body.LinearVelocity -= velocityChange * z;
				}
				else
				{
					// Accelerate
					velocityChange = Math.Min(dt * this.Acceleration, speed - netZVelocity);
					this.Body.LinearVelocity += velocityChange * z;
				}
			}
			else
			{
				float deceleration;
				if (this.HasTraction)
					deceleration = dt * this.TractionDeceleration;
				else
					deceleration = dt * this.SlidingDeceleration;
				//Remove from the character a portion of velocity defined by the deceleration.
				Vector3 bodyHorizontalVelocity = this.Body.LinearVelocity - Vector3.Dot(this.Body.LinearVelocity, supportNormal) * supportNormal;
				Vector3 supportHorizontalVelocity = supportLocationVelocity - Vector3.Dot(supportLocationVelocity, supportNormal) * supportNormal;
				Vector3 relativeVelocity = bodyHorizontalVelocity - supportHorizontalVelocity;
				float speed = relativeVelocity.Length();
				if (speed > 0)
				{
					Vector3 horizontalDirection = relativeVelocity / speed;
					float velocityChange = Math.Min(speed, deceleration);
					this.Body.LinearVelocity -= velocityChange * horizontalDirection;
				}
			}
		}

		private void handleNoTraction(float dt, float tractionDecelerationRatio, float accelerationRatio, float speedRatio)
		{
			float tractionDeceleration = this.TractionDeceleration * tractionDecelerationRatio;
			float acceleration = this.Acceleration * accelerationRatio;
			float maxSpeed = this.MaxSpeed * speedRatio;
			if (this.MovementDirection != Vector2.Zero)
			{
				//Identify a coordinate system that uses the support normal as Y.
				//X is the axis point along the left (negative) and right (positive) relative to the movement direction.
				//Z points forward (positive) and backward (negative) in the movement direction modified to be parallel to the surface.
				Vector3 horizontal = new Vector3(this.MovementDirection.Value.X, 0, this.MovementDirection.Value.Y);
				horizontal.Normalize();
				Vector3 x = Vector3.Cross(horizontal, Vector3.Up);
				Vector3 z = Vector3.Cross(Vector3.Up, x);

				//Remove from the character a portion of velocity which pushes it horizontally off the desired movement track defined by the movementDirection.
				float bodyXVelocity = Vector3.Dot(this.Body.LinearVelocity, x);
				float velocityChange = MathHelper.Clamp(bodyXVelocity, -dt * tractionDeceleration, dt * tractionDeceleration);
				this.Body.LinearVelocity -= velocityChange * x;

				float bodyZVelocity = Vector3.Dot(Body.LinearVelocity, z);
				//The velocity difference along the Z axis should accelerate/decelerate to match the goal velocity (max speed).
				if (bodyZVelocity > maxSpeed)
				{
					//Decelerate
					velocityChange = Math.Min(dt * tractionDeceleration, bodyZVelocity - maxSpeed);
					this.Body.LinearVelocity -= velocityChange * z;
				}
				else
				{
					//Accelerate
					velocityChange = Math.Min(dt * acceleration, maxSpeed - bodyZVelocity);
					this.Body.LinearVelocity += velocityChange * z;
				}
			}
			else
			{
				float deceleration = dt * tractionDeceleration;
				//Remove from the character a portion of velocity defined by the deceleration.
				Vector3 bodyHorizontalVelocity = this.Body.LinearVelocity;
				bodyHorizontalVelocity.Y = 0.0f;
				float speed = bodyHorizontalVelocity.Length();
				if (speed > 0)
				{
					Vector3 horizontalDirection = bodyHorizontalVelocity / speed;
					float velocityChange = Math.Min(speed, deceleration);
					this.Body.LinearVelocity -= velocityChange * horizontalDirection;
				}
			}
		}

		/// <summary>
		/// Activates the character, adding its components to the space. 
		/// </summary>
		public void Activate()
		{
			if (!this.IsUpdating)
			{
				this.IsUpdating = true;
				if (this.Body.Space == null)
				{
					this.Space.Add(this.Body);
					this.Space.Add(this.collisionPairCollector);
				}
				this.HasTraction.Value = false;
				this.IsSupported.Value = false;
				this.Body.LinearVelocity = Vector3.Zero;
			}
		}

		/// <summary>
		/// Deactivates the character, removing its components from the space.
		/// </summary>
		public void Deactivate()
		{
			if (this.IsUpdating)
			{
				this.IsUpdating = false;
				this.Body.Position = new Vector3(10000, 0, 0);
				if (this.Body.Space != null)
				{
					this.Body.Space.Remove(this.Body);
					this.collisionPairCollector.Space.Remove(this.collisionPairCollector);
				}
			}
		}

		/// <summary>
		/// Called by the engine when the character is added to the space.
		/// Activates the character.
		/// </summary>
		/// <param name="newSpace">Space to which the character was added.</param>
		public override void OnAdditionToSpace(ISpace newSpace)
		{
			base.OnAdditionToSpace(newSpace); //sets this object's space to the newSpace.
			this.Activate();
		}

		/// <summary>
		/// Called by the engine when the character is removed from the space.
		/// Deactivates the character.
		/// </summary>
		public override void OnRemovalFromSpace(ISpace oldSpace)
		{
			this.Deactivate();
			base.OnRemovalFromSpace(oldSpace); //Sets this object's space to null.
		}
	}
}
