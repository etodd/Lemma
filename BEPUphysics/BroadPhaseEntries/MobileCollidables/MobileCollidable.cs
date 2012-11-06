namespace BEPUphysics.Collidables.MobileCollidables
{
    //This is implemented by anything which wants the engine to update its bounding box every frame (so long as it is 'active').
    ///<summary>
    /// Superclass of all collidables which are capable of movement, and thus need bounding box updates every frame.
    ///</summary>
    public abstract class MobileCollidable : Collidable
    {
        //TODO: Imagine needing to calculate the bounding box for a data structure that is not axis-aligned.  Being able to return BB without 'setting' would be helpful.
        //Possibly require second method.  The parameterless one uses 'self data' to do the calculation, as a sort of convenience.  The parameterful would return without setting.
        ///<summary>
        /// Updates the bounding box of the mobile collidable.
        ///</summary>
        ///<param name="dt">Timestep with which to update the bounding box.</param>
        public abstract void UpdateBoundingBox(float dt);


    }
}
