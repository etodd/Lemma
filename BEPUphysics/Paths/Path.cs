namespace BEPUphysics.Paths
{
    /// <summary>
    /// Superclass of a variety of classes that can be evaluated at a time to retrieve a value associated with that time.
    /// </summary>
    /// <typeparam name="TValue">Type of the value of the path.</typeparam>
    public abstract class Path<TValue>
    {
        /// <summary>
        /// Computes the value of the path at a given time.
        /// </summary>
        /// <param name="time">Time at which to evaluate the path.</param>
        /// <param name="value">Path value at the given time.</param>
        public abstract void Evaluate(double time, out TValue value);

        /// <summary>
        /// Gets the starting and ending times of the path.
        /// </summary>
        /// <param name="startingTime">Beginning time of the path.</param>
        /// <param name="endingTime">Ending time of the path.</param>
        public abstract void GetPathBoundsInformation(out double startingTime, out double endingTime);

        /// <summary>
        /// Computes the value of the path at a given time.
        /// </summary>
        /// <param name="time">Time at which to evaluate the path.</param>
        /// <returns>Path value at the given time.</returns>
        public TValue Evaluate(double time)
        {
            TValue toReturn;
            Evaluate(time, out toReturn);
            return toReturn;
        }
    }
}