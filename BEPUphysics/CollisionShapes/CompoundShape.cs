using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUutilities.DataStructures;
using BEPUutilities;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// Contains a shape and its local transform relative to its owning compound shape.
    /// This is used to construct compound shapes.
    ///</summary>
    public struct CompoundShapeEntry
    {
        ///<summary>
        /// Local transform of the shape relative to its owning compound shape.
        ///</summary>
        public RigidTransform LocalTransform;
        /// <summary>
        /// Shape used by the compound.
        /// </summary>
        public EntityShape Shape;
        /// <summary>
        /// Weight of the entry.  This defines how much the entry contributes to its owner
        /// for the purposes of center of rotation computation.
        /// </summary>
        public float Weight;

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="localTransform">Local transform of the shape.</param>
        ///<param name="weight">Weight of the entry.  This defines how much the entry contributes to its owner
        /// for the purposes of center of rotation computation.</param>
        public CompoundShapeEntry(EntityShape shape, RigidTransform localTransform, float weight)
        {
            localTransform.Validate();
            LocalTransform = localTransform;
            Shape = shape;
            Weight = weight;
        }

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="position">Local position of the shape.</param>
        ///<param name="weight">Weight of the entry.  This defines how much the entry contributes to its owner
        /// for the purposes of center of mass and inertia computation.</param>
        public CompoundShapeEntry(EntityShape shape, Vector3 position, float weight)
        {
            position.Validate();
            LocalTransform = new RigidTransform(position);
            Shape = shape;
            Weight = weight;
        }

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="orientation">Local orientation of the shape.</param>
        ///<param name="weight">Weight of the entry.  This defines how much the entry contributes to its owner
        /// for the purposes of center of rotation computation.</param>
        public CompoundShapeEntry(EntityShape shape, Quaternion orientation, float weight)
        {
            orientation.Validate();
            LocalTransform = new RigidTransform(orientation);
            Shape = shape;
            Weight = weight;
        }
        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="weight">Weight of the entry.  This defines how much the entry contributes to its owner
        /// for the purposes of center of rotation computation.</param>
        public CompoundShapeEntry(EntityShape shape, float weight)
        {
            LocalTransform = RigidTransform.Identity;
            Shape = shape;
            Weight = weight;
        }

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="localTransform">Local transform of the shape.</param>
        public CompoundShapeEntry(EntityShape shape, RigidTransform localTransform)
        {
            localTransform.Validate();
            LocalTransform = localTransform;
            Shape = shape;
            Weight = shape.ComputeVolume();
        }

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="position">Local position of the shape.</param>
        public CompoundShapeEntry(EntityShape shape, Vector3 position)
        {
            position.Validate();
            LocalTransform = new RigidTransform(position);
            Shape = shape;
            Weight = shape.ComputeVolume();
        }

        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        ///<param name="orientation">Local orientation of the shape.</param>
        public CompoundShapeEntry(EntityShape shape, Quaternion orientation)
        {
            orientation.Validate();
            LocalTransform = new RigidTransform(orientation);
            Shape = shape;
            Weight = shape.ComputeVolume();
        }
        ///<summary>
        /// Constructs a new compound shape entry using the volume of the shape as a weight.
        ///</summary>
        ///<param name="shape">Shape to use.</param>
        public CompoundShapeEntry(EntityShape shape)
        {
            LocalTransform = RigidTransform.Identity;
            Shape = shape;
            Weight = shape.ComputeVolume();
        }
    }




    ///<summary>
    /// Shape composed of multiple other shapes.
    ///</summary>
    public class CompoundShape : EntityShape
    {
        internal RawList<CompoundShapeEntry> shapes;
        ///<summary>
        /// Gets the list of shapes in the compound shape.
        ///</summary>
        public ReadOnlyList<CompoundShapeEntry> Shapes
        {
            get
            {
                return new ReadOnlyList<CompoundShapeEntry>(shapes);
            }
        }



        ///<summary>
        /// Constructs a compound shape.
        ///</summary>
        ///<param name="shapes">Shape entries used to create the compound.</param>
        /// <param name="center">Computed center of the compound shape, using the entry weights.</param>
        public CompoundShape(IList<CompoundShapeEntry> shapes, out Vector3 center)
        {
            if (shapes.Count > 0)
            {
                center = ComputeCenter(shapes);
                this.shapes = new RawList<CompoundShapeEntry>(shapes);
                for (int i = 0; i < this.shapes.Count; i++)
                {
                    this.shapes.Elements[i].LocalTransform.Position -= center;
                }
            }
            else
            {
                throw new ArgumentException("Compound shape must have at least 1 subshape.");
            }
        }

        ///<summary>
        /// Constructs a compound shape.
        ///</summary>
        ///<param name="shapes">Shape entries used to create the compound.</param>
        public CompoundShape(IList<CompoundShapeEntry> shapes)
        {
            if (shapes.Count > 0)
            {
                Vector3 center = ComputeCenter(shapes);
                this.shapes = new RawList<CompoundShapeEntry>(shapes);
                for (int i = 0; i < this.shapes.Count; i++)
                {
                    this.shapes.Elements[i].LocalTransform.Position -= center;
                }
            }
            else
            {
                throw new ArgumentException("Compound shape must have at least 1 subshape.");
            }
        }

        #region EntityShape members and support

        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass, based on the weightings of entries in the shape.
        /// For properly calibrated compound shapes, this will return a zero vector,
        /// since the shape recenters itself on construction.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter()
        {
            float totalWeight = 0;
            var center = new Vector3();
            for (int i = 0; i < shapes.Count; i++)
            {
                totalWeight += shapes.Elements[i].Weight;
                Vector3 centerContribution;
                Vector3.Multiply(ref shapes.Elements[i].LocalTransform.Position, shapes.Elements[i].Weight, out centerContribution);
                Vector3.Add(ref center, ref centerContribution, out center);

            }
            if (totalWeight <= 0)
                throw new NotFiniteNumberException("Cannot compute center; the total weight of a compound shape must be positive.");
            Vector3.Divide(ref center, totalWeight, out center);
            center.Validate();
            return center;
        }


        ///<summary>
        /// Computes the center of a compound using its child data.
        ///</summary>
        ///<param name="childData">Child data to use to compute the center.</param>
        ///<returns>Center of the children.</returns>
        public static Vector3 ComputeCenter(IList<CompoundChildData> childData)
        {
            var center = new Vector3();
            float totalWeight = 0;
            for (int i = 0; i < childData.Count; i++)
            {
                float weight = childData[i].Entry.Weight;
                totalWeight += weight;
                center += childData[i].Entry.LocalTransform.Position * weight;
            }
            if (totalWeight <= 0)
                throw new NotFiniteNumberException("Cannot compute center; the total weight of a compound shape must be positive.");
            Vector3.Divide(ref center, totalWeight, out center);
            center.Validate();
            return center;

        }

        ///<summary>
        /// Computes the center of a compound using its child data.
        /// Children are weighted using their volumes for contribution to the center of 'mass.'
        ///</summary>
        ///<param name="childData">Child data to use to compute the center.</param>
        ///<returns>Center of the children.</returns>
        public static Vector3 ComputeCenter(IList<CompoundShapeEntry> childData)
        {
            var center = new Vector3();
            float totalWeight = 0;
            for (int i = 0; i < childData.Count; i++)
            {
                float weight = childData[i].Weight;
                totalWeight += weight;
                center += childData[i].LocalTransform.Position * weight;
            }
            if (totalWeight <= 0)
                throw new NotFiniteNumberException("Cannot compute center; the total weight of a compound shape must be positive.");
            Vector3.Divide(ref center, totalWeight, out center);
            center.Validate();
            return center;

        }

        /// <summary>
        /// Computes the volume of the shape.
        /// This is approximate; it will double count the intersection of multiple subshapes.
        /// </summary>
        /// <returns>Volume of the shape.</returns>
        public override float ComputeVolume()
        {
            float volume = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                volume += shapes.Elements[i].Shape.ComputeVolume();
            }
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
            volume = ComputeVolume();
            return ComputeVolumeDistribution();
        }

        /// <summary>
        /// Computes the volume distribution of the shape.
        /// </summary>
        /// <returns>Volume distribution of the shape.</returns>
        public override Matrix3x3 ComputeVolumeDistribution()
        {
            var volumeDistribution = new Matrix3x3();
            float totalWeight = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                totalWeight += shapes.Elements[i].Weight;
                Matrix3x3 contribution;
                GetContribution(shapes.Elements[i].Shape, ref shapes.Elements[i].LocalTransform, ref Toolbox.ZeroVector, shapes.Elements[i].Weight, out contribution);
                Matrix3x3.Add(ref contribution, ref volumeDistribution, out volumeDistribution);

            } 
            if (totalWeight <= 0)
                throw new NotFiniteNumberException("Cannot compute distribution; the total weight of a compound shape must be positive.");
            Matrix3x3.Multiply(ref volumeDistribution, 1 / totalWeight, out volumeDistribution);
            volumeDistribution.Validate();
            return volumeDistribution;
        }

        /// <summary>
        /// Computes the volume distribution and center of the shape.
        /// </summary>
        /// <param name="entries">Mass-weighted entries of the compound.</param>
        /// <param name="center">Center of the compound.</param>
        /// <returns>Volume distribution of the shape.</returns>
        public static Matrix3x3 ComputeVolumeDistribution(IList<CompoundShapeEntry> entries, out Vector3 center)
        {
            center = new Vector3();
            float totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                center += entries[i].LocalTransform.Position * entries[i].Weight;
                totalWeight += entries[i].Weight;
            }
            if (totalWeight <= 0)
                throw new NotFiniteNumberException("Cannot compute distribution; the total weight of a compound shape must be positive.");
            float totalWeightInverse = 1 / totalWeight;
            totalWeightInverse.Validate();
            center *= totalWeightInverse;

            var volumeDistribution = new Matrix3x3();
            for (int i = 0; i < entries.Count; i++)
            {
                RigidTransform transform = entries[i].LocalTransform;
                Matrix3x3 contribution;
                GetContribution(entries[i].Shape, ref transform, ref center, entries[i].Weight, out contribution);
                Matrix3x3.Add(ref volumeDistribution, ref contribution, out volumeDistribution);
            }
            Matrix3x3.Multiply(ref volumeDistribution, totalWeightInverse, out volumeDistribution);
            volumeDistribution.Validate();
            return volumeDistribution;
        }

        ///<summary>
        /// Gets the volume distribution contributed by a single shape.
        ///</summary>
        ///<param name="shape">Shape to use to compute a contribution.</param>
        ///<param name="transform">Transform of the shape.</param>
        ///<param name="center">Center to use when computing the distribution.</param>
        ///<param name="weight">Weighting to apply to the contribution.</param>
        ///<param name="contribution">Volume distribution of the contribution.</param>
        public static void GetContribution(EntityShape shape, ref RigidTransform transform, ref Vector3 center, float weight, out Matrix3x3 contribution)
        {
            contribution = shape.ComputeVolumeDistribution();
            TransformContribution(ref transform, ref center, ref contribution, weight, out contribution);
            //return TransformContribution(ref transform, ref center, ref contribution, weight);
        }



        /// <summary>
        /// Modifies a contribution using a transform, position, and weight.
        /// </summary>
        /// <param name="transform">Transform to use to modify the contribution.</param>
        /// <param name="center">Center to use to modify the contribution.</param>
        /// <param name="baseContribution">Original unmodified contribution.</param>
        /// <param name="weight">Weight of the contribution.</param>
        /// <param name="contribution">Transformed contribution.</param>
        public static void TransformContribution(ref RigidTransform transform, ref Vector3 center, ref Matrix3x3 baseContribution, float weight, out Matrix3x3 contribution)
        {
            Matrix3x3 rotation;
            Matrix3x3.CreateFromQuaternion(ref transform.Orientation, out rotation);
            Matrix3x3 temp;

            //TODO: Verify contribution

            //Do angular transformed contribution first...
            Matrix3x3.MultiplyTransposed(ref rotation, ref baseContribution, out temp);
            Matrix3x3.Multiply(ref temp, ref rotation, out temp);

            contribution = temp;

            //Now add in the offset from the origin.
            Vector3 offset;
            Vector3.Subtract(ref transform.Position, ref center, out offset);
            Matrix3x3 innerProduct;
            Matrix3x3.CreateScale(offset.LengthSquared(), out innerProduct);
            Matrix3x3 outerProduct;
            Matrix3x3.CreateOuterProduct(ref offset, ref offset, out outerProduct);

            Matrix3x3.Subtract(ref innerProduct, ref outerProduct, out temp);

            Matrix3x3.Add(ref contribution, ref temp, out contribution);
            Matrix3x3.Multiply(ref contribution, weight, out contribution);

        }


        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new CompoundCollidable(this);
        }


        /// <summary>
        /// Computes the center of the shape and its volume.
        /// </summary>
        /// <param name="volume">Volume of the compound.</param>
        /// <returns>Volume of the compound.</returns>
        public override Vector3 ComputeCenter(out float volume)
        {
            volume = ComputeVolume();
            return ComputeCenter();
        }

        /// <summary>
        /// Computes a variety of shape information all at once.
        /// </summary>
        /// <param name="shapeInfo">Properties of the shape.</param>
        public override void ComputeDistributionInformation(out ShapeDistributionInformation shapeInfo)
        {
            shapeInfo.VolumeDistribution = ComputeVolumeDistribution(out shapeInfo.Volume);
            shapeInfo.Center = ComputeCenter();
        }

        #endregion

        /// <summary>
        /// Computes and returns the volume, volume distribution, and center contributions from each child shape in the compound shape.
        /// </summary>
        /// <returns>Volume, volume distribution, and center contributions from each child shape in the compound shape.</returns>
        public ShapeDistributionInformation[] ComputeChildContributions()
        {
            var toReturn = new ShapeDistributionInformation[shapes.Count];
            for (int i = 0; i < shapes.Count; i++)
            {
                shapes.Elements[i].Shape.ComputeDistributionInformation(out toReturn[i]);
            }
            return toReturn;
        }

        /// <summary>
        /// Computes a bounding box for the shape given the specified transform.
        /// </summary>
        /// <param name="transform">Transform to apply to the shape to compute the bounding box.</param>
        /// <param name="boundingBox">Bounding box for the shape given the transform.</param>
        public override void GetBoundingBox(ref RigidTransform transform, out BoundingBox boundingBox)
        {
            RigidTransform combinedTransform;
            RigidTransform.Transform(ref shapes.Elements[0].LocalTransform, ref transform, out combinedTransform);
            shapes.Elements[0].Shape.GetBoundingBox(ref combinedTransform, out boundingBox);

            for (int i = 0; i < shapes.Count; i++)
            {
                RigidTransform.Transform(ref shapes.Elements[i].LocalTransform, ref transform, out combinedTransform);
                BoundingBox childBoundingBox;
                shapes.Elements[i].Shape.GetBoundingBox(ref combinedTransform, out childBoundingBox);
                BoundingBox.CreateMerged(ref boundingBox, ref childBoundingBox, out boundingBox);
            }
        }
    }


}
