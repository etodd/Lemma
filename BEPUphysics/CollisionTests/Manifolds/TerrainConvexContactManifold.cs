using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionTests.CollisionAlgorithms;

namespace BEPUphysics.CollisionTests.Manifolds
{
    public class TerrainConvexContactManifold : TerrainContactManifold
    {
        UnsafeResourcePool<TriangleConvexPairTester> testerPool = new UnsafeResourcePool<TriangleConvexPairTester>();
        protected override TrianglePairTester GetTester()
        {
            return testerPool.Take();
        }

        protected override void GiveBackTester(TrianglePairTester tester)
        {
            testerPool.GiveBack((TriangleConvexPairTester)tester);
        }

    }
}
