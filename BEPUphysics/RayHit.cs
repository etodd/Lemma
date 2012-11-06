using Microsoft.Xna.Framework;

namespace BEPUphysics
{
    ///<summary>
    /// Contains ray hit data.
    ///</summary>
    public struct RayHit
    {
        ///<summary>
        /// Location of the ray hit.
        ///</summary>
        public Vector3 Location;
        ///<summary>
        /// Normal of the ray hit.
        ///</summary>
        public Vector3 Normal;
        ///<summary>
        /// T parameter of the ray hit.  
        /// The ray hit location is equal to the ray origin added to the ray direction multiplied by T.
        ///</summary>
        public float T;
    }
}
