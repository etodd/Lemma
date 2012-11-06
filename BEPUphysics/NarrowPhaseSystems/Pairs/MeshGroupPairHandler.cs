using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionTests;
using BEPUphysics.Materials;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    /// <summary>
    /// Contains a triangle collidable and its index.  Used by mobile mesh-mesh collisions.
    /// </summary>
    public struct TriangleEntry : IEquatable<TriangleEntry>
    {
        /// <summary>
        /// Index of the triangle that was the source of this entry.
        /// </summary>
        public int Index;
        /// <summary>
        /// Collidable for the triangle.
        /// </summary>
        public TriangleCollidable Collidable;

        /// <summary>
        /// Gets the hash code of the object.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return Index;
        }


        /// <summary>
        /// Determines if two colliders refer to the same triangle.
        /// </summary>
        /// <param name="other">Object to compare.</param>
        /// <returns>Whether or not the objects are equal.</returns>
        public bool Equals(TriangleEntry other)
        {
            return other.Index == Index;
        }
    }

    //TODO: Lots of overlap with the GroupPairHandler.
    ///<summary>
    /// Superclass of pair handlers which have multiple index-based collidable child pairs.
    ///</summary>
    public abstract class MeshGroupPairHandler : CollidablePairHandler, IPairHandlerParent
    {
        ContactManifoldConstraintGroup manifoldConstraintGroup;

        Dictionary<TriangleEntry, MobileMeshPairHandler> subPairs = new Dictionary<TriangleEntry, MobileMeshPairHandler>();
        HashSet<TriangleEntry> containedPairs = new HashSet<TriangleEntry>();
        RawList<TriangleEntry> pairsToRemove = new RawList<TriangleEntry>();


        ///<summary>
        /// Gets a list of the pairs associated with children.
        ///</summary>
        public ReadOnlyDictionary<TriangleEntry, MobileMeshPairHandler> ChildPairs
        {
            get
            {
                return new ReadOnlyDictionary<TriangleEntry, MobileMeshPairHandler>(subPairs);
            }
        }

        /// <summary>
        /// Material of the first collidable.
        /// </summary>
        protected abstract Material MaterialA { get; }
        /// <summary>
        /// Material of the second collidable.
        /// </summary>
        protected abstract Material MaterialB { get; }

        ///<summary>
        /// Constructs a new compound-convex pair handler.
        ///</summary>
        protected MeshGroupPairHandler()
        {
            manifoldConstraintGroup = new ContactManifoldConstraintGroup();
        }




        ///<summary>
        /// Forces an update of the pair's material properties.
        ///</summary>
        ///<param name="a">Material of the first member of the pair.</param>
        ///<param name="b">Material of the second member of the pair.</param>
        public override void UpdateMaterialProperties(Material a, Material b)
        {
            foreach (CollidablePairHandler pairHandler in subPairs.Values)
            {
                pairHandler.UpdateMaterialProperties(a, b);
            }
        }


        /// <summary>
        /// Updates the material interaction properties of the pair handler's constraint.
        /// </summary>
        /// <param name="properties">Properties to use.</param>
        public override void UpdateMaterialProperties(InteractionProperties properties)
        {
            foreach (CollidablePairHandler pairHandler in subPairs.Values)
            {
                pairHandler.UpdateMaterialProperties(properties);
            }
        }

        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {

            //Child initialization is responsible for setting up the entries.
            //Child initialization is responsible for setting up the manifold, if any.
            manifoldConstraintGroup.Initialize(EntityA, EntityB);

            base.Initialize(entryA, entryB);
        }

        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {

            //The pair handler cleanup will get rid of contacts.
            foreach (CollidablePairHandler pairHandler in subPairs.Values)
            {
                pairHandler.CleanUp();             
                //Don't forget to give the pair back to the factory!
                //There'd be a lot of leaks otherwise.
                pairHandler.Factory.GiveBack(pairHandler);
            }
            subPairs.Clear();
            //don't need to remove constraints directly from our group, since cleaning up our children should get rid of them.


            base.CleanUp();

            //Child type needs to null out the references.
        }

        protected void TryToAdd(int index)
        {
            var entry = new TriangleEntry { Index = index };
            if (!subPairs.ContainsKey(entry))
            {
                var collidablePair = new CollidablePair(CollidableA, entry.Collidable = GetOpposingCollidable(index));
                var newPair = (MobileMeshPairHandler)NarrowPhaseHelper.GetPairHandler(ref collidablePair);
                if (newPair != null)
                {
                    newPair.CollisionRule = CollisionRule;
                    newPair.UpdateMaterialProperties(MaterialA, MaterialB);  //Override the materials, if necessary.  Meshes don't currently support custom materials but..
                    newPair.Parent = this;
                    subPairs.Add(entry, newPair);
                }
            }
            containedPairs.Add(entry);

        }

        /// <summary>
        /// Get a collidable from CollidableB to represent the object at the given index.
        /// </summary>
        /// <param name="index">Index to create a collidable for.</param>
        /// <returns>Collidable for the object at the given index.</returns>
        protected abstract TriangleCollidable GetOpposingCollidable(int index);

        /// <summary>
        /// Configure a triangle from CollidableB to represent the object at the given index.
        /// </summary>
        /// <param name="entry">Entry to configure.</param>
        /// <param name="dt">Time step duration.</param>
        protected abstract void ConfigureCollidable(TriangleEntry entry, float dt);

        /// <summary>
        /// Cleans up the collidable.
        /// </summary>
        /// <param name="collidable">Collidable to clean up.</param>
        protected virtual void CleanUpCollidable(TriangleCollidable collidable)
        {
            Resources.GiveBack(collidable);
        }

        protected abstract void UpdateContainedPairs(float dt);


        ///<summary>
        /// Updates the pair handler's contacts.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        protected virtual void UpdateContacts(float dt)
        {

            UpdateContainedPairs(dt);
            //Eliminate old pairs.
            foreach (var pair in subPairs.Keys)
            {
                if (!containedPairs.Contains(pair))
                    pairsToRemove.Add(pair);
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

            foreach (var pair in subPairs)
            {
                if (pair.Value.BroadPhaseOverlap.collisionRule < CollisionRule.NoNarrowPhaseUpdate) //Don't test if the collision rules say don't.
                {
                    ConfigureCollidable(pair.Key, dt);
                    //Update the contact count using our (the parent) contact count so that the child can avoid costly solidity testing.
                    pair.Value.MeshManifold.parentContactCount = contactCount;
                    pair.Value.UpdateCollision(dt);
                }
            }


        }


        ///<summary>
        /// Updates the pair handler.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void UpdateCollision(float dt)
        {

            if (!suppressEvents)
            {
                CollidableA.EventTriggerer.OnPairUpdated(CollidableB, this);
                CollidableB.EventTriggerer.OnPairUpdated(CollidableA, this);
            }

            UpdateContacts(dt);


            if (contactCount > 0)
            {
                if (!suppressEvents)
                {
                    CollidableA.EventTriggerer.OnPairTouching(CollidableB, this);
                    CollidableB.EventTriggerer.OnPairTouching(CollidableA, this);
                }

                if (previousContactCount == 0)
                {
                    //collision started!
                    CollidableA.EventTriggerer.OnInitialCollisionDetected(CollidableB, this);
                    CollidableB.EventTriggerer.OnInitialCollisionDetected(CollidableA, this);

                    //No solver updateable addition in this method since it's handled by the "AddSolverUpdateable" method.
                }
            }
            else if (previousContactCount > 0 && !suppressEvents)
            {
                //collision ended!
                CollidableA.EventTriggerer.OnCollisionEnded(CollidableB, this);
                CollidableB.EventTriggerer.OnCollisionEnded(CollidableA, this);

                //No solver updateable removal in this method since it's handled by the "RemoveSolverUpdateable" method.
            }
            previousContactCount = contactCount;

        }

        ///<summary>
        /// Updates the time of impact for the pair.
        ///</summary>
        ///<param name="requester">Collidable requesting the update.</param>
        ///<param name="dt">Timestep duration.</param>
        public override void UpdateTimeOfImpact(Collidable requester, float dt)
        {
            timeOfImpact = 1;
            foreach (var pair in subPairs.Values)
            {
                //The system uses the identity of the requester to determine if it needs to do handle the TOI calculation.
                //Use the child pair's own entries as a proxy.
                if (BroadPhaseOverlap.entryA == requester)
                    pair.UpdateTimeOfImpact((Collidable)pair.BroadPhaseOverlap.entryA, dt);
                else
                    pair.UpdateTimeOfImpact((Collidable)pair.BroadPhaseOverlap.entryB, dt);
                if (pair.timeOfImpact < timeOfImpact)
                    timeOfImpact = pair.timeOfImpact;
            }
        }


        protected internal override void GetContactInformation(int index, out ContactInformation info)
        {
            foreach (CollidablePairHandler pair in subPairs.Values)
            {
                int count = pair.Contacts.Count;
                if (index - count < 0)
                {
                    pair.GetContactInformation(index, out info);
                    return;
                }
                index -= count;
            }
            throw new IndexOutOfRangeException("Contact index is not present in the pair.");

        }


        void IPairHandlerParent.AddSolverUpdateable(EntitySolverUpdateable addedItem)
        {

            manifoldConstraintGroup.Add(addedItem);
            //If this is the first child solver item to be added, we need to add ourselves to our parent too.
            if (manifoldConstraintGroup.SolverUpdateables.Count == 1)
            {
                if (Parent != null)
                    Parent.AddSolverUpdateable(manifoldConstraintGroup);
                else if (NarrowPhase != null)
                    NarrowPhase.NotifyUpdateableAdded(manifoldConstraintGroup);
            }

        }

        void IPairHandlerParent.RemoveSolverUpdateable(EntitySolverUpdateable removedItem)
        {

            manifoldConstraintGroup.Remove(removedItem);

            //If this is the last child solver item, we need to remove ourselves from our parent too.
            if (manifoldConstraintGroup.SolverUpdateables.Count == 0)
            {
                if (Parent != null)
                    Parent.RemoveSolverUpdateable(manifoldConstraintGroup);
                else if (NarrowPhase != null)
                    NarrowPhase.NotifyUpdateableRemoved(manifoldConstraintGroup);

            }


        }


        void IPairHandlerParent.OnContactAdded(Contact contact)
        {
            contactCount++;
            OnContactAdded(contact);
        }

        void IPairHandlerParent.OnContactRemoved(Contact contact)
        {
            contactCount--;
            OnContactRemoved(contact);
        }




        int contactCount;
        /// <summary>
        /// Gets the number of contacts in the pair.
        /// </summary>
        protected internal override int ContactCount
        {
            get { return contactCount; }
        }

        /// <summary>
        /// Clears the pair's contacts.
        /// </summary>
        public override void ClearContacts()
        {
            foreach (var pair in subPairs.Values)
            {
                pair.ClearContacts();
            }
            base.ClearContacts();
        }
    }
}
