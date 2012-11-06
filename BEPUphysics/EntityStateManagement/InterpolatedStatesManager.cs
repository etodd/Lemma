using System;
using BEPUphysics.Entities;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.Threading;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// Manages the interpolated states of entities.  Interpolated states are those
    /// based on the previous entity states and the current entity states, blended together
    /// using the time remainder from internal time stepping.
    ///</summary>
    public class InterpolatedStatesManager : MultithreadedProcessingStage
    {
        ///<summary>
        /// Gets or sets whether or not the manager is updating.
        ///</summary>
        ///<exception cref="InvalidOperationException">Thrown when enabling the interpolated manager without having the read buffers active.</exception>
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                if (base.Enabled && !value)
                {
                    Disable();
                    base.Enabled = false;
                }
                else if (!base.Enabled && value)
                {
                    if (!manager.ReadBuffers.Enabled)
                        throw new InvalidOperationException("Cannot enable interpolated states unless the read buffers are enabled.");
                    Enable();
                    base.Enabled = true;
                }
            }
        }

        internal void Enable()
        {
            //Turn everything on.
            lock (FlipLocker)
            {
                int initialCount = Math.Max(manager.entities.Count, 64);
                backBuffer = new RigidTransform[initialCount];
                states = new RigidTransform[initialCount];
                for (int i = 0; i < manager.entities.Count; i++)
                {
                    Entity entity = manager.entities[i];
                    backBuffer[i].Position = entity.position;
                    backBuffer[i].Orientation = entity.orientation;
                }
                Array.Copy(backBuffer, states, backBuffer.Length);
            }

        }

        internal void Disable()
        {
            //Turn everything off.
            lock (FlipLocker)
            {
                backBuffer = null;
                states = null;
            }
        }
        private BufferedStatesManager manager;
        ///<summary>
        /// Gets the synchronization object locked prior to flipping the internal buffers.
        /// Acquiring a lock on this object will prevent the internal buffers from flipping for the duration
        /// of the lock.
        ///</summary>
        public object FlipLocker { get; private set; }

        RigidTransform[] backBuffer;
        RigidTransform[] states = new RigidTransform[64];

        ///<summary>
        /// Constructs a new interpolated states manager.
        ///</summary>
        ///<param name="manager">Owning buffered states manager.</param>
        public InterpolatedStatesManager(BufferedStatesManager manager)
        {
            this.manager = manager;
            multithreadedWithReadBuffersDelegate = UpdateIndex;
            FlipLocker = new object();
        }

        ///<summary>
        /// Constructs a new interpolated states manager.
        ///</summary>
        ///<param name="manager">Owning buffered states manager.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public InterpolatedStatesManager(BufferedStatesManager manager, IThreadManager threadManager)
        {
            this.manager = manager;
            multithreadedWithReadBuffersDelegate = UpdateIndex;
            FlipLocker = new object();
            ThreadManager = threadManager;
            AllowMultithreading = true;
        }


        float blendAmount;
        ///<summary>
        /// Gets or sets the blending amount to use.
        /// This is set automatically when the space is using internal timestepping
        /// (I.E. when Space.Update(dt) is called).  It is a value from 0 to 1
        /// that defines the amount of the previous and current frames to include
        /// in the blended state.  A value of 1 means use only the current frame;
        /// a value of 0 means use only the previous frame.
        ///</summary>
        public float BlendAmount
        {
            get
            {
                return blendAmount;
            }
            set
            {
                blendAmount = MathHelper.Clamp(value, 0, 1);
            }
        }

        Action<int> multithreadedWithReadBuffersDelegate;
        void UpdateIndex(int i)
        {
            Entity entity = manager.entities[i];
            //Blend between previous and current states.
            //Interpolated updates occur after proper updates complete.
            //That means that the internal positions and the front buffer positions are equivalent.
            //However, the backbuffer is uncontested and contains the previous frame's data.
            Vector3.Lerp(ref manager.ReadBuffers.backBuffer[i].Position, ref entity.position, blendAmount, out backBuffer[i].Position);
            Quaternion.Slerp(ref manager.ReadBuffers.backBuffer[i].Orientation, ref entity.orientation, blendAmount, out backBuffer[i].Orientation);
        }



        protected override void UpdateMultithreaded()
        {
            ThreadManager.ForLoop(0, manager.entities.Count, multithreadedWithReadBuffersDelegate);
            FlipBuffers();
        }

        protected override void UpdateSingleThreaded()
        {
            for (int i = 0; i < manager.entities.Count; i++)
            {
                UpdateIndex(i);
            }
            FlipBuffers();
        }

        ///<summary>
        /// Acquires a lock on the FlipLocker and flips the internal buffers.
        ///</summary>
        public void FlipBuffers()
        {
            lock (FlipLocker)
            {
                RigidTransform[] formerFrontBuffer = states;
                states = backBuffer;
                backBuffer = formerFrontBuffer;
            }
        }

        ///<summary>
        /// Returns an interpolated state associated with an entity with the given index.
        /// Does not lock the FlipLocker.
        ///</summary>
        ///<param name="motionStateIndex">Motion state of the entity.</param>
        ///<returns>Interpolated state associated with the entity at the given index.</returns>
        public RigidTransform GetState(int motionStateIndex)
        {
            return states[motionStateIndex];
        }

        ///<summary>
        /// Gets the interpolated states of all entities.
        ///</summary>
        ///<param name="states">Interpolated states of all entities.</param>
        ///<exception cref="InvalidOperationException">Thrown when the array is too small to hold the states.</exception>
        public void GetStates(RigidTransform[] states)
        {
            lock (FlipLocker)
            {
                if (states.Length < manager.entities.Count)
                {
                    throw new ArgumentException("Array is not large enough to hold the buffer.", "states");
                }
                Array.Copy(this.states, states, manager.entities.Count);
            }
        }

        internal void Add(Entity e)
        {
            //Don't need to lock since the parent manager handles it.
            if (states.Length <= e.BufferedStates.motionStateIndex)
            {
                var newStates = new RigidTransform[states.Length * 2];
                states.CopyTo(newStates, 0);
                states = newStates;
            }
            states[e.BufferedStates.motionStateIndex].Position = e.position;
            states[e.BufferedStates.motionStateIndex].Orientation = e.orientation;

            if (backBuffer.Length <= e.BufferedStates.motionStateIndex)
            {
                var newStates = new RigidTransform[backBuffer.Length * 2];
                backBuffer.CopyTo(newStates, 0);
                backBuffer = newStates;
            }
            backBuffer[e.BufferedStates.motionStateIndex].Position = e.position;
            backBuffer[e.BufferedStates.motionStateIndex].Orientation = e.orientation;
        }

        internal void Remove(int index, int endIndex)
        {
            //Don't need to lock since the parent manager handles it.
            states[index] = states[endIndex];
            backBuffer[index] = backBuffer[endIndex];
        }
    }
}
