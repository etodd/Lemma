using System;
using System.Collections.Generic;
using BEPUphysics.Threading;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace BEPUphysics.DeactivationManagement
{
    ///<summary>
    /// Manages the sleeping states of objects.
    ///</summary>
    public class DeactivationManager : MultithreadedProcessingStage
    {
        private int maximumDeactivationAttemptsPerFrame = 100;
        private int deactivationIslandIndex;

        internal float velocityLowerLimitSquared = .07f;
        internal float lowVelocityTimeMinimum = 1f;

        ///<summary>
        /// Gets or sets the velocity under which the deactivation system will consider 
        /// objects to be deactivation candidates (if their velocity stays below the limit
        /// for the LowVelocityTimeMinimum).
        /// Defaults to 0.26.
        ///</summary>
        public float VelocityLowerLimit
        {
            get
            {
                return (float)Math.Sqrt(velocityLowerLimitSquared);
            }
            set
            {
                velocityLowerLimitSquared = value * value;
            }
        }

        /// <summary>
        /// Gets or sets the time limit above which the deactivation system will consider
        /// objects to be deactivation candidates (if their velocity stays below the VelocityLowerLimit for the duration).
        /// Defaults to 1.
        /// </summary>
        public float LowVelocityTimeMinimum
        {
            get
            {
                return lowVelocityTimeMinimum;
            }
            set
            {
                if (value <= 0)
                    throw new Exception("Must use a positive, non-zero value for deactivation time minimum.");
                lowVelocityTimeMinimum = value;
            }
        }

        internal bool useStabilization = true;
        ///<summary>
        /// Gets or sets whether or not to use a stabilization effect on nearly motionless objects.
        /// This removes a lot of energy from a system when things are settling down, allowing them to go 
        /// to sleep faster.  It also makes most simulations appear a lot more robust.
        /// Defaults to true.
        ///</summary>
        public bool UseStabilization
        {
            get
            {
                return useStabilization;
            }
            set
            {
                useStabilization = value;
            }
        }



        //TryToSplit is NOT THREAD SAFE.  Only one TryToSplit should ever be run.
        Queue<SimulationIslandMember> member1Friends = new Queue<SimulationIslandMember>(), member2Friends = new Queue<SimulationIslandMember>();
        List<SimulationIslandMember> searchedMembers1 = new List<SimulationIslandMember>(), searchedMembers2 = new List<SimulationIslandMember>();

        ///<summary>
        /// Gets or sets the maximum number of objects to attempt to deactivate each frame.
        /// Defaults to 100.
        ///</summary>
        public int MaximumDeactivationAttemptsPerFrame { get { return maximumDeactivationAttemptsPerFrame; } set { maximumDeactivationAttemptsPerFrame = value; } }

        TimeStepSettings timeStepSettings;
        ///<summary>
        /// Gets or sets the time step settings used by the deactivation manager.
        ///</summary>
        public TimeStepSettings TimeStepSettings
        {
            get
            {
                return timeStepSettings;
            }
            set
            {
                timeStepSettings = value;
            }
        }

        ///<summary>
        /// Constructs a deactivation manager.
        ///</summary>
        ///<param name="timeStepSettings">The time step settings used by the manager.</param>
        public DeactivationManager(TimeStepSettings timeStepSettings)
        {
            Enabled = true;
            multithreadedCandidacyLoopDelegate = MultithreadedCandidacyLoop;
            this.timeStepSettings = timeStepSettings;
        }

        ///<summary>
        /// Constructs a deactivation manager.
        ///</summary>
        ///<param name="timeStepSettings">The time step settings used by the manager.</param>
        /// <param name="threadManager">Thread manager used by the manager.</param>
        public DeactivationManager(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : this(timeStepSettings)
        {
            ThreadManager = threadManager;
            AllowMultithreading = true;
        }
        //TODO: Deactivation Candidate Detection
        //-Could scan the entities of CURRENTLY ACTIVE simulation islands.
        //-Requires a List-format of active sim islands.
        //-Requires sim islands have a list-format entity set.
        //-Simulation islands of different sizes won't load-balance well on the xbox360; it would be fine on the pc though.
        //TODO: Simulation Island Deactivation

        RawList<SimulationIslandMember> simulationIslandMembers = new RawList<SimulationIslandMember>();
        RawList<SimulationIsland> simulationIslands = new RawList<SimulationIsland>();

        ///<summary>
        /// Gets the simulation islands currently in the manager.
        ///</summary>
        public ReadOnlyList<SimulationIsland> SimulationIslands
        {
            get
            {
                return new ReadOnlyList<SimulationIsland>(simulationIslands);
            }
        }

        UnsafeResourcePool<SimulationIsland> islandPool = new UnsafeResourcePool<SimulationIsland>();

        void GiveBackIsland(SimulationIsland island)
        {
            island.CleanUp();
            islandPool.GiveBack(island);
        }

        ///<summary>
        /// Adds a simulation island member to the manager.
        ///</summary>
        ///<param name="simulationIslandMember">Member to add.</param>
        ///<exception cref="Exception">Thrown if the member already belongs to a manager.</exception>
        public void Add(SimulationIslandMember simulationIslandMember)
        {
            if (simulationIslandMember.DeactivationManager == null)
            {
                simulationIslandMember.Activate();
                simulationIslandMember.DeactivationManager = this;
                simulationIslandMembers.Add(simulationIslandMember);
                if (simulationIslandMember.IsDynamic)
                {
                    AddSimulationIslandToMember(simulationIslandMember);
                }
                else
                {
                    RemoveSimulationIslandFromMember(simulationIslandMember);
                }
            }
            else
                throw new Exception("Cannot add that member to this DeactivationManager; it already belongs to a manager.");
        }

        /// <summary>
        /// Removes the member from this island.
        /// </summary>
        /// <param name="simulationIslandMember">Removes the member from the manager.</param>
        public void Remove(SimulationIslandMember simulationIslandMember)
        {
            if (simulationIslandMember.DeactivationManager == this)
            {
                if (simulationIslandMember.IsDynamic)
                    simulationIslandMember.Activate();
                else
                {
                    //If the object was NOT dynamic, then simply calling activate will be insufficient
                    //because it does not share any simulation island with connected objects.
                    //We need to notify its connections directly.
                    foreach (var connection in simulationIslandMember.connections)
                    {
                        foreach (var entry in connection.entries)
                        {
                            if (entry.Member != simulationIslandMember)
                                entry.Member.Activate();
                        }
                    }
                }
                simulationIslandMember.DeactivationManager = null;
                simulationIslandMembers.Remove(simulationIslandMember);
                RemoveSimulationIslandFromMember(simulationIslandMember);

            }
            else
                throw new Exception("Cannot remove that member from this DeactivationManager; it belongs to a different or no manager.");
        }

        Action<int> multithreadedCandidacyLoopDelegate;
        void MultithreadedCandidacyLoop(int i)
        {
            simulationIslandMembers.Elements[i].UpdateDeactivationCandidacy(timeStepSettings.TimeStepDuration);
        }

        protected override void UpdateMultithreaded()
        {
            FlushSplits();

            ThreadManager.ForLoop(0, simulationIslandMembers.count, multithreadedCandidacyLoopDelegate);

            DeactivateObjects();
        }

        //RawList<SimulationIslandConnection> debugConnections = new RawList<SimulationIslandConnection>();
        protected override void UpdateSingleThreaded()
        {
            FlushSplits();

            for (int i = 0; i < simulationIslandMembers.count; i++)
                simulationIslandMembers.Elements[i].UpdateDeactivationCandidacy(timeStepSettings.TimeStepDuration);

            DeactivateObjects();

        }


        Queue<SimulationIslandConnection> splitAttempts = new Queue<SimulationIslandConnection>();

        static float maximumSplitAttemptsFraction = .01f;
        /// <summary>
        /// Gets or sets the fraction of splits that the deactivation manager will attempt in a single frame.
        /// The total splits queued multiplied by this value results in the number of splits managed.
        /// Defaults to .04f.
        /// </summary>
        public static float MaximumSplitAttemptsFraction
        {
            get
            {
                return maximumSplitAttemptsFraction;
            }
            set
            {
                if (value > 1 || value < 0)
                    throw new Exception("Value must be from zero to one.");
                maximumSplitAttemptsFraction = value;
            }
        }
        static int minimumSplitAttempts = 3;
        /// <summary>
        /// Gets or sets the minimum number of splits attempted in a single frame.
        /// Defaults to 5.
        /// </summary>
        public static int MinimumSplitAttempts
        {
            get
            {
                return minimumSplitAttempts;
            }
            set
            {
                if (value >= 0)
                    throw new Exception("Minimum split count must be nonnegative.");
                minimumSplitAttempts = value;
            }
        }
        void FlushSplits()
        {
            //Only do a portion of the total splits.
            int maxAttempts = Math.Max(minimumSplitAttempts, (int)(splitAttempts.Count * maximumSplitAttemptsFraction));
            int attempts = 0;
            while (attempts < maxAttempts && splitAttempts.Count > 0)
            {
                var attempt = splitAttempts.Dequeue();
                if (attempt.SlatedForRemoval) //If it was re-added, don't split!
                {
                    attempt.SlatedForRemoval = false; //Reset the removal state so that future adds will add back references, since we're about to remove them.
                    attempt.RemoveReferencesFromConnectedMembers();
                    bool triedToSplit = false;
                    for (int i = 0; i < attempt.entries.count; i++)
                    {
                        for (int j = i + 1; j < attempt.entries.count; j++)
                        {
                            triedToSplit |= TryToSplit(attempt.entries.Elements[i].Member, attempt.entries.Elements[j].Member);
                        }
                    }
                    //Only count the split if it does any work.
                    if (triedToSplit)
                        attempts++;
                    if (attempt.Owner == null)
                    {
                        //It's an orphan connection.  No one owns it, and now that it's been dequeued from the deactivation manager,
                        //it has no home at all.
                        //Don't let it rot- return it to the pool!
                        Resources.GiveBack(attempt);
                        //This occurs when a constraint changes members.
                        //Because connections need to be immutable for this scheme to work,
                        //the old connection is orphaned and put into the deactivation manager's removal queue
                        //while a new one from the pool takes its place.
                    }
                }

            }

        }


        void DeactivateObjects()
        {
            //Deactivate only some objects each frame.
            int numberOfEntitiesDeactivated = 0;
            int numberOfIslandsChecked = 0;
            int originalIslandCount = simulationIslands.count;
            while (numberOfEntitiesDeactivated < maximumDeactivationAttemptsPerFrame && simulationIslands.count > 0 && numberOfIslandsChecked < originalIslandCount)
            {
                deactivationIslandIndex = (deactivationIslandIndex + 1) % simulationIslands.count;
                var island = simulationIslands.Elements[deactivationIslandIndex];
                if (island.memberCount == 0)
                {
                    //Found an orphan island left over from merge procedures or removal procedures.
                    //Shoo it on out.
                    simulationIslands.FastRemoveAt(deactivationIslandIndex);
                    GiveBackIsland(island);
                }
                else
                {
                    island.TryToDeactivate();
                    numberOfEntitiesDeactivated += island.memberCount;
                }
                numberOfIslandsChecked++;
            }
        }

        ///<summary>
        /// Adds a simulation island connection to the deactivation manager.
        ///</summary>
        ///<param name="connection">Connection to add.</param>
        ///<exception cref="ArgumentException">Thrown if the connection already belongs to a manager.</exception>
        public void Add(SimulationIslandConnection connection)
        {
            //DO A MERGE IF NECESSARY
            if (connection.DeactivationManager == null)
            {
                connection.DeactivationManager = this;
                if (connection.entries.count > 0)
                {
                    var island = connection.entries.Elements[0].Member.SimulationIsland;
                    for (int i = 1; i < connection.entries.count; i++)
                    {
                        SimulationIsland opposingIsland;
                        if (island != (opposingIsland = connection.entries.Elements[i].Member.SimulationIsland))
                        {
                            //Need to do a merge between the two islands.
                            //Note that this merge may eliminate the need for a merge with subsequent connection if they belong to the same island.
                            island = Merge(island, opposingIsland);
                        }

                    }

                    if (connection.SlatedForRemoval)
                        connection.SlatedForRemoval = false; //If it was slated for removal, that means connections are still present.  Just set the flag and the removal system will ignore the removal order.
                    else
                        connection.AddReferencesToConnectedMembers();
                    //debugConnections.Add(connection);
                }
            }
            else
            {
                throw new ArgumentException("Cannot add connection to deactivation manager; it already belongs to one.");
            }
        }

        private SimulationIsland Merge(SimulationIsland s1, SimulationIsland s2)
        {
            //Pull the smaller island into the larger island and set all members
            //of the smaller island to refer to the new island.

            //The simulation islands can be null; a connection can be a kinematic entity, which has no simulation island.
            //'Merging' a null island with an island simply gets back the island.
            if (s1 == null)
            {
                //Should still activate the island, though.
                s2.Activate();
                return s2;
            }
            if (s2 == null)
            {
                //Should still activate the island, though.
                s1.Activate();
                return s1;
            }

            //Swap if needed so s1 is the bigger island
            if (s1.memberCount < s2.memberCount)
            {
                var biggerIsland = s2;
                s2 = s1;
                s1 = biggerIsland;
            }

            s1.Activate();
            s2.immediateParent = s1;

            //This is a bit like a 'union by rank.'
            //But don't get confused- simulation islands are not a union find structure.
            //This parenting simply avoids the need for maintaining a list of members in each simulation island.
            //In the subsequent frame, the deactivation candidacy update will go through the parents and eat away
            //at the child simulation island.  Then, in a later TryToDeactivate phase, the then-empty simulation island
            //will be removed.

            //The larger one survives.
            return s1;
        }



        ///<summary>
        /// Removes a simulation island connection from the manager.
        ///</summary>
        ///<param name="connection">Connection to remove from the manager.</param>
        ///<exception cref="ArgumentException">Thrown if the connection does not belong to this manager.</exception>
        public void Remove(SimulationIslandConnection connection)
        {
            if (connection.DeactivationManager == this)
            {
                connection.DeactivationManager = null; //TODO: Should it remove here, or in the deferred final case? probably here, since otherwise Add-Remove-Add would throw an exception!
                //Try to split by examining the connections and breadth-first searching outward.
                //If it is determined that a split is required, grab a new island and add it.
                //This is a little tricky because it's a theoretically N-way split.

                //For two members which have the same simulation island (they will initially), try to split.
                //debugConnections.Remove(connection);
                connection.SlatedForRemoval = true;
                //Don't immediately do the removal.
                //Defer them!

                splitAttempts.Enqueue(connection);

                //connection.RemoveReferencesFromConnectedMembers();
                //for (int i = 0; i < connection.members.count; i++)
                //{
                //    for (int j = i + 1; j < connection.members.count; j++)
                //    {
                //        //Notice that the splits are not performed immediately! They are deferred and spread over multiple frames.
                //        //Split operations aren't cheap, and overdoing them can lead to pointless re-merging and re-splitting.
                //        splitAttempts.Enqueue(new SplitAttempt() { a = connection.members.Elements[i], b = connection.members.Elements[j] });
                //        //TryToSplit(connection.ConnectedMembers[i], connection.ConnectedMembers[j]);
                //    }
                //}

            }
            else
            {
                throw new ArgumentException("Cannot remove connection from activity manager; it is owned by a different or no activity manager.");
            }



        }


        /// <summary>
        /// Tries to split connections between the two island members.
        /// </summary>
        /// <param name="member1">First island member.</param>
        /// <param name="member2">Second island member.</param>
        /// <returns>Whether a split operation was run.  This does not mean a split was
        /// successful, just that the expensive test was performed.</returns>
        private bool TryToSplit(SimulationIslandMember member1, SimulationIslandMember member2)
        {
            //Can't split if they aren't even in the same island.
            //This also covers the case where the connection involves a kinematic entity that has no 
            //simulation island at all.
            if (member1.SimulationIsland != member2.SimulationIsland ||
                member1.SimulationIsland == null ||
                member2.SimulationIsland == null)
                return false;


            //By now, we know the members belong to the same island and are not null.
            //Start a BFS starting from each member.
            //Two-way can complete the search quicker.

            member1Friends.Enqueue(member1);
            member2Friends.Enqueue(member2);
            searchedMembers1.Add(member1);
            searchedMembers2.Add(member2);
            member1.searchState = SimulationIslandSearchState.OwnedByFirst;
            member2.searchState = SimulationIslandSearchState.OwnedBySecond;

            while (member1Friends.Count > 0 && member2Friends.Count > 0)
            {


                SimulationIslandMember currentNode = member1Friends.Dequeue();
                for (int i = 0; i < currentNode.connections.count; i++)
                {
                    for (int j = 0; j < currentNode.connections.Elements[i].entries.count; j++)
                    {
                        SimulationIslandMember connectedNode;
                        if ((connectedNode = currentNode.connections.Elements[i].entries.Elements[j].Member) != currentNode &&
                            connectedNode.SimulationIsland != null) //The connection could be connected to something that isn't in the Space and has no island, or it's not dynamic.
                        {
                            switch (connectedNode.searchState)
                            {
                                case SimulationIslandSearchState.Unclaimed:
                                    //Found a new friend :)
                                    member1Friends.Enqueue(connectedNode);
                                    connectedNode.searchState = SimulationIslandSearchState.OwnedByFirst;
                                    searchedMembers1.Add(connectedNode);
                                    break;
                                case SimulationIslandSearchState.OwnedBySecond:
                                    //Found our way to member2Friends set; cannot split!
                                    member1Friends.Clear();
                                    member2Friends.Clear();
                                    goto ResetSearchStates;
                            }

                        }
                    }
                }

                currentNode = member2Friends.Dequeue();
                for (int i = 0; i < currentNode.connections.count; i++)
                {
                    for (int j = 0; j < currentNode.connections.Elements[i].entries.count; j++)
                    {
                        SimulationIslandMember connectedNode;
                        if ((connectedNode = currentNode.connections.Elements[i].entries.Elements[j].Member) != currentNode &&
                            connectedNode.SimulationIsland != null) //The connection could be connected to something that isn't in the Space and has no island, or it's not dynamic.
                        {
                            switch (connectedNode.searchState)
                            {
                                case SimulationIslandSearchState.Unclaimed:
                                    //Found a new friend :)
                                    member2Friends.Enqueue(connectedNode);
                                    connectedNode.searchState = SimulationIslandSearchState.OwnedBySecond;
                                    searchedMembers2.Add(connectedNode);
                                    break;
                                case SimulationIslandSearchState.OwnedByFirst:
                                    //Found our way to member1Friends set; cannot split!
                                    member1Friends.Clear();
                                    member2Friends.Clear();
                                    goto ResetSearchStates;
                            }

                        }
                    }
                }
            }
            //If one of the queues empties out without finding anything, it means it's isolated.  The other one will never find it.
            //Now we can do a split.  Grab a new Island, fill it with the isolated search stuff.  Remove the isolated search stuff from the old Island.


            SimulationIsland newIsland = islandPool.Take();
            simulationIslands.Add(newIsland);
            if (member1Friends.Count == 0)
            {

                //Member 1 is isolated, give it its own simulation island!
                for (int i = 0; i < searchedMembers1.Count; i++)
                {
                    searchedMembers1[i].simulationIsland.Remove(searchedMembers1[i]);
                    newIsland.Add(searchedMembers1[i]);
                }
                member2Friends.Clear();
            }
            else if (member2Friends.Count == 0)
            {

                //Member 2 is isolated, give it its own simulation island!
                for (int i = 0; i < searchedMembers2.Count; i++)
                {
                    searchedMembers2[i].simulationIsland.Remove(searchedMembers2[i]);
                    newIsland.Add(searchedMembers2[i]);
                }
                member1Friends.Clear();
            }

            //Force the system awake.
            //Technically, the members should already be awake.
            //However, calling Activate on them resets the members'
            //deactivation candidacy timers.  This prevents the island
            //from instantly going back to sleep, which could leave
            //objects hanging in mid-air.
            member1.Activate();
            member2.Activate();


        ResetSearchStates:
            for (int i = 0; i < searchedMembers1.Count; i++)
            {
                searchedMembers1[i].searchState = SimulationIslandSearchState.Unclaimed;
            }
            for (int i = 0; i < searchedMembers2.Count; i++)
            {
                searchedMembers2[i].searchState = SimulationIslandSearchState.Unclaimed;
            }
            searchedMembers1.Clear();
            searchedMembers2.Clear();
            return true;

        }




        ///<summary>
        /// Strips a member of its simulation island.
        ///</summary>
        ///<param name="member">Member to be stripped.</param>
        public void RemoveSimulationIslandFromMember(SimulationIslandMember member)
        {

            //Becoming kinematic eliminates the member as a possible path.
            //Splits must be attempted between its connected members.
            //Don't need to split same-connection members.  Splitting one non-null entry against a non null entry in each of the other connections will do the trick.
            if (member.simulationIsland != null)
            {
                //Note that this is using the most immediate simulation island.  This is because the immediate simulation island
                //is the one who 'owns' the member; not the root parent.  The root parent will own the member in the next frame
                //after the deactivation candidacy loop runs.
                SimulationIsland island = member.simulationIsland;
                island.Remove(member);
                if (island.memberCount == 0)
                {
                    simulationIslands.Remove(island);
                    GiveBackIsland(island);
                    //Even though we appear to have connections, the island was only me!
                    //We can stop now.
                    //Note that we do NOT remove the island from the simulation islands list here.
                    //That would take an O(n) search.  Instead, orphan it and let the TryToDeactivate loop find it.
                    return;
                }
            }
            if (member.connections.count > 0)
            {
                for (int i = 0; i < member.Connections.Count; i++)
                {
                    //Find a member with a non-null island to represent connection i.
                    SimulationIslandMember representativeA = null;
                    for (int j = 0; j < member.connections.Elements[i].entries.count; j++)
                    {
                        if (member.connections.Elements[i].entries.Elements[j].Member.SimulationIsland != null)
                        {
                            representativeA = member;
                            break;
                        }
                    }

                    if (representativeA == null)
                    {
                        //There was no representative!  That means it was a connection in which
                        //no member had a simulation island.  Consider removing a dynamic box from the space
                        //while it sits on a kinematic box.  Neither object has a simulation island.
                        //In this case, simply try the next connection.
                        continue;
                    }
                    //Split the representative against representatives from other connections.
                    for (int j = i + 1; j < member.Connections.Count; j++)
                    {
                        //Find a representative for another connection.
                        SimulationIslandMember representativeB = null;
                        for (int k = 0; k < member.connections.Elements[j].entries.count; k++)
                        {
                            if (member.connections.Elements[j].entries.Elements[k].Member.SimulationIsland != null)
                            {
                                representativeB = member;
                                break;
                            }
                        }

                        if (representativeB == null)
                        {
                            //There was no representative!  Same idea as above.
                            //Try the next connection.
                            continue;
                        }

                        //Try to split the representatives.
                        //Don't bother doing any deferring; this is a rare activity
                        //and it's best just to do it up front.
                        TryToSplit(representativeA, representativeB);


                    }
                }
            }


        }

        ///<summary>
        /// Adds a simulation island to a member.
        ///</summary>
        ///<param name="member">Member to gain a simulation island.</param>
        ///<exception cref="Exception">Thrown if the member already has a simulation island.</exception>
        public void AddSimulationIslandToMember(SimulationIslandMember member)
        {
            if (member.SimulationIsland != null)
            {
                throw new Exception("Cannot initialize member's simulation island; it already has one.");
            }
            if (member.Connections.Count > 0)
            {
                SimulationIsland island = null;
                //Find a simulation starting island to live in.
                for (int i = 0; i < member.Connections.Count; i++)
                {
                    for (int j = 0; j < member.connections.Elements[i].entries.count; j++)
                    {
                        island = member.connections.Elements[i].entries.Elements[j].Member.SimulationIsland;
                        if (island != null)
                        {
                            island.Add(member);
                            break;
                        }
                    }
                    if (island != null)
                        break;
                }
                if (member.SimulationIsland == null)
                {
                    //No non-null entries in any connections.  That's weird.
                    //Maybe it's connected to a bunch of kinematics, or maybe it's a vehicle-like situation
                    //where the body is associated with a 'vehicle' connection which sometimes contains only the body.

                    //No friends to merge with.
                    SimulationIsland newIsland = islandPool.Take();
                    simulationIslands.Add(newIsland);
                    newIsland.Add(member);
                    return;
                }


                //Becoming dynamic adds a new path.
                //Merges must be attempted between its connected members.
                for (int i = 0; i < member.connections.count; i++)
                {
                    for (int j = 0; j < member.connections.Elements[i].entries.count; j++)
                    {
                        if (member.connections.Elements[i].entries.Elements[j].Member == member)
                            continue; //Don't bother trying to compare against ourselves.  That would cause an erroneous early-out sometimes.
                        SimulationIsland opposingIsland = member.connections.Elements[i].entries.Elements[j].Member.SimulationIsland;
                        if (opposingIsland != null)
                        {
                            if (island != opposingIsland)
                            {
                                island = Merge(island, opposingIsland);
                            }
                            //All non-null simulation islands in a single connection are guaranteed to be the same island due to previous merges.
                            //Once we find one, we can stop.
                            break;
                        }
                    }
                }
            }
            else
            {
                //No friends to merge with.
                SimulationIsland newIsland = islandPool.Take();
                simulationIslands.Add(newIsland);
                newIsland.Add(member);
            }

        }
    }
}
