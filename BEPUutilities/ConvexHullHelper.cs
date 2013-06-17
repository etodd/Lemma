using System;
using System.Collections.Generic;
using BEPUutilities;
using BEPUutilities.DataStructures;
using BEPUutilities.ResourceManagement;
using Microsoft.Xna.Framework;

namespace BEPUutilities
{
    /// <summary>
    /// Processes vertex data into convex hulls.
    /// </summary>
    public static partial class ConvexHullHelper
    {

        /// <summary>
        /// Identifies the indices of points in a set which are on the outer convex hull of the set.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="indices">List of indices composing the triangulated surface of the convex hull.
        /// Each group of 3 indices represents a triangle on the surface of the hull.</param>
        public static void GetConvexHull(IList<Vector3> points, IList<int> indices)
        {
            var rawPoints = CommonResources.GetVectorList();
            var rawIndices = CommonResources.GetIntList();
            rawPoints.AddRange(points);
            GetConvexHull(rawPoints, rawIndices);
            CommonResources.GiveBack(rawPoints);
            for (int i = 0; i < rawIndices.Count; i++)
            {
                indices.Add(rawIndices[i]);
            }
            CommonResources.GiveBack(rawIndices);
        }

        /// <summary>
        /// Identifies the points on the surface of hull.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="outputSurfacePoints">Unique points on the surface of the convex hull.</param>
        public static void GetConvexHull(IList<Vector3> points, IList<Vector3> outputSurfacePoints)
        {
            var rawPoints = CommonResources.GetVectorList();
            rawPoints.AddRange(points);
            GetConvexHull(rawPoints, outputSurfacePoints);
            CommonResources.GiveBack(rawPoints);
        }

        /// <summary>
        /// Identifies the points on the surface of hull.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="outputSurfacePoints">Unique points on the surface of the convex hull.</param>
        public static void GetConvexHull(RawList<Vector3> points, IList<Vector3> outputSurfacePoints)
        {
            var indices = CommonResources.GetIntList();
            GetConvexHull(points, indices, outputSurfacePoints);
            CommonResources.GiveBack(indices);
        }

        /// <summary>
        /// Identifies the points on the surface of hull.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="outputIndices">List of indices composing the triangulated surface of the convex hull.
        /// Each group of 3 indices represents a triangle on the surface of the hull.</param>
        /// <param name="outputSurfacePoints">Unique points on the surface of the convex hull.</param>
        public static void GetConvexHull(IList<Vector3> points, IList<int> outputIndices, IList<Vector3> outputSurfacePoints)
        {
            var rawPoints = CommonResources.GetVectorList();
            var rawIndices = CommonResources.GetIntList();
            rawPoints.AddRange(points);
            GetConvexHull(rawPoints, rawIndices, outputSurfacePoints);
            CommonResources.GiveBack(rawPoints);
            for (int i = 0; i < rawIndices.Count; i++)
            {
                outputIndices.Add(rawIndices[i]);
            }
            CommonResources.GiveBack(rawIndices);
        }

        /// <summary>
        /// Identifies the points on the surface of hull.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="outputTriangleIndices">List of indices composing the triangulated surface of the convex hull.
        /// Each group of 3 indices represents a triangle on the surface of the hull.</param>
        /// <param name="outputSurfacePoints">Unique points on the surface of the convex hull.</param>
        public static void GetConvexHull(RawList<Vector3> points, RawList<int> outputTriangleIndices, IList<Vector3> outputSurfacePoints)
        {
            GetConvexHull(points, outputTriangleIndices);

            var alreadyContainedIndices = CommonResources.GetIntSet();

            for (int i = outputTriangleIndices.Count - 1; i >= 0; i--)
            {
                int index = outputTriangleIndices[i];
                if (!alreadyContainedIndices.Contains(index))
                {
                    outputSurfacePoints.Add(points[index]);
                    alreadyContainedIndices.Add(index);
                }
            }

            CommonResources.GiveBack(alreadyContainedIndices);


        }

        /// <summary>
        /// Identifies the indices of points in a set which are on the outer convex hull of the set.
        /// </summary>
        /// <param name="points">List of points in the set.</param>
        /// <param name="outputTriangleIndices">List of indices composing the triangulated surface of the convex hull.
        /// Each group of 3 indices represents a triangle on the surface of the hull.</param>
        public static void GetConvexHull(RawList<Vector3> points, RawList<int> outputTriangleIndices)
        {
            if (points.Count == 0)
            {
                throw new ArgumentException("Point set must have volume.");
            }
            RawList<int> outsidePoints = CommonResources.GetIntList();
            if (outsidePoints.Capacity < points.Count - 4)
                outsidePoints.Capacity = points.Count - 4;

            //Build the initial tetrahedron.
            //It will also give us the location of a point which is guaranteed to be within the
            //final convex hull.  We can use this point to calibrate the winding of triangles.
            //A set of outside point candidates (all points other than those composing the tetrahedron) will be returned in the outsidePoints list.
            //That list will then be further pruned by the RemoveInsidePoints call.
            Vector3 insidePoint;
            ComputeInitialTetrahedron(points, outsidePoints, outputTriangleIndices, out insidePoint);

            //Compute outside points.
            RemoveInsidePoints(points, outputTriangleIndices, outsidePoints);

            var edges = CommonResources.GetIntList();
            var toRemove = CommonResources.GetIntList();
            var newTriangles = CommonResources.GetIntList();

            //We're now ready to begin the main loop.
            while (outsidePoints.Count > 0)
            {
                //While the convex hull is incomplete...
                for (int k = 0; k < outputTriangleIndices.Count; k += 3)
                {
                    //Find the normal of the triangle
                    Vector3 normal;
                    FindNormal(outputTriangleIndices, points, k, out normal);

                    //Get the furthest point in the direction of the normal.
                    int maxIndexInOutsideList = GetExtremePoint(ref normal, points, outsidePoints);
                    int maxIndex = outsidePoints.Elements[maxIndexInOutsideList];
                    Vector3 maximum = points.Elements[maxIndex];

                    //If the point is beyond the current triangle, continue.
                    Vector3 offset;
                    Vector3.Subtract(ref maximum, ref points.Elements[outputTriangleIndices.Elements[k]], out offset);
                    float dot;
                    Vector3.Dot(ref normal, ref offset, out dot);
                    if (dot > 0)
                    {
                        //It's been picked! Remove the maximum point from the outside.
                        outsidePoints.FastRemoveAt(maxIndexInOutsideList);
                        //Remove any triangles that can see the point, including itself!
                        edges.Clear();
                        toRemove.Clear();
                        for (int n = outputTriangleIndices.Count - 3; n >= 0; n -= 3)
                        {
                            //Go through each triangle, if it can be seen, delete it and use maintainEdge on its edges.
                            if (IsTriangleVisibleFromPoint(outputTriangleIndices, points, n, ref maximum))
                            {
                                //This triangle can see it!
                                //TODO: CONSIDER CONSISTENT WINDING HAPPYTIMES
                                MaintainEdge(outputTriangleIndices[n], outputTriangleIndices[n + 1], edges);
                                MaintainEdge(outputTriangleIndices[n], outputTriangleIndices[n + 2], edges);
                                MaintainEdge(outputTriangleIndices[n + 1], outputTriangleIndices[n + 2], edges);
                                //Because fast removals are being used, the order is very important.
                                //It's pulling indices in from the end of the list in order, and also ensuring
                                //that we never issue a removal order beyond the end of the list.
                                outputTriangleIndices.FastRemoveAt(n + 2);
                                outputTriangleIndices.FastRemoveAt(n + 1);
                                outputTriangleIndices.FastRemoveAt(n);

                            }
                        }
                        //Create new triangles.
                        for (int n = 0; n < edges.Count; n += 2)
                        {
                            //For each edge, create a triangle with the extreme point.
                            newTriangles.Add(edges[n]);
                            newTriangles.Add(edges[n + 1]);
                            newTriangles.Add(maxIndex);
                        }
                        //Only verify the windings of the new triangles.
                        VerifyWindings(newTriangles, points, ref insidePoint);
                        outputTriangleIndices.AddRange(newTriangles);
                        newTriangles.Clear();

                        //Remove all points from the outsidePoints if they are inside the polyhedron
                        RemoveInsidePoints(points, outputTriangleIndices, outsidePoints);

                        //The list has been significantly messed with, so restart the loop.
                        break;
                    }
                }
            }


            CommonResources.GiveBack(outsidePoints);
            CommonResources.GiveBack(edges);
            CommonResources.GiveBack(toRemove);
            CommonResources.GiveBack(newTriangles);
        }

        private static void MaintainEdge(int a, int b, RawList<int> edges)
        {
            bool contained = false;
            int index = 0;
            for (int k = 0; k < edges.Count; k += 2)
            {
                if ((edges[k] == a && edges[k + 1] == b) || (edges[k] == b && edges[k + 1] == a))
                {
                    contained = true;
                    index = k;
                }
            }
            //If it isn't present, add it to the edge list.
            if (!contained)
            {
                edges.Add(a);
                edges.Add(b);
            }
            else
            {
                //If it is present, that means both edge-connected triangles were deleted now, so get rid of it.
                edges.FastRemoveAt(index + 1);
                edges.FastRemoveAt(index);
            }
        }

        private static int GetExtremePoint(ref Vector3 direction, RawList<Vector3> points, RawList<int> outsidePoints)
        {
            float maximumDot = -float.MaxValue;
            int extremeIndex = 0;
            for (int i = 0; i < outsidePoints.Count; ++i)
            {
                float dot;
                Vector3.Dot(ref points.Elements[outsidePoints[i]], ref direction, out dot);
                if (dot > maximumDot)
                {
                    maximumDot = dot;
                    extremeIndex = i;
                }
            }
            return extremeIndex;
        }

        private static void GetExtremePoints(ref Vector3 direction, RawList<Vector3> points, out float maximumDot, out float minimumDot, out int maximumIndex, out int minimumIndex)
        {
            maximumIndex = 0;
            minimumIndex = 0;

            float dot;
            Vector3.Dot(ref points.Elements[0], ref direction, out dot);
            minimumDot = dot;
            maximumDot = dot;
            for (int i = 1; i < points.Count; ++i)
            {
                Vector3.Dot(ref points.Elements[i], ref direction, out dot);
                if (dot > maximumDot)
                {
                    maximumDot = dot;
                    maximumIndex = i;
                }
                else if (dot < minimumDot)
                {
                    minimumDot = dot;
                    minimumIndex = i;
                }
            }
        }

        private static void ComputeInitialTetrahedron(RawList<Vector3> points, RawList<int> outsidePointCandidates, RawList<int> triangleIndices, out Vector3 centroid)
        {
            //Find four points on the hull.
            //We'll start with using the x axis to identify two points on the hull.
            int a, b, c, d;
            Vector3 direction;
            //Find the extreme points along the x axis.
            float minimumX = float.MaxValue, maximumX = -float.MaxValue;
            int minimumXIndex = 0, maximumXIndex = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                var v = points.Elements[i];
                if (v.X > maximumX)
                {
                    maximumX = v.X;
                    maximumXIndex = i;
                }
                else if (v.X < minimumX)
                {
                    minimumX = v.X;
                    minimumXIndex = i;
                }
            }
            a = minimumXIndex;
            b = maximumXIndex;
            //Check for redundancies..
            if (a == b)
                throw new ArgumentException("Point set is degenerate; convex hulls must have volume.");

            //Now, use a second axis perpendicular to the two points we found.
            Vector3 ab;
            Vector3.Subtract(ref points.Elements[b], ref points.Elements[a], out ab);
            Vector3.Cross(ref ab, ref Toolbox.UpVector, out direction);
            if (direction.LengthSquared() < Toolbox.Epsilon)
                Vector3.Cross(ref ab, ref Toolbox.RightVector, out direction);
            float minimumDot, maximumDot;
            int minimumIndex, maximumIndex;
            GetExtremePoints(ref direction, points, out maximumDot, out minimumDot, out maximumIndex, out minimumIndex);
            //Compare the location of the extreme points to the location of the axis.
            float dot;
            Vector3.Dot(ref direction, ref points.Elements[a], out dot);
            //Use the point further from the axis.
            if (Math.Abs(dot - minimumDot) > Math.Abs(dot - maximumDot))
            {
                //In this case, we should use the minimum index.
                c = minimumIndex;
            }
            else
            {
                //In this case, we should use the maximum index.
                c = maximumIndex;
            }

            //Check for redundancies..
            if (a == c || b == c)
                throw new ArgumentException("Point set is degenerate; convex hulls must have volume.");

            //Use a third axis perpendicular to the plane defined by the three unique points a, b, and c.
            Vector3 ac;
            Vector3.Subtract(ref points.Elements[c], ref points.Elements[a], out ac);
            Vector3.Cross(ref ab, ref ac, out direction);

            GetExtremePoints(ref direction, points, out maximumDot, out minimumDot, out maximumIndex, out minimumIndex);
            //Compare the location of the extreme points to the location of the plane.
            Vector3.Dot(ref direction, ref points.Elements[a], out dot);
            //Use the point further from the plane. 
            if (Math.Abs(dot - minimumDot) > Math.Abs(dot - maximumDot))
            {
                //In this case, we should use the minimum index.
                d = minimumIndex;
            }
            else
            {
                //In this case, we should use the maximum index.
                d = maximumIndex;
            }

            //Check for redundancies..
            if (a == d || b == d || c == d)
                throw new ArgumentException("Point set is degenerate; convex hulls must have volume.");

            //Add the triangles.
            triangleIndices.Add(a);
            triangleIndices.Add(b);
            triangleIndices.Add(c);

            triangleIndices.Add(a);
            triangleIndices.Add(b);
            triangleIndices.Add(d);

            triangleIndices.Add(a);
            triangleIndices.Add(c);
            triangleIndices.Add(d);

            triangleIndices.Add(b);
            triangleIndices.Add(c);
            triangleIndices.Add(d);

            //The centroid is guaranteed to be within the convex hull.  It will be used to verify the windings of triangles throughout the hull process.
            Vector3.Add(ref points.Elements[a], ref points.Elements[b], out centroid);
            Vector3.Add(ref centroid, ref points.Elements[c], out centroid);
            Vector3.Add(ref centroid, ref points.Elements[d], out centroid);
            Vector3.Multiply(ref centroid, 0.25f, out centroid);

            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                var vA = points.Elements[triangleIndices.Elements[i]];
                var vB = points.Elements[triangleIndices.Elements[i + 1]];
                var vC = points.Elements[triangleIndices.Elements[i + 2]];

                //Check the signed volume of a parallelepiped with the edges of this triangle and the centroid.
                Vector3 cross;
                Vector3.Subtract(ref vB, ref vA, out ab);
                Vector3.Subtract(ref vC, ref vA, out ac);
                Vector3.Cross(ref ac, ref ab, out cross);
                Vector3 offset;
                Vector3.Subtract(ref vA, ref centroid, out offset);
                float volume;
                Vector3.Dot(ref offset, ref cross, out volume);
                //This volume/cross product could also be used to check for degeneracy, but we already tested for that.
                if (Math.Abs(volume) < Toolbox.BigEpsilon)
                {
                    throw new ArgumentException("Point set is degenerate; convex hulls must have volume.");
                }
                if (volume < 0)
                {
                    //If the signed volume is negative, that means the triangle's winding is opposite of what we want.
                    //Flip it around!
                    var temp = triangleIndices.Elements[i];
                    triangleIndices.Elements[i] = triangleIndices.Elements[i + 1];
                    triangleIndices.Elements[i + 1] = temp;
                }
            }

            //Points which belong to the tetrahedra are guaranteed to be 'in' the convex hull. Do not allow them to be considered.
            var tetrahedronIndices = CommonResources.GetIntList();
            tetrahedronIndices.Add(a);
            tetrahedronIndices.Add(b);
            tetrahedronIndices.Add(c);
            tetrahedronIndices.Add(d);
            //Sort the indices to allow a linear time loop.
            Array.Sort(tetrahedronIndices.Elements, 0, 4);
            int tetrahedronIndex = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                if (tetrahedronIndex < 4 && i == tetrahedronIndices[tetrahedronIndex])
                {
                    //Don't add a tetrahedron index. Now that we've found this index, though, move on to the next one.
                    ++tetrahedronIndex;
                }
                else
                {
                    outsidePointCandidates.Add(i);
                }
            }
            CommonResources.GiveBack(tetrahedronIndices);
        }

        private static void RemoveInsidePoints(RawList<Vector3> points, RawList<int> triangleIndices, RawList<int> outsidePoints)
        {
            var insidePoints = CommonResources.GetIntList();
            //We're going to remove points from this list as we go to prune it down to the truly inner points.
            insidePoints.AddRange(outsidePoints);
            outsidePoints.Clear();

            for (int i = 0; i < triangleIndices.Count && insidePoints.Count > 0; i += 3)
            {
                //Compute the triangle's plane in point-normal representation to test other points against.
                Vector3 normal;
                FindNormal(triangleIndices, points, i, out normal);
                Vector3 p = points.Elements[triangleIndices.Elements[i]];

                for (int j = insidePoints.Count - 1; j >= 0; --j)
                {
                    //Offset from the triangle to the current point, tested against the normal, determines if the current point is visible
                    //from the triangle face.
                    Vector3 offset;
                    Vector3.Subtract(ref points.Elements[insidePoints.Elements[j]], ref p, out offset);
                    float dot;
                    Vector3.Dot(ref offset, ref normal, out dot);
                    //If it's visible, then it's outside!
                    if (dot > 0)
                    {
                        //This point is known to be on the outside; put it on the outside!
                        outsidePoints.Add(insidePoints.Elements[j]);
                        insidePoints.FastRemoveAt(j);
                    }
                }
            }
            CommonResources.GiveBack(insidePoints);
        }


        private static void FindNormal(RawList<int> indices, RawList<Vector3> points, int triangleIndex, out Vector3 normal)
        {
            var a = points.Elements[indices.Elements[triangleIndex]];
            Vector3 ab, ac;
            Vector3.Subtract(ref points.Elements[indices.Elements[triangleIndex + 1]], ref a, out ab);
            Vector3.Subtract(ref points.Elements[indices.Elements[triangleIndex + 2]], ref a, out ac);
            Vector3.Cross(ref ac, ref ab, out normal);
        }

        private static bool IsTriangleVisibleFromPoint(RawList<int> indices, RawList<Vector3> points, int triangleIndex, ref Vector3 point)
        {
            //Compute the normal of the triangle.
            var a = points.Elements[indices.Elements[triangleIndex]];
            Vector3 ab, ac;
            Vector3.Subtract(ref points.Elements[indices.Elements[triangleIndex + 1]], ref a, out ab);
            Vector3.Subtract(ref points.Elements[indices.Elements[triangleIndex + 2]], ref a, out ac);
            Vector3 normal;
            Vector3.Cross(ref ac, ref ab, out normal);
            //Assume a consistent winding.  Check to see if the normal points at the point.
            Vector3 offset;
            Vector3.Subtract(ref point, ref a, out offset);
            float dot;
            Vector3.Dot(ref offset, ref normal, out dot);
            return dot >= 0;
        }

        private static void VerifyWindings(RawList<int> newIndices, RawList<Vector3> points, ref Vector3 centroid)
        {
            //Go through every triangle.
            for (int k = 0; k < newIndices.Count; k += 3)
            {
                //Check if the triangle faces away or towards the centroid.

                if (IsTriangleVisibleFromPoint(newIndices, points, k, ref centroid))
                {
                    //If it's towards, flip the winding.
                    int temp = newIndices[k + 1];
                    newIndices[k + 1] = newIndices[k + 2];
                    newIndices[k + 2] = temp;
                }
            }
        }
    }
}
