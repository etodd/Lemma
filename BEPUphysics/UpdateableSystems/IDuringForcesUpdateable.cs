namespace BEPUphysics.UpdateableSystems
{
    ///<summary>
    /// Defines an object which is updated by a space during force application.
    ///</summary>
    public interface IDuringForcesUpdateable : ISpaceUpdateable
    {

        ///<summary>
        /// Updates the object during force application.
        ///</summary>
        ///<param name="dt">Time step duration.</param>
        void Update(float dt);

    }
}
