using System;

namespace BEPUphysics.Paths
{
    /// <summary>
    /// Manages a curve in 3D space that supports interpolation.
    /// </summary>
    /// <typeparam name="TValue">Type of values in the curve.</typeparam>
    public abstract class Curve<TValue> : Path<TValue>
    {
        /// <summary>
        /// Constructs a new 3D curve.
        /// </summary>
        protected Curve()
        {
            ControlPoints = new CurveControlPointList<TValue>(this);
        }

        /// <summary>
        /// Gets the list of control points in the curve.
        /// </summary>
        public CurveControlPointList<TValue> ControlPoints { get; private set; }

        /// <summary>
        /// Defines how the curve is sampled when the evaluation time exceeds the final control point.
        /// </summary>
        public CurveEndpointBehavior PostLoop { get; set; }

        /// <summary>
        /// Defines how the curve is sampled when the evaluation time exceeds the beginning control point.
        /// </summary>
        public CurveEndpointBehavior PreLoop { get; set; }

        /// <summary>
        /// Converts an unbounded time to a time within the curve's interval using the 
        /// endpoint behavior.
        /// </summary>
        /// <param name="time">Time to convert.</param>
        /// <param name="intervalBegin">Beginning of the curve's interval.</param>
        /// <param name="intervalEnd">End of the curve's interval.</param>
        /// <param name="preLoop">Looping behavior of the curve before the first endpoint's time.</param>
        /// <param name="postLoop">Looping behavior of the curve after the last endpoint's time.</param>
        /// <returns>Time within the curve's interval.</returns>
        public static double ModifyTime(double time, float intervalBegin, float intervalEnd, CurveEndpointBehavior preLoop, CurveEndpointBehavior postLoop)
        {
            if (time < intervalBegin)
            {
                switch (preLoop)
                {
                    case CurveEndpointBehavior.Wrap:
                        double modifiedTime = time - intervalBegin;
                        double intervalLength = intervalEnd - intervalBegin;
                        modifiedTime %= intervalLength;
                        return intervalEnd + modifiedTime;
                    case CurveEndpointBehavior.Clamp:
                        return Math.Max(intervalBegin, time);
                    case CurveEndpointBehavior.Mirror:
                        modifiedTime = time - intervalBegin;
                        intervalLength = intervalEnd - intervalBegin;
                        var numFlips = (int) (modifiedTime / intervalLength);
                        if (numFlips % 2 == 0)
                            return intervalBegin - modifiedTime % intervalLength;
                        return intervalEnd + modifiedTime % intervalLength;
                }
            }
            else if (time >= intervalEnd)
            {
                switch (postLoop)
                {
                    case CurveEndpointBehavior.Wrap:
                        double modifiedTime = time - intervalEnd;
                        double intervalLength = intervalEnd - intervalBegin;
                        modifiedTime %= intervalLength;
                        return intervalBegin + modifiedTime;
                    case CurveEndpointBehavior.Clamp:
                        return Math.Min(intervalEnd, time);
                    case CurveEndpointBehavior.Mirror:
                        modifiedTime = time - intervalEnd;
                        intervalLength = intervalEnd - intervalBegin;
                        var numFlips = (int) (modifiedTime / intervalLength);
                        if (numFlips % 2 == 0)
                            return intervalEnd - modifiedTime % intervalLength;
                        return intervalBegin + modifiedTime % intervalLength;
                }
            }
            return time;
        }

        /// <summary>
        /// Evaluates the curve section starting at the control point index using
        /// the weight value.
        /// </summary>
        /// <param name="controlPointIndex">Index of the starting control point of the subinterval.</param>
        /// <param name="weight">Location to evaluate on the subinterval from 0 to 1.</param>
        /// <param name="value">Value at the given location.</param>
        public abstract void Evaluate(int controlPointIndex, float weight, out TValue value);

        /// <summary>
        /// Gets the curve's bounding index information.
        /// </summary>
        /// <param name="minIndex">Index of the minimum control point in the active curve segment.</param>
        /// <param name="maxIndex">Index of the maximum control point in the active curve segment.</param>
        public abstract void GetCurveIndexBoundsInformation(out int minIndex, out int maxIndex);

        /// <summary>
        /// Computes the value of the curve at a given time.
        /// </summary>
        /// <param name="time">Time at which to evaluate the curve.</param>
        /// <param name="value">Curve value at the given time.</param>
        public override void Evaluate(double time, out TValue value)
        {
            float firstTime, lastTime;
            int minIndex, maxIndex;
            GetCurveBoundsInformation(out firstTime, out lastTime, out minIndex, out maxIndex);
            if (minIndex < 0 || maxIndex < 0)
            {
                //Invalid bounds, quit with default
                value = default(TValue);
                return;
            }
            if (minIndex == maxIndex)
            {
                //1-length curve; asking the system to evaluate
                //this will be a waste of time AND
                //crash since +1 will be outside scope
                value = ControlPoints[minIndex].Value;
                return;
            }
            time = ModifyTime(time, firstTime, lastTime, PreLoop, PostLoop);

            int index = GetPreviousIndex(time);
            if (index == maxIndex)
            {
                //Somehow the index is the very last index, so next index would be invalid.
                //Just 'clamp' it.
                //This generally implies a bug, but it might also just be some very close floating point issue.
                value = ControlPoints[maxIndex].Value;
            }
            else
            {
                float denominator = ControlPoints[index + 1].Time - ControlPoints[index].Time;

                float intervalTime;
                if (denominator < Toolbox.Epsilon)
                    intervalTime = 0;
                else
                    intervalTime = (float) (time - ControlPoints[index].Time) / denominator;


                Evaluate(index, intervalTime, out value);
            }
        }

        /// <summary>
        /// Gets the starting and ending times of the path.
        /// </summary>
        /// <param name="startingTime">Beginning time of the path.</param>
        /// <param name="endingTime">Ending time of the path.</param>
        public override void GetPathBoundsInformation(out float startingTime, out float endingTime)
        {
            int index;
            GetCurveBoundsInformation(out startingTime, out endingTime, out index, out index);
        }

        /// <summary>
        /// Gets information about the curve's total active interval.
        /// These are not always the first and last endpoints in a curve.
        /// </summary>
        /// <param name="firstIndexTime">Time of the first index.</param>
        /// <param name="lastIndexTime">Time of the last index.</param>
        /// <param name="minIndex">First index in the reachable curve.</param>
        /// <param name="maxIndex">Last index in the reachable curve.</param>
        public void GetCurveBoundsInformation(out float firstIndexTime, out float lastIndexTime, out int minIndex, out int maxIndex)
        {
            GetCurveIndexBoundsInformation(out minIndex, out maxIndex);
            if (minIndex >= 0 && maxIndex < ControlPoints.Count && minIndex <= maxIndex)
            {
                firstIndexTime = ControlPoints[minIndex].Time;
                lastIndexTime = ControlPoints[maxIndex].Time;
            }
            else
            {
                firstIndexTime = 0;
                lastIndexTime = 0;
            }
        }

        /// <summary>
        /// Computes the indices of control points surrounding the time.
        /// If the time is equal to a control point's time, indexA will
        /// be that control point's index.
        /// </summary>
        /// <param name="time">Time to index.</param>
        /// <returns>Index prior to or equal to the given time.</returns>
        public int GetPreviousIndex(double time)
        {
            int indexMin = 0;
            int indexMax = ControlPoints.Count;
            if (indexMax == 0)
                return -1;
            //If time < controlpoints.mintime, should be... 0 or -1?
            while (indexMax - indexMin > 1) //if time belongs to min
            {
                int midIndex = (indexMin + indexMax) / 2;
                if (time > ControlPoints[midIndex].Time)
                {
                    //Use midIndex as the minimum.
                    indexMin = midIndex;
                }
                else if (time < ControlPoints[midIndex].Time)
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
            if (ControlPoints[indexMin].Time <= time)
                return indexMin;
            return indexMin - 1;
        }


        internal void InternalControlPointTimeChanged(CurveControlPoint<TValue> controlPoint)
        {
            int oldIndex = ControlPoints.list.IndexOf(controlPoint);
            ControlPoints.list.RemoveAt(oldIndex);
            int index = GetPreviousIndex(controlPoint.Time) + 1;
            ControlPoints.list.Insert(index, controlPoint);
            ControlPointTimeChanged(controlPoint, oldIndex, index);
        }


        /// <summary>
        /// Called when a control point is added.
        /// </summary>
        /// <param name="curveControlPoint">New control point.</param>
        /// <param name="index">Index of the control point.</param>
        protected internal abstract void ControlPointAdded(CurveControlPoint<TValue> curveControlPoint, int index);

        /// <summary>
        /// Called when a control point is removed.
        /// </summary>
        /// <param name="curveControlPoint">Removed control point.</param>
        /// <param name="oldIndex">Index of the control point before it was removed.</param>
        protected internal abstract void ControlPointRemoved(CurveControlPoint<TValue> curveControlPoint, int oldIndex);

        /// <summary>
        /// Called when a control point belonging to the curve has its time changed.
        /// </summary>
        /// <param name="curveControlPoint">Changed control point.</param>
        /// <param name="oldIndex">Old index of the control point.</param>
        /// <param name="newIndex">New index of the control point.</param>
        protected internal abstract void ControlPointTimeChanged(CurveControlPoint<TValue> curveControlPoint, int oldIndex, int newIndex);

        /// <summary>
        /// Called when a control point belonging to the curve has its value changed.
        /// </summary>
        /// <param name="curveControlPoint">Changed control point.</param>
        protected internal abstract void ControlPointValueChanged(CurveControlPoint<TValue> curveControlPoint);
    }
}