using Microsoft.Xna.Framework;

namespace BEPUphysics.MathExtensions
{
    /// <summary>
    /// 3 row, 2 column matrix.
    /// </summary>
    public struct Matrix3X2
    {
        /// <summary>
        /// Value at row 1, column 1 of the matrix.
        /// </summary>
        public float M11;

        /// <summary>
        /// Value at row 1, column 2 of the matrix.
        /// </summary>
        public float M12;

        /// <summary>
        /// Value at row 2, column 1 of the matrix.
        /// </summary>
        public float M21;

        /// <summary>
        /// Value at row 2, column 2 of the matrix.
        /// </summary>
        public float M22;

        /// <summary>
        /// Value at row 3, column 1 of the matrix.
        /// </summary>
        public float M31;

        /// <summary>
        /// Value at row 3, column 2 of the matrix.
        /// </summary>
        public float M32;


        /// <summary>
        /// Constructs a new 3 row, 2 column matrix.
        /// </summary>
        /// <param name="m11">Value at row 1, column 1 of the matrix.</param>
        /// <param name="m12">Value at row 1, column 2 of the matrix.</param>
        /// <param name="m21">Value at row 2, column 1 of the matrix.</param>
        /// <param name="m22">Value at row 2, column 2 of the matrix.</param>
        /// <param name="m31">Value at row 2, column 1 of the matrix.</param>
        /// <param name="m32">Value at row 2, column 2 of the matrix.</param>
        public Matrix3X2(float m11, float m12, float m21, float m22, float m31, float m32)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
            M31 = m31;
            M32 = m32;
        }


        /// <summary>
        /// Adds the two matrices together on a per-element basis.
        /// </summary>
        /// <param name="a">First matrix to add.</param>
        /// <param name="b">Second matrix to add.</param>
        /// <param name="result">Sum of the two matrices.</param>
        public static void Add(ref Matrix3X2 a, ref Matrix3X2 b, out Matrix3X2 result)
        {
            float m11 = a.M11 + b.M11;
            float m12 = a.M12 + b.M12;

            float m21 = a.M21 + b.M21;
            float m22 = a.M22 + b.M22;

            float m31 = a.M31 + b.M31;
            float m32 = a.M32 + b.M32;

            result.M11 = m11;
            result.M12 = m12;

            result.M21 = m21;
            result.M22 = m22;

            result.M31 = m31;
            result.M32 = m32;
        }

        /// <summary>
        /// Multiplies the two matrices.
        /// </summary>
        /// <param name="a">First matrix to multiply.</param>
        /// <param name="b">Second matrix to multiply.</param>
        /// <param name="result">Product of the multiplication.</param>
        public static void Multiply(ref Matrix3X3 a, ref Matrix3X2 b, out Matrix3X2 result)
        {
            float resultM11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
            float resultM12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;

            float resultM21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
            float resultM22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;

            float resultM31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
            float resultM32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;

            result.M11 = resultM11;
            result.M12 = resultM12;

            result.M21 = resultM21;
            result.M22 = resultM22;

            result.M31 = resultM31;
            result.M32 = resultM32;
        }

        /// <summary>
        /// Multiplies the two matrices.
        /// </summary>
        /// <param name="a">First matrix to multiply.</param>
        /// <param name="b">Second matrix to multiply.</param>
        /// <param name="result">Product of the multiplication.</param>
        public static void Multiply(ref Matrix a, ref Matrix3X2 b, out Matrix3X2 result)
        {
            float resultM11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
            float resultM12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;

            float resultM21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
            float resultM22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;

            float resultM31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
            float resultM32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;

            result.M11 = resultM11;
            result.M12 = resultM12;

            result.M21 = resultM21;
            result.M22 = resultM22;

            result.M31 = resultM31;
            result.M32 = resultM32;
        }

        /// <summary>
        /// Negates every element in the matrix.
        /// </summary>
        /// <param name="matrix">Matrix to negate.</param>
        /// <param name="result">Negated matrix.</param>
        public static void Negate(ref Matrix3X2 matrix, out Matrix3X2 result)
        {
            float m11 = -matrix.M11;
            float m12 = -matrix.M12;

            float m21 = -matrix.M21;
            float m22 = -matrix.M22;

            float m31 = -matrix.M31;
            float m32 = -matrix.M32;

            result.M11 = m11;
            result.M12 = m12;

            result.M21 = m21;
            result.M22 = m22;

            result.M31 = m31;
            result.M32 = m32;
        }

        /// <summary>
        /// Subtracts the two matrices from each other on a per-element basis.
        /// </summary>
        /// <param name="a">First matrix to subtract.</param>
        /// <param name="b">Second matrix to subtract.</param>
        /// <param name="result">Difference of the two matrices.</param>
        public static void Subtract(ref Matrix3X2 a, ref Matrix3X2 b, out Matrix3X2 result)
        {
            float m11 = a.M11 - b.M11;
            float m12 = a.M12 - b.M12;

            float m21 = a.M21 - b.M21;
            float m22 = a.M22 - b.M22;

            float m31 = a.M31 - b.M31;
            float m32 = a.M32 - b.M32;

            result.M11 = m11;
            result.M12 = m12;

            result.M21 = m21;
            result.M22 = m22;

            result.M31 = m31;
            result.M32 = m32;
        }

        /// <summary>
        /// Transforms the vector by the matrix.
        /// </summary>
        /// <param name="v">Vector2 to transform.  Considered to be a column vector for purposes of multiplication.</param>
        /// <param name="matrix">Matrix to use as the transformation.</param>
        /// <param name="result">Column vector product of the transformation.</param>
        public static void Transform(ref Vector2 v, ref Matrix3X2 matrix, out Vector3 result)
        {
#if !WINDOWS
            result = new Vector3();
#endif
            result.X = matrix.M11 * v.X + matrix.M12 * v.Y;
            result.Y = matrix.M21 * v.X + matrix.M22 * v.Y;
            result.Z = matrix.M31 * v.X + matrix.M32 * v.Y;
        }

        /// <summary>
        /// Transforms the vector by the matrix.
        /// </summary>
        /// <param name="v">Vector2 to transform.  Considered to be a row vector for purposes of multiplication.</param>
        /// <param name="matrix">Matrix to use as the transformation.</param>
        /// <param name="result">Row vector product of the transformation.</param>
        public static void Transform(ref Vector3 v, ref Matrix3X2 matrix, out Vector2 result)
        {
#if !WINDOWS
            result = new Vector2();
#endif
            result.X = v.X * matrix.M11 + v.Y * matrix.M21 + v.Z * matrix.M31;
            result.Y = v.X * matrix.M12 + v.Y * matrix.M22 + v.Z * matrix.M32;
        }


        /// <summary>
        /// Computes the transposed matrix of a matrix.
        /// </summary>
        /// <param name="matrix">Matrix to transpose.</param>
        /// <param name="result">Transposed matrix.</param>
        public static void Transpose(ref Matrix3X2 matrix, out Matrix2X3 result)
        {
            result.M11 = matrix.M11;
            result.M12 = matrix.M21;
            result.M13 = matrix.M31;

            result.M21 = matrix.M12;
            result.M22 = matrix.M22;
            result.M23 = matrix.M32;
        }


        /// <summary>
        /// Creates a string representation of the matrix.
        /// </summary>
        /// <returns>A string representation of the matrix.</returns>
        public override string ToString()
        {
            return "{" + M11 + ", " + M12 + "} " +
                   "{" + M21 + ", " + M22 + "} " +
                   "{" + M31 + ", " + M32 + "}";
        }
    }
}