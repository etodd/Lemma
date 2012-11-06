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
    public class EntityMover : Updateable, IDuringForcesUpdateable
    {
        private Entity entity;


        /// <summary>
        /// Constructs a new EntityMover.
        /// </summary>
        /// <param name="e">Entity to move.</param>
        public EntityMover(Entity e)
        {
            IsUpdatedSequentially = false;
            LinearMotor = new SingleEntityLinearMotor(e, e.Position);
            Entity = e;

            LinearMotor.Settings.Mode = MotorMode.Servomechanism;
            TargetPosition = e.Position;
        }

        /// <summary>
        /// Constructs a new EntityMover.
        /// </summary>
        /// <param name="e">Entity to move.</param>
        /// <param name="linearMotor">Motor to use for linear motion if the entity is dynamic.</param>
        public EntityMover(Entity e, SingleEntityLinearMotor linearMotor)
        {
            IsUpdatedSequentially = false;
            LinearMotor = linearMotor;
            Entity = e;

            linearMotor.Entity = Entity;
            linearMotor.Settings.Mode = MotorMode.Servomechanism;
            TargetPosition = e.Position;
        }


        /// <summary>
        /// Gets or sets the entity being pushed by the entity mover.
        /// </summary>
        public Entity Entity
        {
            get { return entity; }
            set
            {
                entity = value;
                LinearMotor.Entity = value;
            }
        }

        /// <summary>
        /// Gets the linear motor used by the entity mover.
        /// When the affected entity is dynamic, it is pushed by motors.
        /// This ensures that its interactions and collisions with
        /// other entities remain stable.
        /// </summary>
        public SingleEntityLinearMotor LinearMotor { get; private set; }

        /// <summary>
        /// Gets or sets the point in the entity's local space that will be moved towards the target position.
        /// </summary>
        public Vector3 LocalOffset
        {
            get { return LinearMotor.LocalPoint; }
            set { LinearMotor.LocalPoint = value; }
        }

        /// <summary>
        /// Gets or sets the point attached to the entity in world space that will be moved towards the target position.
        /// </summary>
        public Vector3 Offset
        {
            get { return LinearMotor.Point; }
            set { LinearMotor.Point = value; }
        }

        /// <summary>
        /// Gets or sets the target location of the entity mover.
        /// </summary>
        public Vector3 TargetPosition { get; set; }

        /// <summary>
        /// Gets the angular velocity necessary to change an entity's orientation from
        /// the starting quaternion to the ending quaternion over time dt.
        /// </summary>
        /// <param name="start">Initial position.</param>
        /// <param name="end">Final position.</param>
        /// <param name="dt">Time over which the angular velocity is to be applied.</param>
        /// <returns>Angular velocity to reach the goal in time.</returns>
        public static Vector3 GetLinearVelocity(Vector3 start, Vector3 end, float dt)
        {
            Vector3 offset;
            Vector3.Subtract(ref end, ref start, out offset);
            Vector3.Divide(ref offset, dt, out offset);
            return offset;
        }

        /// <summary>
        /// Adds the motors to the space.  Called automatically.
        /// </summary>
        public override void OnAdditionToSpace(ISpace newSpace)
        {
            newSpace.Add(LinearMotor);
        }

        /// <summary>
        /// Removes the motors from the space.  Called automatically.
        /// </summary>
        public override void OnRemovalFromSpace(ISpace oldSpace)
        {
            oldSpace.Remove(LinearMotor);
        }

        /// <summary>
        /// Called automatically by the space.
        /// </summary>
        /// <param name="dt">Simulation timestep.</param>
        void IDuringForcesUpdateable.Update(float dt)
        {
            if (Entity != LinearMotor.Entity)
                throw new InvalidOperationException(
                    "EntityMover's entity differs from EntityMover's motors' entities.  Ensure that the moved entity is only changed by setting the EntityMover's entity property.");

            if (Entity.IsDynamic)
            {
                LinearMotor.IsActive = true;
                LinearMotor.Settings.Servo.Goal = TargetPosition;
            }
            else
            {
                LinearMotor.IsActive = false;
                Vector3 worldMovedPoint = Vector3.Transform(LocalOffset, entity.WorldTransform);
                Entity.LinearVelocity = GetLinearVelocity(worldMovedPoint, TargetPosition, dt);
            }
        }
    }
}