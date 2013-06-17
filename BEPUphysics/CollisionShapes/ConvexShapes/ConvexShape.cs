using System;
using System.Diagnostics;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using Microsoft.Xna.Framework;
using BEPUutilities;
using BEPUphysics.Settings;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Superclass of convex collision shapes.
    ///</summary>
    public abstract class ConvexShape : EntityShape
    {
        protected internal float collisionMargin = CollisionDetectionSettings.DefaultMargin;
        ///<summary>
        /// Collision margin of the convex shape.  The margin is a small spherical expansion around
        /// entities which allows specialized collision detection algorithms to be used.
        /// It's recommended that this be left unchanged.
        ///</summary>
        public float CollisionMargin
        {
            get
            {
                return collisionMargin;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Collision margin must be nonnegative..");
                collisionMargin = value;
                OnShapeChanged();
            }
        }

        protected internal float minimumRadius;
        /// <summary>
        /// Gets or sets the minimum radius of the collidable's shape.  This is initialized to a value that is
        /// guaranteed to be equal to or smaller than the actual minimum radius.  When setting this property,
        /// ensure that the inner sphere formed by the new minimum radius is fully contained within the shape.
        /// </summary>
        public float MinimumRadius { get { return minimumRadius; } set { minimumRadius = value; } }

        protected internal float maximumRadius;
        /// <summary>
        /// Gets the maximum radius of the collidable's shape.  This is initialized to a value that is
        /// guaranteed to be equal to or larger than the actual maximum radius.
        /// </summary>
        public float MaximumRadius { get { return maximumRadius; } }

        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public abstract void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint);

        ///<summary>
        /// Gets the extreme point of the shape in world space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        /// <param name="shapeTransform">Transform to use for the shape.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public void GetExtremePointWithoutMargin(Vector3 direction, ref RigidTransform shapeTransform, out Vector3 extremePoint)
        {
            Quaternion conjugate;
            Quaternion.Conjugate(ref shapeTransform.Orientation, out conjugate);
            Vector3.Transform(ref direction, ref conjugate, out direction);
            GetLocalExtremePointWithoutMargin(ref direction, out extremePoint);

            Vector3.Transform(ref extremePoint, ref shapeTransform.Orientation, out extremePoint);
            Vector3.Add(ref extremePoint, ref shapeTransform.Position, out extremePoint);
        }

        ///<summary>
        /// Gets the extreme point of the shape in world space in a given direction with margin expansion.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        /// <param name="shapeTransform">Transform to use for the shape.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public void GetExtremePoint(Vector3 direction, ref RigidTransform shapeTransform, out Vector3 extremePoint)
        {
            GetExtremePointWithoutMargin(direction, ref shapeTransform, out extremePoint);

            float directionLength = direction.LengthSquared();
            if (directionLength > Toolbox.Epsilon)
            {
                Vector3.Multiply(ref direction, collisionMargin / (float)Math.Sqrt(directionLength), out direction);
                Vector3.Add(ref extremePoint, ref direction, out extremePoint);
            }

        }

        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction with margin expansion.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public void GetLocalExtremePoint(Vector3 direction, out Vector3 extremePoint)
        {
            GetLocalExtremePointWithoutMargin(ref direction, out extremePoint);

            float directionLength = direction.LengthSquared();
            if (directionLength > Toolbox.Epsilon)
            {
                Vector3.Multiply(ref direction, collisionMargin / (float)Math.Sqrt(directionLength), out direction);
                Vector3.Add(ref extremePoint, ref direction, out extremePoint);
            }
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
            Matrix3x3 o;
            Matrix3x3.CreateFromQuaternion(ref shapeTransform.Orientation, out o);
            //Sample the local directions from the orientation matrix, implicitly transposed.

            Vector3 right;
            var direction = new Vector3(o.M11, o.M21, o.M31);
            GetLocalExtremePointWithoutMargin(ref direction, out right);

            Vector3 left;
            direction = new Vector3(-o.M11, -o.M21, -o.M31);
            GetLocalExtremePointWithoutMargin(ref direction, out left);

            Vector3 up;
            direction = new Vector3(o.M12, o.M22, o.M32);
            GetLocalExtremePointWithoutMargin(ref direction, out up);

            Vector3 down;
            direction = new Vector3(-o.M12, -o.M22, -o.M32);
            GetLocalExtremePointWithoutMargin(ref direction, out down);

            Vector3 backward;
            direction = new Vector3(o.M13, o.M23, o.M33);
            GetLocalExtremePointWithoutMargin(ref direction, out backward);

            Vector3 forward;
            direction = new Vector3(-o.M13, -o.M23, -o.M33);
            GetLocalExtremePointWithoutMargin(ref direction, out forward);


            Matrix3x3.Transform(ref right, ref o, out right);
            Matrix3x3.Transform(ref left, ref o, out left);
            Matrix3x3.Transform(ref up, ref o, out up);
            Matrix3x3.Transform(ref down, ref o, out down);
            Matrix3x3.Transform(ref backward, ref o, out backward);
            Matrix3x3.Transform(ref forward, ref o, out forward);

            //These right/up/backward represent the extreme points in world space along the world space axes.

            boundingBox.Max.X = shapeTransform.Position.X + collisionMargin + right.X;
            boundingBox.Max.Y = shapeTransform.Position.Y + collisionMargin + up.Y;
            boundingBox.Max.Z = shapeTransform.Position.Z + collisionMargin + backward.Z;

            boundingBox.Min.X = shapeTransform.Position.X - collisionMargin + left.X;
            boundingBox.Min.Y = shapeTransform.Position.Y - collisionMargin + down.Y;
            boundingBox.Min.Z = shapeTransform.Position.Z - collisionMargin + forward.Z;

        }

        /// <summary>
        /// Gets the intersection between the convex shape and the ray.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="transform">Transform of the convex shape.</param>
        /// <param name="maximumLength">Maximum distance to travel in units of the ray direction's length.</param>
        /// <param name="hit">Ray hit data, if any.</param>
        /// <returns>Whether or not the ray hit the target.</returns>
        public virtual bool RayTest(ref Ray ray, ref RigidTransform transform, float maximumLength, out RayHit hit)
        {
            
            //RayHit newHit;
            //bool newBool = GJKToolbox.RayCast(ray, this, ref transform, maximumLength, out newHit);
            //RayHit oldHit;
            //bool oldBool = OldGJKVerifier.RayCastGJK(ray.Position, ray.Direction, maximumLength, this, transform, out oldHit.Location, out oldHit.Normal, out oldHit.T);
            //bool mprBool = MPRToolbox.RayCast(ray, maximumLength, this, ref transform, out hit);
            ////if (newBool != oldBool || ((newBool && oldBool) && Vector3.DistanceSquared(newHit.Location, hit.Location) > .01f))
            ////    Debug.WriteLine("break.");
            //return mprBool;

            //if (GJKToolbox.RayCast(ray, this, ref transform, maximumLength, out hit))
            //{
            //    //GJK toolbox doesn't normalize the hit normal; it's unnecessary for some other systems so it just saves on time.
            //    //It would be nice if ray tests always normalized it though.
            //    float length = hit.Normal.LengthSquared();
            //    if (length > Toolbox.Epsilon)
            //        Vector3.Divide(ref hit.Normal, (float) Math.Sqrt(length), out hit.Normal);
            //    else
            //        hit.Normal = new Vector3();
            //    return true;
            //}

            //return false;

            return MPRToolbox.RayCast(ray, maximumLength, this, ref transform, out hit);
        }

        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter()
        {
            return InertiaHelper.ComputeCenter(this);
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
            return InertiaHelper.ComputeCenter(this, out volume);
        }

        /// <summary>
        /// Computes the volume of the shape.
        /// </summary>
        /// <returns>Volume of the shape.</returns>
        public override float ComputeVolume()
        {
            float volume;
            ComputeVolumeDistribution(out volume);
            return volume;
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
            return InertiaHelper.ComputeVolumeDistribution(this, out volume);
        }

        protected override void OnShapeChanged()
        {
            minimumRadius = ComputeMinimumRadius();
            maximumRadius = ComputeMaximumRadius();
            base.OnShapeChanged();
        }

        /// <summary>
        /// Computes the volume distribution of the shape.
        /// The volume distribution can be used to compute inertia tensors when
        /// paired with mass and other tuning factors.
        /// </summary>
        /// <returns>Volume distribution of the shape.</returns>
        public override Matrix3x3 ComputeVolumeDistribution()
        {
            float volume;
            return ComputeVolumeDistribution(out volume);
        }

        public override void ComputeDistributionInformation(out ShapeDistributionInformation shapeInfo)
        {
            shapeInfo.VolumeDistribution = ComputeVolumeDistribution(out shapeInfo.Volume);
            shapeInfo.Center = ComputeCenter();
        }

        /// <summary>
        /// Computes a bounding box for the shape and expands it.
        /// </summary>
        /// <param name="transform">Transform to use to position the shape.</param>
        /// <param name="sweep">Extra to add to the bounding box.</param>
        /// <param name="boundingBox">Expanded bounding box.</param>
        public void GetSweptBoundingBox(ref RigidTransform transform, ref Vector3 sweep, out BoundingBox boundingBox)
        {
            GetBoundingBox(ref transform, out boundingBox);
            Toolbox.ExpandBoundingBox(ref boundingBox, ref sweep);

        }

        /// <summary>
        /// Gets the bounding box of the convex shape transformed first into world space, and then into the local space of another affine transform.
        /// </summary>
        /// <param name="shapeTransform">Transform to use to put the shape into world space.</param>
        /// <param name="spaceTransform">Used as the frame of reference to compute the bounding box.
        /// In effect, the shape is transformed by the inverse of the space transform to compute its bounding box in local space.</param>
        /// <param name="sweep">Vector to expand the bounding box with in local space.</param>
        /// <param name="boundingBox">Bounding box in the local space.</param>
        public void GetSweptLocalBoundingBox(ref RigidTransform shapeTransform, ref AffineTransform spaceTransform, ref Vector3 sweep, out BoundingBox boundingBox)
        {
            GetLocalBoundingBox(ref shapeTransform, ref spaceTransform, out boundingBox);
            Vector3 expansion;
            Matrix3x3.TransformTranspose(ref sweep, ref spaceTransform.LinearTransform, out expansion);
            Toolbox.ExpandBoundingBox(ref boundingBox, ref expansion);
        }

        //Transform the convex into the space of something else.
        /// <summary>
        /// Gets the bounding box of the convex shape transformed first into world space, and then into the local space of another affine transform.
        /// </summary>
        /// <param name="shapeTransform">Transform to use to put the shape into world space.</param>
        /// <param name="spaceTransform">Used as the frame of reference to compute the bounding box.
        /// In effect, the shape is transformed by the inverse of the space transform to compute its bounding box in local space.</param>
        /// <param name="boundingBox">Bounding box in the local space.</param>
        public void GetLocalBoundingBox(ref RigidTransform shapeTransform, ref AffineTransform spaceTransform, out BoundingBox boundingBox)
        {
#if !WINDOWS
            boundingBox = new BoundingBox();
#endif
            //TODO: This method peforms quite a few sqrts because the collision margin can get scaled, and so cannot be applied as a final step.
            //There should be a better way to do this.
            //Additionally, this bounding box is not consistent in all cases with the post-add version.  Adding the collision margin at the end can
            //slightly overestimate the size of a margin expanded shape at the corners, which is fine (and actually important for the box-box special case).

            //Move forward into convex's space, backwards into the new space's local space.
            AffineTransform transform;
            AffineTransform.Invert(ref spaceTransform, out transform);
            AffineTransform.Multiply(ref shapeTransform, ref transform, out transform);

            //Sample the local directions from the orientation matrix, implicitly transposed.

            Vector3 right;
            var direction = new Vector3(transform.LinearTransform.M11, transform.LinearTransform.M21, transform.LinearTransform.M31);
            GetLocalExtremePoint(direction, out right);

            Vector3 left;
            direction = new Vector3(-transform.LinearTransform.M11, -transform.LinearTransform.M21, -transform.LinearTransform.M31);
            GetLocalExtremePoint(direction, out left);

            Vector3 up;
            direction = new Vector3(transform.LinearTransform.M12, transform.LinearTransform.M22, transform.LinearTransform.M32);
            GetLocalExtremePoint(direction, out up);

            Vector3 down;
            direction = new Vector3(-transform.LinearTransform.M12, -transform.LinearTransform.M22, -transform.LinearTransform.M32);
            GetLocalExtremePoint(direction, out down);

            Vector3 backward;
            direction = new Vector3(transform.LinearTransform.M13, transform.LinearTransform.M23, transform.LinearTransform.M33);
            GetLocalExtremePoint(direction, out backward);

            Vector3 forward;
            direction = new Vector3(-transform.LinearTransform.M13, -transform.LinearTransform.M23, -transform.LinearTransform.M33);
            GetLocalExtremePoint(direction, out forward);


            //This could be optimized.  Unnecessary transformation information gets computed.
            Matrix3x3.Transform(ref right, ref transform.LinearTransform, out right);
            Matrix3x3.Transform(ref left, ref transform.LinearTransform, out left);
            Matrix3x3.Transform(ref up, ref transform.LinearTransform, out up);
            Matrix3x3.Transform(ref down, ref transform.LinearTransform, out down);
            Matrix3x3.Transform(ref backward, ref transform.LinearTransform, out backward);
            Matrix3x3.Transform(ref forward, ref transform.LinearTransform, out forward);

            //These right/up/backward represent the extreme points in world space along the world space axes.
            boundingBox.Max.X = transform.Translation.X + right.X;
            boundingBox.Max.Y = transform.Translation.Y + up.Y;
            boundingBox.Max.Z = transform.Translation.Z + backward.Z;

            boundingBox.Min.X = transform.Translation.X + left.X;
            boundingBox.Min.Y = transform.Translation.Y + down.Y;
            boundingBox.Min.Z = transform.Translation.Z + forward.Z;
        }


        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public abstract float ComputeMinimumRadius();
        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public abstract float ComputeMaximumRadius();
    }
}
