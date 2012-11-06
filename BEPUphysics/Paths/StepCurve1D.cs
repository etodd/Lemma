namespace BEPUphysics.Paths
{
    /// <summary>
    /// One dimensional-valued curve that does not interpolate values.
    /// Instead, it just picks the value from the previous control point.
    /// </summary>
    public class StepCurve1D : Curve<float>
    {
        /// <summary>
        /// Evaluates the curve at a given time using linear interpolation.
        /// </summary>
        /// <param name="controlPointIndex">Index of the control point at the beginning of the evaluation interval.</param>
        /// <param name="weight">Value of 0 to 1 representing how far along the interval to sample.</param>
        /// <param name="value">Value of the curve at the given location.</param>
        public override void Evaluate(int controlPointIndex, float weight, out float value)
        {
            value = ControlPoints[controlPointIndex].Value;
        }

        /// <summary>
        /// Computes the bounds of the curve.
        /// </summary>
        /// <param name="minIndex">Minimum index of the curve.</param>
        /// <param name="maxIndex">Maximum index of the curve.</param>
        public override void GetCurveIndexBoundsInformation(out int minIndex, out int maxIndex)
        {
            maxIndex = ControlPoints.Count - 1;
            if (maxIndex < 0)
                minIndex = -1;
            else
                minIndex = 0;
        }

        protected internal override void ControlPointAdded(CurveControlPoint<float> curveControlPoint, int index)
        {
        }

        protected internal override void ControlPointRemoved(CurveControlPoint<float> curveControlPoint, int oldIndex)
        {
        }

        protected internal override void ControlPointTimeChanged(CurveControlPoint<float> curveControlPoint, int oldIndex, int newIndex)
        {
        }

        protected internal override void ControlPointValueChanged(CurveControlPoint<float> curveControlPoint)
        {
        }
    }
}