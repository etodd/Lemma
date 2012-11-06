using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// Superclass of all collision shapes that are used by Entities.
    ///</summary>
    public abstract class EntityShape : CollisionShape
    {

        /// <summary>
        /// Computes the volume of the shape.
        /// </summary>
        /// <returns>Volume of the shape.</returns>
        public virtual float ComputeVolume()
        {
            ShapeDistributionInformation shapeInfo;
            ComputeDistributionInformation(out shapeInfo);
            return shapeInfo.Volume;
        }
        /// <summary>
        /// Computes the volume distribution of the shape as well as its volume.
        /// The volume distribution can be used to compute inertia tensors when
        /// paired with mass and other tuning factors.
        /// </summary>
        /// <param name="volume">Volume of the shape.</param>
        /// <returns>Volume distribution of the shape.</returns>
        public virtual Matrix3X3 ComputeVolumeDistribution(out float volume)
        {
            ShapeDistributionInformation shapeInfo;
            ComputeDistributionInformation(out shapeInfo);
            volume = shapeInfo.Volume;
            return shapeInfo.VolumeDistribution;
        }
        /// <summary>
        /// Computes the volume distribution of the shape.
        /// The volume distribution can be used to compute inertia tensors when
        /// paired with mass and other tuning factors.
        /// </summary>
        /// <returns>Volume distribution of the shape.</returns>
        public virtual Matrix3X3 ComputeVolumeDistribution()
        {
            ShapeDistributionInformation shapeInfo;
            ComputeDistributionInformation(out shapeInfo);
            return shapeInfo.VolumeDistribution;
        }
        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public virtual Vector3 ComputeCenter()
        {
            ShapeDistributionInformation shapeInfo;
            ComputeDistributionInformation(out shapeInfo);
            return shapeInfo.Center;
        }
        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.  This calculation is often associated with the 
        /// volume calculation, which is given by this method as well.
        /// </summary>
        /// <param name="volume">Volume of the shape.</param>
        /// <returns>Center of the shape.</returns>
        public virtual Vector3 ComputeCenter(out float volume)
        {
            ShapeDistributionInformation shapeInfo;
            ComputeDistributionInformation(out shapeInfo);
            volume = shapeInfo.Volume;
            return shapeInfo.Center;
        }

        /// <summary>
        /// Computes a variety of shape information all at once.
        /// </summary>
        /// <param name="shapeInfo">Properties of the shape.</param>
        public abstract void ComputeDistributionInformation(out ShapeDistributionInformation shapeInfo);

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public abstract EntityCollidable GetCollidableInstance();

    }
}
