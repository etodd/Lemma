using System;
using BEPUphysics.Threading;

namespace BEPUphysics
{
    ///<summary>
    /// Superclass of processing systems which can use multiple threads.
    ///</summary>
    public abstract class MultithreadedProcessingStage
    {
        ///<summary>
        /// Gets or sets whether or not the processing stage is active.
        ///</summary>
        public virtual bool Enabled { get; set; }

        ///<summary>
        /// Gets or sets whether or not the processing stage should allow multithreading.
        ///</summary>
        public bool AllowMultithreading { get; set; }

        ///<summary>
        /// Gets or sets the thread manager used by the stage.
        ///</summary>
        public IThreadManager ThreadManager { get; set; }

        ///<summary>
        /// Fires when the processing stage begins working.
        ///</summary>
        public event Action Starting;

        /// <summary>
        /// Fires when the processing stage finishes working.
        /// </summary>
        public event Action Finishing;

        protected bool ShouldUseMultithreading
        {
            get
            {
                return AllowMultithreading && ThreadManager != null && ThreadManager.ThreadCount > 1;
            }
        }

        ///<summary>
        /// Runs the processing stage.
        ///</summary>
        public void Update()
        {
            if (!Enabled)
                return;
            if (Starting != null)
                Starting();
            if (ShouldUseMultithreading)
            {
                UpdateMultithreaded();
            }
            else
            {
                UpdateSingleThreaded();
            }
            if (Finishing != null)
                Finishing();
        }
        protected abstract void UpdateMultithreaded();
        protected abstract void UpdateSingleThreaded();
    }
}
