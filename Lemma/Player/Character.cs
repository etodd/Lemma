using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.UpdateableSystems;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Lemma.Components;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using System.Xml.Serialization;

namespace Lemma.Util
{
	public class Character : Updateable, IEndOfTimeStepUpdateable
	{
		public const float DefaultMaxSpeed = 8.8f;
		public const float DefaultJumpSpeed = 9.5f;
		public const float DefaultRadius = 1.75f * 0.5f;
		public const float DefaultHeight = 2.75f;
		public const float DefaultCrouchedHeight = 1.7f;
		public const float DefaultSupportHeight = 1.25f;
		public const float DefaultCrouchedSupportHeight = 0.5f;
		public const float DefaultTotalHeight = DefaultHeight + DefaultSupportHeight;
		public const float DefaultCrouchedTotalHeight = DefaultCrouchedHeight + DefaultCrouchedSupportHeight;
		public const float DefaultMass = 4.0f;

		public const float InitialAccelerationSpeedThreshold = 4.0f;

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
		public Property<float> Acceleration = new Property<float> { Value = 4.5f };

		public Property<float> InitialAcceleration = new Property<float> { Value = 25.0f };

		/// <summary>
		/// The character's physical representation that handles iteractions with the environment.
		/// </summary>
		[XmlIgnore]
		public Capsule Body;

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
		public Property<float> JumpSpeed = new Property<float> { Value = Character.DefaultJumpSpeed };

		// Input property
		public Property<WallRun.State> WallRunState = new Property<WallRun.State>();

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

		public Property<bool> SwimUp = new Property<bool>();

		public Property<float> Height = new Property<float>();
		public Property<float> Radius = new Property<float>();
		public Property<float> Mass = new Property<float>();

		/// <summary>
		/// Deceleration applied to oppose horizontal movement when the character does not have a steady foothold on the ground (hasTraction == false).
		/// </summary>
		public Property<float> SlidingDeceleration = new Property<float> { Value = 0.3f };

		public Property<float> NoTractionAcceleration = new Property<float> { Value = 7.0f };

		public Property<Vector3> VelocityAdjustments = new Property<Vector3>();

		/// <summary>
		/// Deceleration applied to oppose uncontrolled horizontal movement when the character has a steady foothold on the ground (hasTraction == true).
		/// </summary>
		public Property<float> TractionDeceleration = new Property<float> { Value = 100.0f };

		public Property<bool> EnableWalking = new Property<bool> { Value = true };

		/// <summary>
		/// The location of the player's feet.
		/// </summary>
		public Property<Vector3> SupportLocation = new Property<Vector3>();

		public Property<Vector3> SupportVelocity = new Property<Vector3>();

		public Property<Matrix> Transform = new Property<Matrix>();

		public Property<Vector3> LinearVelocity = new Property<Vector3>();

		/// <summary>
		/// The physics entity the player is currently standing on.
		/// </summary>
		public Property<BEPUphysics.Entities.Entity> SupportEntity = new Property<BEPUphysics.Entities.Entity>();

		public Command<Collidable, ContactCollection> Collided = new Command<Collidable, ContactCollection>();

		public float NormalHeight;
		public float NormalSupportHeight;

		public float CrouchedHeight;
		public float CrouchedSupportHeight;

		public Property<bool> Crouched = new Property<bool>();
		public Property<bool> AllowUncrouch = new Property<bool>();

		private Vector3[] rayOffsets;

		public static readonly CollisionGroup NoCollideGroup = new CollisionGroup();
		public static readonly CollisionGroup CharacterGroup = new CollisionGroup();

		static Character()
		{
			CollisionRules.CollisionGroupRules.Add(new CollisionGroupPair(Character.NoCollideGroup, Character.CharacterGroup), CollisionRule.NoBroadPhase);
		}

		private float edgeTimer;
		private const float edgeTime = 0.3f; // For a split second after walking off an edge, pretend like we're still supported

		private Main main;

		/// <summary>
		/// Constructs a simple character controller.
		/// </summary>
		/// <param name="position">Location to initially place the character.</param>
		/// <param name="height">The height of the character.</param>
		/// <param name="radius">The diameter of the character.</param>
		/// <param name="supportHeight">The distance above the ground that the bottom of the character's body floats.</param>
		/// <param name="mass">Total mass of the character.</param>
		public Character(Main main, Bindable bindable, Vector3 position, float height = Character.DefaultHeight, float crouchedHeight = Character.DefaultCrouchedHeight, float radius = Character.DefaultRadius, float supportHeight = Character.DefaultSupportHeight, float crouchedSupportHeight = Character.DefaultCrouchedSupportHeight, float mass = Character.DefaultMass)
		{
			this.main = main;
			this.Radius.Value = radius;
			this.Mass.Value = mass;
			this.Body = new Capsule(position, height, radius, mass);
			this.Body.Tag = this;
			this.Body.CollisionInformation.Tag = this;
			this.Body.IgnoreShapeChanges = true;
			this.Body.LinearDamping = 0.0f;
			this.Body.CollisionInformation.CollisionRules.Group = Character.CharacterGroup;
			this.NormalHeight = height;
			this.CrouchedHeight = crouchedHeight;
			this.Body.CollisionInformation.Events.ContactCreated += new BEPUphysics.BroadPhaseEntries.Events.ContactCreatedEventHandler<EntityCollidable>(Events_ContactCreated);
			this.collisionPairCollector = new Box(position + new Vector3(0, (height * -0.5f) - supportHeight, 0), radius * 2, supportHeight * 2, radius, 1);
			this.collisionPairCollector.CollisionInformation.CollisionRules.Personal = CollisionRule.NoNarrowPhaseUpdate; //Prevents collision detection/contact generation from being run.
			this.collisionPairCollector.IsAffectedByGravity = false;
			this.collisionPairCollector.CollisionInformation.CollisionRules.Group = Character.CharacterGroup;
			CollisionRules.AddRule(this.collisionPairCollector, this.Body, CollisionRule.NoBroadPhase); //Prevents the creation of any collision pairs between the body and the collector.
			this.SupportHeight.Value = supportHeight;
			this.NormalSupportHeight = supportHeight;
			this.CrouchedSupportHeight = crouchedSupportHeight;

			this.Body.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
			this.collisionPairCollector.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

			bindable.Add(new ChangeBinding<bool>(this.Crouched, delegate(bool old, bool value)
			{
				if (value && !old)
				{
					this.Body.Position += new Vector3(0, (this.CrouchedSupportHeight - this.NormalSupportHeight) + 0.5f * (this.CrouchedHeight - this.NormalHeight), 0);
					this.Height.Value = this.CrouchedHeight;
					this.Body.Length = this.Height.Value - this.Radius * 2;
					this.SupportHeight.Value = this.CrouchedSupportHeight;
				}
				else if (!value && old)
				{
					this.Height.Value = this.NormalHeight;
					this.Body.Length = this.Height.Value - this.Radius * 2;
					this.Body.Position += new Vector3(0, (this.NormalSupportHeight - this.CrouchedSupportHeight) + 0.5f * (this.NormalHeight - this.CrouchedHeight), 0);
					this.SupportHeight.Value = this.NormalSupportHeight;
				}
				this.collisionPairCollector.Height = this.SupportHeight * 2;
				this.Transform.Value = this.Body.WorldTransform;
			}));

			bindable.Add(new SetBinding<Matrix>(this.Transform, delegate(Matrix m)
			{
				this.Body.WorldTransform = m;
			}));

			bindable.Add(new SetBinding<Vector3>(this.LinearVelocity, delegate(Vector3 v)
			{
				this.Body.LinearVelocity = v;
			}));

			//Make the body slippery.
			//Note that this will not make all collisions have zero friction;
			//the friction coefficient between a pair of objects is based
			//on a blending of the two objects' materials.
			this.Body.Material.KineticFriction = 0.0f;
			this.Body.Material.StaticFriction = 0.0f;
			this.Body.Material.Bounciness = 0.0f;

			const int rayChecks = 4;
			float rayCheckRadius = radius - 0.1f;
			this.rayOffsets = new[] { Vector3.Zero }.Concat(Enumerable.Range(0, rayChecks).Select(
			delegate(int x)
			{
				float angle = x * ((2.0f * (float)Math.PI) / (float)rayChecks);
				return new Vector3((float)Math.Cos(angle) * rayCheckRadius, 0, (float)Math.Sin(angle) * rayCheckRadius);
			})).ToArray();
			this.IsUpdating = false;
		}

		private void Events_ContactCreated(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, BEPUphysics.CollisionTests.ContactData contact)
		{
			this.Collided.Execute(other, pair.Contacts);
		}

		private bool lastSupported = true;
		public Property<float> LastSupportedSpeed = new Property<float>();
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

			bool foundSupport = this.findSupport(out supportEntityTag, out supportEntity, out supportLocation, out supportNormal, out supportDistance);

			if (!foundSupport && this.WallRunState.Value == WallRun.State.None)
			{
				// Keep the player from getting stuck on corners
				foreach (Contact contact in this.Body.CollisionInformation.Pairs.SelectMany(x => x.Contacts.Select(y => y.Contact)))
				{
					Vector3 normal = (contact.Position - this.Body.Position).SetComponent(Direction.PositiveY, 0);
					float length = normal.Length();
					if (length > 0.5f)
						this.Body.LinearVelocity += -0.1f * (normal / length);
				}
			}

			// Support location only has velocity if we're actually sitting on an entity, as opposed to some static geometry.
			// linear velocity of point on body relative to center
			Vector3 supportLocationVelocity;
			if (supportEntity != null)
			{
				supportLocationVelocity = supportEntity.LinearVelocity // linear component
					+ Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
			}
			else
			{
				Voxel supportVoxel = supportEntityTag as Voxel;
				if (supportVoxel == null)
					supportLocationVelocity = Vector3.Zero;
				else
				{
					supportLocationVelocity = supportVoxel.LinearVelocity // linear component
						+ Vector3.Cross(supportVoxel.AngularVelocity, supportLocation - supportVoxel.Transform.Value.Translation);
				}
			}
			

			if (supportLocationVelocity.Y < this.Body.LinearVelocity.Y - 4.0f)
				foundSupport = false;

			if (!this.IsSwimming && foundSupport)
			{
				this.SupportEntity.Value = supportEntity;
				this.SupportLocation.Value = supportLocation;
				this.SupportVelocity.Value = supportLocationVelocity;
				this.IsSupported.Value = true;
				this.lastSupported = true;
				this.support(supportLocationVelocity, supportNormal, supportDistance, dt);
				this.HasTraction.Value = this.isSupportSlopeWalkable(supportNormal);
				this.handleHorizontalMotion(supportLocationVelocity, supportNormal, dt);
			}
			else
			{
				if (this.lastSupported)
				{
					this.LastSupportedSpeed.Value = new Vector2(this.Body.LinearVelocity.X, this.Body.LinearVelocity.Z).Length();
					this.lastSupported = false;
					this.edgeTimer = 0;
				}

				this.edgeTimer += dt;
				if (this.edgeTimer > edgeTime)
				{
					this.SupportEntity.Value = null;
					this.SupportVelocity.Value = Vector3.Zero;
					this.IsSupported.Value = false;
					this.HasTraction.Value = false;
				}

				if (this.EnableWalking)
				{
					if (this.IsSwimming)
						this.handleNoTraction(dt, this.TractionDeceleration * 0.7f, this.MaxSpeed * 0.7f, this.SwimUp);
					else
						this.handleNoTraction(dt, 0.0f, this.LastSupportedSpeed, false);
				}
			}

			if (this.Crouched && this.AllowUncrouch)
			{
				// Try to uncrouch

				Vector3 rayOrigin = this.Body.Position;

				bool foundCeiling = false;

				foreach (Vector3 rayStart in this.rayOffsets.Select(x => x + rayOrigin))
				{
					RayCastResult rayHit;
					if (this.main.Space.RayCast(new Ray(rayStart, Vector3.Up), (this.NormalHeight * 0.5f) + (this.NormalSupportHeight - this.SupportHeight), Character.raycastFilter, out rayHit))
					{
						foundCeiling = true;
						break;
					}
				}

				if (!foundCeiling)
					this.Crouched.Value = false;
			}
			else if (!this.Crouched && this.IsSupported)
			{
				// Keep the player from fitting into spaces that are too small vertically
				Vector3 pos = this.Body.Position;
				Vector2 offset = new Vector2(supportLocation.X - pos.X, supportLocation.Z - pos.Z);
				if (offset.LengthSquared() > 0)
				{
					RayCastResult rayHit;
					Vector3 rayStart = supportLocation;
					rayStart.Y = pos.Y + (this.Height * 0.5f) - 1.0f;
					if (this.main.Space.RayCast(new Ray(rayStart, Vector3.Up), 1.0f, Character.raycastFilter, out rayHit))
					{
						offset.Normalize();
						Vector2 velocity = new Vector2(this.Body.LinearVelocity.X, this.Body.LinearVelocity.Z);
						float speed = Vector2.Dot(velocity, offset);
						if (speed > 0)
						{
							velocity -= offset * speed * 1.5f;
							this.Body.LinearVelocity = new Vector3(velocity.X, this.Body.LinearVelocity.Y, velocity.Y);
						}
					}
				}
			}

			this.collisionPairCollector.LinearVelocity = this.Body.LinearVelocity;
			this.collisionPairCollector.Position = this.Body.Position + new Vector3(0, (this.Height * -0.5f) - this.SupportHeight, 0);
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
			supportLocation = BEPUutilities.Toolbox.NoVector;
			supportNormal = BEPUutilities.Toolbox.NoVector;
			supportDistance = float.MaxValue;

			const float fudgeFactor = 0.1f;
			Vector3 rayOrigin = this.Body.Position;
			rayOrigin.Y += fudgeFactor + this.Height * -0.5f;

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
					maximumDistance = fudgeFactor + (this.SupportHeight * 2.0f);
				else
					maximumDistance = fudgeFactor + this.SupportHeight;

				foreach (Vector3 rayStart in this.rayOffsets.Select(x => x + rayOrigin))
				{
					BEPUutilities.RayHit rayHit;
					// Fire a ray at the candidate and determine some details!
					if (candidate.RayCast(new Ray(rayStart, Vector3.Down), maximumDistance, out rayHit))
					{
						Vector3 n = Vector3.Normalize(rayHit.Normal);

						if (n.Y > supportNormal.Y)
							supportNormal = n;

						// We want to find the closest support, so compare it against the last closest support.
						if (rayHit.T < supportDistance && n.Y > 0.25f)
						{
							supportDistance = rayHit.T - fudgeFactor;
							supportLocation = rayHit.Location;
							if (rayHit.T < 0.0f)
								supportNormal = Vector3.Up;

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
			return Math.Acos(Math.Abs(Math.Min(supportNormal.Y, 1))) <= this.MaxSlope;
		}

		/// <summary>
		/// Maintains the position of the character's body above the ground.
		/// </summary>
		/// <param name="supportLocationVelocity">Velocity of the support point connected to the supportEntity.</param>
		/// <param name="supportNormal">The normal at the surface where the ray hit the entity.</param>
		/// <param name="supportDistance">Distance from the character to the support location.</param>
		private void support(Vector3 supportLocationVelocity, Vector3 supportNormal, float supportDistance, float dt)
		{
			//Put the character at the right distance from the ground.
			float supportVerticalVelocity = Math.Max(supportLocationVelocity.Y, -0.1f);
			float heightDifference = this.SupportHeight - supportDistance;
			this.Body.Position += (new Vector3(0, MathHelper.Clamp(heightDifference, (supportVerticalVelocity - 10.0f) * dt, (supportVerticalVelocity + 10.0f) * dt), 0));

			//Remove from the character velocity which would push it toward or away from the surface.
			//This is a relative velocity, so the velocity of the body and the velocity of a point on the support entity must be found.
			float bodyNormalVelocity = Vector3.Dot(this.Body.LinearVelocity, supportNormal);
			float supportEntityNormalVelocity = Vector3.Dot(supportLocationVelocity, supportNormal);
			Vector3 diff = (bodyNormalVelocity - supportEntityNormalVelocity) * -supportNormal;
			diff.Y = Math.Max(diff.Y, 0);
			this.Body.LinearVelocity += diff;

			BEPUphysics.Entities.Entity supportEntity = this.SupportEntity;
			if (supportEntity != null && supportEntity.IsAffectedByGravity)
			{
				Vector3 supportLocation = this.SupportLocation;
				Vector3 impulse = (this.Body.Mass * 1.5f) * ((Space)this.Space).ForceUpdater.Gravity * dt;
				supportEntity.ApplyImpulse(ref supportLocation, ref impulse);
				supportEntity.ActivityInformation.Activate();
			}
		}

		private static Func<BEPUphysics.BroadPhaseEntries.BroadPhaseEntry, bool> raycastFilter = a => a.CollisionRules.Group != Character.CharacterGroup && a.CollisionRules.Group != Character.NoCollideGroup;

		private bool slide(ref Vector2 movement, Vector3 wallRay)
		{
			RayCastResult rayHit;
			if (this.main.Space.RayCast(new Ray(this.Body.Position, wallRay), this.Radius + 0.25f, Character.raycastFilter, out rayHit)
				|| this.main.Space.RayCast(new Ray(this.Body.Position + new Vector3(0, 1, 0), wallRay), this.Radius + 0.25f, Character.raycastFilter, out rayHit)
				|| this.main.Space.RayCast(new Ray(this.Body.Position + new Vector3(0, -1, 0), wallRay), this.Radius + 0.25f, Character.raycastFilter, out rayHit))
			{
				Vector3 orthogonal = Vector3.Cross(rayHit.HitData.Normal, wallRay);
				Vector3 newMovement3 = Vector3.Cross(rayHit.HitData.Normal, orthogonal);
				Vector2 newMovement = new Vector2(newMovement3.X, newMovement3.Z);
				newMovement.Normalize();
				if (Vector2.Dot(newMovement, movement) < 0)
					newMovement *= -1.0f;

				if (Vector2.Dot(newMovement, movement) > 0.5f) // The new direction is similar to what we want. Go ahead.
				{
					newMovement *= movement.Length();
					movement = newMovement;
					return true;
				}
				else
					return false; // New direction is too different. Continue in the old direction.
			}
			else
				return false;
		}

		/// <summary>
		/// Manages movement acceleration, deceleration, and sliding.
		/// </summary>
		/// <param name="supportLocationVelocity">Velocity of the support point connected to the supportEntity.</param>
		/// <param name="supportNormal">The normal at the surface where the ray hit the entity.</param>
		/// <param name="dt">Timestep of the simulation.</param>
		private void handleHorizontalMotion(Vector3 supportLocationVelocity, Vector3 supportNormal, float dt)
		{
			Vector3 velocityAdjustment = new Vector3(0.0f);
			if (this.HasTraction && this.MovementDirection != Vector2.Zero && this.EnableWalking)
			{
				// Identify a coordinate system that uses the support normal as Y.
				// X is the axis point along the left (negative) and right (positive) relative to the movement direction.
				// Z points forward (positive) and backward (negative) in the movement direction modified to be parallel to the surface.

				Vector2 movement = this.MovementDirection;
				if (!this.slide(ref movement, new Vector3(movement.X, 0, movement.Y)))
				{
					float angle = (float)Math.Atan2(movement.Y, movement.X);
					if (!this.slide(ref movement, new Vector3((float)Math.Cos(angle + (float)Math.PI * 0.25f), 0, (float)Math.Sin(angle + (float)Math.PI * 0.25f))))
						this.slide(ref movement, new Vector3((float)Math.Cos(angle - (float)Math.PI * 0.25f), 0, (float)Math.Sin(angle - (float)Math.PI * 0.25f)));
				}

				Vector3 horizontal = new Vector3(movement.X, 0, movement.Y);
				Vector3 x = Vector3.Normalize(Vector3.Cross(horizontal, supportNormal));
				Vector3 z = Vector3.Normalize(Vector3.Cross(supportNormal, x)) * horizontal.Length();

				Vector2 netVelocity = new Vector2(this.Body.LinearVelocity.X - supportLocationVelocity.X, this.Body.LinearVelocity.Z - supportLocationVelocity.Z);
				float accel;
				if (Vector2.Dot(new Vector2(horizontal.X, horizontal.Z), netVelocity) < 0)
					accel = this.TractionDeceleration;
				else if (netVelocity.Length() < Character.InitialAccelerationSpeedThreshold)
					accel = this.InitialAcceleration;
				else
					accel = this.Acceleration;
				accel += Math.Abs(Vector2.Dot(new Vector2(x.X, x.Z), netVelocity)) * this.Acceleration * 2.0f;

				// Remove from the character a portion of velocity which pushes it horizontally off the desired movement track defined by the movementDirection.

				float bodyXVelocity = Vector3.Dot(this.Body.LinearVelocity, x);
				float supportEntityXVelocity = Vector3.Dot(supportLocationVelocity, x);
				float velocityChange = MathHelper.Clamp(bodyXVelocity - supportEntityXVelocity, -dt * this.TractionDeceleration, dt * this.TractionDeceleration);
				velocityAdjustment -= velocityChange * x;

				float bodyZVelocity = Vector3.Dot(this.Body.LinearVelocity, z);
				float supportEntityZVelocity = Vector3.Dot(supportLocationVelocity, z);
				float netZVelocity = bodyZVelocity - supportEntityZVelocity;

				// The velocity difference along the Z axis should accelerate/decelerate to match the goal velocity (max speed).
				float speed = this.Crouched ? this.MaxSpeed * 0.3f : this.MaxSpeed;
				if (netZVelocity > speed)
				{
					// Decelerate
					velocityChange = Math.Min(dt * this.TractionDeceleration, netZVelocity - speed);
					velocityAdjustment -= velocityChange * z;
				}
				else
				{
					// Accelerate
					velocityChange = Math.Min(dt * accel, speed - netZVelocity);
					velocityAdjustment += velocityChange * z;
					if (z.Y > 0.0f)
						velocityAdjustment += new Vector3(0, z.Y * Math.Min(dt * this.Acceleration * 2.0f, speed - netZVelocity) * 2.0f, 0);
				}
			}
			else
			{
				float deceleration;
				if (this.HasTraction)
					deceleration = dt * this.TractionDeceleration;
				else
					deceleration = dt * this.SlidingDeceleration;
				// Remove from the character a portion of velocity defined by the deceleration.
				Vector3 bodyHorizontalVelocity = this.Body.LinearVelocity - Vector3.Dot(this.Body.LinearVelocity, supportNormal) * supportNormal;
				Vector3 supportHorizontalVelocity = supportLocationVelocity - Vector3.Dot(supportLocationVelocity, supportNormal) * supportNormal;
				Vector3 relativeVelocity = bodyHorizontalVelocity - supportHorizontalVelocity;
				float speed = relativeVelocity.Length();
				if (speed > 0)
				{
					Vector3 horizontalDirection = relativeVelocity / speed;
					float velocityChange = Math.Min(speed, deceleration);
					velocityAdjustment -= velocityChange * horizontalDirection;
				}
			}
			this.Body.LinearVelocity += velocityAdjustment;
			this.VelocityAdjustments.Value = velocityAdjustment;
		}

		private void handleNoTraction(float dt, float tractionDeceleration, float maxSpeed, bool swimUp)
		{
			Vector3 velocityAdjustment = new Vector3(0.0f);
			if (this.MovementDirection != Vector2.Zero)
			{
				//Identify a coordinate system that uses the support normal as Y.
				//X is the axis point along the left (negative) and right (positive) relative to the movement direction.
				//Z points forward (positive) and backward (negative) in the movement direction modified to be parallel to the surface.
				Vector3 horizontal = new Vector3(this.MovementDirection.Value.X, 0, this.MovementDirection.Value.Y);
				Vector3 x = Vector3.Cross(horizontal, Vector3.Up);

				//Remove from the character a portion of velocity which pushes it horizontally off the desired movement track defined by the movementDirection.
				float bodyXVelocity = Vector3.Dot(this.Body.LinearVelocity, x);
				float velocityChange = MathHelper.Clamp(bodyXVelocity, -dt * tractionDeceleration, dt * tractionDeceleration);
				velocityAdjustment -= velocityChange * x;

				float bodyZVelocity = Vector3.Dot(this.Body.LinearVelocity, horizontal);
				//The velocity difference along the Z axis should accelerate/decelerate to match the goal velocity (max speed).
				if (bodyZVelocity > maxSpeed)
				{
					//Decelerate
					velocityChange = Math.Min(dt * this.NoTractionAcceleration, bodyZVelocity - maxSpeed);
					velocityAdjustment -= velocityChange * horizontal;
				}
				else
				{
					//Accelerate
					velocityChange = Math.Min(dt * this.NoTractionAcceleration, maxSpeed - bodyZVelocity);
					velocityAdjustment += velocityChange * horizontal;
				}
			}
			else
			{
				float deceleration = dt * tractionDeceleration;
				// Remove from the character a portion of velocity defined by the deceleration.
				Vector3 bodyHorizontalVelocity = this.Body.LinearVelocity;
				bodyHorizontalVelocity.Y = 0.0f;
				float speed = bodyHorizontalVelocity.Length();
				if (speed > 0)
				{
					Vector3 horizontalDirection = bodyHorizontalVelocity / speed;
					float velocityChange = Math.Min(speed, deceleration);
					velocityAdjustment -= velocityChange * horizontalDirection;
				}
			}

			if (swimUp)
				velocityAdjustment += new Vector3(0, 7.0f * dt, 0);

			this.VelocityAdjustments.Value = velocityAdjustment;
			this.Body.LinearVelocity += velocityAdjustment;
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