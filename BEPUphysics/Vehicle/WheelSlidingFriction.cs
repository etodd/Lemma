using System;
using BEPUphysics.Constraints;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.Materials;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Attempts to resist sliding motion of a vehicle.
    /// </summary>
    public class WheelSlidingFriction : ISolverSettings
    {
        #region Static Stuff

        /// <summary>
        /// Default blender used by WheelSlidingFriction constraints.
        /// </summary>
        public static WheelFrictionBlender DefaultSlidingFrictionBlender;

        static WheelSlidingFriction()
        {
            DefaultSlidingFrictionBlender = BlendFriction;
        }

        /// <summary>
        /// Function which takes the friction values from a wheel and a supporting material and computes the blended friction.
        /// </summary>
        /// <param name="wheelFriction">Friction coefficient associated with the wheel.</param>
        /// <param name="materialFriction">Friction coefficient associated with the support material.</param>
        /// <param name="usingKineticFriction">True if the friction coefficients passed into the blender are kinetic coefficients, false otherwise.</param>
        /// <param name="wheel">Wheel being blended.</param>
        /// <returns>Blended friction coefficient.</returns>
        public static float BlendFriction(float wheelFriction, float materialFriction, bool usingKinematicFriction, Wheel wheel)
        {
            return wheelFriction * materialFriction;
        }

        #endregion

        internal float accumulatedImpulse;

        //float linearBX, linearBY, linearBZ;
        private float angularAX, angularAY, angularAZ;
        private float angularBX, angularBY, angularBZ;
        internal bool isActive = true;
        private float linearAX, linearAY, linearAZ;
        private float blendedCoefficient;
        private float kineticCoefficient;
        private WheelFrictionBlender frictionBlender = DefaultSlidingFrictionBlender;
        internal Vector3 slidingFrictionAxis;
        internal SolverSettings solverSettings = new SolverSettings();
        private float staticCoefficient;
        private float staticFrictionVelocityThreshold = 5;
        private Wheel wheel;
        internal int numIterationsAtZeroImpulse;
        private Entity vehicleEntity, supportEntity;

        //Inverse effective mass matrix
        private float velocityToImpulse;

        /// <summary>
        /// Constructs a new sliding friction object for a wheel.
        /// </summary>
        /// <param name="dynamicCoefficient">Coefficient of dynamic sliding friction to be blended with the supporting entity's friction.</param>
        /// <param name="staticCoefficient">Coefficient of static sliding friction to be blended with the supporting entity's friction.</param>
        public WheelSlidingFriction(float dynamicCoefficient, float staticCoefficient)
        {
            KineticCoefficient = dynamicCoefficient;
            StaticCoefficient = staticCoefficient;
        }

        internal WheelSlidingFriction(Wheel wheel)
        {
            Wheel = wheel;
        }

        /// <summary>
        /// Gets the coefficient of sliding friction between the wheel and support.
        /// This coefficient is the blended result of the supporting entity's friction and the wheel's friction.
        /// </summary>
        public float BlendedCoefficient
        {
            get { return blendedCoefficient; }
        }

        /// <summary>
        /// Gets or sets the coefficient of dynamic horizontal sliding friction for this wheel.
        /// This coefficient and the supporting entity's coefficient of friction will be 
        /// taken into account to determine the used coefficient at any given time.
        /// </summary>
        public float KineticCoefficient
        {
            get { return kineticCoefficient; }
            set { kineticCoefficient = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the function used to blend the supporting entity's friction and the wheel's friction.
        /// </summary>
        public WheelFrictionBlender FrictionBlender
        {
            get { return frictionBlender; }
            set { frictionBlender = value; }
        }

        /// <summary>
        /// Gets the axis along which sliding friction is applied.
        /// </summary>
        public Vector3 SlidingFrictionAxis
        {
            get { return slidingFrictionAxis; }
        }

        /// <summary>
        /// Gets or sets the coefficient of static horizontal sliding friction for this wheel.
        /// This coefficient and the supporting entity's coefficient of friction will be 
        /// taken into account to determine the used coefficient at any given time.
        /// </summary>
        public float StaticCoefficient
        {
            get { return staticCoefficient; }
            set { staticCoefficient = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the velocity under which the coefficient of static friction will be used instead of the dynamic one.
        /// </summary>
        public float StaticFrictionVelocityThreshold
        {
            get { return staticFrictionVelocityThreshold; }
            set { staticFrictionVelocityThreshold = Math.Abs(value); }
        }

        /// <summary>
        /// Gets the force 
        /// </summary>
        public float TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        /// <summary>
        /// Gets the wheel that this sliding friction applies to.
        /// </summary>
        public Wheel Wheel
        {
            get { return wheel; }
            internal set { wheel = value; }
        }

        #region ISolverSettings Members

        /// <summary>
        /// Gets the solver settings used by this wheel constraint.
        /// </summary>
        public SolverSettings SolverSettings
        {
            get { return solverSettings; }
        }

        #endregion

        bool supportIsDynamic;

        ///<summary>
        /// Gets the relative velocity along the sliding direction at the wheel contact.
        ///</summary>
        public float RelativeVelocity
        {
            get
            {
                float velocity = vehicleEntity.linearVelocity.X * linearAX + vehicleEntity.linearVelocity.Y * linearAY + vehicleEntity.linearVelocity.Z * linearAZ +
                            vehicleEntity.angularVelocity.X * angularAX + vehicleEntity.angularVelocity.Y * angularAY + vehicleEntity.angularVelocity.Z * angularAZ;
                if (supportEntity != null)
                    velocity += -supportEntity.linearVelocity.X * linearAX - supportEntity.linearVelocity.Y * linearAY - supportEntity.linearVelocity.Z * linearAZ +
                                supportEntity.angularVelocity.X * angularBX + supportEntity.angularVelocity.Y * angularBY + supportEntity.angularVelocity.Z * angularBZ;
                return velocity;
            }
        }

        internal float ApplyImpulse()
        {
            //Compute relative velocity and convert to an impulse
            float lambda = RelativeVelocity * velocityToImpulse;


            //Clamp accumulated impulse
            float previousAccumulatedImpulse = accumulatedImpulse;
            float maxForce = -blendedCoefficient * wheel.suspension.accumulatedImpulse;
            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, -maxForce, maxForce);
            lambda = accumulatedImpulse - previousAccumulatedImpulse;

            //Apply the impulse
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = lambda * linearAX;
            linear.Y = lambda * linearAY;
            linear.Z = lambda * linearAZ;
            if (vehicleEntity.isDynamic)
            {
                angular.X = lambda * angularAX;
                angular.Y = lambda * angularAY;
                angular.Z = lambda * angularAZ;
                vehicleEntity.ApplyLinearImpulse(ref linear);
                vehicleEntity.ApplyAngularImpulse(ref angular);
            }
            if (supportIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = lambda * angularBX;
                angular.Y = lambda * angularBY;
                angular.Z = lambda * angularBZ;
                supportEntity.ApplyLinearImpulse(ref linear);
                supportEntity.ApplyAngularImpulse(ref angular);
            }

            return lambda;
        }

        internal void PreStep(float dt)
        {
            vehicleEntity = wheel.Vehicle.Body;
            supportEntity = wheel.SupportingEntity;
            supportIsDynamic = supportEntity != null && supportEntity.isDynamic;
            Vector3.Cross(ref wheel.worldForwardDirection, ref wheel.normal, out slidingFrictionAxis);
            float axisLength = slidingFrictionAxis.LengthSquared();
            //Safety against bad cross product
            if (axisLength < Toolbox.BigEpsilon)
            {
                Vector3.Cross(ref wheel.worldForwardDirection, ref Toolbox.UpVector, out slidingFrictionAxis);
                axisLength = slidingFrictionAxis.LengthSquared();
                if (axisLength < Toolbox.BigEpsilon)
                {
                    Vector3.Cross(ref wheel.worldForwardDirection, ref Toolbox.RightVector, out slidingFrictionAxis);
                }
            }
            slidingFrictionAxis.Normalize();

            linearAX = slidingFrictionAxis.X;
            linearAY = slidingFrictionAxis.Y;
            linearAZ = slidingFrictionAxis.Z;

            //angular A = Ra x N
            angularAX = (wheel.ra.Y * linearAZ) - (wheel.ra.Z * linearAY);
            angularAY = (wheel.ra.Z * linearAX) - (wheel.ra.X * linearAZ);
            angularAZ = (wheel.ra.X * linearAY) - (wheel.ra.Y * linearAX);

            //Angular B = N x Rb
            angularBX = (linearAY * wheel.rb.Z) - (linearAZ * wheel.rb.Y);
            angularBY = (linearAZ * wheel.rb.X) - (linearAX * wheel.rb.Z);
            angularBZ = (linearAX * wheel.rb.Y) - (linearAY * wheel.rb.X);

            //Compute inverse effective mass matrix
            float entryA, entryB;

            //these are the transformed coordinates
            float tX, tY, tZ;
            if (vehicleEntity.isDynamic)
            {
                tX = angularAX * vehicleEntity.inertiaTensorInverse.M11 + angularAY * vehicleEntity.inertiaTensorInverse.M21 + angularAZ * vehicleEntity.inertiaTensorInverse.M31;
                tY = angularAX * vehicleEntity.inertiaTensorInverse.M12 + angularAY * vehicleEntity.inertiaTensorInverse.M22 + angularAZ * vehicleEntity.inertiaTensorInverse.M32;
                tZ = angularAX * vehicleEntity.inertiaTensorInverse.M13 + angularAY * vehicleEntity.inertiaTensorInverse.M23 + angularAZ * vehicleEntity.inertiaTensorInverse.M33;
                entryA = tX * angularAX + tY * angularAY + tZ * angularAZ + vehicleEntity.inverseMass;
            }
            else
                entryA = 0;

            if (supportIsDynamic)
            {
                tX = angularBX * supportEntity.inertiaTensorInverse.M11 + angularBY * supportEntity.inertiaTensorInverse.M21 + angularBZ * supportEntity.inertiaTensorInverse.M31;
                tY = angularBX * supportEntity.inertiaTensorInverse.M12 + angularBY * supportEntity.inertiaTensorInverse.M22 + angularBZ * supportEntity.inertiaTensorInverse.M32;
                tZ = angularBX * supportEntity.inertiaTensorInverse.M13 + angularBY * supportEntity.inertiaTensorInverse.M23 + angularBZ * supportEntity.inertiaTensorInverse.M33;
                entryB = tX * angularBX + tY * angularBY + tZ * angularBZ + supportEntity.inverseMass;
            }
            else
                entryB = 0;

            velocityToImpulse = -1 / (entryA + entryB); //Softness?

            //Compute friction.
            //Which coefficient? Check velocity.
            if (Math.Abs(RelativeVelocity) < staticFrictionVelocityThreshold)
                blendedCoefficient = frictionBlender(staticCoefficient, wheel.supportMaterial.staticFriction, false, wheel);
            else
                blendedCoefficient = frictionBlender(kineticCoefficient, wheel.supportMaterial.kineticFriction, true, wheel);



        }

        internal void ExclusiveUpdate()
        {
            //Warm starting
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = accumulatedImpulse * linearAX;
            linear.Y = accumulatedImpulse * linearAY;
            linear.Z = accumulatedImpulse * linearAZ;
            if (vehicleEntity.isDynamic)
            {
                angular.X = accumulatedImpulse * angularAX;
                angular.Y = accumulatedImpulse * angularAY;
                angular.Z = accumulatedImpulse * angularAZ;
                vehicleEntity.ApplyLinearImpulse(ref linear);
                vehicleEntity.ApplyAngularImpulse(ref angular);
            }
            if (supportIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse * angularBX;
                angular.Y = accumulatedImpulse * angularBY;
                angular.Z = accumulatedImpulse * angularBZ;
                supportEntity.ApplyLinearImpulse(ref linear);
                supportEntity.ApplyAngularImpulse(ref angular);
            }
        }
    }
}