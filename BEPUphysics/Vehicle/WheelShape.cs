using System;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Materials;
using BEPUphysics.Collidables;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Superclass for the shape of the tires of a vehicle.
    /// Responsible for figuring out where the wheel touches the ground and
    /// managing graphical properties.
    /// </summary>
    public abstract class WheelShape : ICollisionRulesOwner
    {
        private float airborneWheelAcceleration = 40;


        private float airborneWheelDeceleration = 4;
        private float brakeFreezeWheelDeceleration = 40;

        /// <summary>
        /// Collects collision pairs from the environment.
        /// </summary>
        protected internal Box detector = new Box(Vector3.Zero, 0, 0, 0);

        protected internal Matrix localGraphicTransform;
        protected float spinAngle;


        protected float spinVelocity;
        internal float steeringAngle;

        internal Matrix steeringTransform;
        protected internal Wheel wheel;

        protected internal Matrix worldTransform;

        CollisionRules collisionRules = new CollisionRules() { Group = CollisionRules.DefaultDynamicCollisionGroup};
        /// <summary>
        /// Gets or sets the collision rules used by the wheel.
        /// </summary>
        public CollisionRules CollisionRules
        {
            get { return collisionRules; }
            set { collisionRules = value; }
        }

        /// <summary>
        /// Gets or sets the graphical radius of the wheel.
        /// </summary>
        public abstract float Radius { get; set; }

        /// <summary>
        /// Gets or sets the rate at which the wheel's spinning velocity increases when accelerating and airborne.
        /// This is a purely graphical effect.
        /// </summary>
        public float AirborneWheelAcceleration
        {
            get { return airborneWheelAcceleration; }
            set { airborneWheelAcceleration = Math.Abs(value); }
        }

        /// <summary>
        /// Gets or sets the rate at which the wheel's spinning velocity decreases when the wheel is airborne and its motor is idle.
        /// This is a purely graphical effect.
        /// </summary>
        public float AirborneWheelDeceleration
        {
            get { return airborneWheelDeceleration; }
            set { airborneWheelDeceleration = Math.Abs(value); }
        }

        /// <summary>
        /// Gets or sets the rate at which the wheel's spinning velocity decreases when braking.
        /// This is a purely graphical effect.
        /// </summary>
        public float BrakeFreezeWheelDeceleration
        {
            get { return brakeFreezeWheelDeceleration; }
            set { brakeFreezeWheelDeceleration = Math.Abs(value); }
        }

        /// <summary>
        /// Gets the detector entity used by the wheelshape to collect collision pairs.
        /// </summary>
        public Box Detector
        {
            get { return detector; }
        }

        /// <summary>
        /// Gets or sets whether or not to halt the wheel spin while the WheelBrake is active.
        /// </summary>
        public bool FreezeWheelsWhileBraking { get; set; }

        /// <summary>
        /// Gets or sets the local graphic transform of the wheel shape.
        /// This transform is applied first when creating the shape's worldTransform.
        /// </summary>
        public Matrix LocalGraphicTransform
        {
            get { return localGraphicTransform; }
            set { localGraphicTransform = value; }
        }

        /// <summary>
        /// Gets or sets the current spin angle of this wheel.
        /// This changes each frame based on the relative velocity between the
        /// support and the wheel.
        /// </summary>
        public float SpinAngle
        {
            get { return spinAngle; }
            set { spinAngle = value; }
        }

        /// <summary>
        /// Gets or sets the graphical spin velocity of the wheel based on the relative velocity 
        /// between the support and the wheel.  Whenever the wheel is in contact with
        /// the ground, the spin velocity will be each frame.
        /// </summary>
        public float SpinVelocity
        {
            get { return spinVelocity; }
            set { spinVelocity = value; }
        }

        /// <summary>
        /// Gets or sets the current steering angle of this wheel.
        /// </summary>
        public float SteeringAngle
        {
            get { return steeringAngle; }
            set { steeringAngle = value; }
        }

        /// <summary>
        /// Gets the wheel object associated with this shape.
        /// </summary>
        public Wheel Wheel
        {
            get { return wheel; }
            internal set { wheel = value; }
        }

        /// <summary>
        /// Gets the world matrix of the wheel for positioning a graphic.
        /// </summary>
        public Matrix WorldTransform
        {
            get { return worldTransform; }
        }


        /// <summary>
        /// Updates the wheel's world transform for graphics.
        /// Called automatically by the owning wheel at the end of each frame.
        /// If the engine is updating asynchronously, you can call this inside of a space read buffer lock
        /// and update the wheel transforms safely.
        /// </summary>
        public abstract void UpdateWorldTransform();


        internal void OnAdditionToSpace(ISpace space)
        {
            detector.CollisionInformation.collisionRules.Specific.Add(wheel.vehicle.Body.CollisionInformation.collisionRules, CollisionRule.NoBroadPhase);
            detector.CollisionInformation.collisionRules.Personal = CollisionRule.NoNarrowPhaseUpdate;
            detector.CollisionInformation.collisionRules.group = CollisionRules.DefaultDynamicCollisionGroup;

        }

        internal void OnRemovalFromSpace(ISpace space)
        {
            detector.CollisionInformation.CollisionRules.Specific.Remove(wheel.vehicle.Body.CollisionInformation.collisionRules);
        }

        /// <summary>
        /// Updates the spin velocity and spin angle for the shape.
        /// </summary>
        /// <param name="dt">Simulation timestep.</param>
        internal void UpdateSpin(float dt)
        {
            if (wheel.HasSupport && !(wheel.brake.IsBraking && FreezeWheelsWhileBraking))
            {
                //On the ground, not braking.
                spinVelocity = wheel.drivingMotor.RelativeVelocity / Radius;
            }
            else if (wheel.HasSupport && wheel.brake.IsBraking && FreezeWheelsWhileBraking)
            {
                //On the ground, braking
                float deceleratedValue = 0;
                if (spinVelocity > 0)
                    deceleratedValue = Math.Max(spinVelocity - brakeFreezeWheelDeceleration * dt, 0);
                else if (spinVelocity < 0)
                    deceleratedValue = Math.Min(spinVelocity + brakeFreezeWheelDeceleration * dt, 0);

                spinVelocity = wheel.drivingMotor.RelativeVelocity / Radius;

                if (Math.Abs(deceleratedValue) < Math.Abs(spinVelocity))
                    spinVelocity = deceleratedValue;
            }
            else if (!wheel.HasSupport && wheel.drivingMotor.TargetSpeed != 0)
            {
                //Airborne and accelerating, increase spin velocity.
                float maxSpeed = Math.Abs(wheel.drivingMotor.TargetSpeed) / Radius;
                spinVelocity = MathHelper.Clamp(spinVelocity + Math.Sign(wheel.drivingMotor.TargetSpeed) * airborneWheelAcceleration * dt, -maxSpeed, maxSpeed);
            }
            else if (!wheel.HasSupport && wheel.Brake.IsBraking)
            {
                //Airborne and braking
                if (spinVelocity > 0)
                    spinVelocity = Math.Max(spinVelocity - brakeFreezeWheelDeceleration * dt, 0);
                else if (spinVelocity < 0)
                    spinVelocity = Math.Min(spinVelocity + brakeFreezeWheelDeceleration * dt, 0);
            }
            else if (!wheel.HasSupport)
            {
                //Just idly slowing down.
                if (spinVelocity > 0)
                    spinVelocity = Math.Max(spinVelocity - airborneWheelDeceleration * dt, 0);
                else if (spinVelocity < 0)
                    spinVelocity = Math.Min(spinVelocity + airborneWheelDeceleration * dt, 0);
            }
            spinAngle += spinVelocity * dt;
        }

        /// <summary>
        /// Finds a supporting entity, the contact location, and the contact normal.
        /// </summary>
        /// <param name="location">Contact point between the wheel and the support.</param>
        /// <param name="normal">Contact normal between the wheel and the support.</param>
        /// <param name="suspensionLength">Length of the suspension at the contact.</param>
        /// <param name="supportCollidable">Collidable supporting the wheel, if any.</param>
        /// <param name="entity">Entity supporting the wheel, if any.</param>
        /// <param name="material">Material of the support.</param>
        /// <returns>Whether or not any support was found.</returns>
        protected internal abstract bool FindSupport(out Vector3 location, out Vector3 normal, out float suspensionLength, out Collidable supportCollidable, out Entity entity, out Material material);

        /// <summary>
        /// Initializes the detector entity and any other necessary logic.
        /// </summary>
        protected internal abstract void Initialize();

        /// <summary>
        /// Updates the position of the detector before each step.
        /// </summary>
        protected internal abstract void UpdateDetectorPosition();

    }
}