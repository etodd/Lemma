using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Paths
{
    /// <summary>
    /// Wrapper that controls the speed at which a curve is traversed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Even if a curve is evaluated at linearly increasing positions,
    /// the distance between consecutive values can be different.  This
    /// has the effect of a curve-following object having variable velocity.
    /// </para>
    /// <para>
    /// To counteract the variable velocity, this wrapper samples the curve
    /// and produces a reparameterized, distance-based curve.  Changing the
    /// evaluated curve position will linearly change the value.
    /// </para>
    /// </remarks>
    public abstract class SpeedControlledCurve<TValue> : Path<TValue>
    {
        private readonly List<Vector2> samples = new List<Vector2>(); //  X is wrapped view, Y is associated curve view

        private Curve<TValue> curve;
        private int samplesPerInterval;


        /// <summary>
        /// Constructs a new speed controlled curve.
        /// </summary>
        protected SpeedControlledCurve()
        {
        }

        /// <summary>
        /// Constructs a new speed-controlled curve.
        /// </summary>
        /// <param name="curve">Curve to wrap.</param>
        protected SpeedControlledCurve(Curve<TValue> curve)
        {
            samplesPerInterval = 10;
            this.curve = curve;
        }

        /// <summary>
        /// Constructs a new speed-controlled curve.
        /// </summary>
        /// <param name="curve">Curve to wrap.</param>
        /// <param name="samplesPerInterval">Number of samples to use when constructing the wrapper curve.
        /// More samples increases the accuracy of the speed requirement at the cost of performance.</param>
        protected SpeedControlledCurve(Curve<TValue> curve, int samplesPerInterval)
        {
            this.curve = curve;
            this.samplesPerInterval = samplesPerInterval;
        }

        /// <summary>
        /// Gets or sets the curve wrapped by this instance.
        /// </summary>
        public Curve<TValue> Curve
        {
            get { return curve; }
            set
            {
                curve = value;
                if (Curve != null)
                    ResampleCurve();
            }
        }

        /// <summary>
        /// Defines how the curve is sampled when the evaluation time exceeds the final control point.
        /// </summary>
        public CurveEndpointBehavior PostLoop { get; set; }

        /// <summary>
        /// Defines how the curve is sampled when the evaluation time exceeds the beginning control point.
        /// </summary>
        public CurveEndpointBehavior PreLoop { get; set; }

        /// <summary>
        /// Gets or sets the number of samples to use per interval in the curve.
        /// </summary>
        public int SamplesPerInterval
        {
            get { return samplesPerInterval; }
            set
            {
                samplesPerInterval = value;
                if (Curve != null)
                    ResampleCurve();
            }
        }

        /// <summary>
        /// Gets the desired speed at a given time.
        /// </summary>
        /// <param name="time">Time to check for speed.</param>
        /// <returns>Speed at the given time.</returns>
        public abstract float GetSpeedAtCurveTime(float time);

        /// <summary>
        /// Gets the time at which the internal curve would be evaluated at the given time.
        /// </summary>
        /// <param name="time">Time to evaluate the speed-controlled curve.</param>
        /// <returns>Time at which the internal curve would be evaluated.</returns>
        public double GetInnerTime(double time)
        {
            if (Curve == null)
                throw new InvalidOperationException("SpeedControlledCurve's internal curve is null; ensure that its curve property is set prior to evaluation.");
            float firstTime, lastTime;
            GetPathBoundsInformation(out firstTime, out lastTime);
            time = Curve<TValue>.ModifyTime(time, firstTime, lastTime, Curve.PreLoop, Curve.PostLoop);

            int indexMin = 0;
            int indexMax = samples.Count;
            if (indexMax == 0)
            {
                return 0;
            }
            //If time < controlpoints.mintime, should be... 0 or -1?
            while (indexMax - indexMin > 1) //if time belongs to min
            {
                int midIndex = (indexMin + indexMax) / 2;
                if (time > samples[midIndex].X)
                {
                    //Use midIndex as the minimum.
                    indexMin = midIndex;
                }
                else if (time < samples[midIndex].X)
                {
                    //Use midindex as the max.
                    indexMax = midIndex;
                }
                else
                {
                    //Equal; use it.
                    indexMin = midIndex;
                    break;
                }
            }
            if (samples[indexMin].X > time)
                indexMin -= 1;

            double curveTime = (time - samples[indexMin].X) / (samples[indexMin + 1].X - samples[indexMin].X);
            return (1 - curveTime) * samples[indexMin].Y + (curveTime) * samples[indexMin + 1].Y;
        }

        /// <summary>
        /// Computes the value of the curve at a given time.
        /// </summary>
        /// <param name="time">Time to evaluate the curve at.</param>
        /// <param name="value">Value of the curve at the given time.</param>
        /// <param name="innerTime">Time at which the internal curve was evaluated to get the value.</param>
        public void Evaluate(double time, out TValue value, out double innerTime)
        {
            Curve.Evaluate(innerTime = GetInnerTime(time), out value);
        }

        /// <summary>
        /// Computes the value of the curve at a given time.
        /// </summary>
        /// <param name="time">Time to evaluate the curve at.</param>
        /// <param name="value">Value of the curve at the given time.</param>
        public override void Evaluate(double time, out TValue value)
        {
            Curve.Evaluate(GetInnerTime(time), out value);
        }

        /// <summary>
        /// Gets the starting and ending times of the path.
        /// </summary>
        /// <param name="startingTime">Beginning time of the path.</param>
        /// <param name="endingTime">Ending time of the path.</param>
        public override void GetPathBoundsInformation(out float startingTime, out float endingTime)
        {
            if (samples.Count > 0)
            {
                startingTime = 0;
                endingTime = samples[samples.Count - 1].X;
            }
            else
            {
                startingTime = 0;
                endingTime = 0;
            }
        }

        /// <summary>
        /// Forces a recalculation of curve samples.
        /// This needs to be called if the wrapped curve
        /// is changed.
        /// </summary>
        public void ResampleCurve()
        {
            //TODO: Call this from curve if add/remove/timechange/valuechange happens
            //Could hide it then.
            samples.Clear();
            float firstTime, lastTime;
            int minIndex, maxIndex;
            curve.GetCurveBoundsInformation(out firstTime, out lastTime, out minIndex, out maxIndex);

            //Curve isn't valid.
            if (minIndex < 0 || maxIndex < 0)
                return;

            float timeElapsed = 0;
            //TODO: useless calculation due to this
            TValue currentValue = Curve.ControlPoints[minIndex].Value;
            TValue previousValue = currentValue;

            float inverseSampleCount = 1f / (SamplesPerInterval + 1);

            float speed = GetSpeedAtCurveTime(Curve.ControlPoints[minIndex].Time);
            float previousSpeed = speed;
            for (int i = minIndex; i < maxIndex; i++)
            {
                previousValue = currentValue;
                currentValue = Curve.ControlPoints[i].Value;

                if (speed != 0)
                    timeElapsed += GetDistance(previousValue, currentValue) / speed;
                previousSpeed = speed;
                speed = GetSpeedAtCurveTime(Curve.ControlPoints[i].Time);

                samples.Add(new Vector2(timeElapsed, Curve.ControlPoints[i].Time));

                float curveTime = Curve.ControlPoints[i].Time;
                float intervalLength = Curve.ControlPoints[i + 1].Time - curveTime;
                float curveTimePerSample = intervalLength / (SamplesPerInterval + 1);
                for (int j = 1; j <= SamplesPerInterval; j++)
                {
                    previousValue = currentValue;
                    Curve.Evaluate(i, j * inverseSampleCount, out currentValue);

                    curveTime += curveTimePerSample;
                    if (speed != 0)
                        timeElapsed += GetDistance(previousValue, currentValue) / speed;

                    previousSpeed = speed;
                    speed = GetSpeedAtCurveTime(curveTime);

                    samples.Add(new Vector2(timeElapsed, curveTime));
                }
            }
            timeElapsed += GetDistance(previousValue, currentValue) / previousSpeed;
            samples.Add(new Vector2(timeElapsed, Curve.ControlPoints[maxIndex].Time));
        }

        /// <summary>
        /// Computes the distance between the two values.
        /// </summary>
        /// <param name="start">Starting value.</param>
        /// <param name="end">Ending value.</param>
        /// <returns>Distance between the values.</returns>
        protected abstract float GetDistance(TValue start, TValue end);
    }
}