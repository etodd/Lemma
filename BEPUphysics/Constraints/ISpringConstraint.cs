namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Implemented by constraints that support springlike behavior.
    /// </summary>
    public interface ISpringSettings
    {
        /// <summary>
        /// Gets the spring settings used by the constraint.
        /// </summary>
        SpringSettings SpringSettings { get; }
    }
}