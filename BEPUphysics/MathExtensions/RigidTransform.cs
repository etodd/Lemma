using Microsoft.Xna.Framework;

namespace BEPUphysics.MathExtensions
{
    ///<summary>
    /// Transform composed of a rotation and translation.
    ///</summary>
    public struct RigidTransform
    {
        ///<summary>
        /// Translation component of the transform.
        ///</summary>
        public Vector3 Position;
        ///<summary>
        /// Rotation component of the transform.
        ///</summary>
        public Quaternion Orientation;

        ///<summary>
        /// Constructs a new rigid transform.
        ///</summary>
        ///<param name="position">Translation component of the transform.</param>
        ///<param name="orienation">Rotation component of the transform.</param>
        public RigidTransform(Vector3 position, Quaternion orienation)
        {
            Position = position;
            Orientation = orienation;
        }

        ///<summary>
        /// Constructs a new rigid transform.
        ///</summary>
        ///<param name="position">Translation component of the transform.</param>
        public RigidTransform(Vector3 position)
        {
            Position = position;
            Orientation = Quaternion.Identity;
        }

        ///<summary>
        /// Constructs a new rigid transform.
        ///</summary>
        ///<param name="orienation">Rotation component of the transform.</param>
        public RigidTransform(Quaternion orienation)
        {
            Position = new Vector3();
            Orientation = orienation;
        }

        /// <summary>
        /// Gets the orientation matrix created from the orientation of the rigid transform.
        /// </summary>
        public Matrix OrientationMatrix
        {
            get
            {
                Matrix toReturn;
                Matrix.CreateFromQuaternion(ref Orientation, out toReturn);
                return toReturn;
            }
        }
        ///<summary>
        /// Gets the 4x4 matrix created from the rigid transform.
        ///</summary>
        public Matrix Matrix
        {
            get
            {
                Matrix toReturn;
                Matrix.CreateFromQuaternion(ref Orientation, out toReturn);
                toReturn.Translation = Position;
                return toReturn;
            }
        }

      

        ///<summary>
        /// Gets the identity rigid transform.
        ///</summary>
        public static RigidTransform Identity
        {
            get
            {
                var t = new RigidTransform {Orientation = Quaternion.Identity, Position = new Vector3()};
                return t;
            }
        }

        /// <summary>
        /// Inverts a rigid transform.
        /// </summary>
        /// <param name="transform">Transform to invert.</param>
        /// <param name="inverse">Inverse of the transform.</param>
        public static void Invert(ref RigidTransform transform, out RigidTransform inverse)
        {
            Quaternion.Conjugate(ref transform.Orientation, out inverse.Orientation);
            Vector3.Transform(ref transform.Position, ref inverse.Orientation, out inverse.Position);
            Vector3.Negate(ref inverse.Position, out inverse.Position);
        }

        ///<summary>
        /// Transforms a rigid transform by another rigid transform.
        ///</summary>
        ///<param name="a">The first, "local" rigid transform.</param>
        ///<param name="b">The second, "world" rigid transform.</param>
        ///<param name="combined">Combined rigid transform.</param>
        public static void Transform(ref RigidTransform a, ref RigidTransform b, out RigidTransform combined)
        {
            Vector3 intermediate;
            Vector3.Transform(ref a.Position, ref b.Orientation, out intermediate);
            Vector3.Add(ref intermediate, ref b.Position, out combined.Position);
            Quaternion.Concatenate(ref a.Orientation, ref b.Orientation, out combined.Orientation);

        }
        ///<summary>
        /// Transforms a rigid transform by another rigid transform's inverse.
        ///</summary>
        ///<param name="a">The first rigid transform.</param>
        ///<param name="b">The second rigid transform, to be inverted.</param>
        ///<param name="combinedTransform">Combined rigid transform.</param>
        public static void TransformByInverse(ref RigidTransform a, ref RigidTransform b, out RigidTransform combinedTransform)
        {
            Invert(ref b, out combinedTransform);
            Transform(ref a, ref combinedTransform, out combinedTransform);
        }

        ///<summary>
        /// Transforms a position by a rigid transform.
        ///</summary>
        ///<param name="position">Position to transform.</param>
        ///<param name="transform">Transform to apply.</param>
        ///<param name="result">Transformed position.</param>
        public static void Transform(ref Vector3 position, ref RigidTransform transform, out Vector3 result)
        {
            Vector3 intermediate;
            Vector3.Transform(ref position, ref transform.Orientation, out intermediate);
            Vector3.Add(ref intermediate, ref transform.Position, out result);
        }


        ///<summary>
        /// Transforms a position by a rigid transform's inverse.
        ///</summary>
        ///<param name="position">Position to transform.</param>
        ///<param name="transform">Transform to invert and apply.</param>
        ///<param name="result">Transformed position.</param>
        public static void TransformByInverse(ref Vector3 position, ref RigidTransform transform, out Vector3 result)
        {
            Quaternion orientation;
            Vector3 intermediate;
            Vector3.Subtract(ref position, ref transform.Position, out intermediate);
            Quaternion.Conjugate(ref transform.Orientation, out orientation);
            Vector3.Transform(ref intermediate, ref orientation, out result);
        }


    }
}
