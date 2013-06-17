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

#if PROFILE
        /// <summary>
        /// Gets the time elapsed in the previous execution of this stage, not including any hooked Starting or Finishing events.
        /// </summary>
        public double Time
        {
            get
            {
                return (end - start) / (double)Stopwatch.Frequency;
            }
        }

        long start, end;

        private void StartClock()
        {
            start = Stopwatch.GetTimestamp();
        }
        private void StopClock()
        {
            end = Stopwatch.GetTimestamp();
        }
#endif

        ///<summary>
        /// Runs the processing stage.
        ///</summary>
        public void Update()
        {
            if (!Enabled)
                return;
            if (Starting != null)
                Starting();
#if PROFILE
            StartClock();
#endif
            if (ShouldUseMultithreading)
            {
                UpdateMultithreaded();
            }
            else
            {
                UpdateSingleThreaded();
            }
#if PROFILE
            StopClock();
#endif
            if (Finishing != null)
                Finishing();
        }
        protected abstract void UpdateMultithreaded();
        protected abstract void UpdateSingleThreaded();
    }
}
