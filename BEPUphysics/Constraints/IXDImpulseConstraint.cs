using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Implemented by solver updateables which have a one dimensional impulse.
    /// </summary>
    public interface I1DImpulseConstraint
    {
        /// <summary>
        /// Gets the current relative velocity of the constraint.
        /// Computed based on the current connection velocities and jacobians.
        /// </summary>
        float RelativeVelocity { get; }

        /// <summary>
        /// Gets the total impulse a constraint has applied.
        /// </summary>
        float TotalImpulse { get; }
    }

    /// <summary>
    /// Implemented by solver updateables which have a one dimensional impulse.
    /// </summary>
    public interface I1DImpulseConstraintWithError : I1DImpulseConstraint
    {
        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        float Error { get; }
    }

    /// <summary>
    /// Implemented by solver updateables which have a two dimensional impulse.
    /// </summary>
    public interface I2DImpulseConstraint
    {
        /// <summary>
        /// Gets the current relative velocity of the constraint.
        /// Computed based on the current connection velocities and jacobians.
        /// </summary>
        Vector2 RelativeVelocity { get; }

        /// <summary>
        /// Gets the total impulse a constraint has applied.
        /// </summary>
        Vector2 TotalImpulse { get; }
    }

    /// <summary>
    /// Implemented by solver updateables which have a two dimensional impulse.
    /// </summary>
    public interface I2DImpulseConstraintWithError : I2DImpulseConstraint
    {
        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        Vector2 Error { get; }
    }

    /// <summary>
    /// Implemented by solver updateables which have a three dimensional impulse.
    /// </summary>
    public interface I3DImpulseConstraint
    {
        /// <summary>
        /// Gets the current relative velocity of the constraint.
        /// Computed based on the current connection velocities and jacobians.
        /// </summary>
        Vector3 RelativeVelocity { get; }

        /// <summary>
        /// Gets the total impulse a constraint has applied.
        /// </summary>
        Vector3 TotalImpulse { get; }
    }

    /// <summary>
    /// Implemented by solver updateables which have a three dimensional impulse.
    /// </summary>
    public interface I3DImpulseConstraintWithError : I3DImpulseConstraint
    {
        /// <summary>
        /// Gets the current constraint error.
        /// </summary>
        Vector3 Error { get; }
    }
}