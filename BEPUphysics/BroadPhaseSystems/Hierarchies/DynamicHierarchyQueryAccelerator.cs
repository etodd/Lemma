using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using Microsoft.Xna.Framework;

namespace BEPUphysics.BroadPhaseSystems.Hierarchies
{
    ///<summary>
    /// Interface to the DynamicHierarchy's volume query systems.
    ///</summary>
    public class DynamicHierarchyQueryAccelerator : IQueryAccelerator
    {
        private readonly DynamicHierarchy hierarchy;
        internal DynamicHierarchyQueryAccelerator(DynamicHierarchy hierarchy)
        {
            this.hierarchy = hierarchy;
        }

        /// <summary>
        /// Gets the broad phase associated with this query accelerator.
        /// </summary>
        public BroadPhase BroadPhase
        {
            get
            {
                return hierarchy;
            }
        }

        /// <summary>
        /// Collects all entries with bounding boxes which intersect the given bounding box.
        /// </summary>
        /// <param name="box">Bounding box to test against the world.</param>
        /// <param name="entries">Entries of the space which intersect the bounding box.</param>
        public void GetEntries(BoundingBox box, IList<BroadPhaseEntry> entries)
        {
            if (hierarchy.root != null)
                hierarchy.root.GetOverlaps(ref box, entries);

        }

        /// <summary>
        /// Collects all entries with bounding boxes which intersect the given frustum.
        /// </summary>
        /// <param name="frustum">Frustum to test against the world.</param>
        /// <param name="entries">Entries of the space which intersect the frustum.</param>
        public void GetEntries(BoundingFrustum frustum, IList<BroadPhaseEntry> entries)
        {
            if (hierarchy.root != null)
                hierarchy.root.GetOverlaps(ref frustum, entries);

        }

        /// <summary>
        /// Collects all entries with bounding boxes which intersect the given sphere.
        /// </summary>
        /// <param name="sphere">Sphere to test against the world.</param>
        /// <param name="entries">Entries of the space which intersect the sphere.</param>
        public void GetEntries(BoundingSphere sphere, IList<BroadPhaseEntry> entries)
        {
            if (hierarchy.root != null)
                hierarchy.root.GetOverlaps(ref sphere, entries);

        }


        /// <summary>
        /// Finds all intersections between the ray and broad phase entries.
        /// </summary>
        /// <param name="ray">Ray to test against the structure.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray's direction's length.</param>
        /// <param name="entries">Entries which have bounding boxes that overlap the ray.</param>
        public bool RayCast(Ray ray, float maximumLength, IList<BroadPhaseEntry> entries)
        {
            if (hierarchy.root != null)
            {
                hierarchy.root.GetOverlaps(ref ray, maximumLength, entries);

                return entries.Count > 0;
            }
            return false;
        }


        /// <summary>
        /// Finds all intersections between the ray and broad phase entries.
        /// </summary>
        /// <param name="ray">Ray to test against the structure.</param>
        /// <param name="entries">Entries which have bounding boxes that overlap the ray.</param>
        public bool RayCast(Ray ray, IList<BroadPhaseEntry> entries)
        {
            if (hierarchy.root != null)
            {
                hierarchy.root.GetOverlaps(ref ray, float.MaxValue, entries);

                return entries.Count > 0;
            }
            return false;
        }


    }
}
