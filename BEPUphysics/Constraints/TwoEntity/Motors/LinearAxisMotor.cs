using System;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.Constraints.TwoEntity.Motors
{
    /// <summary>
    /// Constrains anchors on two entities to move relative to each other on a line.
    /// </summary>
    public class LinearAxisMotor : Motor, I1DImpulseConstraintWithError, I1DJacobianConstraint
    {
        private readonly MotorSettings1D settings;

        private float accumulatedImpulse;
        private float biasVelocity;
        private Vector3 jAngularA, jAngularB;
        private Vector3 jLinearA, jLinearB;
        private Vector3 localAnchorA;
        private Vector3 localAnchorB;
        private float massMatrix;
        private float error;
        private Vector3 localAxis;
        private Vector3 worldAxis;
        private Vector3 rA; //Jacobian entry for entity A.
        private Vector3 worldAnchorA;
        private Vector3 worldAnchorB;
        private Vector3 worldOffsetA, worldOffsetB;

        /// <summary>
        /// Constrains anchors on two entities to move relative to each other on a line.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the AnchorA, AnchorB and the Axis (or their entity-local versions).
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public LinearAxisMotor()
        {
            settings = new MotorSettings1D(this);
            IsActive = false;
        }


        /// <summary>
        /// Constrains anchors on two entities to move relative to each other on a line.
        /// </summary>
        /// <param name="connectionA">First connection of the pair.</param>
        /// <param name="connectionB">Second connection of the pair.</param>
        /// <param name="anchorA">World space point to attach to connection A that will be constrained.</param>
        /// <param name="anchorB">World space point to attach to connection B that will be constrained.</param>
        /// <param name="axis">Limited axis in world space to attach to connection A.</param>
        public LinearAxisMotor(Entity connectionA, Entity connectionB, Vector3 anchorA, Vector3 anchorB, Vector3 axis)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;
            AnchorA = anchorA;
            AnchorB = anchorB;
            Axis = axis;

            settings = new MotorSettings1D(this);
        }

        /// <summary>
        /// Gets or sets the anchor point attached to entity A in world space.
        /// </summary>
        public Vector3 AnchorA
        {
            get { return worldAnchorA; }
            set
            {
                worldAnchorA = value;
                worldOffsetA = worldAnchorA - connectionA.position;
                Matrix3X3.TransformTranspose(ref worldOffsetA, ref connectionA.orientationMatrix, out localAnchorA);
            }
        }

        /// <summary>
        /// Gets or sets the anchor point attached to entity A in world space.
        /// </summary>
        public Vector3 AnchorB
        {
            get { return worldAnchorB; }
            set
            {
                worldAnchorB = value;
                worldOffsetB = worldAnchorB - connectionB.position;
                Matrix3X3.TransformTranspose(ref worldOffsetB, ref connectionB.orientationMatrix, out localAnchorB);
            }
        }


        /// <summary>
        /// Gets or sets the motorized axis in world space.
        /// </summary>
        public Vector3 Axis
        {
            get { return worldAxis; }
            set
            {
                worldAxis = Vector3.Normalize(value);
                Matrix3X3.TransformTranspose(ref worldAxis, ref connectionA.orientationMatrix, out localAxis);
            }
        }

        /// <summary>
        /// Gets or sets the limited axis in the local space of connection A.
        /// </summary>
        public Vector3 LocalAxis
        {
            get { return localAxis; }
            set
            {
                localAxis = Vector3.Normalize(value);
                Matrix3X3.Transform(ref localAxis, ref connectionA.orientationMatrix, out worldAxis);
            }
        }

        /// <summary>
        /// Gets or sets the offset from the first entity's center of mass to the anchor point in its local space.
        /// </summary>
        public Vector3 LocalOffsetA
        {
            get { return localAnchorA; }
            set
            {
                localAnchorA = value;
                Matrix3X3.Transform(ref localAnchorA, ref connectionA.orientationMatrix, out worldOffsetA);
                worldAnchorA = connectionA.position + worldOffsetA;
            }
        }

        /// <summary>
        /// Gets or sets the offset from the second entity's center of mass to the anchor point in its local space.
        /// </summary>
        public Vector3 LocalOffsetB
        {
            get { return localAnchorB; }
            set
            {
                localAnchorB = value;
                Matrix3X3.Transform(ref localAnchorB, ref connectionB.orientationMatrix, out worldOffsetB);
                worldAnchorB = connectionB.position + worldOffsetB;
            }
        }

        /// <summary>
        /// Gets or sets the offset from the first entity's center of mass to the anchor point in world space.
        /// </summary>
        public Vector3 OffsetA
        {
            get { return worldOffsetA; }
            set
            {
                worldOffsetA = value; 
                worldAnchorA = connectionA.position + worldOffsetA;
                Matrix3X3.TransformTranspose(ref worldOffsetA, ref connectionA.orientationMatrix, out localAnchorA); //Looks weird, but localAnchorA is "localOffsetA."
                
            }
        }

        /// <summary>
        /// Gets or sets the offset from the second entity's center of mass to the anchor point in world space.
        /// </summary>
        public Vector3 OffsetB
        {
            get { return worldOffsetB; }
            set
            {
                worldOffsetB = value;
                worldAnchorB = connectionB.position + worldOffsetB;
                Matrix3X3.TransformTranspose(ref worldOffsetB, ref connectionB.orientationMatrix, out localAnchorB);//Looks weird, but localAnchorB is "localOffsetB."
            }
        }

        /// <summary>
        /// Gets the motor's velocity and servo settings.
        /// </summary>
        public MotorSettings1D Settings
        {
            get { return settings; }
        }

        #region I1DImpulseConstraintWithError Members

        /// <summary>
        /// Gets the current relative velocity between the connected entities with respect to the constraint.
        /// </summary>
        public float RelativeVelocity
        {
            get
            {
                float lambda, dot;
                Vector3.Dot(ref jLinearA, ref connectionA.linearVelocity, out lambda);
                Vector3.Dot(ref jAngularA, ref connectionA.angularVelocity, out dot);
                lambda += dot;
                Vector3.Dot(ref jLinearB, ref connectionB.linearVelocity, out dot);
                lambda += dot;
                Vector3.Dot(ref jAngularB, ref connectionB.angularVelocity, out dot);
                lambda += dot;
                return lambda;
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
        /// If the motor is in velocity only mode, the error will be zero.
        /// </summary>
        public float Error
        {
            get { return error; }
        }

        #endregion

        //Jacobians

        #region I1DJacobianConstraint Members

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the first connected entity.</param>
        public void GetLinearJacobianA(out Vector3 jacobian)
        {
            jacobian = jLinearA;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out Vector3 jacobian)
        {
            jacobian = jLinearB;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out Vector3 jacobian)
        {
            jacobian = jAngularA;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobian">Angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out Vector3 jacobian)
        {
            jacobian = jAngularB;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="outputMassMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out float outputMassMatrix)
        {
            outputMassMatrix = massMatrix;
        }

        #endregion

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            //Compute the current relative velocity.
            float lambda, dot;
            Vector3.Dot(ref jLinearA, ref connectionA.linearVelocity, out lambda);
            Vector3.Dot(ref jAngularA, ref connectionA.angularVelocity, out dot);
            lambda += dot;
            Vector3.Dot(ref jLinearB, ref connectionB.linearVelocity, out dot);
            lambda += dot;
            Vector3.Dot(ref jAngularB, ref connectionB.angularVelocity, out dot);
            lambda += dot;

            //Add in the constraint space bias velocity
            lambda = -lambda + biasVelocity - usedSoftness * accumulatedImpulse;

            //Transform to an impulse
            lambda *= massMatrix;

            //Clamp accumulated impulse
            float previousAccumulatedImpulse = accumulatedImpulse;
            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, -maxForceDt, maxForceDt);
            lambda = accumulatedImpulse - previousAccumulatedImpulse;

            //Apply the impulse
            Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3.Multiply(ref jLinearA, lambda, out impulse);
                connectionA.ApplyLinearImpulse(ref impulse);
                Vector3.Multiply(ref jAngularA, lambda, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Multiply(ref jLinearB, lambda, out impulse);
                connectionB.ApplyLinearImpulse(ref impulse);
                Vector3.Multiply(ref jAngularB, lambda, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }

            return (Math.Abs(lambda));
        }

        public override void Update(float dt)
        {
            //Compute the 'pre'-jacobians
            Matrix3X3.Transform(ref localAnchorA, ref connectionA.orientationMatrix, out worldOffsetA);
            Matrix3X3.Transform(ref localAnchorB, ref connectionB.orientationMatrix, out worldOffsetB);
            Vector3.Add(ref worldOffsetA, ref connectionA.position, out worldAnchorA);
            Vector3.Add(ref worldOffsetB, ref connectionB.position, out worldAnchorB);
            Vector3.Subtract(ref worldAnchorB, ref connectionA.position, out rA);
            Matrix3X3.Transform(ref localAxis, ref connectionA.orientationMatrix, out worldAxis);

            if (settings.mode == MotorMode.Servomechanism)
            {
                //Compute error
#if !WINDOWS
            Vector3 separation = new Vector3();
#else
                Vector3 separation;
#endif
                separation.X = worldAnchorB.X - worldAnchorA.X;
                separation.Y = worldAnchorB.Y - worldAnchorA.Y;
                separation.Z = worldAnchorB.Z - worldAnchorA.Z;

                Vector3.Dot(ref separation, ref worldAxis, out error);

                //Compute error
                error = error - settings.servo.goal;


                //Compute bias
                float absErrorOverDt = Math.Abs(error / dt);
                float errorReduction;
                settings.servo.springSettings.ComputeErrorReductionAndSoftness(dt, out errorReduction, out usedSoftness);
                biasVelocity = Math.Sign(error) * MathHelper.Min(settings.servo.baseCorrectiveSpeed, absErrorOverDt) + error * errorReduction;
                biasVelocity = MathHelper.Clamp(biasVelocity, -settings.servo.maxCorrectiveVelocity, settings.servo.maxCorrectiveVelocity);
            }
            else
            {
                biasVelocity = -settings.velocityMotor.goalVelocity;
                usedSoftness = settings.velocityMotor.softness / dt;
                error = 0;
            }

            //Compute jacobians
            jLinearA = worldAxis;
            jLinearB.X = -jLinearA.X;
            jLinearB.Y = -jLinearA.Y;
            jLinearB.Z = -jLinearA.Z;
            Vector3.Cross(ref rA, ref jLinearA, out jAngularA);
            Vector3.Cross(ref worldOffsetB, ref jLinearB, out jAngularB);

            //compute mass matrix
            float entryA, entryB;
            Vector3 intermediate;
            if (connectionA.isDynamic)
            {
                Matrix3X3.Transform(ref jAngularA, ref connectionA.inertiaTensorInverse, out intermediate);
                Vector3.Dot(ref intermediate, ref jAngularA, out entryA);
                entryA += connectionA.inverseMass;
            }
            else
                entryA = 0;
            if (connectionB.isDynamic)
            {
                Matrix3X3.Transform(ref jAngularB, ref connectionB.inertiaTensorInverse, out intermediate);
                Vector3.Dot(ref intermediate, ref jAngularB, out entryB);
                entryB += connectionB.inverseMass;
            }
            else
                entryB = 0;
            massMatrix = 1 / (entryA + entryB + usedSoftness);

            //Update the maximum force
            ComputeMaxForces(settings.maximumForce, dt);


            
        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm starting
            Vector3 impulse;
            if (connectionA.isDynamic)
            {
                Vector3.Multiply(ref jLinearA, accumulatedImpulse, out impulse);
                connectionA.ApplyLinearImpulse(ref impulse);
                Vector3.Multiply(ref jAngularA, accumulatedImpulse, out impulse);
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Multiply(ref jLinearB, accumulatedImpulse, out impulse);
                connectionB.ApplyLinearImpulse(ref impulse);
                Vector3.Multiply(ref jAngularB, accumulatedImpulse, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }
        }
    }
}