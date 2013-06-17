using BEPUphysics.CollisionTests.Manifolds;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a terrain-convex collision pair.
    ///</summary>
    public sealed class TerrainConvexPairHandler : TerrainPairHandler
    {
        protected TerrainConvexContactManifold contactManifold = new TerrainConvexContactManifold();
        protected override TerrainContactManifold TerrainManifold
        {
            get { return contactManifold; }
        }

    }

}
