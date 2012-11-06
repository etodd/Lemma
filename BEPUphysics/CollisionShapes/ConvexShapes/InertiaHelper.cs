using System.Collections.Generic;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using BEPUphysics.DataStructures;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Helper class used to compute volume distribution information, which is in turn used to compute inertia tensor information.
    ///</summary>
    public class InertiaHelper
    {
        /// <summary>
        /// Value to scale any created entities' inertia tensors by.
        /// Larger tensors (above 1) improve stiffness of constraints and contacts, while smaller values (towards 1) are closer to 'realistic' behavior.
        /// Defaults to 2.5.
        /// </summary>
        public static float InertiaTensorScale = 2.5f;

        ///<summary>
        /// Number of samples the system takes along a side of an object's AABB when voxelizing it.
        ///</summary>
        public static int NumberOfSamplesPerDimension = 10;

        ///<summary>
        /// Computes the center of a convex shape.
        ///</summary>
        ///<param name="shape">Shape to compute the center of.</param>
        ///<returns>Center of the shape.</returns>
        public static Vector3 ComputeCenter(ConvexShape shape)
        {
            float volume;
            return ComputeCenter(shape, out volume);
        }

        ///<summary>
        /// Computes the center and volume of a convex shape.
        ///</summary>
        ///<param name="shape">Shape to compute the center of.</param>
        ///<param name="volume">Volume of the shape.</param>
        ///<returns>Center of the shape.</returns>
        public static Vector3 ComputeCenter(ConvexShape shape, out float volume)
        {
            var pointContributions = Resources.GetVectorList();
            GetPoints(shape, out volume, pointContributions);
            Vector3 center = AveragePoints(pointContributions);
            Resources.GiveBack(pointContributions);
            return center;
        }

        ///<summary>
        /// Averages together all the points in the point list.
        ///</summary>
        ///<param name="pointContributions">Point list to average.</param>
        ///<returns>Averaged point.</returns>
        public static Vector3 AveragePoints(RawList<Vector3> pointContributions)
        {
            var center = new Vector3();
            for (int i = 0; i < pointContributions.Count; i++)
            {
                center += pointContributions[i]; //Every point has equal weight.
            }
            return center / pointContributions.Count;
        }

        ///<summary>
        /// Computes the volume and volume distribution of a shape.
        ///</summary>
        ///<param name="shape">Shape to compute the volume information of.</param>
        ///<param name="volume">Volume of the shape.</param>
        ///<returns>Volume distribution of the shape.</returns>
        public static Matrix3X3 ComputeVolumeDistribution(ConvexShape shape, out float volume)
        {
            var pointContributions = Resources.GetVectorList();
            GetPoints(shape, out volume, pointContributions);
            Vector3 center = AveragePoints(pointContributions);
            Matrix3X3 volumeDistribution = ComputeVolumeDistribution(pointContributions, ref center);
            Resources.GiveBack(pointContributions);
            return volumeDistribution;
        }

        
        ///<summary>
        /// Computes the volume and volume distribution of a shape based on a given center.
        ///</summary>
        ///<param name="shape">Shape to compute the volume information of.</param>
        ///<param name="center">Location to use as the center of the shape when computing the volume distribution.</param>
        ///<param name="volume">Volume of the shape.</param>
        ///<returns>Volume distribution of the shape.</returns>
        public static Matrix3X3 ComputeVolumeDistribution(ConvexShape shape, ref Vector3 center, out float volume)
        {
            var pointContributions = Resources.GetVectorList();
            GetPoints(shape, out volume, pointContributions);
            Matrix3X3 volumeDistribution = ComputeVolumeDistribution(pointContributions, ref center);
            Resources.GiveBack(pointContributions);
            return volumeDistribution;
        }

        ///<summary>
        /// Computes a volume distribution based on a bunch of point contributions.
        ///</summary>
        ///<param name="pointContributions">Point contributions to the volume distribution.</param>
        ///<param name="center">Location to use as the center for purposes of computing point contributions.</param>
        ///<returns>Volume distribution of the point contributions.</returns>
        public static Matrix3X3 ComputeVolumeDistribution(RawList<Vector3> pointContributions, ref Vector3 center)
        {
            var volumeDistribution = new Matrix3X3();
            float pointWeight = 1f / pointContributions.Count;
            for (int i = 0; i < pointContributions.Count; i++)
            {
                Matrix3X3 contribution;
                GetPointContribution(pointWeight, ref center, pointContributions[i], out contribution);
                Matrix3X3.Add(ref volumeDistribution, ref contribution, out volumeDistribution);
            }
            return volumeDistribution;
        }



        ///<summary>
        /// Gets the point contributions within a convex shape.
        ///</summary>
        ///<param name="shape">Shape to compute the point contributions of.</param>
        ///<param name="volume">Volume of the shape.</param>
        ///<param name="outputPointContributions">Point contributions of the shape.</param>
        public static void GetPoints(ConvexShape shape, out float volume, RawList<Vector3> outputPointContributions)
        {
            RigidTransform transform = RigidTransform.Identity;
            BoundingBox boundingBox;
            shape.GetBoundingBox(ref transform, out boundingBox);

            //Find the direction which maximizes the possible hits.  Generally, this is the smallest area axis.
            //Possible options are:
            //YZ -> use X
            //XZ -> use Y
            //XY -> use Z
            Ray ray;
            float width = boundingBox.Max.X - boundingBox.Min.X;
            float height = boundingBox.Max.Y - boundingBox.Min.Y;
            float length = boundingBox.Max.Z - boundingBox.Min.Z;
            float yzArea = height * length;
            float xzArea = width * length;
            float xyArea = width * height;
            Vector3 increment1, increment2;
            float incrementMultiplier = 1f / NumberOfSamplesPerDimension;
            float maxLength;
            float rayIncrement;
            if (yzArea > xzArea && yzArea > xyArea)
            {
                //use the x axis as the direction.
                ray.Direction = Vector3.Right;
                ray.Position = new Vector3(boundingBox.Min.X, boundingBox.Min.Y + .5f * incrementMultiplier * height, boundingBox.Min.Z + .5f * incrementMultiplier * length);
                increment1 = new Vector3(0, incrementMultiplier * height, 0);
                increment2 = new Vector3(0, 0, incrementMultiplier * length);
                rayIncrement = incrementMultiplier * width;
                maxLength = width;
            }
            else if (xzArea > xyArea) //yz is not the max, given by the previous if.  Is xz or xy the max?
            {
                //use the y axis as the direction.
                ray.Direction = Vector3.Up;
                ray.Position = new Vector3(boundingBox.Min.X + .5f * incrementMultiplier * width, boundingBox.Min.Y, boundingBox.Min.Z + .5f * incrementMultiplier * length);
                increment1 = new Vector3(incrementMultiplier * width, 0, 0);
                increment2 = new Vector3(0, 0, incrementMultiplier * height);
                rayIncrement = incrementMultiplier * height;
                maxLength = height;
            }
            else
            {
                //use the z axis as the direction.
                ray.Direction = Vector3.Backward;
                ray.Position = new Vector3(boundingBox.Min.X + .5f * incrementMultiplier * width, boundingBox.Min.Y + .5f * incrementMultiplier * height, boundingBox.Min.Z);
                increment1 = new Vector3(incrementMultiplier * width, 0, 0);
                increment2 = new Vector3(0, incrementMultiplier * height, 0);
                rayIncrement = incrementMultiplier * length;
                maxLength = length;
            }


            Ray oppositeRay;
            volume = 0;
            for (int i = 0; i < NumberOfSamplesPerDimension; i++)
            {
                for (int j = 0; j < NumberOfSamplesPerDimension; j++)
                {
                    //Ray cast from one direction.  If it succeeds, try the other way.  This forms an interval in which inertia tensor contributions are contained.
                    RayHit hit;
                    if (shape.RayTest(ref ray, ref transform, maxLength, out hit))
                    {
                        Vector3.Multiply(ref ray.Direction, maxLength, out oppositeRay.Position);
                        Vector3.Add(ref oppositeRay.Position, ref ray.Position, out oppositeRay.Position);
                        Vector3.Negate(ref ray.Direction, out oppositeRay.Direction);
                        RayHit oppositeHit;
                        if (shape.RayTest(ref oppositeRay, ref transform, maxLength, out oppositeHit))
                        {
                            //It should always get here if one direction casts, but there may be numerical issues.
                            float scanVolume;
                            ScanObject(rayIncrement, maxLength, ref increment1, ref increment2, ref ray, ref hit, ref oppositeHit, outputPointContributions, out scanVolume);
                            volume += scanVolume;
                        }
                    }
                    Vector3.Add(ref ray.Position, ref increment2, out ray.Position);
                }
                Vector3.Add(ref ray.Position, ref increment1, out ray.Position);
                //Move the ray back to the starting position along the other axis.
                Vector3 subtract;
                Vector3.Multiply(ref increment2, NumberOfSamplesPerDimension, out subtract);
                Vector3.Subtract(ref ray.Position, ref subtract, out ray.Position);
            }


        }



        private static void ScanObject(float rayIncrement, float maxLength, ref Vector3 increment1, ref Vector3 increment2, ref Ray ray, ref RayHit startHit, ref RayHit endHit, RawList<Vector3> pointContributions, out float volume)
        {
            Vector3 cell;
            Vector3.Multiply(ref ray.Direction, rayIncrement, out cell);
            Vector3.Add(ref increment1, ref cell, out cell);
            Vector3.Add(ref increment2, ref cell, out cell);
            float perCellVolume = cell.X * cell.Y * cell.Z;

            volume = 0;

            for (int i = (int)(startHit.T / rayIncrement); i <= (int)((maxLength - endHit.T) / rayIncrement); i++)
            {
                Vector3 position;
                Vector3.Multiply(ref ray.Direction, (i + .5f) * rayIncrement, out position);
                Vector3.Add(ref position, ref ray.Position, out position);
                pointContributions.Add(position);
                volume += perCellVolume;
            }
        }



        ///<summary>
        /// Computes the volume contribution of a point.
        ///</summary>
        ///<param name="pointWeight">Weight of the point.</param>
        ///<param name="center">Location to use as the center for the purposes of computing the contribution.</param>
        ///<param name="p">Point to compute the contribution of.</param>
        ///<param name="contribution">Contribution of the point.</param>
        public static void GetPointContribution(float pointWeight, ref Vector3 center, Vector3 p, out Matrix3X3 contribution)
        {
            Vector3.Subtract(ref p, ref center, out p);
            float xx = pointWeight * p.X * p.X;
            float yy = pointWeight * p.Y * p.Y;
            float zz = pointWeight * p.Z * p.Z;
            contribution.M11 = yy + zz;
            contribution.M22 = xx + zz;
            contribution.M33 = xx + yy;
            contribution.M12 = -pointWeight * p.X * p.Y;
            contribution.M13 = -pointWeight * p.X * p.Z;
            contribution.M23 = -pointWeight * p.Y * p.Z;
            contribution.M21 = contribution.M12;
            contribution.M31 = contribution.M13;
            contribution.M32 = contribution.M23;
        }
    }
}
