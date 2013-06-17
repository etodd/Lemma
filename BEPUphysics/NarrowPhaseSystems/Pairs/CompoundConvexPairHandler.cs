using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Microsoft.Xna.Framework;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a compound and convex collision pair.
    ///</summary>
    public class CompoundConvexPairHandler : CompoundGroupPairHandler
    {
        ConvexCollidable convexInfo;


        public override Collidable CollidableB
        {
            get { return convexInfo; }
        }

        public override Entities.Entity EntityB
        {
            get { return convexInfo.entity; }
        }



        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            convexInfo = entryA as ConvexCollidable;
            if (convexInfo == null)
            {
                convexInfo = entryB as ConvexCollidable;
                if (convexInfo == null)
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
            convexInfo = null;
        }



        protected override void UpdateContainedPairs()
        {
            var overlappedElements = PhysicsResources.GetCompoundChildList();
            compoundInfo.hierarchy.Tree.GetOverlaps(convexInfo.boundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.Count; i++)
            {
                TryToAdd(overlappedElements.Elements[i].CollisionInformation, CollidableB, overlappedElements.Elements[i].Material);
            }

            PhysicsResources.GiveBack(overlappedElements);


        }

    }
}
