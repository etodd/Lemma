using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace BEPUphysics.MathExtensions
{
    /// <summary>
    /// Contains conditional extensions to check for bad values in various structures.
    /// </summary>
    public static class MathChecker
    {
        /// <summary>
        /// Checks a single float for validity.  Separate from the extension method to avoid throwing exceptions deep in a call tree.
        /// </summary>
        /// <param name="f">Value to validate.</param>
        /// <returns>True if the value is invalid, false if it is valid.</returns>
        private static bool IsInvalid(float f)
        {
            return float.IsNaN(f) || float.IsInfinity(f);
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this float f)
        {
            if (IsInvalid(f))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Vector3 v)
        {
            if (IsInvalid(v.X) || IsInvalid(v.Y) || IsInvalid(v.Z))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Matrix2X2 m)
        {
            if (IsInvalid(m.M11) || IsInvalid(m.M12) ||
                IsInvalid(m.M21) || IsInvalid(m.M22))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Matrix3X2 m)
        {
            if (IsInvalid(m.M11) || IsInvalid(m.M12) ||
                IsInvalid(m.M21) || IsInvalid(m.M22) ||
                IsInvalid(m.M31) || IsInvalid(m.M32))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Matrix2X3 m)
        {
            if (IsInvalid(m.M11) || IsInvalid(m.M12) || IsInvalid(m.M13) ||
                IsInvalid(m.M21) || IsInvalid(m.M22) || IsInvalid(m.M23))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Matrix3X3 m)
        {
            if (IsInvalid(m.M11) || IsInvalid(m.M12) || IsInvalid(m.M13) ||
                IsInvalid(m.M21) || IsInvalid(m.M22) || IsInvalid(m.M23) ||
                IsInvalid(m.M31) || IsInvalid(m.M32) || IsInvalid(m.M33))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Matrix m)
        {
            if (IsInvalid(m.M11) || IsInvalid(m.M12) || IsInvalid(m.M13) || IsInvalid(m.M14) ||
                IsInvalid(m.M21) || IsInvalid(m.M22) || IsInvalid(m.M23) || IsInvalid(m.M24) ||
                IsInvalid(m.M31) || IsInvalid(m.M32) || IsInvalid(m.M33) || IsInvalid(m.M34) ||
                IsInvalid(m.M41) || IsInvalid(m.M42) || IsInvalid(m.M43) || IsInvalid(m.M44))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this Quaternion q)
        {
            if (IsInvalid(q.W) || IsInvalid(q.X) || IsInvalid(q.Y) || IsInvalid(q.Z))
            {
                throw new NotFiniteNumberException("Invalid value.");
            }
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this RigidTransform r)
        {
            r.Position.Validate();
            r.Orientation.Validate();
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this AffineTransform a)
        {
            a.LinearTransform.Validate();
            a.Translation.Validate();
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this BoundingBox b)
        {
            b.Min.Validate();
            b.Max.Validate();
        }

        /// <summary>
        /// Checks the value to see if it is a NaN or infinite.  If it is, an exception is thrown.
        /// This is only run when the CHECKMATH symbol is defined.
        /// </summary>
        [Conditional("CHECKMATH")]
        public static void Validate(this BoundingSphere b)
        {
            b.Center.Validate();
            if(IsInvalid(b.Radius))
                throw new NotFiniteNumberException("Invalid value.");
        }

    }
}
