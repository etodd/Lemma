namespace BEPUphysics.PositionUpdating
{
    ///<summary>
    /// Update modes for position updateables.
    ///</summary>
    public enum PositionUpdateMode : byte
    {
        /// <summary>
        /// Updates position discretely regardless of its collision pairs.
        /// </summary>
        Discrete,
        /// <summary>
        /// Updates position discretely in isolation; when a Continuous object collides with it,
        /// its position update will be bounded by the time of impact.
        /// </summary>
        Passive,
        /// <summary>
        /// Updates position continuously.  Continuous objects will integrate up to their earliest collision time.
        /// </summary>
        Continuous
    }

    ///<summary>
    /// A position updateable that can be updated continuously.
    ///</summary>
    public interface ICCDPositionUpdateable : IPositionUpdateable
    {
        ///<summary>
        /// Updates the time of impacts associated with the updateable.
        ///</summary>
        ///<param name="dt">Time step duration.</param>
        void UpdateTimesOfImpact(float dt);

        /// <summary>
        /// Updates the updateable using its continuous nature.
        /// </summary>
        /// <param name="dt">Time step duration.</param>
        void UpdatePositionContinuously(float dt);

        /// <summary>
        /// Gets or sets the position update mode of the object.
        /// The position update mode defines the way the object
        /// interacts with continuous collision detection.
        /// </summary>
        PositionUpdateMode PositionUpdateMode { get; set; }
        
        /// <summary>
        /// Resets the times of impact for pairs associated with this position updateable.
        /// </summary>
        void ResetTimesOfImpact();
    }
}
