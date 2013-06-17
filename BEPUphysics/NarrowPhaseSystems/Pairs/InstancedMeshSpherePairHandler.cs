using BEPUphysics.CollisionTests.Manifolds;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a instanced mesh-convex collision pair.
    ///</summary>
    public class InstancedMeshSpherePairHandler : InstancedMeshPairHandler
    {
        InstancedMeshSphereContactManifold contactManifold = new InstancedMeshSphereContactManifold();
        protected override InstancedMeshContactManifold MeshManifold
        {
            get { return contactManifold; }
        }
        
    }

}
