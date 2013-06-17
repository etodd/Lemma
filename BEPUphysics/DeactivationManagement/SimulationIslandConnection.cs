using System.Threading;
using BEPUutilities.DataStructures;

namespace BEPUphysics.DeactivationManagement
{
    
    /// <summary>
    /// Connects simulation island members together.
    /// </summary>
    public class SimulationIslandConnection
    {
        /// <summary>
        /// Stores members and the index at which this connection is located in that member's connections list.
        /// This allows connections to be removed from members in constant time rather than linear time.
        /// </summary>
        internal struct Entry
        {
            internal SimulationIslandMember Member;
            internal int Index;
        }

        internal RawList<Entry> entries = new RawList<Entry>(2);
        /// <summary>
        /// Gets a list of members connected by the connection.
        /// </summary>
        public SimulationIslandMemberList Members
        {
            get
            {
                return new SimulationIslandMemberList(entries);
            }
        }

        /// <summary>
        /// Gets or sets the owner of the connection.
        /// </summary>
        public ISimulationIslandConnectionOwner Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether or not this connection is going to be removed
        /// by the next DeactivationManager stage run.  Connections
        /// slated for removal should not be considered to be part of
        /// a member's 'real' connections.
        /// </summary>
        public bool SlatedForRemoval { get; internal set; }



        /// <summary>
        /// Adds the connection to the connected members.
        /// </summary>
        public void AddReferencesToConnectedMembers()
        {
            //Add back the references to this to entities
            for (int i = 0; i < entries.Count; i++)
            {
                entries.Elements[i].Index = entries.Elements[i].Member.AddConnectionReference(this);
            }
        }

        /// <summary>
        /// Removes the connection from the connected members.
        /// </summary>
        public void RemoveReferencesFromConnectedMembers()
        {
            //Clean out the references entities may have had to this solver updateable.
            for (int i = 0; i < entries.Count; i++)
            {
                entries.Elements[i].Member.RemoveConnectionReference(this, entries.Elements[i].Index);
            }
        }

        /// <summary>
        /// Searches the list of members related to this connection and sets the index associated with this connection to the given value.
        /// </summary>
        /// <param name="member">Member to change the index for.</param>
        /// <param name="index">New index of this connection in the member's connections list.</param>
        internal void SetListIndex(SimulationIslandMember member, int index)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (member == entries.Elements[i].Member)
                {
                    entries.Elements[i].Index = index;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the deactivation manager that owns the connection.
        /// </summary>
        public DeactivationManager DeactivationManager { get; internal set; }


        internal void CleanUp()
        {
            SlatedForRemoval = false;
            entries.Clear();
            Owner = null;
            DeactivationManager = null;
        }

        /// <summary>
        /// Adds the member to the connection.
        /// </summary>
        /// <param name="simulationIslandMember">Member to add.</param>
        internal void Add(SimulationIslandMember simulationIslandMember)
        {
            //Note that the simulation member does not yet know about this connection, so the index is assigned to -1.
            entries.Add(new Entry { Index = -1, Member = simulationIslandMember });
        }
    }
}
