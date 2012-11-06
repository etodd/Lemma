using System;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;
using BEPUphysics.Materials;
using BEPUphysics.Collidables;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Supports a Vehicle.
    /// </summary>
    public class Wheel
    {
        internal Vector3 ra, rb;

        /// <summary>
        /// Used for solver early outing.
        /// </summary>
        internal bool isActiveInSolver = true;
        internal bool isSupported;

        internal WheelBrake brake;
        internal WheelDrivingMotor drivingMotor;


        internal Vector3 localForwardDirection = new Vector3(0, 0, -1);

        internal Vector3 normal;
        internal WheelShape shape;
        internal WheelSlidingFriction slidingFriction;

        internal Collidable supportingCollidable;
        internal Material supportMaterial;
        internal Vector3 supportLocation;
        internal Entity supportingEntity;
        internal WheelSuspension suspension;
        internal Vehicle vehicle;
        internal Vector3 worldForwardDirection;


        /// <summary>
        /// Constructs a new wheel.  The WheelSlidingFriction, WheelBrake, WheelSuspension, and WheelDrivingMotor should be configured prior to using this wheel.
        /// </summary>
        /// <param name="shape">Shape of the wheel.</param>
        public Wheel(WheelShape shape)
        {
            slidingFriction = new WheelSlidingFriction(this);
            brake = new WheelBrake(this);
            suspension = new WheelSuspension(this);
            drivingMotor = new WheelDrivingMotor(this);
            Shape = shape;
        }

        /// <summary>
        /// Constructs a new wheel.
        /// </summary>
        /// <param name="shape">Shape of the wheel.</param>
        /// <param name="suspension">Springy support of the vehicle.</param>
        /// <param name="motor">Driving force for the wheel.</param>
        /// <param name="rollingFriction">Friction force resisting the forward and backward motion of the wheel.</param>
        /// <param name="slidingFriction">Friction force resisting the side to side motion of the wheel.</param>
        public Wheel(WheelShape shape, WheelSuspension suspension, WheelDrivingMotor motor, WheelBrake rollingFriction, WheelSlidingFriction slidingFriction)
        {
            Suspension = suspension;
            DrivingMotor = motor;
            Brake = rollingFriction;
            this.SlidingFriction = slidingFriction;
            Shape = shape;
        }

        internal void UpdateSolverActivity()
        {
            isActiveInSolver = true;
        }

        /// <summary>
        /// Gets the brake for this wheel.
        /// </summary>
        public WheelBrake Brake
        {
            get { return brake; }
            set
            {
                if (brake != null)
                    brake.Wheel = null;
                if (value != null)
                {
                    if (value.Wheel == null)
                    {
                        value.Wheel = this;
                    }
                    else
                        throw new InvalidOperationException("Can't use a rolling friction object that already belongs to another wheel.");
                }
                brake = value;
            }
        }

        /// <summary>
        /// Gets the motor that turns the wheel.
        /// </summary>
        public WheelDrivingMotor DrivingMotor
        {
            get { return drivingMotor; }
            set
            {
                if (drivingMotor != null)
                    drivingMotor.Wheel = null;
                if (value != null)
                {
                    if (value.Wheel == null)
                    {
                        value.Wheel = this;
                    }
                    else
                        throw new InvalidOperationException("Can't use a motor object that already belongs to another wheel.");
                }
                drivingMotor = value;
            }
        }

        /// <summary>
        /// Gets whether or not this wheel is sitting on anything.
        /// </summary>
        public bool HasSupport
        {
            get { return isSupported; }
        }

        /// <summary>
        /// Gets or sets the local space forward direction of the wheel.
        /// </summary>
        public Vector3 LocalForwardDirection
        {
            get { return localForwardDirection; }
            set
            {
                localForwardDirection = Vector3.Normalize(value);
                if (vehicle != null)
                    Matrix3X3.Transform(ref localForwardDirection, ref Vehicle.Body.orientationMatrix, out worldForwardDirection);
                else
                    worldForwardDirection = localForwardDirection;
            }
        }

        /// <summary>
        /// Gets or sets the shape of this wheel used to find collisions with the ground.
        /// </summary>
        public WheelShape Shape
        {
            get { return shape; }
            set
            {
                if (shape != null)
                    shape.Wheel = null;
                if (value != null)
                {
                    if (value.Wheel == null)
                    {
                        value.Wheel = this;
                        value.Initialize();
                    }
                    else
                        throw new InvalidOperationException("Can't use a wheel shape object that already belongs to another wheel.");
                }
                shape = value;
            }
        }

        /// <summary>
        /// Gets the sliding friction settings for this wheel.
        /// </summary>
        public WheelSlidingFriction SlidingFriction
        {
            get { return slidingFriction; }
            set
            {
                if (slidingFriction != null)
                    slidingFriction.Wheel = null;
                if (value != null)
                {
                    if (value.Wheel == null)
                    {
                        value.Wheel = this;
                    }
                    else
                        throw new InvalidOperationException("Can't use a sliding friction object that already belongs to another wheel.");
                }
                slidingFriction = value;
            }
        }

        /// <summary>
        /// Gets the current support location of this wheel.
        /// </summary>
        public Vector3 SupportLocation
        {
            get { return supportLocation; }
        }

        /// <summary>
        /// Gets the normal 
        /// </summary>
        public Vector3 SupportNormal
        {
            get { return normal; }
        }

        /// <summary>
        /// Gets the entity supporting the wheel, if any.
        /// </summary>
        public Entity SupportingEntity
        {
            get { return supportingEntity; }
        }

        /// <summary>
        /// Gets the collidable supporting the wheel, if any.
        /// </summary>
        public Collidable SupportingCollidable
        {
            get
            {
                return supportingCollidable;
            }
        }

        /// <summary>
        /// Gets the material associated with the support, if any.
        /// </summary>
        public Material SupportMaterial
        {
            get
            {
                return supportMaterial;
            }
        }

        /// <summary>
        /// Gets the suspension supporting this wheel.
        /// </summary>
        public WheelSuspension Suspension
        {
            get { return suspension; }
            set
            {
                if (suspension != null)
                    suspension.Wheel = null;
                if (value != null)
                {
                    if (value.Wheel == null)
                    {
                        value.Wheel = this;
                    }
                    else
                        throw new InvalidOperationException("Can't use a suspension object that already belongs to another wheel.");
                }
                suspension = value;
            }
        }

        /// <summary>
        /// Gets the vehicle this wheel is attached to.
        /// </summary>
        public Vehicle Vehicle
        {
            get { return vehicle; }
        }

        /// <summary>
        /// Gets or sets the world space forward direction of the wheel.
        /// </summary>
        public Vector3 WorldForwardDirection
        {
            get { return worldForwardDirection; }
            set
            {
                worldForwardDirection = Vector3.Normalize(value);
                if (vehicle != null)
                {
                    Quaternion conjugate;
                    Quaternion.Conjugate(ref Vehicle.Body.orientation, out conjugate);
                    Vector3.Transform(ref worldForwardDirection, ref conjugate, out localForwardDirection);
                }
                else
                    localForwardDirection = worldForwardDirection;
            }
        }


        internal void PreStep(float dt)
        {
            Matrix.CreateFromAxisAngle(ref suspension.localDirection, shape.steeringAngle, out shape.steeringTransform);
            Vector3.TransformNormal(ref localForwardDirection, ref shape.steeringTransform, out worldForwardDirection);
            Matrix3X3.Transform(ref worldForwardDirection, ref Vehicle.Body.orientationMatrix, out worldForwardDirection);
            if (HasSupport)
            {
                Vector3.Subtract(ref supportLocation, ref Vehicle.Body.position, out ra);
                if (supportingEntity != null)
                    Vector3.Subtract(ref supportLocation, ref SupportingEntity.position, out rb);


                //Mind the order of updating!  sliding friction must come before driving force or rolling friction
                //because it computes the sliding direction.

                suspension.isActive = true;
                suspension.numIterationsAtZeroImpulse = 0;
                suspension.solverSettings.currentIterations = 0;
                slidingFriction.isActive = true;
                slidingFriction.numIterationsAtZeroImpulse = 0;
                slidingFriction.solverSettings.currentIterations = 0;
                drivingMotor.isActive = true;
                drivingMotor.numIterationsAtZeroImpulse = 0;
                drivingMotor.solverSettings.currentIterations = 0;
                brake.isActive = true;
                brake.numIterationsAtZeroImpulse = 0;
                brake.solverSettings.currentIterations = 0;

                suspension.PreStep(dt);
                slidingFriction.PreStep(dt);
                drivingMotor.PreStep(dt);
                brake.PreStep(dt);
            }
            else
            {
                //No support, don't need any solving.
                suspension.isActive = false;
                slidingFriction.isActive = false;
                drivingMotor.isActive = false;
                brake.isActive = false;

                suspension.accumulatedImpulse = 0;
                slidingFriction.accumulatedImpulse = 0;
                drivingMotor.accumulatedImpulse = 0;
                brake.accumulatedImpulse = 0;
            }
        }

        internal void ExclusiveUpdate()
        {
            if (HasSupport)
            {
                suspension.ExclusiveUpdate();
                slidingFriction.ExclusiveUpdate();
                drivingMotor.ExclusiveUpdate();
                brake.ExclusiveUpdate();
            }
        }


        /// <summary>
        /// Applies impulses and returns whether or not this wheel should be updated more.
        /// </summary>
        /// <returns>Whether not the wheel is done updating for the frame.</returns>
        internal bool ApplyImpulse()
        {
            int numActiveConstraints = 0;
            if (suspension.isActive)
            {
                if (++suspension.solverSettings.currentIterations <= suspension.solverSettings.maximumIterations)
                    if (Math.Abs(suspension.ApplyImpulse()) < suspension.solverSettings.minimumImpulse)
                    {
                        suspension.numIterationsAtZeroImpulse++;
                        if (suspension.numIterationsAtZeroImpulse > suspension.solverSettings.minimumIterations)
                            suspension.isActive = false;
                        else
                        {
                            numActiveConstraints++;
                            suspension.numIterationsAtZeroImpulse = 0;
                        }
                    }
                    else
                    {
                        numActiveConstraints++;
                        suspension.numIterationsAtZeroImpulse = 0;
                    }
                else
                    suspension.isActive = false;
            }
            if (slidingFriction.isActive)
            {
                if (++slidingFriction.solverSettings.currentIterations <= suspension.solverSettings.maximumIterations)
                    if (Math.Abs(slidingFriction.ApplyImpulse()) < slidingFriction.solverSettings.minimumImpulse)
                    {
                        slidingFriction.numIterationsAtZeroImpulse++;
                        if (slidingFriction.numIterationsAtZeroImpulse > slidingFriction.solverSettings.minimumIterations)
                            slidingFriction.isActive = false;
                        else
                        {
                            numActiveConstraints++;
                            slidingFriction.numIterationsAtZeroImpulse = 0;
                        }
                    }
                    else
                    {
                        numActiveConstraints++;
                        slidingFriction.numIterationsAtZeroImpulse = 0;
                    }
                else
                    slidingFriction.isActive = false;
            }
            if (drivingMotor.isActive)
            {
                if (++drivingMotor.solverSettings.currentIterations <= suspension.solverSettings.maximumIterations)
                    if (Math.Abs(drivingMotor.ApplyImpulse()) < drivingMotor.solverSettings.minimumImpulse)
                    {
                        drivingMotor.numIterationsAtZeroImpulse++;
                        if (drivingMotor.numIterationsAtZeroImpulse > drivingMotor.solverSettings.minimumIterations)
                            drivingMotor.isActive = false;
                        else
                        {
                            numActiveConstraints++;
                            drivingMotor.numIterationsAtZeroImpulse = 0;
                        }
                    }
                    else
                    {
                        numActiveConstraints++;
                        drivingMotor.numIterationsAtZeroImpulse = 0;
                    }
                else
                    drivingMotor.isActive = false;
            }
            if (brake.isActive)
            {
                if (++brake.solverSettings.currentIterations <= suspension.solverSettings.maximumIterations)
                    if (Math.Abs(brake.ApplyImpulse()) < brake.solverSettings.minimumImpulse)
                    {
                        brake.numIterationsAtZeroImpulse++;
                        if (brake.numIterationsAtZeroImpulse > brake.solverSettings.minimumIterations)
                            brake.isActive = false;
                        else
                        {
                            numActiveConstraints++;
                            brake.numIterationsAtZeroImpulse = 0;
                        }
                    }
                    else
                    {
                        numActiveConstraints++;
                        brake.numIterationsAtZeroImpulse = 0;
                    }
                else
                    brake.isActive = false;
            }

            return numActiveConstraints > 0;
        }

        internal void FindSupport()
        {
            if (!(isSupported = shape.FindSupport(out supportLocation, out normal, out suspension.currentLength, out supportingCollidable, out supportingEntity, out supportMaterial)))
                suspension.currentLength = suspension.restLength;
        }

        internal void OnAdditionToSpace(ISpace space)
        {
            //Make sure it doesn't collide with anything.

            shape.OnAdditionToSpace(space);
            shape.UpdateDetectorPosition(); //Need to put the detectors in appropriate locations before adding since otherwise overloads the broadphase
            space.Add(shape.detector);
        }



        internal void OnRemovalFromSpace(ISpace space)
        {
            space.Remove(shape.detector);

            shape.OnRemovalFromSpace(space);
        }

        internal void OnAddedToVehicle(Vehicle vehicle)
        {
            this.vehicle = vehicle;
            ISpace space = (vehicle as ISpaceUpdateable).Space;
            if (space != null)
            {
                OnAdditionToSpace(space);
            }
            LocalForwardDirection = LocalForwardDirection;
            suspension.OnAdditionToVehicle();
        }

        internal void OnRemovedFromVehicle()
        {
            ISpace space = (vehicle as ISpaceUpdateable).Space;
            if (space != null)
            {
                OnRemovalFromSpace(space);
            }
            vehicle = null;
        }


        internal void UpdateAtEndOfFrame(float dt)
        {
            shape.UpdateWorldTransform();
        }

        internal void UpdateAtEndOfUpdate(float dt)
        {
            shape.UpdateSpin(dt);
        }

        internal void UpdateDuringForces(float dt)
        {
            suspension.ComputeWorldSpaceData();
            shape.UpdateDetectorPosition();
        }

    }
}