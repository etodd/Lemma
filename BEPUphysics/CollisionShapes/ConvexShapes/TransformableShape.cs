using System;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{

    ///<summary>
    /// Shape which can take any convex shape and use a linear transform to shear, scale, and rotate it.
    ///</summary>
    public class TransformableShape : ConvexShape
    {
        protected ConvexShape shape;
        ///<summary>
        /// Gets or sets the convex shape to be transformed.
        ///</summary>
        public ConvexShape Shape
        {
            get
            {
                return shape;
            }
            set
            {
                shape = value;
                OnShapeChanged();
            }
        }

        protected Matrix3X3 transform;
        ///<summary>
        /// Gets or sets the linear transform used to transform the convex shape.
        ///</summary>
        public Matrix3X3 Transform
        {
            get
            {
                return transform;
            }
            set
            {
                transform = value;
                OnShapeChanged();
            }
        }

        ///<summary>
        /// Constructs a new transformable shape.
        ///</summary>
        ///<param name="shape">Base shape to transform.</param>
        ///<param name="transform">Transform to use.</param>
        public TransformableShape(ConvexShape shape, Matrix3X3 transform)
        {
            this.shape = shape;
            Transform = transform;

        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            Vector3 d;
            Matrix3X3.TransformTranspose(ref direction, ref transform, out d);
            shape.GetLocalExtremePoint(d, out extremePoint);
            Matrix3X3.Transform(ref extremePoint, ref transform, out extremePoint);
        }


        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            //This will overestimate the actual maximum radius, but such is the defined behavior of the ComputeMaximumRadius function.  It's not exact; it's an upper bound on the actual maximum.
            RigidTransform transform = RigidTransform.Identity;
            BoundingBox boundingBox;
            GetBoundingBox(ref transform, out boundingBox);
            Vector3 diameter;
            Vector3.Subtract(ref boundingBox.Max, ref boundingBox.Min, out diameter);
            return diameter.Length();
            
        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            //Sample the shape in directions pointing to the vertices of a regular tetrahedron.
            Vector3 a, b, c, d;
            var direction = new Vector3(1, 1, 1);
            GetLocalExtremePointWithoutMargin(ref direction, out a);
            direction = new Vector3(-1, -1, 1);
            GetLocalExtremePointWithoutMargin(ref direction, out b);
            direction = new Vector3(-1, 1, -1);
            GetLocalExtremePointWithoutMargin(ref direction, out c);
            direction = new Vector3(1, -1, -1);
            GetLocalExtremePointWithoutMargin(ref direction, out d);
            Vector3 ab, cb, ac, ad, cd;
            Vector3.Subtract(ref b, ref a, out ab);
            Vector3.Subtract(ref b, ref c, out cb);
            Vector3.Subtract(ref c, ref a, out ac);
            Vector3.Subtract(ref d, ref a, out ad);
            Vector3.Subtract(ref d, ref c, out cd);
            //Find normals of triangles: ABC, CBD, ACD, ADB
            Vector3 nABC, nCBD, nACD, nADB;
            Vector3.Cross(ref ac, ref ab, out nABC);
            Vector3.Cross(ref cd, ref cb, out nCBD);
            Vector3.Cross(ref ad, ref ac, out nACD);
            Vector3.Cross(ref ab, ref ad, out nADB);
            //Find distances to planes.
            float dABC, dCBD, dACD, dADB;
            Vector3.Dot(ref a, ref nABC, out dABC);
            Vector3.Dot(ref c, ref nCBD, out dCBD);
            Vector3.Dot(ref a, ref nACD, out dACD);
            Vector3.Dot(ref a, ref nADB, out dADB);
            dABC /= nABC.Length();
            dCBD /= nCBD.Length();
            dACD /= nACD.Length();
            dADB /= nADB.Length();

            return collisionMargin + Math.Min(dABC, Math.Min(dCBD, Math.Min(dACD, dADB)));
        }


        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter()
        {
            //All convexes under acceptably normal circumstances are centered on the local origin.
            //The linear transform performs rotation, scaling, and shearing.  All of these operations will not move the origin.
            return Vector3.Zero;
        }

        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.  This calculation is often associated with the 
        /// volume calculation, which is given by this method as well.
        /// </summary>
        /// <param name="volume">Volume of the shape.</param>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter(out float volume)
        {
            volume = ComputeVolume();
            return ComputeCenter();
        }

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<TransformableShape>(this);
        }

    }
}
