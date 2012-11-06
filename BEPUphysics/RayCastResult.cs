using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;

namespace BEPUphysics
{
    ///<summary>
    /// Contains information about a ray cast hit.
    ///</summary>
    public struct RayCastResult
    {
        ///<summary>
        /// Position, normal, and t paramater of the hit.
        ///</summary>
        public RayHit HitData;
        /// <summary>
        /// Object hit by the ray.
        /// </summary>
        public BroadPhaseEntry HitObject;

        ///<summary>
        /// Constructs a new ray cast result.
        ///</summary>
        ///<param name="hitData">Ray cast hit data.</param>
        ///<param name="hitObject">Object hit by the ray.</param>
        public RayCastResult(RayHit hitData, BroadPhaseEntry hitObject)
        {
            HitData = hitData;
            HitObject = hitObject;
        }
    }
}
