using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a compound-instanced mesh collision pair.
    ///</summary>
    public class StaticGroupMobileMeshPairHandler : StaticGroupPairHandler
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
            var overlappedElements = PhysicsResources.GetCollidableList();
            staticGroup.Shape.CollidableTree.GetOverlaps(mesh.boundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.Count; i++)
            {
                var staticCollidable = overlappedElements.Elements[i] as StaticCollidable;
                TryToAdd(overlappedElements.Elements[i], mesh, staticCollidable != null ? staticCollidable.Material : staticGroup.Material);
            }

            PhysicsResources.GiveBack(overlappedElements);

        }

    }
}
