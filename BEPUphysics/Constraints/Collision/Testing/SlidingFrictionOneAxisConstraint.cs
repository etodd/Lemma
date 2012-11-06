using System;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Handles collision pair sliding friction.
    /// </summary>
    internal class SlidingFrictionOneAxisConstraint
    {
        private readonly CollisionPair pair;
        private float accumulatedImpulse;

        //Jacobian entries
        //float linearBX, linearBY, linearBZ;
        private float angularAX, angularAY, angularAZ;
        private float angularBX, angularBY, angularBZ;
        internal bool isActive = true;
        private float linearAX, linearAY, linearAZ;
        private float maximumFrictionForce;
        internal int numIterationsAtZeroImpulse;
        private Entity parentA, parentB;

        //Inverse effective mass matrix
        private float velocityToImpulse;

        /// <summary>
        /// Constructs a new linear friction constraint.
        /// </summary>
        /// <param name="pair">Collision pair owning this friction constraint.</param>
        internal SlidingFrictionOneAxisConstraint(CollisionPair pair)
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

            return lambda;
        }

        /// <summary>
        /// Initializes the constraint for this frame.
        /// </summary>
        /// <param name="dt">Time since the last frame.</param>
        /// <param name="manifoldCenter">Computed center of manifold.</param>
        internal void PreStep(float dt, out Vector3 manifoldCenter)
        {
            parentA = pair.ParentA;
            parentB = pair.ParentB;

            int count = pair.Contacts.Count;
            switch (count)
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
            Vector3 ra, rb;
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

            float friction;
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

            maximumFrictionForce = 0;
            for (int i = 0; i < count; i++)
            {
                maximumFrictionForce += pair.Contacts[i].penetrationConstraint.accumulatedImpulse;
            }
            maximumFrictionForce *= friction;

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
            if (parentA.isDynamic)
            {
                tX = angularAX * parentA.inertiaTensorInverse.M11 + angularAY * parentA.inertiaTensorInverse.M21 + angularAZ * parentA.inertiaTensorInverse.M31;
                tY = angularAX * parentA.inertiaTensorInverse.M12 + angularAY * parentA.inertiaTensorInverse.M22 + angularAZ * parentA.inertiaTensorInverse.M32;
                tZ = angularAX * parentA.inertiaTensorInverse.M13 + angularAY * parentA.inertiaTensorInverse.M23 + angularAZ * parentA.inertiaTensorInverse.M33;
                entryA = tX * angularAX + tY * angularAY + tZ * angularAZ + 1 / parentA.mass;
            }
            else
                entryA = 0;

            if (parentB.isDynamic)
            {
                tX = angularBX * parentB.inertiaTensorInverse.M11 + angularBY * parentB.inertiaTensorInverse.M21 + angularBZ * parentB.inertiaTensorInverse.M31;
                tY = angularBX * parentB.inertiaTensorInverse.M12 + angularBY * parentB.inertiaTensorInverse.M22 + angularBZ * parentB.inertiaTensorInverse.M32;
                tZ = angularBX * parentB.inertiaTensorInverse.M13 + angularBY * parentB.inertiaTensorInverse.M23 + angularBZ * parentB.inertiaTensorInverse.M33;
                entryB = tX * angularBX + tY * angularBY + tZ * angularBZ + 1 / parentB.mass;
            }
            else
                entryB = 0;

            velocityToImpulse = -1 / (entryA + entryB); //Softness?


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
        }
    }
}