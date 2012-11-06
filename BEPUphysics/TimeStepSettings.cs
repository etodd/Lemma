namespace BEPUphysics
{
    ///<summary>
    /// Contains settings for the instance's time step.
    ///</summary>
    public class TimeStepSettings
    {
        /// <summary>
        /// Maximum number of timesteps to perform during a given frame when Space.Update(float) is used.  The unsimulated time will be accumulated for subsequent calls to Space.Update(float).
        /// Defaults to 3.
        /// </summary>
        public int MaximumTimeStepsPerFrame = 3;

        /// <summary>
        /// Length of each integration step.  Calling a Space's Update() method moves time forward this much.
        /// The other method, Space.Update(float), will try to move time forward by the amount specified in the parameter by taking steps of TimeStepDuration size.
        /// Defaults to 1/60.
        /// </summary>
        public float TimeStepDuration = 1f / 60;

        /// <summary>
        /// Amount of time accumulated by previous calls to Space.Update(float) that has not yet been simulated.
        /// </summary>
        public float AccumulatedTime;
    }
}
