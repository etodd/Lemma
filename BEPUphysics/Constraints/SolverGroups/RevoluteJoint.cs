using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.JointLimits;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints.SolverGroups
{
    /// <summary>
    /// Restricts linear motion while allowing one degree of angular freedom.
    /// Acts like a normal door hinge.
    /// </summary>
    public class RevoluteJoint : SolverGroup
    {

        /// <summary>
        /// Constructs a new constraint which restricts three degrees of linear freedom and two degrees of angular freedom between two entities.
        /// This constructs the internal constraints, but does not configure them.  Before using a constraint constructed in this manner,
        /// ensure that its active constituent constraints are properly configured.  The entire group as well as all internal constraints are initially inactive (IsActive = false).
        /// </summary>
        public RevoluteJoint()
        {
            IsActive = false;
            BallSocketJoint = new BallSocketJoint();
            AngularJoint = new RevoluteAngularJoint();
            Limit = new RevoluteLimit();
            Motor = new RevoluteMotor();


            Add(BallSocketJoint);
            Add(AngularJoint);
            Add(Limit);
            Add(Motor);
        }


        /// <summary>
        /// Constructs a new constraint which restricts three degrees of linear freedom and two degrees of angular freedom between two entities.
        /// </summary>
        /// <param name="connectionA">First entity of the constraint pair.</param>
        /// <param name="connectionB">Second entity of the constraint pair.</param>
        /// <param name="anchor">Point around which both entities rotate.</param>
        /// <param name="freeAxis">Axis around which the hinge can rotate.</param>
        public RevoluteJoint(Entity connectionA, Entity connectionB, Vector3 anchor, Vector3 freeAxis)
        {
            if (connectionA == null)
                connectionA = TwoEntityConstraint.WorldEntity;
            if (connectionB == null)
                connectionB = TwoEntityConstraint.WorldEntity;
            BallSocketJoint = new BallSocketJoint(connectionA, connectionB, anchor);
            AngularJoint = new RevoluteAngularJoint(connectionA, connectionB, freeAxis);
            Limit = new RevoluteLimit(connectionA, connectionB);
            Motor = new RevoluteMotor(connectionA, connectionB, freeAxis);
            Limit.IsActive = false;
            Motor.IsActive = false;

            //Ensure that the base and test direction is perpendicular to the free axis.
            Vector3 baseAxis = anchor - connectionA.position;
            if (baseAxis.LengthSquared() < Toolbox.BigEpsilon) //anchor and connection a in same spot, so try the other way.
                baseAxis = connectionB.position - anchor;
            baseAxis -= Vector3.Dot(baseAxis, freeAxis) * freeAxis;
            if (baseAxis.LengthSquared() < Toolbox.BigEpsilon)
            {
                //However, if the free axis is totally aligned (like in an axis constraint), pick another reasonable direction.
                baseAxis = Vector3.Cross(freeAxis, Vector3.Up);
                if (baseAxis.LengthSquared() < Toolbox.BigEpsilon)
                {
                    baseAxis = Vector3.Cross(freeAxis, Vector3.Right);
                }
            }
            Limit.Basis.SetWorldAxes(freeAxis, baseAxis, connectionA.orientationMatrix);
            Motor.Basis.SetWorldAxes(freeAxis, baseAxis, connectionA.orientationMatrix);

            baseAxis = connectionB.position - anchor;
            baseAxis -= Vector3.Dot(baseAxis, freeAxis) * freeAxis;
            if (baseAxis.LengthSquared() < Toolbox.BigEpsilon)
            {
                //However, if the free axis is totally aligned (like in an axis constraint), pick another reasonable direction.
                baseAxis = Vector3.Cross(freeAxis, Vector3.Up);
                if (baseAxis.LengthSquared() < Toolbox.BigEpsilon)
                {
                    baseAxis = Vector3.Cross(freeAxis, Vector3.Right);
                }
            }
            Limit.TestAxis = baseAxis;
            Motor.TestAxis = baseAxis;


            Add(BallSocketJoint);
            Add(AngularJoint);
            Add(Limit);
            Add(Motor);
        }

        /// <summary>
        /// Gets the angular joint which removes two degrees of freedom.
        /// </summary>
        public RevoluteAngularJoint AngularJoint { get; private set; }

        /// <summary>
        /// Gets the ball socket joint that restricts linear degrees of freedom.
        /// </summary>
        public BallSocketJoint BallSocketJoint { get; private set; }

        /// <summary>
        /// Gets the rotational limit of the hinge.
        /// </summary>
        public RevoluteLimit Limit { get; private set; }

        /// <summary>
        /// Gets the motor of the hinge.
        /// </summary>
        public RevoluteMotor Motor { get; private set; }
    }
}