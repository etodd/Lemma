using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.CollisionRuleManagement;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Superclass of objects which handle a collision between two broad phase entries.
    ///</summary>
    public abstract class NarrowPhasePair
    {

        ///<summary>
        /// Updates the collision between the broad phase entries.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public abstract void UpdateCollision(float dt);

        ///<summary>
        /// Gets or sets whether or not the pair needs to be updated.
        /// Used by the NarrowPhase to manage pairs.
        ///</summary>
        public bool NeedsUpdate { get; set; }

        ///<summary>
        /// Gets or sets the collision rule governing this pair.
        ///</summary>
        public CollisionRule CollisionRule
        {
            get
            {
                return broadPhaseOverlap.collisionRule;
            }
            set
            {
                broadPhaseOverlap.collisionRule = value;
            }
        }

        internal BroadPhaseOverlap broadPhaseOverlap;
        ///<summary>
        /// Gets the overlap used to create the pair.
        ///</summary>
        public BroadPhaseOverlap BroadPhaseOverlap
        {
            get
            {
                return broadPhaseOverlap;
            }
            set
            {
                broadPhaseOverlap = value;
                Initialize(value.entryA, value.entryB);
            }
        }

        ///<summary>
        /// Gets the factory that created the pair.
        ///</summary>
        public NarrowPhasePairFactory Factory { get; internal set; }

        /// <summary>
        /// Gets the narrow phase that owns this pair.
        /// </summary>
        public NarrowPhase NarrowPhase { get; internal set; }

        ///<summary>
        /// Initializes the pair.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public abstract void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB);
        /// <summary>
        /// Called when the pair is added to the narrow phase.
        /// </summary>
        protected internal abstract void OnAddedToNarrowPhase();
        /// <summary>
        /// Cleans up the pair, preparing it to go inactive.
        /// </summary>
        public abstract void CleanUp();

    }
}
