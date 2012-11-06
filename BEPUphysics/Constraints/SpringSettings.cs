using System;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Contains the error reduction factor and softness of a constraint.
    /// These can be used to make the same behaviors as the stiffness and damping constants,
    /// but may provide a more intuitive representation for rigid constraints.
    /// </summary>
    public class SpringAdvancedSettings
    {
        internal float errorReductionFactor = .1f;

        internal float softness = .00001f;

        internal bool useAdvancedSettings;

        /// <summary>
        /// Gets or sets the error reduction parameter of the spring.
        /// </summary>
        public float ErrorReductionFactor
        {
            get { return errorReductionFactor; }
            set { errorReductionFactor = MathHelper.Clamp(value, 0, 1); }
        }

        /// <summary>
        /// Gets or sets the softness of the joint.  Higher values allow the constraint to be violated more.
        /// </summary>
        public float Softness
        {
            get { return softness; }
            set { softness = MathHelper.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets whether or not to use the advanced settings.
        /// If this is set to true, the errorReductionFactor and softness will be used instead
        /// of the stiffness constant and damping constant.
        /// </summary>
        public bool UseAdvancedSettings
        {
            get { return useAdvancedSettings; }
            set { useAdvancedSettings = value; }
        }
    }


    /// <summary>
    /// Specifies the way in which a constraint's spring component behaves.
    /// </summary>
    public class SpringSettings
    {
        private readonly SpringAdvancedSettings advanced = new SpringAdvancedSettings();

        internal float dampingConstant = 90000;
        internal float stiffnessConstant = 600000;

        /// <summary>
        /// Gets an object containing the solver's direct view of the spring behavior.
        /// </summary>
        public SpringAdvancedSettings Advanced
        {
            get { return advanced; }
        }

        /// <summary>
        /// Gets or sets the damping constant of this spring.  Higher values reduce oscillation more.
        /// </summary>
        public float DampingConstant
        {
            get { return dampingConstant; }
            set { dampingConstant = MathHelper.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets the spring constant of this spring.  Higher values make the spring stiffer.
        /// </summary>
        public float StiffnessConstant
        {
            get { return stiffnessConstant; }
            set { stiffnessConstant = Math.Max(0, value); }
        }

        /// <summary>
        /// Computes the error reduction parameter and softness of a constraint based on its constants.
        /// Automatically called by constraint presteps to compute their per-frame values.
        /// </summary>
        /// <param name="dt">Simulation timestep.</param>
        /// <param name="errorReduction">Error reduction factor to use this frame.</param>
        /// <param name="softness">Adjusted softness of the constraint for this frame.</param>
        public void ComputeErrorReductionAndSoftness(float dt, out float errorReduction, out float softness)
        {
            if (advanced.useAdvancedSettings)
            {
                errorReduction = advanced.errorReductionFactor / dt;
                softness = advanced.softness / dt;
            }
            else
            {
                if (stiffnessConstant == 0 && dampingConstant == 0)
                    throw new InvalidOperationException("Constraints cannot have both 0 stiffness and 0 damping.");
                errorReduction = stiffnessConstant / (dt * stiffnessConstant + dampingConstant);
                softness = 1 / (dt * (dt * stiffnessConstant + dampingConstant));
            }
        }
    }
}