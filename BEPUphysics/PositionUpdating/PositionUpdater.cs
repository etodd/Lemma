using System;
using BEPUphysics.Threading;

namespace BEPUphysics.PositionUpdating
{
    ///<summary>
    /// Superclass of updaters which manage the position of objects.
    ///</summary>
    public abstract class PositionUpdater : MultithreadedProcessingStage
    {
        protected TimeStepSettings timeStepSettings;
        ///<summary>
        /// Gets or sets the time step settings used by the updater.
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

        protected PositionUpdater(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            :this(timeStepSettings)
        {
            ThreadManager = threadManager;
            AllowMultithreading = true;
        }

        protected PositionUpdater(TimeStepSettings timeStepSettings)
        {
            this.timeStepSettings = timeStepSettings;
            Enabled = true;
        }
        ///<summary>
        /// Adds an object to the position updater.
        ///</summary>
        ///<param name="updateable">Updateable to add.</param>
        ///<exception cref="Exception">Thrown if the updateable already belongs to a position updater.</exception>
        public abstract void Add(IPositionUpdateable updateable);
        ///<summary>
        /// Removes an updateable from the updater.
        ///</summary>
        ///<param name="updateable">Item to remove.</param>
        ///<exception cref="Exception">Thrown if the updater does not own the updateable.</exception>
        public abstract void Remove(IPositionUpdateable updateable);
    }
}
