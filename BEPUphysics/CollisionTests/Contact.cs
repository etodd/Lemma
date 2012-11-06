using System;
using Microsoft.Xna.Framework;

namespace BEPUphysics.CollisionTests
{
    /// <summary>
    /// Handles information about a contact point during a collision between two bodies.
    /// </summary>
    public class Contact : IEquatable<Contact>
    {
        /// <summary>
        /// Amount of penetration between the two objects.
        /// </summary>
        public float PenetrationDepth;

        /// <summary>
        /// Feature-based id used to match contacts from the previous frame to their current versions.
        /// </summary>
        public int Id = -1;

        /// <summary>
        /// Normal direction of the surface at the contact point.
        /// </summary>
        public Vector3 Normal;


        /// <summary>
        /// Position of the contact point.
        /// </summary>
        public Vector3 Position;


        ///<summary>
        /// Sets upt he contact with new information.
        ///</summary>
        ///<param name="candidate">Contact data to initialize the contact with.</param>
        public void Setup(ref ContactData candidate)
        {
            Position = candidate.Position;
            Normal = candidate.Normal;
            PenetrationDepth = candidate.PenetrationDepth;
            Id = candidate.Id;
        }

        //TODO: This implementation is kind of wonky!  Is it even used anywhere? Kill it off if possible.
        /// <summary>
        /// Determines if two contacts are equal using their id and position.
        /// </summary>
        /// <param name="other">Other contact to compare.</param>
        /// <returns>Whether or not the contacts are equivalent.</returns>
        public bool Equals(Contact other)
        {
            //This assumes that the colliders are equal.
            if (Id == other.Id)
            {
                if (Id == -1)
                {
                    float distanceSquared;
                    Vector3.DistanceSquared(ref other.Position, ref Position, out distanceSquared);
                    return distanceSquared < .001f;
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// Outputs the position, normal, and depth information of the contact into a string.
        /// </summary>
        /// <returns>Position, normal, and depth information of the contact in a string.</returns>
        public override string ToString()
        {
            return "Position: " + Position + " Normal: " + Normal + " Depth: " + PenetrationDepth;
        }


    }
}
