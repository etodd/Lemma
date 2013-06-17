using System;
using System.Diagnostics;

namespace BEPUphysics
{
    ///<summary>
    /// Superclass of singlethreaded update systems.
    ///</summary>
    public abstract class ProcessingStage
    {
        ///<summary>
        /// Gets or sets whether or not the stage should update.
        ///</summary>
        public virtual bool Enabled { get; set; }

        ///<summary>
        /// Fires when the stage starts working.
        ///</summary>
        public event Action Starting;

        ///<summary>
        /// Fires when the stage finishes working.
        ///</summary>
        public event Action Finishing;

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
      
          
#endif
        ///<summary>
        /// Updates the stage.
        ///</summary>
        public void Update()
        {
            if (!Enabled)
                return;
            if (Starting != null)
                Starting();
#if PROFILE
            start = Stopwatch.GetTimestamp();
#endif

            UpdateStage();

#if PROFILE
            end = Stopwatch.GetTimestamp();
#endif
            if (Finishing != null)
                Finishing();
        }
        protected abstract void UpdateStage();
    }
}
