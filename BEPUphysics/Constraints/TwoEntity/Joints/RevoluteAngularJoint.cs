using System;
using BEPUphysics.Entities;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints.TwoEntity.Joints
{
    /// <summary>
    /// Constrains two entities to rotate only around a single axis.
    /// Acts like the angular portion of a hinge joint.
    /// </summary>
    public class RevoluteAngularJoint : Joint, I2DImpulseConstraintWithError, I2DJacobianConstraint
    {
        private Vector2 accumulatedImpulse;
        private Vector2 biasVelocity;
        private Matrix2X2 effectiveMassMatrix;
        private Vector3 localAxisA, localAxisB;
        private Vector3 localConstrainedAxis1, localConstrainedAxis2; //Not a and b because they are both based on a...
        private Vector2 error;
        private Vector3 worldAxisA, worldAxisB;
        private Vector3 worldConstrainedAxis1, worldConstrainedAxis2;

        /// <summary>
        /// Constructs a new orientation joint.
        /// Orientation joints can be used to simulate the angular portion of a hinge.
        /// Orientation joints allow rotation around only a single axis.
        /// To finish the initialization, specify the connections (ConnectionA and ConnectionB) 
        /// as well as the WorldFreeAxisA and WorldFreeAxisB (or their entity-local versions).
        /// This constructor sets the constraint's IsActive property to false by default.
        /// </summary>
        public RevoluteAngularJoint()
        {
            IsActive = false;
        }

        /// <summary>
        /// Constructs a new orientation joint.
        /// Orientation joints can be used to simulate the angular portion of a hinge.
        /// Orientation joints allow rotation around only a single axis.
        /// </summary>
        /// <param name="connectionA">First entity connected in the orientation joint.</param>
        /// <param name="connectionB">Second entity connected in the orientation joint.</param>
        /// <param name="freeAxis">Axis allowed to rotate freely in world space.</param>
        public RevoluteAngularJoint(Entity connectionA, Entity connectionB, Vector3 freeAxis)
        {
            ConnectionA = connectionA;
            ConnectionB = connectionB;

            //rA and rB store the local version of the axis.
            WorldFreeAxisA = freeAxis;
            WorldFreeAxisB = freeAxis;
        }

        /// <summary>
        /// Gets or sets the free axis in connection A's local space.
        /// Updates the internal restricted axes.
        /// </summary>
        public Vector3 LocalFreeAxisA
        {
            get { return localAxisA; }
            set
            {
                localAxisA = Vector3.Normalize(value);
                Matrix3X3.Transform(ref localAxisA, ref connectionA.orientationMatrix, out worldAxisA);
                UpdateRestrictedAxes();
            }
        }

        /// <summary>
        /// Gets or sets the free axis in connection B's local space.
        /// </summary>
        public Vector3 LocalFreeAxisB
        {
            get { return localAxisB; }
            set
            {
                localAxisB = Vector3.Normalize(value);
                Matrix3X3.Transform(ref localAxisB, ref connectionB.orientationMatrix, out worldAxisB);
            }
        }

        /// <summary>
        /// Gets or sets the free axis attached to connection A in world space.
        /// This does not change the other connection's free axis.
        /// Updates the internal restricted axes.
        /// </summary>
        public Vector3 WorldFreeAxisA
        {
            get { return localAxisA; }
            set
            {
                worldAxisA = Vector3.Normalize(value);
                Matrix3X3.TransformTranspose(ref worldAxisA, ref connectionA.orientationMatrix, out localAxisA);
                UpdateRestrictedAxes();
            }
        }

        /// <summary>
        /// Gets or sets the free axis attached to connection A in world space.
        /// This does not change the other connection's free axis.
        /// </summary>
        public Vector3 WorldFreeAxisB
        {
            get { return localAxisB; }
            set
            {
                worldAxisB = Vector3.Normalize(value);
                Matrix3X3.TransformTranspose(ref worldAxisB, ref connectionB.orientationMatrix, out localAxisB);
            }
        }

        #region I2DImpulseConstraintWithError Members

        /// <summary>
        /// Gets the current relative velocity between the connected entities with respect to the constraint.
        /// </summary>
        public Vector2 RelativeVelocity
        {
            get
            {
                Vector3 velocity;
                Vector3.Subtract(ref connectionA.angularVelocity, ref connectionB.angularVelocity, out velocity);

#if !WINDOWS
                Vector2 lambda = new Vector2();
#else
                Vector2 lambda;
#endif
                Vector3.Dot(ref worldConstrainedAxis1, ref velocity, out lambda.X);
                Vector3.Dot(ref worldConstrainedAxis2, ref velocity, out lambda.Y);
                return lambda;
            }
        }

        /// <summary>
        /// Gets the total impulse applied by this constraint.
        /// </summary>
        public Vector2 TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        public Vector2 Error
        {
            get { return error; }
        }

        #endregion

        #region I2DJacobianConstraint Members

        /// <summary>
        /// Gets the linear jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the first connected entity.</param>
        public void GetLinearJacobianA(out Vector3 jacobianX, out Vector3 jacobianY)
        {
            jacobianX = Toolbox.ZeroVector;
            jacobianY = Toolbox.ZeroVector;
        }

        /// <summary>
        /// Gets the linear jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First linear jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second linear jacobian entry for the second connected entity.</param>
        public void GetLinearJacobianB(out Vector3 jacobianX, out Vector3 jacobianY)
        {
            jacobianX = Toolbox.ZeroVector;
            jacobianY = Toolbox.ZeroVector;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the first connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the first connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the first connected entity.</param>
        public void GetAngularJacobianA(out Vector3 jacobianX, out Vector3 jacobianY)
        {
            jacobianX = worldConstrainedAxis1;
            jacobianY = worldConstrainedAxis2;
        }

        /// <summary>
        /// Gets the angular jacobian entry for the second connected entity.
        /// </summary>
        /// <param name="jacobianX">First angular jacobian entry for the second connected entity.</param>
        /// <param name="jacobianY">Second angular jacobian entry for the second connected entity.</param>
        public void GetAngularJacobianB(out Vector3 jacobianX, out Vector3 jacobianY)
        {
            jacobianX = -worldConstrainedAxis1;
            jacobianY = -worldConstrainedAxis2;
        }

        /// <summary>
        /// Gets the mass matrix of the constraint.
        /// </summary>
        /// <param name="massMatrix">Constraint's mass matrix.</param>
        public void GetMassMatrix(out Matrix2X2 massMatrix)
        {
            massMatrix = effectiveMassMatrix;
        }

        #endregion

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {
            // lambda = -mc * (Jv + b)
            // P = JT * lambda
            Vector3 velocity;
            Vector3.Subtract(ref connectionA.angularVelocity, ref connectionB.angularVelocity, out velocity);

#if !WINDOWS
            Vector2 lambda = new Vector2();
#else
            Vector2 lambda;
#endif
            Vector3.Dot(ref worldConstrainedAxis1, ref velocity, out lambda.X);
            Vector3.Dot(ref worldConstrainedAxis2, ref velocity, out lambda.Y);
            Vector2.Add(ref lambda, ref biasVelocity, out lambda);
            Vector2 softnessImpulse;
            Vector2.Multiply(ref accumulatedImpulse, softness, out softnessImpulse);
            Vector2.Add(ref lambda, ref softnessImpulse, out lambda);
            Matrix2X2.Transform(ref lambda, ref effectiveMassMatrix, out lambda);
            Vector2.Add(ref accumulatedImpulse, ref lambda, out accumulatedImpulse);


#if !WINDOWS
            Vector3 impulse = new Vector3();
#else
            Vector3 impulse;
#endif
            impulse.X = worldConstrainedAxis1.X * lambda.X + worldConstrainedAxis2.X * lambda.Y;
            impulse.Y = worldConstrainedAxis1.Y * lambda.X + worldConstrainedAxis2.Y * lambda.Y;
            impulse.Z = worldConstrainedAxis1.Z * lambda.X + worldConstrainedAxis2.Z * lambda.Y;
            if (connectionA.isDynamic)
            {
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Negate(ref impulse, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }

            return (Math.Abs(lambda.X) + Math.Abs(lambda.Y));
        }

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {
            Matrix3X3.Transform(ref localAxisA, ref connectionA.orientationMatrix, out worldAxisA);
            Matrix3X3.Transform(ref localAxisB, ref connectionB.orientationMatrix, out worldAxisB);


            Matrix3X3.Transform(ref localConstrainedAxis1, ref connectionA.orientationMatrix, out worldConstrainedAxis1);
            Matrix3X3.Transform(ref localConstrainedAxis2, ref connectionA.orientationMatrix, out worldConstrainedAxis2);

            Vector3 error;
            Vector3.Cross(ref worldAxisA, ref worldAxisB, out error);

            Vector3.Dot(ref error, ref worldConstrainedAxis1, out this.error.X);
            Vector3.Dot(ref error, ref worldConstrainedAxis2, out this.error.Y);
            float errorReduction;
            springSettings.ComputeErrorReductionAndSoftness(dt, out errorReduction, out softness);
            errorReduction = -errorReduction;
            biasVelocity.X = errorReduction * this.error.X;
            biasVelocity.Y = errorReduction * this.error.Y;


            //Ensure that the corrective velocity doesn't exceed the max.
            float length = biasVelocity.LengthSquared();
            if (length > maxCorrectiveVelocitySquared)
            {
                float multiplier = maxCorrectiveVelocity / (float) Math.Sqrt(length);
                biasVelocity.X *= multiplier;
                biasVelocity.Y *= multiplier;
            }

            Vector3 axis1I, axis2I;
            if (connectionA.isDynamic && connectionB.isDynamic)
            {
                Matrix3X3 inertiaTensorSum;
                Matrix3X3.Add(ref connectionA.inertiaTensorInverse, ref connectionB.inertiaTensorInverse, out inertiaTensorSum);

                Matrix3X3.Transform(ref worldConstrainedAxis1, ref inertiaTensorSum, out axis1I);
                Matrix3X3.Transform(ref worldConstrainedAxis2, ref inertiaTensorSum, out axis2I);
            }
            else if (connectionA.isDynamic && !connectionB.isDynamic)
            {
                Matrix3X3.Transform(ref worldConstrainedAxis1, ref connectionA.inertiaTensorInverse, out axis1I);
                Matrix3X3.Transform(ref worldConstrainedAxis2, ref connectionA.inertiaTensorInverse, out axis2I);
            }
            else if (!connectionA.isDynamic && connectionB.isDynamic)
            {
                Matrix3X3.Transform(ref worldConstrainedAxis1, ref connectionB.inertiaTensorInverse, out axis1I);
                Matrix3X3.Transform(ref worldConstrainedAxis2, ref connectionB.inertiaTensorInverse, out axis2I);
            }
            else
            {
                throw new InvalidOperationException("Cannot constrain two kinematic bodies.");
            }

            Vector3.Dot(ref axis1I, ref worldConstrainedAxis1, out effectiveMassMatrix.M11);
            Vector3.Dot(ref axis1I, ref worldConstrainedAxis2, out effectiveMassMatrix.M12);
            Vector3.Dot(ref axis2I, ref worldConstrainedAxis1, out effectiveMassMatrix.M21);
            Vector3.Dot(ref axis2I, ref worldConstrainedAxis2, out effectiveMassMatrix.M22);
            effectiveMassMatrix.M11 += softness;
            effectiveMassMatrix.M22 += softness;
            Matrix2X2.Invert(ref effectiveMassMatrix, out effectiveMassMatrix);
            Matrix2X2.Negate(ref effectiveMassMatrix, out effectiveMassMatrix);

   
        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            //Warm Starting
#if !WINDOWS
            Vector3 impulse = new Vector3();
#else
            Vector3 impulse;
#endif
            impulse.X = worldConstrainedAxis1.X * accumulatedImpulse.X + worldConstrainedAxis2.X * accumulatedImpulse.Y;
            impulse.Y = worldConstrainedAxis1.Y * accumulatedImpulse.X + worldConstrainedAxis2.Y * accumulatedImpulse.Y;
            impulse.Z = worldConstrainedAxis1.Z * accumulatedImpulse.X + worldConstrainedAxis2.Z * accumulatedImpulse.Y;
            if (connectionA.isDynamic)
            {
                connectionA.ApplyAngularImpulse(ref impulse);
            }
            if (connectionB.isDynamic)
            {
                Vector3.Negate(ref impulse, out impulse);
                connectionB.ApplyAngularImpulse(ref impulse);
            }
        }

        private void UpdateRestrictedAxes()
        {
            localConstrainedAxis1 = Vector3.Cross(Vector3.Up, localAxisA);
            if (localConstrainedAxis1.LengthSquared() < .001f)
            {
                localConstrainedAxis1 = Vector3.Cross(Vector3.Right, localAxisA);
            }
            localConstrainedAxis2 = Vector3.Cross(localAxisA, localConstrainedAxis1);
            localConstrainedAxis1.Normalize();
            localConstrainedAxis2.Normalize();
        }
    }
}