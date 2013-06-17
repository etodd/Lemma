using BEPUphysics.CollisionTests.Manifolds;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a terrain-sphere collision pair.
    ///</summary>
    public sealed class TerrainSpherePairHandler : TerrainPairHandler
    {
        protected TerrainSphereContactManifold contactManifold = new TerrainSphereContactManifold();
        protected override TerrainContactManifold TerrainManifold
        {
            get { return contactManifold; }
        }

    }

}
