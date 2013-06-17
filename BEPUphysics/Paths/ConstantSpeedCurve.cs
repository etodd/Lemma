namespace BEPUphysics.Paths
{
    /// <summary>
    /// Superclass of speed-controlled curves that have a constant speed.
    /// </summary>
    /// <typeparam name="TValue">Type of the values in the curve.</typeparam>
    public abstract class ConstantSpeedCurve<TValue> : SpeedControlledCurve<TValue>
    {
        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speed">Speed to maintain while traveling around a curve.</param>
        /// <param name="curve">Curve to wrap.</param>
        protected ConstantSpeedCurve(float speed, Curve<TValue> curve)
            : base(curve)
        {
            Speed = speed;
            ResampleCurve();
        }

        /// <summary>
        /// Constructs a new constant speed curve.
        /// </summary>
        /// <param name="speed">Speed to maintain while traveling around a curve.</param>
        /// <param name="curve">Curve to wrap.</param>
        /// <param name="sampleCount">Number of samples to use when constructing the wrapper curve.
        /// More samples increases the accuracy of the speed requirement at the cost of performance.</param>
        protected ConstantSpeedCurve(float speed, Curve<TValue> curve, int sampleCount)
            : base(curve, sampleCount)
        {
            Speed = speed;
            ResampleCurve();
        }

        /// <summary>
        /// Gets or sets the speed of the curve.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Gets the desired speed at a given time.
        /// </summary>
        /// <param name="time">Time to check for speed.</param>
        /// <returns>Speed at the given time.</returns>
        public override float GetSpeedAtCurveTime(double time)
        {
            return Speed;
        }
    }
}