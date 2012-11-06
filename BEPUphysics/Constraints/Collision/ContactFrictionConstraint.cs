using System;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.Settings;

namespace BEPUphysics.Constraints.Collision
{
    /// <summary>
    /// Computes the friction force for a contact when central friction cannot be used.
    /// </summary>
    public class ContactFrictionConstraint : EntitySolverUpdateable
    {
        private ContactManifoldConstraint contactManifoldConstraint;
        ///<summary>
        /// Gets the manifold constraint associated with this friction constraint.
        ///</summary>
        public ContactManifoldConstraint ContactManifoldConstraint
        {
            get
            {
                return contactManifoldConstraint;
            }
        }
        private ContactPenetrationConstraint penetrationConstraint;
        ///<summary>
        /// Gets the penetration constraint associated with this friction constraint.
        ///</summary>
        public ContactPenetrationConstraint PenetrationConstraint
        {
            get
            {
                return penetrationConstraint;
            }
        }

        ///<summary>
        /// Constructs a new friction constraint.
        ///</summary>
        public ContactFrictionConstraint()
        {
            isActive = false;
        }

        internal float accumulatedImpulse;
        //float linearBX, linearBY, linearBZ;
        private float angularAX, angularAY, angularAZ;
        private float angularBX, angularBY, angularBZ;

        //Inverse effective mass matrix


        private float friction;
        internal float linearAX, linearAY, linearAZ;
        private Entity entityA, entityB;
        private bool entityAIsDynamic, entityBIsDynamic;
        private float velocityToImpulse;


        ///<summary>
        /// Configures the friction constraint for a new contact.
        ///</summary>
        ///<param name="contactManifoldConstraint">Manifold to which the constraint belongs.</param>
        ///<param name="penetrationConstraint">Penetration constraint associated with this friction constraint.</param>
        public void Setup(ContactManifoldConstraint contactManifoldConstraint, ContactPenetrationConstraint penetrationConstraint)
        {
            this.contactManifoldConstraint = contactManifoldConstraint;
            this.penetrationConstraint = penetrationConstraint;
            IsActive = true;
            linearAX = 0;
            linearAY = 0;
            linearAZ = 0;

            entityA = contactManifoldConstraint.EntityA;
            entityB = contactManifoldConstraint.EntityB;
        }

        ///<summary>
        /// Cleans upt he friction constraint.
        ///</summary>
        public void CleanUp()
        {
            accumulatedImpulse = 0;
            contactManifoldConstraint = null;
            penetrationConstraint = null;
            entityA = null;
            entityB = null;
            IsActive = false;
        }

        /// <summary>
        /// Gets the direction in which the friction force acts.
        /// </summary>
        public Vector3 FrictionDirection
        {
            get { return new Vector3(linearAX, linearAY, linearAZ); }
        }

        /// <summary>
        /// Gets the total impulse applied by this friction constraint in the last time step.
        /// </summary>
        public float TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        ///<summary>
        /// Gets the relative velocity of the constraint.  This is the velocity along the tangent movement direction.
        ///</summary>
        public float RelativeVelocity
        {
            get
            {
                float velocity = 0;
                if (entityA != null)
                    velocity += entityA.linearVelocity.X * linearAX + entityA.linearVelocity.Y * linearAY + entityA.linearVelocity.Z * linearAZ +
                                entityA.angularVelocity.X * angularAX + entityA.angularVelocity.Y * angularAY + entityA.angularVelocity.Z * angularAZ;
                if (entityB != null)
                    velocity += -entityB.linearVelocity.X * linearAX - entityB.linearVelocity.Y * linearAY - entityB.linearVelocity.Z * linearAZ +
                                entityB.angularVelocity.X * angularBX + entityB.angularVelocity.Y * angularBY + entityB.angularVelocity.Z * angularBZ;
                return velocity;
            }
        }


        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            //Compute relative velocity and convert to impulse
            float lambda = RelativeVelocity * velocityToImpulse;


            //Clamp accumulated impulse
            float previousAccumulatedImpulse = accumulatedImpulse;
            float maxForce = friction * penetrationConstraint.accumulatedImpulse;
            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, -maxForce, maxForce);
            lambda = accumulatedImpulse - previousAccumulatedImpulse;

            //Apply the impulse
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = lambda * linearAX;
            linear.Y = lambda * linearAY;
            linear.Z = lambda * linearAZ;
            if (entityAIsDynamic)
            {
                angular.X = lambda * angularAX;
                angular.Y = lambda * angularAY;
                angular.Z = lambda * angularAZ;
                entityA.ApplyLinearImpulse(ref linear);
                entityA.ApplyAngularImpulse(ref angular);
            }
            if (entityBIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = lambda * angularBX;
                angular.Y = lambda * angularBY;
                angular.Z = lambda * angularBZ;
                entityB.ApplyLinearImpulse(ref linear);
                entityB.ApplyAngularImpulse(ref angular);
            }

            return Math.Abs(lambda);
        }

        /// <summary>
        /// Initializes the constraint for this frame.
        /// </summary>
        /// <param name="dt">Time since the last frame.</param>
        public override void Update(float dt)
        {


            entityAIsDynamic = entityA != null && entityA.isDynamic;
            entityBIsDynamic = entityB != null && entityB.isDynamic;

            //Compute the three dimensional relative velocity at the point.

            Vector3 velocityA = new Vector3(), velocityB = new Vector3();
            Vector3 ra = penetrationConstraint.ra, rb = penetrationConstraint.rb;
            if (entityA != null)
            {
                Vector3.Cross(ref entityA.angularVelocity, ref ra, out velocityA);
                Vector3.Add(ref velocityA, ref entityA.linearVelocity, out velocityA);
            }
            if (entityB != null)
            {
                Vector3.Cross(ref entityB.angularVelocity, ref rb, out velocityB);
                Vector3.Add(ref velocityB, ref entityB.linearVelocity, out velocityB);
            }
            Vector3 relativeVelocity;
            Vector3.Subtract(ref velocityA, ref velocityB, out relativeVelocity);

            //Get rid of the normal velocity.
            Vector3 normal = penetrationConstraint.contact.Normal;
            float normalVelocityScalar = normal.X * relativeVelocity.X + normal.Y * relativeVelocity.Y + normal.Z * relativeVelocity.Z;
            relativeVelocity.X -= normalVelocityScalar * normal.X;
            relativeVelocity.Y -= normalVelocityScalar * normal.Y;
            relativeVelocity.Z -= normalVelocityScalar * normal.Z;

            //Create the jacobian entry and decide the friction coefficient.
            float length = relativeVelocity.LengthSquared();
            if (length > Toolbox.Epsilon)
            {
                length = (float)Math.Sqrt(length);
                linearAX = relativeVelocity.X / length;
                linearAY = relativeVelocity.Y / length;
                linearAZ = relativeVelocity.Z / length;

                friction = length > CollisionResponseSettings.StaticFrictionVelocityThreshold
                               ? contactManifoldConstraint.materialInteraction.KineticFriction
                               : contactManifoldConstraint.materialInteraction.StaticFriction;
            }
            else
            {
                //If there's no velocity, there's no jacobian.  Give up.
                //This is 'fast' in that it will early out on essentially resting objects,
                //but it may introduce instability.
                //If it doesn't look good, try the next approach.
                //isActive = false;
                //return;

                //if the above doesn't work well, try using the previous frame's jacobian.
                if (linearAX != 0 || linearAY != 0 || linearAZ != 0)
                {
                    friction = contactManifoldConstraint.materialInteraction.StaticFriction;
                }
                else
                {
                    //Can't really do anything here, give up.
                    isActiveInSolver = false;
                    return;
                    //Could also cross the up with normal to get a random direction.  Questionable value.
                }
            }


            //angular A = Ra x N
            angularAX = (ra.Y * linearAZ) - (ra.Z * linearAY);
            angularAY = (ra.Z * linearAX) - (ra.X * linearAZ);
            angularAZ = (ra.X * linearAY) - (ra.Y * linearAX);

            //Angular B = N x Rb
            angularBX = (linearAY * rb.Z) - (linearAZ * rb.Y);
            angularBY = (linearAZ * rb.X) - (linearAX * rb.Z);
            angularBZ = (linearAX * rb.Y) - (linearAY * rb.X);

            //Compute inverse effective mass matrix
            float entryA, entryB;

            //these are the transformed coordinates
            float tX, tY, tZ;
            if (entityAIsDynamic)
            {
                tX = angularAX * entityA.inertiaTensorInverse.M11 + angularAY * entityA.inertiaTensorInverse.M21 + angularAZ * entityA.inertiaTensorInverse.M31;
                tY = angularAX * entityA.inertiaTensorInverse.M12 + angularAY * entityA.inertiaTensorInverse.M22 + angularAZ * entityA.inertiaTensorInverse.M32;
                tZ = angularAX * entityA.inertiaTensorInverse.M13 + angularAY * entityA.inertiaTensorInverse.M23 + angularAZ * entityA.inertiaTensorInverse.M33;
                entryA = tX * angularAX + tY * angularAY + tZ * angularAZ + entityA.inverseMass;
            }
            else
                entryA = 0;

            if (entityBIsDynamic)
            {
                tX = angularBX * entityB.inertiaTensorInverse.M11 + angularBY * entityB.inertiaTensorInverse.M21 + angularBZ * entityB.inertiaTensorInverse.M31;
                tY = angularBX * entityB.inertiaTensorInverse.M12 + angularBY * entityB.inertiaTensorInverse.M22 + angularBZ * entityB.inertiaTensorInverse.M32;
                tZ = angularBX * entityB.inertiaTensorInverse.M13 + angularBY * entityB.inertiaTensorInverse.M23 + angularBZ * entityB.inertiaTensorInverse.M33;
                entryB = tX * angularBX + tY * angularBY + tZ * angularBZ + entityB.inverseMass;
            }
            else
                entryB = 0;

            velocityToImpulse = -1 / (entryA + entryB); //Softness?



        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm starting
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = accumulatedImpulse * linearAX;
            linear.Y = accumulatedImpulse * linearAY;
            linear.Z = accumulatedImpulse * linearAZ;
            if (entityAIsDynamic)
            {
                angular.X = accumulatedImpulse * angularAX;
                angular.Y = accumulatedImpulse * angularAY;
                angular.Z = accumulatedImpulse * angularAZ;
                entityA.ApplyLinearImpulse(ref linear);
                entityA.ApplyAngularImpulse(ref angular);
            }
            if (entityBIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse * angularBX;
                angular.Y = accumulatedImpulse * angularBY;
                angular.Z = accumulatedImpulse * angularBZ;
                entityB.ApplyLinearImpulse(ref linear);
                entityB.ApplyAngularImpulse(ref angular);
            }
        }

        protected internal override void CollectInvolvedEntities(DataStructures.RawList<Entity> outputInvolvedEntities)
        {
            //This should never really have to be called.
            if (entityA != null)
                outputInvolvedEntities.Add(entityA);
            if (entityB != null)
                outputInvolvedEntities.Add(entityB);
        }
    }
}