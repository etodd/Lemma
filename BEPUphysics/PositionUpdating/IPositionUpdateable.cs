namespace BEPUphysics.PositionUpdating
{
    ///<summary>
    /// Defines an object capable of a position update.
    ///</summary>
    public interface IPositionUpdateable
    {
        ///<summary>
        /// Gets whether or not the object is active.
        /// Only active objects will be updated.
        ///</summary>
        bool IsActive { get; }

        ///<summary>
        /// Gets or sets the position updater that owns this updateable.
        ///</summary>
        PositionUpdater PositionUpdater { get; set; }

        ///<summary>
        /// Updates the position state of the object.
        ///</summary>
        ///<param name="dt">Time step duration.</param>
        void PreUpdatePosition(float dt);
    }
}
