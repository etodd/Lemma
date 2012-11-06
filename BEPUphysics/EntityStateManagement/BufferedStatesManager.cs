using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BEPUphysics.Threading;
using BEPUphysics.Entities;
using BEPUphysics.DataStructures;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// Manages the buffered and interpolated states of entities.
    ///</summary>
    public class BufferedStatesManager
    {

        ///<summary>
        /// Gets the buffers of last known entity states.
        ///</summary>
        public StateReadBuffers ReadBuffers { get; private set; }
        ///<summary>
        /// Gets the entity states blended between the current frame and previous frame based on
        /// the time remaining in internal time stepping.
        ///</summary>
        public InterpolatedStatesManager InterpolatedStates { get; private set; }

        internal RawList<Entity> entities = new RawList<Entity>();

        ///<summary>
        /// Gets the list of entities in the manager.
        ///</summary>
        public ReadOnlyList<Entity> Entities
        {
            get
            {
                return new ReadOnlyList<Entity>(entities);
            }
        }

        bool enabled;
        ///<summary>
        /// Gets or sets whether or not the buffered states manager and its submanagers are updating.
        ///</summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (!enabled && value)
                {
                    ReadBuffers.Enabled = true;
                    InterpolatedStates.Enabled = true;
                }
                else if (enabled && !value)
                {
                    InterpolatedStates.Enabled = false;
                    ReadBuffers.Enabled = false;
                }
                enabled = value;
            }
        }

        ///<summary>
        /// Constructs a new manager.
        ///</summary>
        public BufferedStatesManager()
        {
            InterpolatedStates = new InterpolatedStatesManager(this);
            ReadBuffers = new StateReadBuffers(this);

        }

        ///<summary>
        /// Constructs a new manager.
        ///</summary>
        ///<param name="threadManager">Thread manager to be used by the manager.</param>
        public BufferedStatesManager(IThreadManager threadManager)
        {
            InterpolatedStates = new InterpolatedStatesManager(this, threadManager);
            ReadBuffers = new StateReadBuffers(this, threadManager);
        }


        ///<summary>
        /// Adds an entity to the manager.
        ///</summary>
        ///<param name="e">Entity to add.</param>
        ///<exception cref="InvalidOperationException">Thrown if the entity already belongs to a states manager.</exception>
        public void Add(Entity e)
        {
            lock (InterpolatedStates.FlipLocker)
            {
                lock (ReadBuffers.FlipLocker)
                {
                    if (e.BufferedStates.BufferedStatesManager == null)
                    {
                        e.BufferedStates.BufferedStatesManager = this;
                        e.BufferedStates.motionStateIndex = entities.Count;
                        entities.Add(e);
                        if (ReadBuffers.Enabled)
                            ReadBuffers.Add(e);
                        if (InterpolatedStates.Enabled)
                            InterpolatedStates.Add(e);
                    }
                    else
                        throw new InvalidOperationException("Entity already belongs to a BufferedStatesManager; cannot add.");

                }
            }
        }

        ///<summary>
        /// Removes an entity from the manager.
        ///</summary>
        ///<param name="e">Entity to remove.</param>
        ///<exception cref="InvalidOperationException">Thrown if the entity does not belong to this manager.</exception>
        public void Remove(Entity e)
        {
            lock (InterpolatedStates.FlipLocker)
            {
                lock (ReadBuffers.FlipLocker)
                {
                    if (e.BufferedStates.BufferedStatesManager == this)
                    {
                        int index = entities.IndexOf(e);

                        int endIndex = entities.Count - 1;
                        entities[index] = entities[endIndex];
                        entities.RemoveAt(endIndex);
                        if (index < entities.Count)
                            entities[index].BufferedStates.motionStateIndex = index;
                        if (ReadBuffers.Enabled)
                            ReadBuffers.Remove(index, endIndex);
                        if (InterpolatedStates.Enabled)
                            InterpolatedStates.Remove(index, endIndex);

                        e.BufferedStates.BufferedStatesManager = null;
                    }
                    else
                        throw new InvalidOperationException("Entity does not belong to this BufferedStatesManager; cannot remove.");

                }
            }
        }

    }
}
