using System;
using System.Diagnostics;
using BEPUphysics.CollisionTests;
using BEPUutilities;
using Microsoft.Xna.Framework;

namespace BEPUphysics
{
    /// <summary>
    /// Contains conditional extensions to check for bad values in various structures.
    /// </summary>
    public static class PhysicsChecker
    {

        /// <summary>
        /// Checks the contact to see if it contains NaNs or infinities.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Contact contact)
        {
            contact.Normal.Validate();
            if (contact.Normal.LengthSquared() < 0.9f)
                throw new ArithmeticException("Invalid contact normal.");
            contact.Position.Validate();
            contact.PenetrationDepth.Validate();
        }


        /// <summary>
        /// Checks the contact to see if it contains NaNs or infinities.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this ContactData contact)
        {
            contact.Normal.Validate();
            contact.Position.Validate();
            contact.PenetrationDepth.Validate();
        }

    }
}
