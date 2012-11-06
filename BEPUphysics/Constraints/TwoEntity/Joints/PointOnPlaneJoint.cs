using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.Constraints.TwoEntity.Joints
{
    /// <summary>
    /// Constrains a point on one body to be on a plane defined by another body.
    /// </summary>
    public class PointOnPlaneJoint : Joint, I1DImpulseConstraintWithError, I1DJacobianConstraint
    {
        private float accumulatedImpulse;
        private float biasVelocity;
        private float error;

        private Vector3 localPlaneAnchor;
        private Vector3 localPlaneNormal;
        private Vector3 localPointAnchor;

        private Vector3 worldPlaneAnchor;
        private Vector3 worldPlaneNormal;
        private Vector3 worldPointAnchor;
        private float negativeEffectiveMass;
        private Vector3 rA;
        private Vector3 rAcrossN;
        private Vector3 rB;
        private Vector3 rBcrossN;

        /// <summary>
        /// Constructs a new point on plane constraint.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the PlaneAnchor, PlaneNormal, and PointAnchor (or their entity-local versions).
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public PointOnPlaneJoint()
        {
            IsActive = false;
        }

        /// <summary>
        /// Constructs a new point on plane constraint.
        /// </summary>
        /// <param name="connectionA">Entity to which the constraint's plane is attached.</param>
        /// <param name="connectionB">Entity to which the constraint's point is attached.</param>
        /// <param name="planeAnchor">A point on the plane.</param>
        /// <param name="normal">Direction, attached to the first connected entity, defining the plane's normal</param>
        /// <param name="pointAnchor">The point to constrain to the plane, attached to the second connected object.</param>
        public PointOnPlaneJoint(Entity connectionA, Entity connectionB, Vector3 planeAnchor, Vector3 normal, Vector3 pointAnchor)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;

            PointAnchor = pointAnchor;
            PlaneAnchor = planeAnchor;
            PlaneNormal = normal;
        }

        /// <summary>
        /// Gets or sets the plane's anchor in entity A's local space.
        /// </summary>
        public Vector3 LocalPlaneAnchor
        {
            get { return localPlaneAnchor; }
            set
            {
                localPlaneAnchor = value;
                Matrix3X3.Transform(ref localPlaneAnchor, ref connectionA.orientationMatrix, out worldPlaneAnchor);
                Vector3.Add(ref connectionA.position, ref worldPlaneAnchor, out worldPlaneAnchor);
            }
        }

        /// <summary>
        /// Gets or sets the plane's normal in entity A's local space.
        /// </summary>
        public Vector3 LocalPlaneNormal
        {
            get { return localPlaneNormal; }
            set
            {
                localPlaneNormal = Vector3.Normalize(value);
                Matrix3X3.Transform(ref localPlaneNormal, ref connectionA.orientationMatrix, out worldPlaneNormal);
            }
        }

        /// <summary>
        /// Gets or sets the point anchor in entity B's local space.
        /// </summary>
        public Vector3 LocalPointAnchor
        {
            get { return localPointAnchor; }
            set
            {
                localPointAnchor = value;
                Matrix3X3.Transform(ref localPointAnchor, ref connectionB.orientationMatrix, out worldPointAnchor);
                Vector3.Add(ref worldPointAnchor, ref connectionB.position, out worldPointAnchor);
            }
        }

        /// <summary>
        /// Gets the offset from A to the connection point between the entities.
        /// </summary>
        public Vector3 OffsetA
        {
            get { return rA; }
        }

        /// <summary>
        /// Gets the offset from B to the connection point between the entities.
        /// </summary>
        public Vector3 OffsetB
        {
            get { return rB; }
        }

        /// <summary>
        /// Gets or sets the plane anchor in world space.
        /// </summary>
        public Vector3 PlaneAnchor
        {
            get { return worldPlaneAnchor; }
            set
            {
                worldPlaneAnchor = value;
                localPlaneAnchor = value - connectionA.position;
                Matrix3X3.TransformTranspose(ref localPlaneAnchor, ref connectionA.orientationMatrix, out localPlaneAnchor);

            }
        }

        /// <summary>
        /// Gets or sets the plane's normal in world space.
        /// </summary>
        public Vector3 PlaneNormal
        {
            get { return worldPlaneNormal; }
            set
            {
                worldPlaneNormal = Vector3.Normalize(value);
                Matrix3X3.TransformTranspose(ref worldPlaneNormal, ref connectionA.orientationMatrix, out localPlaneNormal);
            }
        }

        /// <summary>
        /// Gets or sets the point anchor in world space.
        /// </summary>
        public Vector3 PointAnchor
        {
            get { return worldPointAnchor; }
            set
            {
                worldPointAnchor = value;
                localPointAnchor = value - connectionB.position;
                Matrix3X3.TransformTranspose(ref localPointAnchor, ref connectionB.orientationMatrix, out localPointAnchor);

            }
        }

        #region I1DImpulseConstraintWithError Members

        /// <summary>
        /// Gets the current relative velocity between the connected entities with respect to the constraint.
        /// </summary>
        public float RelativeVelocity
        {
            get
            {
                Vector3 dv;
                Vector3 aVel, bVel;
                Vector3.Cross(ref connectionA.angularVelocity, ref rA, out aVel);
                Vector3.Add(ref aVel, ref connectionA.linearVelocity, out aVel);
                Vector3.Cross(ref connectionB.angularVelocity, ref rB, out bVel);
                Vector3.Add(ref bVel, ref connectionB.linearVelocity, out bVel);
                Vector3.Subtract(ref aVel, ref bVel, out dv);
                float velocityDifference;
                Vector3.Dot(ref dv, ref worldPlaneNormal, out velocityDifference);
                return velocityDifference;
            }
        }


        /// <summary>
        /// Gets the total impulse applied by this constraint.
        /// </summary>
        public float TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        public float Error
        {
            get { return error; }
        }

        #endregion

        #region I1DJacobianConstraint Members

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the first connected entity.</param>
        public void GetLinearJacobianA(out Vector3 jacobian)
        {
            jacobian = worldPlaneNormal;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out Vector3 jacobian)
        {
            jacobian = -worldPlaneNormal;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out Vector3 jacobian)
        {
            jacobian = rAcrossN;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out Vector3 jacobian)
        {
            jacobian = -rBcrossN;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out float outputMassMatrix)
        {
            outputMassMatrix = -negativeEffectiveMass;
        }

        #endregion

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            //TODO: This could technically be faster.
            //Form the jacobian explicitly.
            //Cross cross add add subtract dot
            //vs
            //dot dot dot dot and then scalar adds
            Vector3 dv;
            Vector3 aVel, bVel;
            Vector3.Cross(ref connectionA.angularVelocity, ref rA, out aVel);
            Vector3.Add(ref aVel, ref connectionA.linearVelocity, out aVel);
            Vector3.Cross(ref connectionB.angularVelocity, ref rB, out bVel);
            Vector3.Add(ref bVel, ref connectionB.linearVelocity, out bVel);
            Vector3.Subtract(ref aVel, ref bVel, out dv);
            float velocityDifference;
            Vector3.Dot(ref dv, ref worldPlaneNormal, out velocityDifference);
            //if(velocityDifference > 0)
            //    Debug.WriteLine("Velocity difference: " + velocityDifference);
            //Debug.WriteLine("softness velocity: " + softness * accumulatedImpulse);
            float lambda = negativeEffectiveMass * (velocityDifference + biasVelocity + softness * accumulatedImpulse);
            accumulatedImpulse += lambda;

            Vector3 impulse;
            Vector3 torque;
            Vector3.Multiply(ref worldPlaneNormal, lambda, out impulse);
            if (connectionA.isDynamic)
            {
                Vector3.Multiply(ref rAcrossN, lambda, out torque);
                connectionA.ApplyLinearImpulse(ref impulse);
                connectionA.ApplyAngularImpulse(ref torque);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Negate(ref impulse, out impulse);
                Vector3.Multiply(ref rBcrossN, lambda, out torque);
                connectionB.ApplyLinearImpulse(ref impulse);
                connectionB.ApplyAngularImpulse(ref torque);
            }

            return lambda;
        }

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {
            Matrix3X3.Transform(ref localPlaneNormal, ref connectionA.orientationMatrix, out worldPlaneNormal);
            Matrix3X3.Transform(ref localPlaneAnchor, ref connectionA.orientationMatrix, out worldPlaneAnchor);
            Vector3.Add(ref worldPlaneAnchor, ref connectionA.position, out worldPlaneAnchor);

            Matrix3X3.Transform(ref localPointAnchor, ref connectionB.orientationMatrix, out rB);
            Vector3.Add(ref rB, ref connectionB.position, out worldPointAnchor);

            //Find rA and rB.
            //So find the closest point on the plane to worldPointAnchor.
            float pointDistance, planeDistance;
            Vector3.Dot(ref worldPointAnchor, ref worldPlaneNormal, out pointDistance);
            Vector3.Dot(ref worldPlaneAnchor, ref worldPlaneNormal, out planeDistance);
            float distanceChange = planeDistance - pointDistance;
            Vector3 closestPointOnPlane;
            Vector3.Multiply(ref worldPlaneNormal, distanceChange, out closestPointOnPlane);
            Vector3.Add(ref closestPointOnPlane, ref worldPointAnchor, out closestPointOnPlane);

            Vector3.Subtract(ref closestPointOnPlane, ref connectionA.position, out rA);

            Vector3.Cross(ref rA, ref worldPlaneNormal, out rAcrossN);
            Vector3.Cross(ref rB, ref worldPlaneNormal, out rBcrossN);
            Vector3.Negate(ref rBcrossN, out rBcrossN);

            Vector3 offset;
            Vector3.Subtract(ref worldPointAnchor, ref closestPointOnPlane, out offset);
            Vector3.Dot(ref offset, ref worldPlaneNormal, out error);
            float errorReduction;
            springSettings.ComputeErrorReductionAndSoftness(dt, out errorReduction, out softness);
            biasVelocity = MathHelper.Clamp(-errorReduction * error, -maxCorrectiveVelocity, maxCorrectiveVelocity);

            if (connectionA.IsDynamic && connectionB.IsDynamic)
            {
                Vector3 IrACrossN, IrBCrossN;
                Matrix3X3.Transform(ref rAcrossN, ref connectionA.inertiaTensorInverse, out IrACrossN);
                Matrix3X3.Transform(ref rBcrossN, ref connectionB.inertiaTensorInverse, out IrBCrossN);
                float angularA, angularB;
                Vector3.Dot(ref rAcrossN, ref IrACrossN, out angularA);
                Vector3.Dot(ref rBcrossN, ref IrBCrossN, out angularB);
                negativeEffectiveMass = connectionA.inverseMass + connectionB.inverseMass + angularA + angularB;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else if (connectionA.IsDynamic && !connectionB.IsDynamic)
            {
                Vector3 IrACrossN;
                Matrix3X3.Transform(ref rAcrossN, ref connectionA.inertiaTensorInverse, out IrACrossN);
                float angularA;
                Vector3.Dot(ref rAcrossN, ref IrACrossN, out angularA);
                negativeEffectiveMass = connectionA.inverseMass + angularA;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else if (!connectionA.IsDynamic && connectionB.IsDynamic)
            {
                Vector3 IrBCrossN;
                Matrix3X3.Transform(ref rBcrossN, ref connectionB.inertiaTensorInverse, out IrBCrossN);
                float angularB;
                Vector3.Dot(ref rBcrossN, ref IrBCrossN, out angularB);
                negativeEffectiveMass = connectionB.inverseMass + angularB;
                negativeEffectiveMass = -1 / (negativeEffectiveMass + softness);
            }
            else
                negativeEffectiveMass = 0;


        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm Starting
            Vector3 impulse;
            Vector3 torque;
            Vector3.Multiply(ref worldPlaneNormal, accumulatedImpulse, out impulse);
            if (connectionA.isDynamic)
            {
                Vector3.Multiply(ref rAcrossN, accumulatedImpulse, out torque);
                connectionA.ApplyLinearImpulse(ref impulse);
                connectionA.ApplyAngularImpulse(ref torque);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Negate(ref impulse, out impulse);
                Vector3.Multiply(ref rBcrossN, accumulatedImpulse, out torque);
                connectionB.ApplyLinearImpulse(ref impulse);
                connectionB.ApplyAngularImpulse(ref torque);
            }
        }
    }
}