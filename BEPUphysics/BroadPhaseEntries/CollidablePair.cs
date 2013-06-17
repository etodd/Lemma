using System;

namespace BEPUphysics.BroadPhaseEntries
{
    ///<summary>
    /// Pair of collidables.
    ///</summary>
    public struct CollidablePair : IEquatable<CollidablePair>
    {
        internal Collidable collidableA;
        ///<summary>
        /// First collidable in the pair.
        ///</summary>
        public Collidable CollidableA
        {
            get { return collidableA; }
        }

        internal Collidable collidableB;
        /// <summary>
        /// Second collidable in the pair.
        /// </summary>
        public Collidable CollidableB
        {
            get { return collidableB; }
        }

        ///<summary>
        /// Constructs a new collidable pair.
        ///</summary>
        ///<param name="collidableA">First collidable in the pair.</param>
        ///<param name="collidableB">Second collidable in the pair.</param>
        public CollidablePair(Collidable collidableA, Collidable collidableB)
        {
            this.collidableA = collidableA;
            this.collidableB = collidableB;
        }


        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            //TODO: Use old prime-based system?
            return collidableA.GetHashCode() + collidableB.GetHashCode();
        }



        #region IEquatable<BroadPhaseOverlap> Members

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(CollidablePair other)
        {
            return (other.collidableA == collidableA && other.collidableB == collidableB) || (other.collidableA == collidableB && other.collidableB == collidableA);
        }

        #endregion
    }
}
