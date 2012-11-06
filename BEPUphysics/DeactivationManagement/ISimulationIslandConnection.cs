using System.Collections.ObjectModel;
using BEPUphysics.DataStructures;

namespace BEPUphysics.DeactivationManagement
{
    ///<summary>
    /// Defines an object which connects simulation islands together.
    ///</summary>
    public interface ISimulationIslandConnection
    {
        ///<summary>
        /// Gets or sets the deactivation member that owns this connection.
        ///</summary>
        DeactivationManager DeactivationManager { get; set; }

        ///<summary>
        /// Gets the simulation island members associated with this connection.
        ///</summary>
        ReadOnlyList<SimulationIslandMember> ConnectedMembers { get; }

        ///<summary>
        /// Adds references to the connection to all connected members.
        ///</summary>
        void AddReferencesToConnectedMembers();

        ///<summary>
        /// Removes references to the connection from all connected members.
        ///</summary>
        void RemoveReferencesFromConnectedMembers();

        //When connections are modified, simulation island needs to be updated.
        //Could have a custom collection that shoots off events when it is changed, or otherwise notifies...
        //But then again, whatever is doing the changing can do the notification without a custom collection.
        //It would need to call merge/trysplit..  Not everything has the ability to change connections, but some do.
        //Leave it up to the implementors :)


        //When "Added," it attempts to merge the simulation islands of its connected members.
        //When "Removed," it attempts to split the simulation islands of its connected members.

        //The SimulationIslandConnection is not itself a thing that is added/removed to a set somewhere.
        //It is a description of some object.  The act of 'adding' a simulation island connection is completed in full by
        //merging simulation islands and the associated bookkeeping- there does not need to be a list anywhere.
        //Likewise, removing a simulation island is completed in full by the split attempt.

        //However, whatever is doing the management of things that happen to be ISimulationIslandConnections must know how to deal with them.
        //(Using the SimulationIsland.Merge, TrySplit methods).
        //This leaves a lot of responsibility in the implementation's hands.
        //Maybe this is just fine.  Consider that this isn't exactly a common situation.
        //Known, in-engine scenarios:
        //Constraints
        //Collision Pairs.
        
        //When constraints are added or removed to the space (or maybe more directly the solver, to unify with cp's), the merge happens.
        //When they are removed from the space, the split happens.
        
        //All SolverItems are also simulation connections, which is why it makes sense to do it at the solver time.... HOWEVER...
        //It is possible to have a simulation island connection that isn't a solver item at all.  Think particle collision.
        //In the particle case, the engine doesn't 'know' anything about the type- it's 100% custom.
        //The OnAdditionToSpace methods come in handy.  Particle collision says, well, the system won't do it for me since I'm not going to be added to the solver.
        //So instead, whatever the thing is that handles the 'force application' part of the particle system would perform the necessary simulation island management.

        //Sidenote:  The particle collision system would, in practice, probably just be a Solver item with 1 iteration.

    }
}
