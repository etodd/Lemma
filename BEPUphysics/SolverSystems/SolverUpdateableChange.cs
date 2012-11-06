namespace BEPUphysics.SolverSystems
{
    ///<summary>
    /// Stores an enqueued solver updateable addition or removal.
    ///</summary>
    public struct SolverUpdateableChange
    {

        ///<summary>
        /// Whether the item is going to be added or removed.
        ///</summary>
        public bool ShouldAdd;
        ///<summary>
        /// Item being added or removed.
        ///</summary>
        public SolverUpdateable Item;

        ///<summary>
        /// Constructs a new solver updateable change.
        ///</summary>
        ///<param name="shouldAdd">Whether the item is going to be added or removed.</param>
        ///<param name="item">Item to add or remove.</param>
        public SolverUpdateableChange(bool shouldAdd, SolverUpdateable item)
        {
            ShouldAdd = shouldAdd;
            Item = item;
        }
    }
}
