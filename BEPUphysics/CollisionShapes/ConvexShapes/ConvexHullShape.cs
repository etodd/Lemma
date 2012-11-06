using System;
using System.Collections.Generic;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;
using System.Collections.ObjectModel;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Convex wrapping around a point set.
    ///</summary>
    public class ConvexHullShape : ConvexShape
    {
        ///<summary>
        /// Gets the point set of the convex hull.
        ///</summary>
        public ReadOnlyList<Vector3> Vertices
        {
            get
            {
                return new ReadOnlyList<Vector3>(vertices);
            }
        }
        RawList<Vector3> vertices;

        ///<summary>
        /// Constructs a new convex hull shape.
        /// The point set will be recentered on the local origin.
        /// If that offset is needed, use the other constructor which outputs the computed center.
        ///</summary>
        ///<param name="vertices">Point set to use to construct the convex hull.</param>
        ///<exception cref="ArgumentException">Thrown when the point set is empty.</exception>
        public ConvexHullShape(IList<Vector3> vertices)
        {
            if (vertices.Count == 0)
                throw new ArgumentException("Vertices list used to create a ConvexHullShape cannot be empty.");

            var surfaceVertices = Resources.GetVectorList();
            ComputeCenter(vertices, surfaceVertices);
            this.vertices = new RawList<Vector3>(surfaceVertices);
            Resources.GiveBack(surfaceVertices);

            OnShapeChanged();
        }

        ///<summary>
        /// Constructs a new convex hull shape.
        /// The point set will be recentered on the local origin.
        ///</summary>
        ///<param name="vertices">Point set to use to construct the convex hull.</param>
        /// <param name="center">Computed center of the convex hull shape prior to recentering.</param>
        ///<exception cref="ArgumentException">Thrown when the point set is empty.</exception>
        public ConvexHullShape(IList<Vector3> vertices, out Vector3 center)
        {
            if (vertices.Count == 0)
                throw new ArgumentException("Vertices list used to create a ConvexHullShape cannot be empty.");

            var surfaceVertices = Resources.GetVectorList();
            center = ComputeCenter(vertices, surfaceVertices);
            this.vertices = new RawList<Vector3>(surfaceVertices);
            Resources.GiveBack(surfaceVertices);

            OnShapeChanged();
        }

        ///<summary>
        /// Constructs a new convex hull shape.
        /// The point set will be recentered on the local origin.
        ///</summary>
        ///<param name="vertices">Point set to use to construct the convex hull.</param>
        /// <param name="center">Computed center of the convex hull shape prior to recentering.</param>
        /// <param name="outputHullTriangleIndices">Triangle indices computed on the surface of the point set.</param>
        /// <param name="outputUniqueSurfaceVertices">Unique vertices on the surface of the convex hull.</param>
        ///<exception cref="ArgumentException">Thrown when the point set is empty.</exception>
        public ConvexHullShape(IList<Vector3> vertices, out Vector3 center, IList<int> outputHullTriangleIndices, IList<Vector3> outputUniqueSurfaceVertices)
        {
            if (vertices.Count == 0)
                throw new ArgumentException("Vertices list used to create a ConvexHullShape cannot be empty.");


            //Ensure that the convex hull is centered on its local origin.
            center = ComputeCenter(vertices, outputHullTriangleIndices, outputUniqueSurfaceVertices);
            this.vertices = new RawList<Vector3>(outputUniqueSurfaceVertices);

            OnShapeChanged();
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

            float minX, maxX;
            float minY, maxY;
            float minZ, maxZ;
            var right = new Vector3(o.M11, o.M21, o.M31);
            var up = new Vector3(o.M12, o.M22, o.M32);
            var backward = new Vector3(o.M13, o.M23, o.M33);
            Vector3.Dot(ref vertices.Elements[0], ref right, out maxX);
            minX = maxX;
            Vector3.Dot(ref vertices.Elements[0], ref up, out maxY);
            minY = maxY;
            Vector3.Dot(ref vertices.Elements[0], ref backward, out maxZ);
            minZ = maxZ;
            int minXIndex = 0;
            int maxXIndex = 0;
            int minYIndex = 0;
            int maxYIndex = 0;
            int minZIndex = 0;
            int maxZIndex = 0;
            for (int i = 1; i < vertices.count; i++)
            {
                float dot;
                Vector3.Dot(ref vertices.Elements[i], ref right, out dot);
                if (dot < minX)
                {
                    minX = dot;
                    minXIndex = i;
                }
                else if (dot > maxX)
                {
                    maxX = dot;
                    maxXIndex = i;
                }

                Vector3.Dot(ref vertices.Elements[i], ref up, out dot);
                if (dot < minY)
                {
                    minY = dot;
                    minYIndex = i;
                }
                else if (dot > maxY)
                {
                    maxY = dot;
                    maxYIndex = i;
                }

                Vector3.Dot(ref vertices.Elements[i], ref backward, out dot);
                if (dot < minZ)
                {
                    minZ = dot;
                    minZIndex = i;
                }
                else if (dot > maxZ)
                {
                    maxZ = dot;
                    maxZIndex = i;
                }
            }

            Vector3 minXpoint, maxXpoint, minYpoint, maxYpoint, minZpoint, maxZpoint;

            Matrix3X3.Transform(ref vertices.Elements[minXIndex], ref o, out minXpoint);
            Matrix3X3.Transform(ref vertices.Elements[maxXIndex], ref o, out maxXpoint);
            Matrix3X3.Transform(ref vertices.Elements[minYIndex], ref o, out minYpoint);
            Matrix3X3.Transform(ref vertices.Elements[maxYIndex], ref o, out maxYpoint);
            Matrix3X3.Transform(ref vertices.Elements[minZIndex], ref o, out minZpoint);
            Matrix3X3.Transform(ref vertices.Elements[maxZIndex], ref o, out maxZpoint);

            boundingBox.Max.X = shapeTransform.Position.X + collisionMargin + maxXpoint.X;
            boundingBox.Max.Y = shapeTransform.Position.Y + collisionMargin + maxYpoint.Y;
            boundingBox.Max.Z = shapeTransform.Position.Z + collisionMargin + maxZpoint.Z;

            boundingBox.Min.X = shapeTransform.Position.X - collisionMargin + minXpoint.X;
            boundingBox.Min.Y = shapeTransform.Position.Y - collisionMargin + minYpoint.Y;
            boundingBox.Min.Z = shapeTransform.Position.Z - collisionMargin + minZpoint.Z;
        }


        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            float max;
            Vector3.Dot(ref vertices.Elements[0], ref direction, out max);
            int maxIndex = 0;
            for (int i = 1; i < vertices.count; i++)
            {
                float dot;
                Vector3.Dot(ref vertices.Elements[i], ref direction, out dot);
                if (dot > max)
                {
                    max = dot;
                    maxIndex = i;
                }
            }
            extremePoint = vertices.Elements[maxIndex];
        }

        #region Shape Information

        /// <summary>
        /// Computes the center of the shape.  This can be considered its 
        /// center of mass.
        /// </summary>
        /// <returns>Center of the shape.</returns>
        public override Vector3 ComputeCenter()
        {
            return ComputeCenter(vertices);
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
            return ComputeCenter(vertices, out volume);
        }

        /// <summary>
        /// Computes the volume of the shape.
        /// </summary>
        /// <returns>Volume of the shape.</returns>
        public override float ComputeVolume()
        {
            float volume;
            ComputeCenter(out volume);
            return volume;
        }

        ///<summary>
        /// Computes the center, volume, and surface triangles of the convex hull shape.
        ///</summary>
        ///<param name="volume">Volume of the hull.</param>
        ///<param name="outputSurfaceTriangles">Surface triangles of the hull.</param>
        ///<param name="outputLocalSurfaceVertices">Surface vertices recentered on the center of volume. </param>
        ///<returns>Center of the hull.</returns>
        public Vector3 ComputeCenter(out float volume, IList<int> outputSurfaceTriangles, IList<Vector3> outputLocalSurfaceVertices)
        {
            return ComputeCenter(vertices, out volume, outputSurfaceTriangles, outputLocalSurfaceVertices);
        }

        ///<summary>
        /// Computes the center of a convex hull defined by the point set.
        ///</summary>
        ///<param name="vertices">Point set defining the convex hull.</param>
        ///<returns>Center of the convex hull.</returns>
        public static Vector3 ComputeCenter(IList<Vector3> vertices)
        {
            float volume;
            return ComputeCenter(vertices, out volume);
        }

        ///<summary>
        /// Computes the center and volume of a convex hull defined by a pointset.
        ///</summary>
        ///<param name="vertices">Point set defining the convex hull.</param>
        ///<param name="volume">Volume of the convex hull.</param>
        ///<returns>Center of the convex hull.</returns>
        public static Vector3 ComputeCenter(IList<Vector3> vertices, out float volume)
        {
            var localSurfaceVertices = Resources.GetVectorList();
            var surfaceTriangles = Resources.GetIntList();
            Vector3 toReturn = ComputeCenter(vertices, out volume, surfaceTriangles, localSurfaceVertices);
            Resources.GiveBack(localSurfaceVertices);
            Resources.GiveBack(surfaceTriangles);
            return toReturn;
        }

        ///<summary>
        /// Computes the center and surface triangles of a convex hull defined by a point set.
        ///</summary>
        ///<param name="vertices">Point set defining the convex hull.</param>
        ///<param name="outputLocalSurfaceVertices">Local positions of vertices on the convex hull.</param>
        ///<returns>Center of the convex hull.</returns>
        public static Vector3 ComputeCenter(IList<Vector3> vertices, IList<Vector3> outputLocalSurfaceVertices)
        {
            float volume;
            var indices = Resources.GetIntList();
            Vector3 toReturn = ComputeCenter(vertices, out volume, indices, outputLocalSurfaceVertices);
            Resources.GiveBack(indices);
            return toReturn;
        }

        ///<summary>
        /// Computes the center and surface triangles of a convex hull defined by a point set.
        ///</summary>
        ///<param name="vertices">Point set defining the convex hull.</param>
        ///<param name="outputSurfaceTriangles">Indices of surface triangles of the convex hull.</param>
        ///<param name="outputLocalSurfaceVertices">Local positions of vertices on the convex hull.</param>
        ///<returns>Center of the convex hull.</returns>
        public static Vector3 ComputeCenter(IList<Vector3> vertices, IList<int> outputSurfaceTriangles, IList<Vector3> outputLocalSurfaceVertices)
        {
            float volume;
            Vector3 toReturn = ComputeCenter(vertices, out volume, outputSurfaceTriangles, outputLocalSurfaceVertices);
            return toReturn;
        }

        ///<summary>
        /// Computes the center, volume, and surface triangles of a convex hull defined by a point set.
        ///</summary>
        ///<param name="vertices">Point set defining the convex hull.</param>
        ///<param name="volume">Volume of the convex hull.</param>
        ///<param name="outputSurfaceTriangles">Indices of surface triangles of the convex hull.</param>
        ///<param name="outputLocalSurfaceVertices">Local positions of vertices on the convex hull.</param>
        ///<returns>Center of the convex hull.</returns>
        public static Vector3 ComputeCenter(IList<Vector3> vertices, out float volume, IList<int> outputSurfaceTriangles, IList<Vector3> outputLocalSurfaceVertices)
        {
            Vector3 centroid = Toolbox.ZeroVector;
            for (int k = 0; k < vertices.Count; k++)
            {
                centroid += vertices[k];
            }
            centroid /= vertices.Count;

            //Toolbox.GetConvexHull(vertices, outputSurfaceTriangles, outputLocalSurfaceVertices);
            ConvexHullHelper.GetConvexHull(vertices, outputSurfaceTriangles, outputLocalSurfaceVertices);

            volume = 0;
            var volumes = Resources.GetFloatList();
            var centroids = Resources.GetVectorList();
            for (int k = 0; k < outputSurfaceTriangles.Count; k += 3)
            {
                volumes.Add(Vector3.Dot(
                    Vector3.Cross(vertices[outputSurfaceTriangles[k + 1]] - vertices[outputSurfaceTriangles[k]],
                                  vertices[outputSurfaceTriangles[k + 2]] - vertices[outputSurfaceTriangles[k]]),
                    centroid - vertices[outputSurfaceTriangles[k]]));
                volume += volumes[k / 3];
                centroids.Add((vertices[outputSurfaceTriangles[k]] + vertices[outputSurfaceTriangles[k + 1]] + vertices[outputSurfaceTriangles[k + 2]] + centroid) / 4);
            }
            Vector3 center = Toolbox.ZeroVector;
            for (int k = 0; k < centroids.Count; k++)
            {
                center += centroids[k] * (volumes[k] / volume);
            }
            volume /= 6;
            for (int k = 0; k < outputLocalSurfaceVertices.Count; k++)
            {
                outputLocalSurfaceVertices[k] -= center;
            }
            Resources.GiveBack(centroids);
            Resources.GiveBack(volumes);
            return center;
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
            var surfaceTriangles = Resources.GetIntList();
            var surfaceVertices = Resources.GetVectorList();
            ComputeCenter(out volume, surfaceTriangles, surfaceVertices);
            Matrix3X3 toReturn = ComputeVolumeDistribution(volume, surfaceTriangles);
            Resources.GiveBack(surfaceTriangles);
            Resources.GiveBack(surfaceVertices);
            return toReturn;
        }

        ///<summary>
        /// Computes the volume distribution of the convex hull, its volume, and its surface triangles.
        ///</summary>
        ///<param name="volume">Volume of the convex hull.</param>
        ///<param name="localSurfaceTriangles">Surface triangles of the convex hull.</param>
        ///<returns>Volume distribution of the convex hull.</returns>
        public Matrix3X3 ComputeVolumeDistribution(float volume, IList<int> localSurfaceTriangles)
        {
            //TODO: This method has a lot of overlap with the volume calculation.  Conceptually very similar, could bundle tighter.

            //Source: Explicit Exact Formulas for the 3-D Tetrahedron Inertia Tensor in Terms of its Vertex Coordinates
            //http://www.scipub.org/fulltext/jms2/jms2118-11.pdf
            //x1, x2, x3, x4 are origin, triangle1, triangle2, triangle3
            //Looking to find inertia tensor matrix of the form
            // [  a  -b' -c' ]
            // [ -b'  b  -a' ]
            // [ -c' -a'  c  ]
            float a = 0, b = 0, c = 0, ao = 0, bo = 0, co = 0;
            Vector3 v2, v3, v4;
            float density = 1 / volume;
            float diagonalFactor = density / 60;
            float offFactor = -density / 120;
            for (int i = 0; i < localSurfaceTriangles.Count; i += 3)
            {
                v2 = vertices[localSurfaceTriangles[i]];
                v3 = vertices[localSurfaceTriangles[i + 1]];
                v4 = vertices[localSurfaceTriangles[i + 2]];
                float determinant = Math.Abs(v2.X * (v3.Y * v4.Z - v3.Z * v4.Y) -
                                             v3.X * (v2.Y * v4.Z - v2.Z * v4.Y) +
                                             v4.X * (v2.Y * v3.Z - v2.Z * v3.Y)); //Determinant is 6 * volume.
                a += determinant * (v2.Y * v2.Y + v2.Y * v3.Y + v3.Y * v3.Y + v2.Y * v4.Y + v3.Y * v4.Y + v4.Y * v4.Y +
                                    v2.Z * v2.Z + v2.Z * v3.Z + v3.Z * v3.Z + v2.Z * v4.Z + v3.Z * v4.Z + v4.Z * v4.Z);
                b += determinant * (v2.X * v2.X + v2.X * v3.X + v3.X * v3.X + v2.X * v4.X + v3.X * v4.X + v4.X * v4.X +
                                    v2.Z * v2.Z + v2.Z * v3.Z + v3.Z * v3.Z + v2.Z * v4.Z + v3.Z * v4.Z + v4.Z * v4.Z);
                c += determinant * (v2.X * v2.X + v2.X * v3.X + v3.X * v3.X + v2.X * v4.X + v3.X * v4.X + v4.X * v4.X +
                                    v2.Y * v2.Y + v2.Y * v3.Y + v3.Y * v3.Y + v2.Y * v4.Y + v3.Y * v4.Y + v4.Y * v4.Y);
                ao += determinant * (2 * v2.Y * v2.Z + v3.Y * v2.Z + v4.Y * v2.Z + v2.Y * v3.Z + 2 * v3.Y * v3.Z + v4.Y * v3.Z + v2.Y * v4.Z + v3.Y * v4.Z + 2 * v4.Y * v4.Z);
                bo += determinant * (2 * v2.X * v2.Z + v3.X * v2.Z + v4.X * v2.Z + v2.X * v3.Z + 2 * v3.X * v3.Z + v4.X * v3.Z + v2.X * v4.Z + v3.X * v4.Z + 2 * v4.X * v4.Z);
                co += determinant * (2 * v2.X * v2.Y + v3.X * v2.Y + v4.X * v2.Y + v2.X * v3.Y + 2 * v3.X * v3.Y + v4.X * v3.Y + v2.X * v4.Y + v3.X * v4.Y + 2 * v4.X * v4.Y);


                /*subInertiaTensor = new Matrix(a, bo, co, 0,
                                              bo, b, ao, 0,
                                              co, ao, c, 0,
                                              0, 0, 0, 0);

                localInertiaTensor += subInertiaTensor;// +(offset.LengthSquared() * Matrix.Identity - Toolbox.getOuterProduct(offset, offset));// *(determinant * density / 6);*/
            }
            a *= diagonalFactor;
            b *= diagonalFactor;
            c *= diagonalFactor;
            ao *= offFactor;
            bo *= offFactor;
            co *= offFactor;
            var distribution = new Matrix3X3(a, bo, co,
                                                   bo, b, ao,
                                                   co, ao, c);

            return distribution;
        }

        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            float maximumRadius = 0;
            for (int i = 0; i < vertices.count; i++)
            {
                float tempDist = vertices.Elements[i].Length();
                if (maximumRadius < tempDist)
                    maximumRadius = tempDist;
            }
            maximumRadius += collisionMargin;
            return maximumRadius;
        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            //Sample the shape in directions pointing to the vertices of a regular tetrahedron.
            Vector3 a, b, c, d;
            var direction = new Vector3(1, 1, 1);
            GetLocalExtremePointWithoutMargin(ref direction, out a);
            direction = new Vector3(-1, -1, 1);
            GetLocalExtremePointWithoutMargin(ref direction, out b);
            direction = new Vector3(-1, 1, -1);
            GetLocalExtremePointWithoutMargin(ref direction, out c);
            direction = new Vector3(1, -1, -1);
            GetLocalExtremePointWithoutMargin(ref direction, out d);
            Vector3 ab, cb, ac, ad, cd;
            Vector3.Subtract(ref b, ref a, out ab);
            Vector3.Subtract(ref b, ref c, out cb);
            Vector3.Subtract(ref c, ref a, out ac);
            Vector3.Subtract(ref d, ref a, out ad);
            Vector3.Subtract(ref d, ref c, out cd);
            //Find normals of triangles: ABC, CBD, ACD, ADB
            Vector3 nABC, nCBD, nACD, nADB;
            Vector3.Cross(ref ac, ref ab, out nABC);
            Vector3.Cross(ref cd, ref cb, out nCBD);
            Vector3.Cross(ref ad, ref ac, out nACD);
            Vector3.Cross(ref ab, ref ad, out nADB);
            //Find distances to planes.
            float dABC, dCBD, dACD, dADB;
            Vector3.Dot(ref a, ref nABC, out dABC);
            Vector3.Dot(ref c, ref nCBD, out dCBD);
            Vector3.Dot(ref a, ref nACD, out dACD);
            Vector3.Dot(ref a, ref nADB, out dADB);
            dABC /= nABC.Length();
            dCBD /= nCBD.Length();
            dACD /= nACD.Length();
            dADB /= nADB.Length();

            return collisionMargin + Math.Min(dABC, Math.Min(dCBD, Math.Min(dACD, dADB)));
        }

        #endregion

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<ConvexHullShape>(this);
        }


    }
}
