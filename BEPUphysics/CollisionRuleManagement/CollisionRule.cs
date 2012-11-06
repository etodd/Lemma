namespace BEPUphysics.CollisionRuleManagement
{
    /// <summary>
    /// Defines a set of rules that collisions can adhere to.
    /// </summary>
    public enum CollisionRule
    {
        /// <summary>
        /// Yields the interaction type's determination to a later stage.
        /// </summary>
        Defer,
        /// <summary>
        /// Uses all of collision detection, including creating a collision pair, creating contacts when appropriate, and responding to those contacts physically.
        /// If a collision pair is forced to use a 'normal' interaction but both entities in the pair are kinematic, the collision response will be skipped.
        /// </summary>
        Normal,
        /// <summary>
        /// Creates a collision pair and undergoes narrow phase testing, but does not collision response in the solver.
        /// </summary>
        NoSolver,
        /// <summary>
        /// Creates a broad phase overlap and narrow phase pair but the collision is never updated.  It cannot generate contacts nor undergo solving.
        /// </summary>
        NoNarrowPhaseUpdate,
        /// <summary>
        /// Creates a broad phase overlap but does not create any narrow phase pairs.  It cannot generate contacts nor undergo solving.
        /// </summary>
        NoNarrowPhasePair,
        /// <summary>
        /// Does not create a broad phase overlap.  No further collision detection or response takes place.
        /// </summary>
        NoBroadPhase
    }
}
