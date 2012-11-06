using System;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.PositionUpdating;
using BEPUphysics.Settings;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.ResourceManagement;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a static mesh-convex collision pair.
    ///</summary>
    public class StaticMeshConvexPairHandler : StaticMeshPairHandler
    {

        StaticMeshConvexContactManifold contactManifold = new StaticMeshConvexContactManifold();
        protected override StaticMeshContactManifold MeshManifold
        {
            get { return contactManifold; }
        }


    }

}
