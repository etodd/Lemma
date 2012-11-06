using System;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Convex shape with width, length, and height.
    ///</summary>
    public class BoxShape : ConvexShape
    {
        internal float halfWidth;
        internal float halfHeight;
        internal float halfLength;


        ///<summary>
        /// Constructs a new box shape.
        ///</summary>
        ///<param name="width">Width of the box.</param>
        ///<param name="height">Height of the box.</param>
        ///<param name="length">Length of the box.</param>
        public BoxShape(float width, float height, float length)
        {
            halfWidth = width * .5f;
            halfHeight = height * .5f;
            halfLength = length * .5f;
            OnShapeChanged();
        }

        /// <summary>
        /// Width of the box divided by two.
        /// </summary>
        public float HalfWidth
        {
            get { return halfWidth; }
            set { halfWidth = value; OnShapeChanged(); }
        }

        /// <summary>
        /// Height of the box divided by two.
        /// </summary>
        public float HalfHeight
        {
            get { return halfHeight; }
            set { halfHeight = value; OnShapeChanged(); }
        }

        /// <summary>
        /// Length of the box divided by two.
        /// </summary>
        public float HalfLength
        {
            get { return halfLength; }
            set { halfLength = value; OnShapeChanged(); }
        }

        /// <summary>
        /// Width of the box.
        /// </summary>
        public float Width
        {
            get { return halfWidth * 2; }
            set { halfWidth = value / 2; OnShapeChanged(); }
        }

        /// <summary>
        /// Height of the box.
        /// </summary>
        public float Height
        {
            get { return halfHeight * 2; }
            set { halfHeight = value / 2; OnShapeChanged(); }
        }

        /// <summary>
        /// Length of the box.
        /// </summary>
        public float Length
        {
            get { return halfLength * 2; }
            set { halfLength = value / 2; OnShapeChanged(); }
        }


        /// <summary>
        /// Gets the bounding box of the shape given a transform.
        /// </summary>
        /// <param name="shapeTransform">Transform to use.</param>
        /// <param name="boundingBox">Bounding box of the transformed shape.</param>
        public override void GetBoundingBox(ref RigidTransform shapeTransform, out BoundingBox boundingBox)
        {
#if !WINDOWS
            boundingBox = new BoundingBox();
#endif

            Matrix3X3 o;
            Matrix3X3.CreateFromQuaternion(ref shapeTransform.Orientation, out o);
            //Sample the local directions from the orientation matrix, implicitly transposed.
            //Notice only three directions are used.  Due to box symmetry, 'left' is just -right.
            var right = new Vector3(Math.Sign(o.M11) * halfWidth, Math.Sign(o.M21) * halfHeight, Math.Sign(o.M31) * halfLength);

            var up = new Vector3(Math.Sign(o.M12) * halfWidth, Math.Sign(o.M22) * halfHeight, Math.Sign(o.M32) * halfLength);

            var backward = new Vector3(Math.Sign(o.M13) * halfWidth, Math.Sign(o.M23) * halfHeight, Math.Sign(o.M33) * halfLength);

            Matrix3X3.Transform(ref right, ref o, out right);
            Matrix3X3.Transform(ref up, ref o, out up);
            Matrix3X3.Transform(ref backward, ref o, out backward);
            //These right/up/backward represent the extreme points in world space along the world space axes.

            boundingBox.Max.X = shapeTransform.Position.X + right.X;
            boundingBox.Max.Y = shapeTransform.Position.Y + up.Y;
            boundingBox.Max.Z = shapeTransform.Position.Z + backward.Z;

            boundingBox.Min.X = shapeTransform.Position.X - right.X;
            boundingBox.Min.Y = shapeTransform.Position.Y - up.Y;
            boundingBox.Min.Z = shapeTransform.Position.Z - backward.Z;

        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            extremePoint = new Vector3(Math.Sign(direction.X) * (halfWidth - collisionMargin), Math.Sign(direction.Y) * (halfHeight - collisionMargin), Math.Sign(direction.Z) * (halfLength - collisionMargin));
        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            return Math.Min(halfWidth, Math.Min(halfHeight, halfLength));
        }

        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            return (float)Math.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight + halfLength * halfLength);
        }

        /// <summary>
        /// Computes the volume distribution of the shape as well as its volume.
        /// The volume distribution can be used to compute inertia tensors when
        /// paired with mass and other tuning factors.
        /// </summary>
        /// <param name="volume">Volume of the shape.</param>
        /// <returns>Volume distribution of the shape.</returns>
        public override Matrix3X3 ComputeVolumeDistribution(out float volume)
        {
            var volumeDistribution = new Matrix3X3();
            float halfWidthSquared = halfWidth * halfWidth;
            float halfHeightSquared = halfHeight * halfHeight;
            float halfLengthSquared = halfLength * halfLength;
            const float inv3 = 1 / 3f;

            volumeDistribution.M11 = (halfHeightSquared + halfLengthSquared) * inv3;
            volumeDistribution.M22 = (halfWidthSquared + halfLengthSquared) * inv3;
            volumeDistribution.M33 = (halfWidthSquared + halfHeightSquared) * inv3;
            volume = ComputeVolume();
            return volumeDistribution;
        }




        /// <summary>
        /// Gets the intersection between the box and the ray.
        /// </summary>
        /// <param name="ray">Ray to test against the box.</param>
        /// <param name="transform">Transform of the shape.</param>
        /// <param name="maximumLength">Maximum distance to travel in units of the direction vector's length.</param>
        /// <param name="hit">Hit data for the raycast, if any.</param>
        /// <returns>Whether or not the ray hit the target.</returns>
        public override bool RayTest(ref Ray ray, ref RigidTransform transform, float maximumLength, out RayHit hit)
        {
            hit = new RayHit();

            Quaternion conjugate;
            Quaternion.Conjugate(ref transform.Orientation, out conjugate);
            Vector3 localOrigin;
            Vector3.Subtract(ref ray.Position, ref transform.Position, out localOrigin);
            Vector3.Transform(ref localOrigin, ref conjugate, out localOrigin);
            Vector3 localDirection;
            Vector3.Transform(ref ray.Direction, ref conjugate, out localDirection);
            Vector3 normal = Toolbox.ZeroVector;
            float temp, tmin = 0, tmax = maximumLength;

            if (Math.Abs(localDirection.X) < Toolbox.Epsilon && (localOrigin.X < -halfWidth || localOrigin.X > halfWidth))
                return false;
            float inverseDirection = 1 / localDirection.X;
            float t1 = (-halfWidth - localOrigin.X) * inverseDirection;
            float t2 = (halfWidth - localOrigin.X) * inverseDirection;
            var tempNormal = new Vector3(-1, 0, 0);
            if (t1 > t2)
            {
                temp = t1;
                t1 = t2;
                t2 = temp;
                tempNormal *= -1;
            }
            temp = tmin;
            tmin = Math.Max(tmin, t1);
            if (temp != tmin)
                normal = tempNormal;
            tmax = Math.Min(tmax, t2);
            if (tmin > tmax)
                return false;
            if (Math.Abs(localDirection.Y) < Toolbox.Epsilon && (localOrigin.Y < -halfHeight || localOrigin.Y > halfHeight))
                return false;
            inverseDirection = 1 / localDirection.Y;
            t1 = (-halfHeight - localOrigin.Y) * inverseDirection;
            t2 = (halfHeight - localOrigin.Y) * inverseDirection;
            tempNormal = new Vector3(0, -1, 0);
            if (t1 > t2)
            {
                temp = t1;
                t1 = t2;
                t2 = temp;
                tempNormal *= -1;
            }
            temp = tmin;
            tmin = Math.Max(tmin, t1);
            if (temp != tmin)
                normal = tempNormal;
            tmax = Math.Min(tmax, t2);
            if (tmin > tmax)
                return false;
            if (Math.Abs(localDirection.Z) < Toolbox.Epsilon && (localOrigin.Z < -halfLength || localOrigin.Z > halfLength))
                return false;
            inverseDirection = 1 / localDirection.Z;
            t1 = (-halfLength - localOrigin.Z) * inverseDirection;
            t2 = (halfLength - localOrigin.Z) * inverseDirection;
            tempNormal = new Vector3(0, 0, -1);
            if (t1 > t2)
            {
                temp = t1;
                t1 = t2;
                t2 = temp;
                tempNormal *= -1;
            }
            temp = tmin;
            tmin = Math.Max(tmin, t1);
            if (temp != tmin)
                normal = tempNormal;
            tmax = Math.Min(tmax, t2);
            if (tmin > tmax)
                return false;
            hit.T = tmin;
            Vector3.Multiply(ref ray.Direction, tmin, out hit.Location);
            Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
            Vector3.Transform(ref normal, ref transform.Orientation, out normal);
            hit.Normal = normal;
            return true;
        }


        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter()
        {
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
        /// Computes the volume of the shape.
        /// </summary>
        /// <returns>Volume of the shape.</returns>
        public override float ComputeVolume()
        {
            return 8 * halfWidth * halfLength * halfHeight;
        }

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<BoxShape>(this);
        }

    }
}
