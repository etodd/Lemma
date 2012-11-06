using System;
using System.Linq;

namespace BEPUphysics.Paths
{
    /// <summary>
    /// Point defining the shape of a 3D curve.
    /// </summary>
    /// <typeparam name="TValue">Type of values in the curve.</typeparam>
    public class CurveControlPoint<TValue> : IComparable<CurveControlPoint<TValue>>
    {
        private float time;

        private TValue value;

        /// <summary>
        /// Constructs a new 3D curve control point.
        /// </summary>
        /// <param name="time">Time at which the point is positioned.</param>
        /// <param name="value">Value of the control point.</param>
        /// <param name="curve">Curve associated with the control point.</param>
        public CurveControlPoint(float time, TValue value, Curve<TValue> curve)
        {
            Curve = curve;
            Time = time;
            Value = value;
        }

        /// <summary>
        /// Gets the curve associated with this control point.
        /// </summary>
        public Curve<TValue> Curve { get; private set; }

        /// <summary>
        /// Gets or sets the time at which this control point is positioned.
        /// </summary>
        public float Time
        {
            get { return time; }
            set
            {
                time = value;
                if (Curve.ControlPoints.Contains(this))
                {
                    Curve.InternalControlPointTimeChanged(this);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of this control point.
        /// </summary>
        public TValue Value
        {
            get { return value; }
            set
            {
                this.value = value;
                if (Curve.ControlPoints.Contains(this))
                {
                    Curve.ControlPointValueChanged(this);
                }
            }
        }

        #region IComparable<CurveControlPoint<TValue>> Members

        /// <summary>
        /// Compares the two control points based on their time.
        /// </summary>
        /// <param name="other">Control point to compare.</param>
        /// <returns>-1 if the current instance has a smaller time, 0 if equal, and 1 if the current instance has a larger time.</returns>
        public int CompareTo(CurveControlPoint<TValue> other)
        {
            if (other.Time < Time)
                return 1;
            if (other.Time > Time)
                return -1;
            return 0;
        }

        #endregion
    }
}