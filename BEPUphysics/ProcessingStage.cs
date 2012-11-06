using System;

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

        ///<summary>
        /// Updates the stage.
        ///</summary>
        public void Update()
        {
            if (!Enabled)
                return;
            if (Starting != null)
                Starting();
            UpdateStage();
            if (Finishing != null)
                Finishing();
        }
        protected abstract void UpdateStage();
    }
}
