using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints.SolverGroups
{
    /// <summary>
    /// Restricts one linear degree of freedom.
    /// </summary>
    public class PlaneSliderJoint : SolverGroup
    {
        /// <summary>
        /// Constructs a new constraint which restricts one linear degree of freedom between two entities.
        /// This constructs the internal constraints, but does not configure them.  Before using a constraint constructed in this manner,
        /// ensure that its active constituent constraints are properly configured.  The entire group as well as all internal constraints are initially inactive (IsActive = false).
        /// </summary>
        public PlaneSliderJoint()
        {
            IsActive = false;
            PointOnPlaneJoint = new PointOnPlaneJoint();
            LimitX = new LinearAxisLimit();
            MotorX = new LinearAxisMotor();
            LimitY = new LinearAxisLimit();
            MotorY = new LinearAxisMotor();

            Add(PointOnPlaneJoint);
            Add(LimitX);
            Add(MotorX);
            Add(LimitY);
            Add(MotorY);
        }

        /// <summary>
        /// Constructs a new constraint which restricts one linear degree of freedom between two entities.
        /// </summary>
        /// <param name="connectionA">First entity of the constraint pair.</param>
        /// <param name="connectionB">Second entity of the constraint pair.</param>
        /// <param name="planeAnchor">Location of the anchor for the plane to be attached to connectionA in world space.</param>
        /// <param name="planeNormal">Normal of the plane constraint in world space.</param>
        /// <param name="xAxis">Direction in world space along which the X axis LinearAxisLimit and LinearAxisMotor work.
        /// This is usually chosen to be perpendicular to the planeNormal and the yAxis.</param>
        /// <param name="yAxis">Direction in world space along which the Y axis LinearAxisLimit and LinearAxisMotor work.
        /// This is usually chosen to be perpendicular to the planeNormal and the xAxis.</param>
        /// <param name="pointAnchor">Location of the anchor for the point to be attached to connectionB in world space.</param>
        public PlaneSliderJoint(Entity connectionA, Entity connectionB, Vector3 planeAnchor, Vector3 planeNormal, Vector3 xAxis, Vector3 yAxis, Vector3 pointAnchor)
        {
            if (connectionA == null)
                connectionA = TwoEntityConstraint.WorldEntity;
            if (connectionB == null)
                connectionB = TwoEntityConstraint.WorldEntity;
            PointOnPlaneJoint = new PointOnPlaneJoint(connectionA, connectionB, planeAnchor, planeNormal, pointAnchor);
            LimitX = new LinearAxisLimit(connectionA, connectionB, planeAnchor, pointAnchor, xAxis, 0, 0);
            MotorX = new LinearAxisMotor(connectionA, connectionB, planeAnchor, pointAnchor, xAxis);
            LimitY = new LinearAxisLimit(connectionA, connectionB, planeAnchor, pointAnchor, yAxis, 0, 0);
            MotorY = new LinearAxisMotor(connectionA, connectionB, planeAnchor, pointAnchor, yAxis);
            LimitX.IsActive = false;
            MotorX.IsActive = false;
            LimitY.IsActive = false;
            MotorY.IsActive = false;
            Add(PointOnPlaneJoint);
            Add(LimitX);
            Add(MotorX);
            Add(LimitY);
            Add(MotorY);
        }

        /// <summary>
        /// Gets the distance limit for the slider along plane's X axis.
        /// </summary>
        public LinearAxisLimit LimitX { get; private set; }

        /// <summary>
        /// Gets the distance limit for the slider along plane's Y axis.
        /// </summary>
        public LinearAxisLimit LimitY { get; private set; }

        /// <summary>
        /// Gets the slider motor for the plane's X axis.
        /// </summary>
        public LinearAxisMotor MotorX { get; private set; }

        /// <summary>
        /// Gets the slider motor for the plane's Y axis.
        /// </summary>
        public LinearAxisMotor MotorY { get; private set; }

        /// <summary>
        /// Gets the plane joint that restricts one linear degree of freedom.
        /// </summary>
        public PointOnPlaneJoint PointOnPlaneJoint { get; private set; }
    }
}