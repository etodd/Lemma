namespace BEPUphysics.OtherSpaceStages
{
    ///<summary>
    /// Defines an object which can be updated using forces by the ForceUpdater.
    ///</summary>
    public interface IForceUpdateable
    {
        ///<summary>
        /// Applies forces to the object.
        ///</summary>
        ///<param name="dt">Time step duration.</param>
        void UpdateForForces(float dt);

        ///<summary>
        /// Force updater that owns this object.
        ///</summary>
        ForceUpdater ForceUpdater { get; set; }

        ///<summary>
        /// Gets whether or not this object is dynamic.
        /// Only dynamic objects are updated by the force updater.
        ///</summary>
        bool IsDynamic { get; }

        ///<summary>
        /// Gets whether or not this object is active.  Only active objects are updated by the force updater.
        ///</summary>
        bool IsActive { get; }
    }
}
