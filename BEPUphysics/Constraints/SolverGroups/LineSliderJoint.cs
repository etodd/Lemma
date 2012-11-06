using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints.SolverGroups
{
    /// <summary>
    /// Restricts two degrees of linear motion while allowing one degree of angular freedom.
    /// </summary>
    public class LineSliderJoint : SolverGroup
    {
        /// <summary>
        /// Constructs a new constraint which restricts two degrees of linear freedom and two degrees of angular freedom between two entities.
        /// This constructs the internal constraints, but does not configure them.  Before using a constraint constructed in this manner,
        /// ensure that its active constituent constraints are properly configured.  The entire group as well as all internal constraints are initially inactive (IsActive = false).
        /// </summary>
        public LineSliderJoint()
        {
            IsActive = false;
            PointOnLineJoint = new PointOnLineJoint();
            AngularJoint = new RevoluteAngularJoint();
            Limit = new LinearAxisLimit();
            Motor = new LinearAxisMotor();
            Add(PointOnLineJoint);
            Add(AngularJoint);
            Add(Limit);
            Add(Motor);
        }


        /// <summary>
        /// Constructs a new constraint which restricts two degrees of linear freedom and two degrees of angular freedom between two entities.
        /// </summary>
        /// <param name="connectionA">First entity of the constraint pair.</param>
        /// <param name="connectionB">Second entity of the constraint pair.</param>
        /// <param name="lineAnchor">Location of the anchor for the line to be attached to connectionA in world space.</param>
        /// <param name="lineDirection">Axis in world space to be attached to connectionA along which connectionB can move and rotate.</param>
        /// <param name="pointAnchor">Location of the anchor for the point to be attached to connectionB in world space.</param>
        public LineSliderJoint(Entity connectionA, Entity connectionB, Vector3 lineAnchor, Vector3 lineDirection, Vector3 pointAnchor)
        {
            if (connectionA == null)
                connectionA = TwoEntityConstraint.WorldEntity;
            if (connectionB == null)
                connectionB = TwoEntityConstraint.WorldEntity;
            PointOnLineJoint = new PointOnLineJoint(connectionA, connectionB, lineAnchor, lineDirection, pointAnchor);
            AngularJoint = new RevoluteAngularJoint(connectionA, connectionB, lineDirection);
            Limit = new LinearAxisLimit(connectionA, connectionB, lineAnchor, pointAnchor, lineDirection, 0, 0);
            Motor = new LinearAxisMotor(connectionA, connectionB, lineAnchor, pointAnchor, lineDirection);
            Limit.IsActive = false;
            Motor.IsActive = false;
            Add(PointOnLineJoint);
            Add(AngularJoint);
            Add(Limit);
            Add(Motor);
        }

        /// <summary>
        /// Gets the angular joint which removes two degrees of freedom.
        /// </summary>
        public RevoluteAngularJoint AngularJoint { get; private set; }

        /// <summary>
        /// Gets the distance limits for the slider.
        /// </summary>
        public LinearAxisLimit Limit { get; private set; }

        /// <summary>
        /// Gets the slider motor.
        /// </summary>
        public LinearAxisMotor Motor { get; private set; }

        /// <summary>
        /// Gets the line joint that restricts two linear degrees of freedom.
        /// </summary>
        public PointOnLineJoint PointOnLineJoint { get; private set; }
    }
}