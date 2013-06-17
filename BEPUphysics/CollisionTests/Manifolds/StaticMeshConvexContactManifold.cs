using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUutilities.ResourceManagement;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contacts between a static mesh and a convex.
    ///</summary>
    public class StaticMeshConvexContactManifold : StaticMeshContactManifold
    {


        UnsafeResourcePool<TriangleConvexPairTester> testerPool = new UnsafeResourcePool<TriangleConvexPairTester>();
        protected override void GiveBackTester(CollisionAlgorithms.TrianglePairTester tester)
        {
            testerPool.GiveBack((TriangleConvexPairTester)tester);
        }

        protected override CollisionAlgorithms.TrianglePairTester GetTester()
        {
            return testerPool.Take();
        }

    }
}
