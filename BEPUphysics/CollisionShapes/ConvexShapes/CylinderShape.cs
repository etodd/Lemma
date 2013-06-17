using System;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUutilities;

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


            Matrix3x3 o;
            Matrix3x3.CreateFromQuaternion(ref shapeTransform.Orientation, out o);
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

            Matrix3x3.Transform(ref right, ref o, out right);
            Matrix3x3.Transform(ref up, ref o, out up);
            Matrix3x3.Transform(ref backward, ref o, out backward);
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
        public override Matrix3x3 ComputeVolumeDistribution(out float volume)
        {
            volume = ComputeVolume();

            var volumeDistribution = new Matrix3x3();

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

        /// <summary>
        /// Gets the intersection between the convex shape and the ray.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="transform">Transform of the convex shape.</param>
        /// <param name="maximumLength">Maximum distance to travel in units of the ray direction's length.</param>
        /// <param name="hit">Ray hit data, if any.</param>
        /// <returns>Whether or not the ray hit the target.</returns>
        public override bool RayTest(ref Ray ray, ref RigidTransform transform, float maximumLength, out RayHit hit)
        {
            //Put the ray into local space.
            Quaternion conjugate;
            Quaternion.Conjugate(ref transform.Orientation, out conjugate);
            Ray localRay;
            Vector3.Subtract(ref ray.Position, ref transform.Position, out localRay.Position);
            Vector3.Transform(ref localRay.Position, ref conjugate, out localRay.Position);
            Vector3.Transform(ref ray.Direction, ref conjugate, out localRay.Direction);

            //Check for containment.
            if (localRay.Position.Y >= -halfHeight && localRay.Position.Y <= halfHeight && localRay.Position.X * localRay.Position.X + localRay.Position.Z * localRay.Position.Z <= radius * radius)
            {
                //It's inside!
                hit.T = 0;
                hit.Location = localRay.Position;
                hit.Normal = new Vector3(hit.Location.X, 0, hit.Location.Z);
                float normalLengthSquared = hit.Normal.LengthSquared();
                if (normalLengthSquared > 1e-9f)
                    Vector3.Divide(ref hit.Normal, (float)Math.Sqrt(normalLengthSquared), out hit.Normal);
                else
                    hit.Normal = new Vector3();
                //Pull the hit into world space.
                Vector3.Transform(ref hit.Normal, ref transform.Orientation, out hit.Normal);
                RigidTransform.Transform(ref hit.Location, ref transform, out hit.Location);
                return true;
            }

            //Project the ray direction onto the plane where the cylinder is a circle.
            //The projected ray is then tested against the circle to compute the time of impact.
            //That time of impact is used to compute the 3d hit location.
            Vector2 planeDirection = new Vector2(localRay.Direction.X, localRay.Direction.Z);
            float planeDirectionLengthSquared = planeDirection.LengthSquared();

            if (planeDirectionLengthSquared < Toolbox.Epsilon)
            {
                //The ray is nearly parallel with the axis.
                //Skip the cylinder-sides test.  We're either inside the cylinder and won't hit the sides, or we're outside
                //and won't hit the sides.  
                if (localRay.Position.Y > halfHeight)
                    goto upperTest;
                if (localRay.Position.Y < -halfHeight)
                    goto lowerTest;


                hit = new RayHit();
                return false;

            }
            Vector2 planeOrigin = new Vector2(localRay.Position.X, localRay.Position.Z);
            float dot;
            Vector2.Dot(ref planeDirection, ref planeOrigin, out dot);
            float closestToCenterT = -dot / planeDirectionLengthSquared;

            Vector2 closestPoint;
            Vector2.Multiply(ref planeDirection, closestToCenterT, out closestPoint);
            Vector2.Add(ref planeOrigin, ref closestPoint, out closestPoint);
            //How close does the ray come to the circle?
            float squaredDistance = closestPoint.LengthSquared();
            if (squaredDistance > radius * radius)
            {
                //It's too far!  The ray cannot possibly hit the capsule.
                hit = new RayHit();
                return false;
            }



            //With the squared distance, compute the distance backward along the ray from the closest point on the ray to the axis.
            float backwardsDistance = radius * (float)Math.Sqrt(1 - squaredDistance / (radius * radius));
            float tOffset = backwardsDistance / (float)Math.Sqrt(planeDirectionLengthSquared);

            hit.T = closestToCenterT - tOffset;

            //Compute the impact point on the infinite cylinder in 3d local space.
            Vector3.Multiply(ref localRay.Direction, hit.T, out hit.Location);
            Vector3.Add(ref hit.Location, ref localRay.Position, out hit.Location);

            //Is it intersecting the cylindrical portion of the capsule?
            if (hit.Location.Y <= halfHeight && hit.Location.Y >= -halfHeight && hit.T < maximumLength)
            {
                //Yup!
                hit.Normal = new Vector3(hit.Location.X, 0, hit.Location.Z);
                float normalLengthSquared = hit.Normal.LengthSquared();
                if (normalLengthSquared > 1e-9f)
                    Vector3.Divide(ref hit.Normal, (float)Math.Sqrt(normalLengthSquared), out hit.Normal);
                else
                    hit.Normal = new Vector3();
                //Pull the hit into world space.
                Vector3.Transform(ref hit.Normal, ref transform.Orientation, out hit.Normal);
                RigidTransform.Transform(ref hit.Location, ref transform, out hit.Location);
                return true;
            }

            if (hit.Location.Y < halfHeight)
                goto lowerTest;
        upperTest:
            //Nope! It may be intersecting the ends of the cylinder though.
            //We're above the cylinder, so cast a ray against the upper cap.
            if (localRay.Direction.Y > -1e-9)
            {
                //Can't hit the upper cap if the ray isn't pointing down.
                hit = new RayHit();
                return false;
            }
            float t = (halfHeight - localRay.Position.Y) / localRay.Direction.Y;
            Vector3 planeIntersection;
            Vector3.Multiply(ref localRay.Direction, t, out planeIntersection);
            Vector3.Add(ref localRay.Position, ref planeIntersection, out planeIntersection);
            if(planeIntersection.X * planeIntersection.X + planeIntersection.Z * planeIntersection.Z < radius * radius + 1e-9 && t < maximumLength)
            {
                //Pull the hit into world space.
                Vector3.Transform(ref Toolbox.UpVector, ref transform.Orientation, out hit.Normal);
                RigidTransform.Transform(ref planeIntersection, ref transform, out hit.Location);
                hit.T = t;
                return true;
            }
            //No intersection! We can't be hitting the other sphere, so it's over!
            hit = new RayHit();
            return false;

        lowerTest:
            //Is it intersecting the bottom cap?
            if (localRay.Direction.Y < 1e-9)
            {
                //Can't hit the bottom cap if the ray isn't pointing up.
                hit = new RayHit();
                return false;
            }
            t = (-halfHeight - localRay.Position.Y) / localRay.Direction.Y;
            Vector3.Multiply(ref localRay.Direction, t, out planeIntersection);
            Vector3.Add(ref localRay.Position, ref planeIntersection, out planeIntersection);
            if (planeIntersection.X * planeIntersection.X + planeIntersection.Z * planeIntersection.Z < radius * radius + 1e-9 && t < maximumLength)
            {
                //Pull the hit into world space.
                Vector3.Transform(ref Toolbox.DownVector, ref transform.Orientation, out hit.Normal);
                RigidTransform.Transform(ref planeIntersection, ref transform, out hit.Location);
                hit.T = t;
                return true;
            }
            //No intersection! We can't be hitting the other sphere, so it's over!
            hit = new RayHit();
            return false;

        }


    }
}
