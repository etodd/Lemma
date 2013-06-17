using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUutilities.ResourceManagement;

namespace BEPUphysics.CollisionTests.Manifolds
{
    public class TerrainSphereContactManifold : TerrainContactManifold
    {
        UnsafeResourcePool<TriangleSpherePairTester> testerPool = new UnsafeResourcePool<TriangleSpherePairTester>();
        protected override TrianglePairTester GetTester()
        {
            return testerPool.Take();
        }

        protected override void GiveBackTester(TrianglePairTester tester)
        {
            testerPool.GiveBack((TriangleSpherePairTester)tester);
        }

    }
}
