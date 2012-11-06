using System;
namespace BEPUphysics.Settings
{
    ///<summary>
    /// Settings class containing global information about collision detection.
    ///</summary>
    public static class CollisionDetectionSettings
    {


        internal static float ContactInvalidationLengthSquared = .01f;

        /// <summary>
        /// For persistent manifolds, contacts are represented by an offset in local space of two colliding bodies.
        /// The distance between these offsets transformed into world space and projected onto a plane defined by the contact normal squared is compared against this value.
        /// If this value is exceeded, the contact is removed from the contact manifold.
        /// 
        /// If the world is smaller or larger than 'normal' for the engine, adjusting this value proportionally can improve contact caching behavior.
        /// The default value of .1f works well for worlds that operate on the order of 1 unit.
        /// </summary>
        public static float ContactInvalidationLength
        {
            get
            {
                return (float)Math.Sqrt(ContactInvalidationLengthSquared);
            }
            set
            {
                ContactInvalidationLengthSquared = value * value;
            }
        }


        internal static float ContactMinimumSeparationDistanceSquared = .0009f;
        /// <summary>
        /// In persistent manifolds, if two contacts are too close together, then 
        /// the system will not use one of them.  This avoids redundant constraints.
        /// Defaults to .03f.
        /// </summary>
        public static float ContactMinimumSeparationDistance
        {
            get
            {
                return (float)Math.Sqrt(ContactMinimumSeparationDistanceSquared);
            }
            set
            {
                ContactMinimumSeparationDistanceSquared = value * value;
            }
        }

        internal static float nonconvexNormalDotMinimum = .99f;
        /// <summary>
        /// In regular convex manifolds, two contacts are considered redundant if their positions are too close together.  
        /// In nonconvex manifolds, the normal must also be tested, since a contact in the same location could have a different normal.
        /// This property is the minimum angle in radians between normals below which contacts are considered redundant.
        /// </summary>
        public static float NonconvexNormalAngleDifferenceMinimum
        {
            get
            {
                return (float)Math.Acos(nonconvexNormalDotMinimum);
            }
            set
            {
                nonconvexNormalDotMinimum = (float)Math.Cos(value);
            }
        }

        /// <summary>
        /// The default amount of allowed penetration into the margin before position correcting impulses will be applied.
        /// Defaults to .01f.
        /// </summary>
        public static float AllowedPenetration = .01f;

        /// <summary>
        /// Default collision margin around objects.  Margins help prevent objects from interpenetrating and improve stability.
        /// Defaults to .04f.
        /// </summary>
        public static float DefaultMargin = .04f;

        internal static float maximumContactDistance = .1f;
        /// <summary>
        /// Maximum distance between the surfaces defining a contact point allowed before removing the contact.
        /// Defaults to .1f.
        /// </summary>
        public static float MaximumContactDistance
        {
            get
            {
                return maximumContactDistance;
            }
            set
            {
                if (value >= 0)
                    maximumContactDistance = value;
                else
                    throw new Exception("Distance must be nonnegative.");
            }
        }


        
    }
}
