using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    internal class ContactPenetrationConstraintTester
    {
        private readonly Contact contact;

        internal float accumulatedImpulse;
        //float linearBX, linearBY, linearBZ;
        internal float angularAX, angularAY, angularAZ;
        internal float angularBX, angularBY, angularBZ;

        //Inverse effective mass matrix

        private float bias;
        internal bool isActive = true;
        private float linearAX, linearAY, linearAZ;
        internal int numIterationsAtZeroImpulse;
        private Entity parentA, parentB;
        internal float velocityToImpulse;

        internal ContactPenetrationConstraintTester(Contact contact)
        {
            this.contact = contact;
        }

        /// <summary>
        /// Computes and applies an impulse to keep the colliders from penetrating.
        /// </summary>
        /// <returns>Impulse applied.</returns>
        internal float ApplyImpulse()
        {
            //Compute relative velocity
            float lambda = (parentA.linearVelocity.X * linearAX + parentA.linearVelocity.Y * linearAY + parentA.linearVelocity.Z * linearAZ +
                            parentA.angularVelocity.X * angularAX + parentA.angularVelocity.Y * angularAY + parentA.angularVelocity.Z * angularAZ -
                            //note negatives, since reusing linearA jacobian entry
                            parentB.linearVelocity.X * linearAX - parentB.linearVelocity.Y * linearAY - parentB.linearVelocity.Z * linearAZ +
                            parentB.angularVelocity.X * angularBX + parentB.angularVelocity.Y * angularBY + parentB.angularVelocity.Z * angularBZ
                            - bias) //Add in position correction/resitution
                           * velocityToImpulse; //convert to impulse


            //Clamp accumulated impulse
            float previousAccumulatedImpulse = accumulatedImpulse;
            accumulatedImpulse = MathHelper.Max(0, accumulatedImpulse + lambda);
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

        internal void PreStep(float dt)
        {
            parentA = contact.collisionPair.ParentA;
            parentB = contact.collisionPair.ParentB;

            //Set up the jacobians.
            linearAX = contact.Normal.X;
            linearAY = contact.Normal.Y;
            linearAZ = contact.Normal.Z;
            //linearBX = -linearAX;
            //linearBY = -linearAY;
            //linearBZ = -linearAZ;

            //angular A = Ra x N
            angularAX = (contact.Ra.Y * linearAZ) - (contact.Ra.Z * linearAY);
            angularAY = (contact.Ra.Z * linearAX) - (contact.Ra.X * linearAZ);
            angularAZ = (contact.Ra.X * linearAY) - (contact.Ra.Y * linearAX);

            //Angular B = N x Rb
            angularBX = (linearAY * contact.Rb.Z) - (linearAZ * contact.Rb.Y);
            angularBY = (linearAZ * contact.Rb.X) - (linearAX * contact.Rb.Z);
            angularBZ = (linearAX * contact.Rb.Y) - (linearAY * contact.Rb.X);

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

            //Bounciness
            bias =
                MathHelper.Min(
                    MathHelper.Max(0, contact.PenetrationDepth - contact.collisionPair.allowedPenetration) *
                    contact.collisionPair.space.simulationSettings.CollisionResponse.PenetrationRecoveryStiffness / dt,
                    contact.collisionPair.space.simulationSettings.CollisionResponse.MaximumPositionCorrectionSpeed);
            if (contact.collisionPair.Bounciness > 0)
            {
                //Compute relative velocity
                float relativeVelocity = parentA.linearVelocity.X * linearAX + parentA.linearVelocity.Y * linearAY + parentA.linearVelocity.Z * linearAZ +
                                         parentA.angularVelocity.X * angularAX + parentA.angularVelocity.Y * angularAY + parentA.angularVelocity.Z * angularAZ -
                                         parentB.linearVelocity.X * linearAX - parentB.linearVelocity.Y * linearAY - parentB.linearVelocity.Z * linearAZ +
                                         parentB.angularVelocity.X * angularBX + parentB.angularVelocity.Y * angularBY + parentB.angularVelocity.Z * angularBZ;
                relativeVelocity *= -1;
                if (relativeVelocity > contact.collisionPair.space.simulationSettings.CollisionResponse.BouncinessVelocityThreshold)
                    bias = MathHelper.Max(relativeVelocity * contact.collisionPair.Bounciness, bias);
            }

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
                //parentA.ApplyLinearImpulse(ref linear);
                //parentA.ApplyAngularImpulse(ref angular);
            }
            if (parentB.isDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse * angularBX;
                angular.Y = accumulatedImpulse * angularBY;
                angular.Z = accumulatedImpulse * angularBZ;
                //parentB.ApplyLinearImpulse(ref linear);
                //parentB.ApplyAngularImpulse(ref angular);
            }
        }
    }
}