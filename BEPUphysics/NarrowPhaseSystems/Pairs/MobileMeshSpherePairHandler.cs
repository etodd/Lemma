using BEPUphysics.CollisionTests.Manifolds;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a mobile mesh-sphere collision pair.
    ///</summary>
    public class MobileMeshSpherePairHandler : MobileMeshPairHandler
    {
        MobileMeshSphereContactManifold contactManifold = new MobileMeshSphereContactManifold();
        protected internal override MobileMeshContactManifold MeshManifold
        {
            get { return contactManifold; }
        }



    }

}
