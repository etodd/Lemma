using System.Collections.Generic;
using System.Diagnostics;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.DataStructures;
using BEPUphysics.Materials;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework.Input;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Superclass of pairs between collidables that generate contact points.
    ///</summary>
    public abstract class DetectorVolumeGroupPairHandler : DetectorVolumePairHandler, IDetectorVolumePairHandlerParent
    {
        Dictionary<EntityCollidable, DetectorVolumePairHandler> subPairs = new Dictionary<EntityCollidable, DetectorVolumePairHandler>();
        HashSet<EntityCollidable> containedPairs = new HashSet<EntityCollidable>();
        RawList<EntityCollidable> pairsToRemove = new RawList<EntityCollidable>();

        /// <summary>
        /// Gets a read-only dictionary of collidables associated with this group pair handler all the subpairs associated with them.
        /// </summary>
        public ReadOnlyDictionary<EntityCollidable, DetectorVolumePairHandler> Pairs
        {
            get { return new ReadOnlyDictionary<EntityCollidable, DetectorVolumePairHandler>(subPairs); }
        }

        ///<summary>
        /// Called when the pair handler is added to the narrow phase.
        ///</summary>
        protected internal override void OnAddedToNarrowPhase()
        {
            DetectorVolume.pairs.Add(Collidable.entity, this);
        }


        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {
            foreach (var pair in subPairs.Values)
            {
                pair.CleanUp();
            }
            subPairs.Clear();
            base.CleanUp();


        }

        protected void TryToAdd(EntityCollidable collidable)
        {
            CollisionRule rule;
            if ((rule = CollisionRules.collisionRuleCalculator(DetectorVolume, collidable)) < CollisionRule.NoNarrowPhasePair)
            {
                //Clamp the rule to the parent's rule.  Always use the more restrictive option.
                //Don't have to test for NoNarrowPhasePair rule on the parent's rule because then the parent wouldn't exist!
                if (rule < CollisionRule)
                    rule = CollisionRule;
                if (!subPairs.ContainsKey(collidable))
                {
                    var newPair = NarrowPhaseHelper.GetPairHandler(DetectorVolume, collidable, rule) as DetectorVolumePairHandler;
                    if (newPair != null)
                    {
                        newPair.Parent = this;
                        subPairs.Add(collidable, newPair);
                    }
                }
                containedPairs.Add(collidable);
            }
        }

        protected abstract void UpdateContainedPairs();

        public override void UpdateCollision(float dt)
        {
            WasContaining = Containing;
            WasTouching = Touching;

            //Gather current pairs.      
            UpdateContainedPairs();

            //Eliminate old pairs.
            foreach (var other in subPairs.Keys)
            {
                if (!containedPairs.Contains(other))
                    pairsToRemove.Add(other);
            }
            for (int i = 0; i < pairsToRemove.count; i++)
            {
                var toReturn = subPairs[pairsToRemove.Elements[i]];
                subPairs.Remove(pairsToRemove.Elements[i]);
                toReturn.CleanUp();
                toReturn.Factory.GiveBack(toReturn);

            }
            containedPairs.Clear();
            pairsToRemove.Clear();


            //Scan the pairs in sequence, updating the state as we go.
            //Touching can be set to true by a single touching subpair.
            Touching = false;
            //Containing can be set to false by a single noncontaining or nontouching subpair.
            Containing = subPairs.Count > 0;
            foreach (var pair in subPairs.Values)
            {
                //For child convex pairs, we don't need to always perform containment checks.
                //Only check if the containment state has not yet been invalidated or a touching state has not been identified.
                var convexPair = pair as DetectorVolumeConvexPairHandler;
                if (convexPair != null)
                    convexPair.CheckContainment = Containing || !Touching;

                pair.UpdateCollision(dt);

                if (pair.Touching)
                    Touching = true; //If one child is touching, then we are touching too.
                else
                    Containing = false; //If one child isn't touching, then we aren't containing.

                if (!pair.Containing) //If one child isn't containing, then we aren't containing.
                    Containing = false;


                if (!Containing && Touching)
                {
                    //If it's touching but not containing, no further pairs will change the state.
                    //Containment has been invalidated by something that either didn't touch or wasn't contained.
                    //Touching has been ensured by at least one object touching.
                    break;
                }
            }

            NotifyDetectorVolumeOfChanges();
        }

    }
}
