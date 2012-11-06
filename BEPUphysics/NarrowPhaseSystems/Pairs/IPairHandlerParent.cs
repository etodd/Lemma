using BEPUphysics.Constraints;
using BEPUphysics.CollisionTests;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Defines a pair handler which can have children.
    ///</summary>
    public interface IPairHandlerParent
    {
        ///<summary>
        /// Called when a child adds a contact.
        ///</summary>
        ///<param name="contact">Contact added.</param>
        void OnContactAdded(Contact contact);

        /// <summary>
        /// Called when a child removes a contact.
        /// </summary>
        /// <param name="contact">Contact removed.</param>
        void OnContactRemoved(Contact contact);

        ///<summary>
        /// Called when a child attempts to add a solver updateable to the solver.
        ///</summary>
        ///<param name="addedItem">Item to add.</param>
        void AddSolverUpdateable(EntitySolverUpdateable addedItem);


        ///<summary>
        /// Called when a child attempts to remove a solver updateable from the solver.
        ///</summary>
        ///<param name="removedItem">Item to remove.</param>
        void RemoveSolverUpdateable(EntitySolverUpdateable removedItem);
    }
}
