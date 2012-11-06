namespace BEPUphysics.Settings
{
    ///<summary>
    /// Contains global settings relating to the collision response system.
    ///</summary>
    public static class CollisionResponseSettings
    {
        /// <summary>
        /// Impact velocity above which the bouciness of the object pair is taken into account.  Below the threshold, no extra energy is added.
        /// Defaults to 1.
        /// </summary>
        public static float BouncinessVelocityThreshold = 1;

        /// <summary>
        /// Maximum speed at which interpenetrating objects will attempt to undo any overlap.
        /// Defaults to 2.
        /// </summary>
        public static float MaximumPenetrationCorrectionSpeed = 2;

        /// <summary>
        /// Fraction of position error to convert into corrective momentum.
        /// Defaults to .2.
        /// </summary>
        public static float PenetrationRecoveryStiffness = .2f;

        /// <summary>
        /// Magnitude of relative velocity at a contact point below which staticFriction is used.
        /// dynamicFriction is used when velocity exceeds this threshold.
        /// Defaults to .2.
        /// </summary>
        public static float StaticFrictionVelocityThreshold = .2f;

        /// <summary>
        /// Value by which a collision pair's friction coefficient will be multiplied to get the twist friction coefficient.
        /// Defaults to 1.
        /// </summary>
        public static float TwistFrictionFactor = 1f;


    }
}
