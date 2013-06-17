using System;
using BEPUphysics.BroadPhaseEntries;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a compound-static mesh collision pair.
    ///</summary>
    public class CompoundStaticMeshPairHandler : CompoundGroupPairHandler
    {
       

        StaticMesh mesh;

        public override Collidable CollidableB
        {
            get { return mesh; }
        }
        public override Entities.Entity EntityB
        {
            get { return null; }
        }

        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            mesh = entryA as StaticMesh;
            if (mesh == null)
            {
                mesh = entryB as StaticMesh;
                if (mesh == null)
                {
                    throw new ArgumentException("Inappropriate types used to initialize pair.");
                }
            }


            base.Initialize(entryA, entryB);
        }


        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {

            base.CleanUp();
            mesh = null;


        }




        protected override void UpdateContainedPairs()
        {
            //TODO: Static triangle meshes have a worldspace hierarchy that could be more efficiently traversed with a tree vs tree test.
            //This is just a lot simpler to manage in the short term.

            var overlappedElements = PhysicsResources.GetCompoundChildList();
            compoundInfo.hierarchy.Tree.GetOverlaps(mesh.boundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.Count; i++)
            {
                TryToAdd(overlappedElements.Elements[i].CollisionInformation, mesh, overlappedElements.Elements[i].Material, mesh.material);
            }

            PhysicsResources.GiveBack(overlappedElements);

        }


    }
}
