using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a compound-static mesh collision pair.
    ///</summary>
    public class DetectorVolumeCompoundPairHandler : DetectorVolumeGroupPairHandler
    {
        private CompoundCollidable compound;

        /// <summary>
        /// Gets the entity collidable associated with the pair.
        /// </summary>
        public override EntityCollidable Collidable
        {
            get { return compound; }
        }

        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            compound = entryA as CompoundCollidable;
            if (compound == null)
            {
                compound = entryB as CompoundCollidable;
                if (compound == null)
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
            compound = null;


        }




        protected override void UpdateContainedPairs()
        {
            //TODO: Triangle meshes have a worldspace hierarchy that could be more efficiently traversed with a tree vs tree test.
            //This is just a lot simpler to manage in the short term.

            for (int i = 0; i < compound.children.Count; i++)
            {
                TryToAdd(compound.children.Elements[i].CollisionInformation);
            }

        }


    }
}
