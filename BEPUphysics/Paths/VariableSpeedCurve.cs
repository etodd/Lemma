namespace BEPUphysics.Paths
{
    /// <summary>
    /// Curve that wraps another curve and travels along it with specified speeds.
    /// </summary>
    /// <typeparam name="TValue">Type of the value of the wrapped curve.</typeparam>
    public abstract class VariableSpeedCurve<TValue> : SpeedControlledCurve<TValue>
    {
        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speedCurve">Curve defining speeds to use.</param>
        /// <param name="curve">Curve to wrap.</param>
        protected VariableSpeedCurve(Path<float> speedCurve, Curve<TValue> curve)
            : base(curve)
        {
            SpeedCurve = speedCurve;
            ResampleCurve();
        }

        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speedCurve">Curve defining speeds to use.</param>
        /// <param name="curve">Curve to wrap.</param>
        /// <param name="sampleCount">Number of samples to use when constructing the wrapper curve.
        /// More samples increases the accuracy of the speed requirement at the cost of performance.</param>
        protected VariableSpeedCurve(Path<float> speedCurve, Curve<TValue> curve, int sampleCount)
            : base(curve, sampleCount)
        {
            SpeedCurve = speedCurve;
            ResampleCurve();
        }

        /// <summary>
        /// Gets or sets the path that defines the speeds at given locations.
        /// The speed curve will be sampled at times associated with the wrapped curve.
        /// </summary>
        public Path<float> SpeedCurve { get; set; }

        /// <summary>
        /// Gets the speed at a given time on the wrapped curve.
        /// </summary>
        /// <param name="time">Time to evaluate.</param>
        /// <returns>Speed at the given time.</returns>
        public override float GetSpeedAtCurveTime(float time)
        {
            return SpeedCurve.Evaluate(time);
        }
    }
}