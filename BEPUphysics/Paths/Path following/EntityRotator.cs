using System;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Paths.PathFollowing
{
    /// <summary>
    /// Pushes an entity around according to goal positions and orientations.
    /// </summary>
    public class EntityRotator : Updateable, IDuringForcesUpdateable
    {
        private Entity entity;

        /// <summary>
        /// Constructs a new EntityRotator.
        /// </summary>
        /// <param name="e">Entity to move.</param>
        public EntityRotator(Entity e)
        {
            IsUpdatedSequentially = false;
            AngularMotor = new SingleEntityAngularMotor(e);
            Entity = e;

            AngularMotor.Settings.Mode = MotorMode.Servomechanism;
            TargetOrientation = e.Orientation;
        }

        /// <summary>
        /// Constructs a new EntityRotator.
        /// </summary>
        /// <param name="e">Entity to move.</param>
        /// <param name="angularMotor">Motor to use for angular motion if the entity is dynamic.</param>
        public EntityRotator(Entity e, SingleEntityAngularMotor angularMotor)
        {
            IsUpdatedSequentially = false;
            AngularMotor = angularMotor;
            Entity = e;

            angularMotor.Entity = Entity;
            angularMotor.Settings.Mode = MotorMode.Servomechanism;
            TargetOrientation = e.Orientation;
        }


        /// <summary>
        /// Gets the angular motor used by the entity rotator.
        /// When the affected entity is dynamic, it is pushed by motors.
        /// This ensures that its interactions and collisions with
        /// other entities remain stable.
        /// </summary>
        public SingleEntityAngularMotor AngularMotor { get; private set; }

        /// <summary>
        /// Gets or sets the entity being pushed by the entity rotator.
        /// </summary>
        public Entity Entity
        {
            get { return entity; }
            set
            {
                entity = value;
                AngularMotor.Entity = value;
            }
        }

        /// <summary>
        /// Gets or sets the target orientation of the entity rotator.
        /// </summary>
        public Quaternion TargetOrientation { get; set; }

        /// <summary>
        /// Gets the angular velocity necessary to change an entity's orientation from
        /// the starting quaternion to the ending quaternion over time dt.
        /// </summary>
        /// <param name="start">Initial orientation.</param>
        /// <param name="end">Final orientation.</param>
        /// <param name="dt">Time over which the angular velocity is to be applied.</param>
        /// <returns>Angular velocity to reach the goal in time.</returns>
        public static Vector3 GetAngularVelocity(Quaternion start, Quaternion end, float dt)
        {
            //Compute the relative orientation R' between R and the target relative orientation.
            Quaternion errorOrientation;
            Quaternion.Conjugate(ref start, out errorOrientation);
            Quaternion.Multiply(ref end, ref errorOrientation, out errorOrientation);

            Vector3 axis;
            float angle;
            //Turn this into an axis-angle representation.
            Toolbox.GetAxisAngleFromQuaternion(ref errorOrientation, out axis, out angle);
            Vector3.Multiply(ref axis, angle / dt, out axis);
            return axis;
        }

        /// <summary>
        /// Adds the motors to the solver.  Called automatically.
        /// </summary>
        public override void OnAdditionToSpace(ISpace newSpace)
        {
            newSpace.Add(AngularMotor);
        }

        /// <summary>
        /// Removes the motors from the solver.  Called automatically.
        /// </summary>
        public override void OnRemovalFromSpace(ISpace oldSpace)
        {
            oldSpace.Remove(AngularMotor);
        }

        /// <summary>
        /// Called automatically by the space.
        /// </summary>
        /// <param name="dt">Simulation timestep.</param>
        void IDuringForcesUpdateable.Update(float dt)
        {
            if (Entity != AngularMotor.Entity)
                throw new InvalidOperationException(
                    "EntityRotator's entity differs from EntityRotator's motor's entities.  Ensure that the moved entity is only changed by setting the EntityRotator's entity property.");

            if (Entity.IsDynamic)
            {
                AngularMotor.IsActive = true;
                AngularMotor.Settings.Servo.Goal = TargetOrientation;
            }
            else
            {
                AngularMotor.IsActive = false;
                Entity.AngularVelocity = GetAngularVelocity(Entity.Orientation, TargetOrientation, dt);
            }
        }
    }
}