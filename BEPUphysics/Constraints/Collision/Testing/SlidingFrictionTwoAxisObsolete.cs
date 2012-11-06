using System;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Handles collision pair sliding friction.
    /// </summary>
    internal class SlidingFrictionTwoAxisObsolete
    {
        private readonly CollisionPair pair;
        private float accumulatedImpulse;
        private float accumulatedImpulse2;

        //Jacobian entries
        //float linearBX, linearBY, linearBZ;
        private float angularAX;
        private float angularAX2;
        private float angularAY;
        private float angularAY2;
        private float angularAZ;
        private float angularAZ2;
        private float angularBX;
        private float angularBX2;
        private float angularBY;
        private float angularBY2;
        private float angularBZ;
        private float angularBZ2;
        private int contactCount;
        private float friction;
        internal bool isActive = true;
        private float linearAX;
        private float linearAX2;
        private float linearAY;
        private float linearAY2;
        private float linearAZ;
        private float linearAZ2;
        /*
                private float maximumFrictionForce;
        */
        internal int numIterationsAtZeroImpulse;
        private Entity parentA, parentB;

        //Inverse effective mass matrix
        private float velocityToImpulse;
        private float velocityToImpulse2;

        /// <summary>
        /// Constructs a new linear friction constraint.
        /// </summary>
        /// <param name="pair">Collision pair owning this friction constraint.</param>
        internal SlidingFrictionTwoAxisObsolete(CollisionPair pair)
        {
            this.pair = pair;
        }


        internal float ApplyImpulse()
        {
            //Compute relative velocity
            float lambda = (parentA.linearVelocity.X * linearAX + parentA.linearVelocity.Y * linearAY + parentA.linearVelocity.Z * linearAZ +
                            parentA.angularVelocity.X * angularAX + parentA.angularVelocity.Y * angularAY + parentA.angularVelocity.Z * angularAZ -
                            //note negatives, since reusing linearA jacobian entry
                            parentB.linearVelocity.X * linearAX - parentB.linearVelocity.Y * linearAY - parentB.linearVelocity.Z * linearAZ +
                            parentB.angularVelocity.X * angularBX + parentB.angularVelocity.Y * angularBY + parentB.angularVelocity.Z * angularBZ)
                           * velocityToImpulse; //convert to impulse

            //Compute maximum force
            float maximumFrictionForce = 0;
            for (int i = 0; i < contactCount; i++)
            {
                maximumFrictionForce += pair.Contacts[i].penetrationConstraint.accumulatedImpulse;
            }
            maximumFrictionForce *= friction;

            //Clamp accumulated impulse
            float previousAccumulatedImpulse = accumulatedImpulse;
            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, -maximumFrictionForce, maximumFrictionForce); //instead of maximumFrictionForce, could recompute each iteration...
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
            if (parentA.isDynamic)
            {
                angular.X = lambda * angularAX;
                angular.Y = lambda * angularAY;
                angular.Z = lambda * angularAZ;
                parentA.ApplyLinearImpulse(ref linear);
                parentA.ApplyAngularImpulse(ref angular);
            }
            if (parentB.isDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = lambda * angularBX;
                angular.Y = lambda * angularBY;
                angular.Z = lambda * angularBZ;
                parentB.ApplyLinearImpulse(ref linear);
                parentB.ApplyAngularImpulse(ref angular);
            }

            //Compute relative velocity
            float lambda2 = (parentA.linearVelocity.X * linearAX2 + parentA.linearVelocity.Y * linearAY2 + parentA.linearVelocity.Z * linearAZ2 +
                             parentA.angularVelocity.X * angularAX2 + parentA.angularVelocity.Y * angularAY2 + parentA.angularVelocity.Z * angularAZ2 -
                             //note negatives, since reusing linearA jacobian entry
                             parentB.linearVelocity.X * linearAX2 - parentB.linearVelocity.Y * linearAY2 - parentB.linearVelocity.Z * linearAZ2 +
                             parentB.angularVelocity.X * angularBX2 + parentB.angularVelocity.Y * angularBY2 + parentB.angularVelocity.Z * angularBZ2)
                            * velocityToImpulse2; //convert to impulse


            //Clamp accumulated impulse
            float previousAccumulatedImpulse2 = accumulatedImpulse2;
            accumulatedImpulse2 = MathHelper.Clamp(accumulatedImpulse2 + lambda2, -maximumFrictionForce, maximumFrictionForce); //instead of maximumFrictionForce, could recompute each iteration...
            lambda2 = accumulatedImpulse2 - previousAccumulatedImpulse2;

            //Apply the impulse
            linear.X = lambda2 * linearAX2;
            linear.Y = lambda2 * linearAY2;
            linear.Z = lambda2 * linearAZ2;
            if (parentA.isDynamic)
            {
                angular.X = lambda2 * angularAX2;
                angular.Y = lambda2 * angularAY2;
                angular.Z = lambda2 * angularAZ2;
                parentA.ApplyLinearImpulse(ref linear);
                parentA.ApplyAngularImpulse(ref angular);
            }
            if (parentB.isDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = lambda2 * angularBX2;
                angular.Y = lambda2 * angularBY2;
                angular.Z = lambda2 * angularBZ2;
                parentB.ApplyLinearImpulse(ref linear);
                parentB.ApplyAngularImpulse(ref angular);
            }

            return lambda;
        }

        /// <summary>
        /// Initializes the constraint for this frame.
        /// </summary>
        /// <param name="dt">Time since the last frame.</param>
        /// <param name="manifoldCenter">Computed center of manifold.</param>
        internal void PreStep(float dt, out Vector3 manifoldCenter)
        {
            numIterationsAtZeroImpulse = 0;
            parentA = pair.ParentA;
            parentB = pair.ParentB;

            contactCount = pair.Contacts.Count;
            switch (contactCount)
            {
                case 1:
                    manifoldCenter = pair.Contacts[0].Position;
                    break;
                case 2:
                    Vector3.Add(ref pair.Contacts[0].Position, ref pair.Contacts[1].Position, out manifoldCenter);
                    manifoldCenter.X *= .5f;
                    manifoldCenter.Y *= .5f;
                    manifoldCenter.Z *= .5f;
                    break;
                case 3:
                    Vector3.Add(ref pair.Contacts[0].Position, ref pair.Contacts[1].Position, out manifoldCenter);
                    Vector3.Add(ref pair.Contacts[2].Position, ref manifoldCenter, out manifoldCenter);
                    manifoldCenter.X *= .333333333f;
                    manifoldCenter.Y *= .333333333f;
                    manifoldCenter.Z *= .333333333f;
                    break;
                case 4:
                    //This isn't actually the center of the manifold.  Is it good enough?
                    Vector3.Add(ref pair.Contacts[0].Position, ref pair.Contacts[1].Position, out manifoldCenter);
                    Vector3.Add(ref pair.Contacts[2].Position, ref manifoldCenter, out manifoldCenter);
                    Vector3.Add(ref pair.Contacts[3].Position, ref manifoldCenter, out manifoldCenter);
                    manifoldCenter.X *= .25f;
                    manifoldCenter.Y *= .25f;
                    manifoldCenter.Z *= .25f;
                    break;
                default:
                    manifoldCenter = Toolbox.NoVector;
                    break;
            }

            //Compute the three dimensional relative velocity at the point.
            Vector3 ra;
            Vector3 rb;
            Vector3.Subtract(ref manifoldCenter, ref parentA.position, out ra);
            Vector3.Subtract(ref manifoldCenter, ref parentB.position, out rb);

            Vector3 velocityA, velocityB;
            Vector3.Cross(ref parentA.angularVelocity, ref ra, out velocityA);
            Vector3.Add(ref velocityA, ref parentA.linearVelocity, out velocityA);

            Vector3.Cross(ref parentB.angularVelocity, ref rb, out velocityB);
            Vector3.Add(ref velocityB, ref parentB.linearVelocity, out velocityB);

            Vector3 relativeVelocity;
            Vector3.Subtract(ref velocityA, ref velocityB, out relativeVelocity);

            //Get rid of the normal velocity.
            Vector3 normal = pair.Contacts[0].Normal;
            float normalVelocityScalar = normal.X * relativeVelocity.X + normal.Y * relativeVelocity.Y + normal.Z * relativeVelocity.Z;
            relativeVelocity.X -= normalVelocityScalar * normal.X;
            relativeVelocity.Y -= normalVelocityScalar * normal.Y;
            relativeVelocity.Z -= normalVelocityScalar * normal.Z;

            //Create the jacobian entry and decide the friction coefficient.
            float length = relativeVelocity.LengthSquared();
            if (length > Toolbox.Epsilon)
            {
                length = (float) Math.Sqrt(length);
                linearAX = relativeVelocity.X / length;
                linearAY = relativeVelocity.Y / length;
                linearAZ = relativeVelocity.Z / length;

                friction = length > pair.space.simulationSettings.CollisionResponse.StaticFrictionVelocityThreshold ? pair.DynamicFriction : pair.StaticFriction;
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
                    friction = pair.StaticFriction;
                }
                else
                {
                    //Can't really do anything here, give up.
                    isActive = false;
                    return;
                }
            }

            //maximumFrictionForce = 0;
            //for (int i = 0; i < count; i++)
            //{
            //    maximumFrictionForce += pair.contacts[i].penetrationConstraint.accumulatedImpulse;
            //}
            //maximumFrictionForce *= friction;

            //linear axis 2 = normal x N
            linearAX2 = (normal.Y * linearAZ) - (normal.Z * linearAY);
            linearAY2 = (normal.Z * linearAX) - (normal.X * linearAZ);
            linearAZ2 = (normal.X * linearAY) - (normal.Y * linearAX);

            //angular A = Ra x N
            angularAX = (ra.Y * linearAZ) - (ra.Z * linearAY);
            angularAY = (ra.Z * linearAX) - (ra.X * linearAZ);
            angularAZ = (ra.X * linearAY) - (ra.Y * linearAX);

            //angular A 2 = Ra x linear axis 2
            angularAX2 = (ra.Y * linearAZ2) - (ra.Z * linearAY2);
            angularAY2 = (ra.Z * linearAX2) - (ra.X * linearAZ2);
            angularAZ2 = (ra.X * linearAY2) - (ra.Y * linearAX2);

            //Angular B = N x Rb
            angularBX = (linearAY * rb.Z) - (linearAZ * rb.Y);
            angularBY = (linearAZ * rb.X) - (linearAX * rb.Z);
            angularBZ = (linearAX * rb.Y) - (linearAY * rb.X);

            //Angular B 2 = linear axis 2 x Rb
            angularBX2 = (linearAY2 * rb.Z) - (linearAZ2 * rb.Y);
            angularBY2 = (linearAZ2 * rb.X) - (linearAX2 * rb.Z);
            angularBZ2 = (linearAX2 * rb.Y) - (linearAY2 * rb.X);

            //Compute inverse effective mass matrix
            float entryA, entryB;
            float entryA2, entryB2;

            //these are the transformed coordinates
            float tX, tY, tZ;
            float tX2, tY2, tZ2;
            if (parentA.isDynamic)
            {
                tX = angularAX * parentA.inertiaTensorInverse.M11 + angularAY * parentA.inertiaTensorInverse.M21 + angularAZ * parentA.inertiaTensorInverse.M31;
                tY = angularAX * parentA.inertiaTensorInverse.M12 + angularAY * parentA.inertiaTensorInverse.M22 + angularAZ * parentA.inertiaTensorInverse.M32;
                tZ = angularAX * parentA.inertiaTensorInverse.M13 + angularAY * parentA.inertiaTensorInverse.M23 + angularAZ * parentA.inertiaTensorInverse.M33;
                entryA = tX * angularAX + tY * angularAY + tZ * angularAZ + 1 / parentA.mass;

                tX2 = angularAX2 * parentA.inertiaTensorInverse.M11 + angularAY2 * parentA.inertiaTensorInverse.M21 + angularAZ2 * parentA.inertiaTensorInverse.M31;
                tY2 = angularAX2 * parentA.inertiaTensorInverse.M12 + angularAY2 * parentA.inertiaTensorInverse.M22 + angularAZ2 * parentA.inertiaTensorInverse.M32;
                tZ2 = angularAX2 * parentA.inertiaTensorInverse.M13 + angularAY2 * parentA.inertiaTensorInverse.M23 + angularAZ2 * parentA.inertiaTensorInverse.M33;
                entryA2 = tX2 * angularAX2 + tY2 * angularAY2 * tZ2 * angularAZ2 + 1 / parentA.mass;
            }
            else
            {
                entryA = 0;
                entryA2 = 0;
            }

            if (parentB.isDynamic)
            {
                tX = angularBX * parentB.inertiaTensorInverse.M11 + angularBY * parentB.inertiaTensorInverse.M21 + angularBZ * parentB.inertiaTensorInverse.M31;
                tY = angularBX * parentB.inertiaTensorInverse.M12 + angularBY * parentB.inertiaTensorInverse.M22 + angularBZ * parentB.inertiaTensorInverse.M32;
                tZ = angularBX * parentB.inertiaTensorInverse.M13 + angularBY * parentB.inertiaTensorInverse.M23 + angularBZ * parentB.inertiaTensorInverse.M33;
                entryB = tX * angularBX + tY * angularBY + tZ * angularBZ + 1 / parentB.mass;

                tX2 = angularBX2 * parentB.inertiaTensorInverse.M11 + angularBY2 * parentB.inertiaTensorInverse.M21 + angularBZ2 * parentB.inertiaTensorInverse.M31;
                tY2 = angularBX2 * parentB.inertiaTensorInverse.M12 + angularBY2 * parentB.inertiaTensorInverse.M22 + angularBZ2 * parentB.inertiaTensorInverse.M32;
                tZ2 = angularBX2 * parentB.inertiaTensorInverse.M13 + angularBY2 * parentB.inertiaTensorInverse.M23 + angularBZ2 * parentB.inertiaTensorInverse.M33;
                entryB2 = tX2 * angularBX2 + tY2 * angularBY2 + tZ2 * angularBZ2 + 1 / parentB.mass;
            }
            else
            {
                entryB = 0;
                entryB2 = 0;
            }
            velocityToImpulse = -1 / (entryA + entryB); //Softness?
            velocityToImpulse2 = -1 / (entryA2 + entryB2);


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
            if (parentA.isDynamic)
            {
                angular.X = accumulatedImpulse * angularAX;
                angular.Y = accumulatedImpulse * angularAY;
                angular.Z = accumulatedImpulse * angularAZ;
                parentA.ApplyLinearImpulse(ref linear);
                parentA.ApplyAngularImpulse(ref angular);
            }
            if (parentB.isDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse * angularBX;
                angular.Y = accumulatedImpulse * angularBY;
                angular.Z = accumulatedImpulse * angularBZ;
                parentB.ApplyLinearImpulse(ref linear);
                parentB.ApplyAngularImpulse(ref angular);
            }

            //Warm starting 2
            linear.X = accumulatedImpulse2 * linearAX2;
            linear.Y = accumulatedImpulse2 * linearAY2;
            linear.Z = accumulatedImpulse2 * linearAZ2;
            if (parentA.isDynamic)
            {
                angular.X = accumulatedImpulse2 * angularAX2;
                angular.Y = accumulatedImpulse2 * angularAY2;
                angular.Z = accumulatedImpulse2 * angularAZ2;
                parentA.ApplyLinearImpulse(ref linear);
                parentA.ApplyAngularImpulse(ref angular);
            }
            if (parentB.isDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse2 * angularBX2;
                angular.Y = accumulatedImpulse2 * angularBY2;
                angular.Z = accumulatedImpulse2 * angularBZ2;
                parentB.ApplyLinearImpulse(ref linear);
                parentB.ApplyAngularImpulse(ref angular);
            }
        }
    }
}