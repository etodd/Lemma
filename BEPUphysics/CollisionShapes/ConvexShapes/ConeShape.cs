using System;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Symmetrical shape with a circular base and a point at the top.
    ///</summary>
    public class ConeShape : ConvexShape
    {

        float radius;
        float height;
        ///<summary>
        /// Gets or sets the height of the cone.
        ///</summary>
        public float Height
        {
            get { return height; }
            set
            {
                height = value;
                OnShapeChanged();
            }
        }
        ///<summary>
        /// Gets or sets the radius of the cone base.
        ///</summary>
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                OnShapeChanged();
            }
        }

        ///<summary>
        /// Constructs a new cone shape.
        ///</summary>
        ///<param name="height">Height of the cone.</param>
        ///<param name="radius">Radius of the cone base.</param>
        public ConeShape(float height, float radius)
        {
            this.height = height;
            Radius = radius;
        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            //Is it the tip of the cone?
            float sinThetaSquared = radius * radius / (radius * radius + height * height);
            //If d.Y * d.Y / d.LengthSquared >= sinthetaSquared
            if (direction.Y > 0 && direction.Y * direction.Y >= direction.LengthSquared() * sinThetaSquared)
            {
                extremePoint = new Vector3(0, .75f * height, 0);
                return;
            }
            //Is it a bottom edge of the cone?
            float horizontalLengthSquared = direction.X * direction.X + direction.Z * direction.Z;
            if (horizontalLengthSquared > Toolbox.Epsilon)
            {
                var radOverSigma = radius / Math.Sqrt(horizontalLengthSquared);
                extremePoint = new Vector3((float)(radOverSigma * direction.X), -.25f * height, (float)(radOverSigma * direction.Z));
            }
            else // It's pointing almost straight down...
                extremePoint = new Vector3(0, -.25f * height, 0);


        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            double denominator = radius / height;
            denominator = denominator / Math.Sqrt(denominator * denominator + 1);
            return (float)(collisionMargin + Math.Min(.25f * height, denominator * .75 * height));
        }

        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            return (float)(collisionMargin + Math.Max(.75 * Height, Math.Sqrt(.0625f * Height * Height + Radius * Radius)));
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

            //Calculate inertia tensor.
            var volumeDistribution = new Matrix3X3();
            float diagValue = (.1f * Height * Height + .15f * Radius * Radius);
            volumeDistribution.M11 = diagValue;
            volumeDistribution.M22 = .3f * Radius * Radius;
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
            return (float)(.333333 * Math.PI * Radius * Radius * Height);
        }

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<ConeShape>(this);
        }

    }
}
