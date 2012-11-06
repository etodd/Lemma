namespace BEPUphysics.OtherSpaceStages
{
    ///<summary>
    /// Defines an object that owns a deferred event creator.
    ///</summary>
    public interface IDeferredEventCreatorOwner
    {
        ///<summary>
        /// Gets the event creator owned by the object.
        ///</summary>
        IDeferredEventCreator EventCreator { get; }
    }
}
