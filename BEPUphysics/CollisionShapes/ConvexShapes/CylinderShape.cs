using System;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Symmetrical object with a circular bottom and top.
    ///</summary>
    public class CylinderShape : ConvexShape
    {
        private float halfHeight;
        private float radius;
        ///<summary>
        /// Constructs a new cylinder shape.
        ///</summary>
        ///<param name="height">Height of the cylinder.</param>
        ///<param name="radius">Radius of the cylinder.</param>
        public CylinderShape(float height, float radius)
        {
            this.halfHeight = height * .5f;
            Radius = radius;
        }

        ///<summary>
        /// Gets or sets the radius of the cylinder.
        ///</summary>
        public float Radius { get { return radius; } set { radius = value; OnShapeChanged(); } }
        ///<summary>
        /// Gets or sets the height of the cylinder.
        ///</summary>
        public float Height { get { return halfHeight * 2; } set { halfHeight = value / 2; OnShapeChanged(); } }

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
            var direction = new Vector3(o.M11, o.M21, o.M31);
            Vector3 right;
            GetLocalExtremePointWithoutMargin(ref direction, out right);

            direction = new Vector3(o.M12, o.M22, o.M32);
            Vector3 up;
            GetLocalExtremePointWithoutMargin(ref direction, out up);

            direction = new Vector3(o.M13, o.M23, o.M33);
            Vector3 backward;
            GetLocalExtremePointWithoutMargin(ref direction, out backward);

            Matrix3X3.Transform(ref right, ref o, out right);
            Matrix3X3.Transform(ref up, ref o, out up);
            Matrix3X3.Transform(ref backward, ref o, out backward);
            //These right/up/backward represent the extreme points in world space along the world space axes.

            boundingBox.Max.X = shapeTransform.Position.X + collisionMargin + right.X;
            boundingBox.Max.Y = shapeTransform.Position.Y + collisionMargin + up.Y;
            boundingBox.Max.Z = shapeTransform.Position.Z + collisionMargin + backward.Z;

            boundingBox.Min.X = shapeTransform.Position.X - collisionMargin - right.X;
            boundingBox.Min.Y = shapeTransform.Position.Y - collisionMargin - up.Y;
            boundingBox.Min.Z = shapeTransform.Position.Z - collisionMargin - backward.Z;
        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            float horizontalLengthSquared = direction.X * direction.X + direction.Z * direction.Z;
            if (horizontalLengthSquared > Toolbox.Epsilon)
            {
                float multiplier = (radius - collisionMargin) / (float)Math.Sqrt(horizontalLengthSquared);
                extremePoint = new Vector3(direction.X * multiplier, Math.Sign(direction.Y) * (halfHeight - collisionMargin), direction.Z * multiplier);
            }
            else
            {
                extremePoint = new Vector3(0, Math.Sign(direction.Y) * (halfHeight - collisionMargin), 0);
            }

        }

        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            return (float)Math.Sqrt(radius * radius + halfHeight * halfHeight);
        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            return Math.Min(radius, halfHeight);
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
            volume = ComputeVolume();

            var volumeDistribution = new Matrix3X3();

            float diagValue = (.0833333333f * Height * Height + .25f * Radius * Radius);
            volumeDistribution.M11 = diagValue;
            volumeDistribution.M22 = .5f * Radius * Radius;
            volumeDistribution.M33 = diagValue;

            return volumeDistribution;
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
            return (float)(Math.PI * Radius * Radius * Height);
        }

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<CylinderShape>(this);
        }

    }
}
