using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a compound-instanced mesh collision pair.
    ///</summary>
    public class CompoundMobileMeshPairHandler : CompoundGroupPairHandler
    {

        MobileMeshCollidable mesh;

        public override Collidable CollidableB
        {
            get { return mesh; }
        }

        public override Entities.Entity EntityB
        {
            get { return mesh.entity; }
        }


        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            mesh = entryA as MobileMeshCollidable;
            if (mesh == null)
            {
                mesh = entryB as MobileMeshCollidable;
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
            //Could go other way; get triangles in mesh that overlap the compound.
            //Could be faster sometimes depending on the way it's set up.

            var overlappedElements = PhysicsResources.GetCompoundChildList();
            compoundInfo.hierarchy.Tree.GetOverlaps(mesh.boundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.Count; i++)
            {
                TryToAdd(overlappedElements.Elements[i].CollisionInformation, mesh, overlappedElements.Elements[i].Material);
            }

            PhysicsResources.GiveBack(overlappedElements);

        }

    }
}
