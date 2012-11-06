using System;
using BEPUphysics.CollisionTests;
using Microsoft.Xna.Framework;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Contact and some associated data used by the convenience ContactCollection.
    ///</summary>
    public struct ContactInformation : IEquatable<ContactInformation>
    {
        /// <summary>
        /// Contact point in the pair.
        /// </summary>
        public Contact Contact;

        /// <summary>
        /// Pair that most directly generated the contact.
        /// The pair may have parents, accessible through the pair's Parent property.
        /// </summary>
        public CollidablePairHandler Pair;

        /// <summary>
        /// Normal impulse applied between the objects at the contact point.
        /// </summary>
        public float NormalImpulse;

        /// <summary>
        /// Friction impulse applied between the objects at the contact point.
        /// This is sometimes an approximation due to the varying ways in which
        /// friction is calculated.
        /// </summary>
        public float FrictionImpulse;
        
        ///<summary>
        /// Relative velocity of the colliding objects at the position of the contact.
        ///</summary>
        public Vector3 RelativeVelocity;


        public override string ToString()
        {
            return Contact + " NormalImpulse: " + NormalImpulse + " FrictionImpulse: " + FrictionImpulse + " RelativeVelocity: " + RelativeVelocity;
        }


        public bool Equals(ContactInformation other)
        {
            return other.Contact == Contact;
        }
    }
}
