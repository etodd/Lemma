using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Threading;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.DataStructures;
using System;

namespace BEPUphysics.BroadPhaseSystems
{
    ///<summary>
    /// Superclass of all broad phases.  Broad phases collect overlapping broad phase entry pairs.
    ///</summary>
    public abstract class BroadPhase : MultithreadedProcessingStage
    {
        readonly SpinLock overlapAddLock = new SpinLock();

        ///<summary>
        /// Gets the object which is locked by the broadphase during synchronized update processes.
        ///</summary>
        public object Locker { get; protected set; }
        protected BroadPhase()
        {
            Locker = new object();
            Enabled = true;
        }

        protected BroadPhase(IThreadManager threadManager)
            : this()
        {
            ThreadManager = threadManager;
            AllowMultithreading = true;
        }
        //TODO: Initial capacity?  Special collection type other than list due to structs? RawList? Clear at beginning of each frame?
        readonly RawList<BroadPhaseOverlap> overlaps = new RawList<BroadPhaseOverlap>();
        /// <summary>
        /// Gets the list of overlaps identified in the previous broad phase update.
        /// </summary>
        public RawList<BroadPhaseOverlap> Overlaps
        {
            get { return overlaps; }
        }



        ///<summary>
        /// Gets an interface to the broad phase's support for volume-based queries.
        ///</summary>
        public IQueryAccelerator QueryAccelerator { get; protected set; }

        /// <summary>
        /// Adds an entry to the broad phase.
        /// </summary>
        /// <param name="entry">Entry to add.</param>
        public virtual void Add(BroadPhaseEntry entry)
        {
            if (entry.BroadPhase == null)
                entry.BroadPhase = this;
            else
                throw new Exception("Cannot add entry; it already belongs to a broad phase.");
        }

        /// <summary>
        /// Removes an entry from the broad phase.
        /// </summary>
        /// <param name="entry">Entry to remove.</param>
        public virtual void Remove(BroadPhaseEntry entry)
        {
            if (entry.BroadPhase == this)
                entry.BroadPhase = null;
            else
                throw new Exception("Cannot remove entry; it does not belong to this broad phase.");
        }

        protected internal void AddOverlap(BroadPhaseOverlap overlap)
        {
            overlapAddLock.Enter();
            overlaps.Add(overlap);
            overlapAddLock.Exit();
        }

        /// <summary>
        /// Adds a broad phase overlap if the collision rules permit it.
        /// </summary>
        /// <param name="entryA">First entry of the overlap.</param>
        /// <param name="entryB">Second entry of the overlap.</param>
        protected internal void TryToAddOverlap(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            CollisionRule rule;
            if ((rule = GetCollisionRule(entryA, entryB)) < CollisionRule.NoBroadPhase)
            {
                overlapAddLock.Enter();
                overlaps.Add(new BroadPhaseOverlap(entryA, entryB, rule));
                overlapAddLock.Exit();
            }
        }

        protected internal CollisionRule GetCollisionRule(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            if (entryA.IsActive || entryB.IsActive)
                return CollisionRules.collisionRuleCalculator(entryA, entryB);
            return CollisionRule.NoBroadPhase;
        }

        //TODO: Consider what happens when an overlap is found twice.  How should it be dealt with?
        //Can the DBH spit out redundancies?
        //The PUG definitely can- consider two entities that are both in two adjacent cells.
        //Could say 'whatever' to it and handle it in the narrow phase-  use the NeedsUpdate property.
        //If NeedsUpdate is false, that means it's already been updated once.  Consider multithreaded problems.
        //Would require an interlocked compare exchange or something similar to protect it.  
        //Slightly ruins the whole 'embarassingly parallel' aspect.

        //Need a something which has O(1) add, O(1) contains check, and fast iteration without requiring external nodes since everything gets regenerated each frame.
    }
}
