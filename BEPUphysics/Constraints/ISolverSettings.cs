namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Implemented by classes which have solver settings.
    /// </summary>
    public interface ISolverSettings
    {
        /// <summary>
        /// Gets the solver settings for this constraint.
        /// </summary>
        SolverSettings SolverSettings { get; }
    }
}