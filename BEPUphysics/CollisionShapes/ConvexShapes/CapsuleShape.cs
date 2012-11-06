using System;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Sphere-expanded line segment.  Another way of looking at it is a cylinder with half-spheres on each end.
    ///</summary>
    public class CapsuleShape : ConvexShape
    {
        ///<summary>
        /// Constructs a new capsule shape.
        ///</summary>
        ///<param name="length">Length of the capsule's inner line segment.</param>
        ///<param name="radius">Radius to expand the line segment width.</param>
        public CapsuleShape(float length, float radius)
        {
            halfLength = length * .5f;
            Radius = radius;
        }

        float halfLength;
        ///<summary>
        /// Gets or sets the length of the capsule's inner line segment.
        ///</summary>
        public float Length
        {
            get
            {
                return halfLength * 2;
            }
            set
            {
                halfLength = value / 2;
                OnShapeChanged();
            }
        }

        //This is a convenience method.  People expect to see a 'radius' of some kind.
        ///<summary>
        /// Gets or sets the radius of the capsule.
        ///</summary>
        public float Radius { get { return collisionMargin; } set { CollisionMargin = value; } }


        public override void GetBoundingBox(ref RigidTransform shapeTransform, out BoundingBox boundingBox)
        {
#if !WINDOWS
            boundingBox = new BoundingBox();
#endif
            var upExtreme = new Vector3(0, halfLength, 0);
            var downExtreme = new Vector3(0, -halfLength, 0);

            Vector3.Transform(ref upExtreme, ref shapeTransform.Orientation, out upExtreme);
            Vector3.Transform(ref downExtreme, ref shapeTransform.Orientation, out downExtreme);

            if (upExtreme.X > downExtreme.X)
            {
                boundingBox.Max.X = upExtreme.X;
                boundingBox.Min.X = downExtreme.X;
            }
            else
            {
                boundingBox.Max.X = downExtreme.X;
                boundingBox.Min.X = upExtreme.X;
            }

            if (upExtreme.Y > downExtreme.Y)
            {
                boundingBox.Max.Y = upExtreme.Y;
                boundingBox.Min.Y = downExtreme.Y;
            }
            else
            {
                boundingBox.Max.Y = downExtreme.Y;
                boundingBox.Min.Y = upExtreme.Y;
            }

            if (upExtreme.Z > downExtreme.Z)
            {
                boundingBox.Max.Z = upExtreme.Z;
                boundingBox.Min.Z = downExtreme.Z;
            }
            else
            {
                boundingBox.Max.Z = downExtreme.Z;
                boundingBox.Min.Z = upExtreme.Z;
            }


            boundingBox.Min.X += shapeTransform.Position.X - collisionMargin;
            boundingBox.Min.Y += shapeTransform.Position.Y - collisionMargin;
            boundingBox.Min.Z += shapeTransform.Position.Z - collisionMargin;
            boundingBox.Max.X += shapeTransform.Position.X + collisionMargin;
            boundingBox.Max.Y += shapeTransform.Position.Y + collisionMargin;
            boundingBox.Max.Z += shapeTransform.Position.Z + collisionMargin;
        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            if (direction.Y > 0)
                extremePoint = new Vector3(0, halfLength, 0);
            else if (direction.Y < 0)
                extremePoint = new Vector3(0, -halfLength, 0);
            else
                extremePoint = Toolbox.ZeroVector;
        }


        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            return Radius;
        }

        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            return halfLength + Radius;
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
            float effectiveLength = Length + Radius / 2;
            float diagValue = (.0833333333f * effectiveLength * effectiveLength + .25f * Radius * Radius);
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
            return (float)(Math.PI * Radius * Radius * Length + 1.333333 * Math.PI * Radius * Radius * Radius);
        }


        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<CapsuleShape>(this);
        }

    }
}
