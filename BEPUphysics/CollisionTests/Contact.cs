using System;
using BEPUphysics.Settings;
using Microsoft.Xna.Framework;

namespace BEPUphysics.CollisionTests
{
    /// <summary>
    /// Handles information about a contact point during a collision between two bodies.
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Amount of penetration between the two objects.
        /// </summary>
        public float PenetrationDepth;

        /// <summary>
        /// Identifier used to link contact data with existing contacts and categorize members of a manifold.
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
        /// Sets up the contact with new information.
        ///</summary>
        ///<param name="candidate">Contact data to initialize the contact with.</param>
        public void Setup(ref ContactData candidate)
        {
            candidate.Validate();
            Position = candidate.Position;
            Normal = candidate.Normal;
            PenetrationDepth = candidate.PenetrationDepth;
            Id = candidate.Id;
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
