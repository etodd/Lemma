using System;
using BEPUphysics.CollisionTests.Manifolds;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;
using BEPUphysics.DataStructures;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// The local space data needed by a Terrain collidable.
    /// Contains the Heightmap and other information.
    ///</summary>
    public class TerrainShape : CollisionShape
    {
        private float[,] heights;
        //note: changing heights in array does not fire OnShapeChanged automatically.
        //Need to notify parent manually if you do it.
        ///<summary>
        /// Gets or sets the height field of the terrain shape.
        ///</summary>
        public float[,] Heights
        {
            get
            {
                return heights;
            }
            set
            {
                heights = value;
                OnShapeChanged();
            }
        }



        QuadTriangleOrganization quadTriangleOrganization;
        ///<summary>
        /// Gets or sets the quad triangle organization.
        ///</summary>
        public QuadTriangleOrganization QuadTriangleOrganization
        {
            get
            {
                return quadTriangleOrganization;
            }
            set
            {
                quadTriangleOrganization = value;
                OnShapeChanged();
            }
        }

        ///<summary>
        /// Constructs a TerrainShape.
        ///</summary>
        ///<param name="heights">Heights array used for the shape.</param>
        ///<param name="triangleOrganization">Triangle organization of each quad.</param>
        ///<exception cref="ArgumentException">Thrown if the heights array has less than 2x2 vertices.</exception>
        public TerrainShape(float[,] heights, QuadTriangleOrganization triangleOrganization)
        {
            if (heights.GetLength(0) <= 1 || heights.GetLength(1) <= 1)
            {
                throw new ArgumentException("Terrains must have a least 2x2 vertices (one quad).");
            }
            this.heights = heights;
            quadTriangleOrganization = triangleOrganization;
        }

        ///<summary>
        /// Constructs a TerrainShape.
        ///</summary>
        ///<param name="heights">Heights array used for the shape.</param>
        public TerrainShape(float[,] heights)
            : this(heights, QuadTriangleOrganization.BottomLeftUpperRight)
        {
        }



        ///<summary>
        /// Constructs the bounding box of the terrain given a transform.
        ///</summary>
        ///<param name="transform">Transform to apply to the terrain during the bounding box calculation.</param>
        ///<param name="boundingBox">Bounding box of the terrain shape when transformed.</param>
        public void GetBoundingBox(ref AffineTransform transform, out BoundingBox boundingBox)
        {
#if !WINDOWS
            boundingBox = new BoundingBox();
#endif
            float minX = float.MaxValue, maxX = -float.MaxValue,
                  minY = float.MaxValue, maxY = -float.MaxValue,
                  minZ = float.MaxValue, maxZ = -float.MaxValue;
            Vector3 minXvertex = new Vector3(),
                    maxXvertex = new Vector3(),
                    minYvertex = new Vector3(),
                    maxYvertex = new Vector3(),
                    minZvertex = new Vector3(),
                    maxZvertex = new Vector3();

            //Find the extreme locations.
            for (int i = 0; i < heights.GetLength(0); i++)
            {
                for (int j = 0; j < heights.GetLength(1); j++)
                {
                    var vertex = new Vector3(i, heights[i, j], j);
                    Matrix3X3.Transform(ref vertex, ref transform.LinearTransform, out vertex);
                    if (vertex.X < minX)
                    {
                        minX = vertex.X;
                        minXvertex = vertex;
                    }
                    else if (vertex.X > maxX)
                    {
                        maxX = vertex.X;
                        maxXvertex = vertex;
                    }

                    if (vertex.Y < minY)
                    {
                        minY = vertex.Y;
                        minYvertex = vertex;
                    }
                    else if (vertex.Y > maxY)
                    {
                        maxY = vertex.Y;
                        maxYvertex = vertex;
                    }

                    if (vertex.Z < minZ)
                    {
                        minZ = vertex.Z;
                        minZvertex = vertex;
                    }
                    else if (vertex.Z > maxZ)
                    {
                        maxZ = vertex.Z;
                        maxZvertex = vertex;
                    }
                }
            }

            //Shift the bounding box.
            boundingBox.Min.X = minXvertex.X + transform.Translation.X;
            boundingBox.Min.Y = minYvertex.Y + transform.Translation.Y;
            boundingBox.Min.Z = minZvertex.Z + transform.Translation.Z;
            boundingBox.Max.X = maxXvertex.X + transform.Translation.X;
            boundingBox.Max.Y = maxYvertex.Y + transform.Translation.Y;
            boundingBox.Max.Z = maxZvertex.Z + transform.Translation.Z;
        }
        ///<summary>
        /// Tests a ray against the terrain shape.
        ///</summary>
        ///<param name="ray">Ray to test against the shape.</param>
        ///<param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="transform">Transform to apply to the terrain shape during the test.</param>
        ///<param name="hit">Hit data of the ray cast, if any.</param>
        ///<returns>Whether or not the ray hit the transformed terrain shape.</returns>
        public bool RayCast(ref Ray ray, float maximumLength, ref AffineTransform transform, out RayHit hit)
        {
            return RayCast(ref ray, maximumLength, ref transform, TriangleSidedness.Counterclockwise, out hit);
        }
        ///<summary>
        /// Tests a ray against the terrain shape.
        ///</summary>
        ///<param name="ray">Ray to test against the shape.</param>
        ///<param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="transform">Transform to apply to the terrain shape during the test.</param>
        ///<param name="sidedness">Sidedness of the triangles to use when raycasting.</param>
        ///<param name="hit">Hit data of the ray cast, if any.</param>
        ///<returns>Whether or not the ray hit the transformed terrain shape.</returns>
        public bool RayCast(ref Ray ray, float maximumLength, ref AffineTransform transform, TriangleSidedness sidedness, out RayHit hit)
        {
            hit = new RayHit();
            //Put the ray into local space.
            Ray localRay;
            AffineTransform inverse;
            AffineTransform.Invert(ref transform, out inverse);
            Matrix3X3.Transform(ref ray.Direction, ref inverse.LinearTransform, out localRay.Direction);
            AffineTransform.Transform(ref ray.Position, ref inverse, out localRay.Position);

            //Use rasterizey traversal.
            //The origin is at 0,0,0 and the map goes +X, +Y, +Z.
            //if it's before the origin and facing away, or outside the max and facing out, early out.
            float maxX = heights.GetLength(0) - 1;
            float maxZ = heights.GetLength(1) - 1;

            Vector3 progressingOrigin = localRay.Position;
            float distance = 0;
            //Check the outside cases first.
            if (progressingOrigin.X < 0)
            {
                if (localRay.Direction.X > 0)
                {
                    //Off the left side.
                    float timeToMinX = -progressingOrigin.X / localRay.Direction.X;
                    distance += timeToMinX;
                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToMinX, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }
                else
                    return false; //Outside and pointing away from the terrain.
            }
            else if (progressingOrigin.X > maxX)
            {
                if (localRay.Direction.X < 0)
                {
                    //Off the left side.
                    float timeToMinX = -(progressingOrigin.X - maxX) / localRay.Direction.X;
                    distance += timeToMinX;
                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToMinX, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }
                else
                    return false; //Outside and pointing away from the terrain.
            }

            if (progressingOrigin.Z < 0)
            {
                if (localRay.Direction.Z > 0)
                {
                    float timeToMinZ = -progressingOrigin.Z / localRay.Direction.Z;
                    distance += timeToMinZ;
                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToMinZ, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }
                else
                    return false;
            }
            else if (progressingOrigin.Z > maxZ)
            {
                if (localRay.Direction.Z < 0)
                {
                    float timeToMinZ = -(progressingOrigin.Z - maxZ) / localRay.Direction.Z;
                    distance += timeToMinZ;
                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToMinZ, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }
                else
                    return false;
            }

            if (distance > maximumLength)
                return false;



            //By now, we should be entering the main body of the terrain.

            int xCell = (int)progressingOrigin.X;
            int zCell = (int)progressingOrigin.Z;
            //If it's hitting the border and going in, then correct the index
            //so that it will initially target a valid quad.
            //Without this, a quad beyond the border would be tried and failed.
            if (xCell == heights.GetLength(0) - 1 && localRay.Direction.X < 0)
                xCell = heights.GetLength(0) - 2;
            if (zCell == heights.GetLength(1) - 1 && localRay.Direction.Z < 0)
                zCell = heights.GetLength(1) - 2;

            while (true)
            {
                //Check for a miss.
                if (xCell < 0 ||
                    zCell < 0 ||
                    xCell >= heights.GetLength(0) - 1 ||
                    zCell >= heights.GetLength(1) - 1)
                    return false;

                //Test the triangles of this cell.
                Vector3 v1, v2, v3, v4;
                // v3 v4
                // v1 v2
                GetLocalPosition(xCell, zCell, out v1);
                GetLocalPosition(xCell + 1, zCell, out v2);
                GetLocalPosition(xCell, zCell + 1, out v3);
                GetLocalPosition(xCell + 1, zCell + 1, out v4);
                RayHit hit1, hit2;
                bool didHit1;
                bool didHit2;

                //Don't bother doing ray intersection tests if the ray can't intersect it.

                float highest = v1.Y;
                float lowest = v1.Y;
                if (v2.Y > highest)
                    highest = v2.Y;
                else if (v2.Y < lowest)
                    lowest = v2.Y;
                if (v3.Y > highest)
                    highest = v3.Y;
                else if (v3.Y < lowest)
                    lowest = v3.Y;
                if (v4.Y > highest)
                    highest = v4.Y;
                else if (v4.Y < lowest)
                    lowest = v4.Y;


                if (!(progressingOrigin.Y > highest && localRay.Direction.Y > 0 ||
                    progressingOrigin.Y < lowest && localRay.Direction.Y < 0))
                {


                    if (quadTriangleOrganization == QuadTriangleOrganization.BottomLeftUpperRight)
                    {
                        //Always perform the raycast as if Y+ in local space is the way the triangles are facing.
                        didHit1 = Toolbox.FindRayTriangleIntersection(ref localRay, maximumLength, sidedness, ref v1, ref v2, ref v3, out hit1);
                        didHit2 = Toolbox.FindRayTriangleIntersection(ref localRay, maximumLength, sidedness, ref v2, ref v4, ref v3, out hit2);
                    }
                    else //if (quadTriangleOrganization == CollisionShapes.QuadTriangleOrganization.BottomRightUpperLeft)
                    {
                        didHit1 = Toolbox.FindRayTriangleIntersection(ref localRay, maximumLength, sidedness, ref v1, ref v2, ref v4, out hit1);
                        didHit2 = Toolbox.FindRayTriangleIntersection(ref localRay, maximumLength, sidedness, ref v1, ref v4, ref v3, out hit2);
                    }
                    if (didHit1 && didHit2)
                    {
                        if (hit1.T < hit2.T)
                        {
                            Vector3.Multiply(ref ray.Direction, hit1.T, out hit.Location);
                            Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                            Matrix3X3.TransformTranspose(ref hit1.Normal, ref inverse.LinearTransform, out hit.Normal);
                            hit.T = hit1.T;
                            return true;
                        }
                        Vector3.Multiply(ref ray.Direction, hit2.T, out hit.Location);
                        Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                        Matrix3X3.TransformTranspose(ref hit2.Normal, ref inverse.LinearTransform, out hit.Normal);
                        hit.T = hit2.T;
                        return true;
                    }
                    else if (didHit1)
                    {
                        Vector3.Multiply(ref ray.Direction, hit1.T, out hit.Location);
                        Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                        Matrix3X3.TransformTranspose(ref hit1.Normal, ref inverse.LinearTransform, out hit.Normal);
                        hit.T = hit1.T;
                        return true;
                    }
                    else if (didHit2)
                    {
                        Vector3.Multiply(ref ray.Direction, hit2.T, out hit.Location);
                        Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                        Matrix3X3.TransformTranspose(ref hit2.Normal, ref inverse.LinearTransform, out hit.Normal);
                        hit.T = hit2.T;
                        return true;
                    }
                }

                //Move to the next cell.

                float timeToX;
                if (localRay.Direction.X < 0)
                    timeToX = -(progressingOrigin.X - xCell) / localRay.Direction.X;
                else if (ray.Direction.X > 0)
                    timeToX = (xCell + 1 - progressingOrigin.X) / localRay.Direction.X;
                else
                    timeToX = float.MaxValue;

                float timeToZ;
                if (localRay.Direction.Z < 0)
                    timeToZ = -(progressingOrigin.Z - zCell) / localRay.Direction.Z;
                else if (localRay.Direction.Z > 0)
                    timeToZ = (zCell + 1 - progressingOrigin.Z) / localRay.Direction.Z;
                else
                    timeToZ = float.MaxValue;

                //Move to the next cell.
                if (timeToX < timeToZ)
                {
                    if (localRay.Direction.X < 0)
                        xCell--;
                    else
                        xCell++;

                    distance += timeToX;
                    if (distance > maximumLength)
                        return false;

                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToX, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }
                else
                {
                    if (localRay.Direction.Z < 0)
                        zCell--;
                    else
                        zCell++;

                    distance += timeToZ;
                    if (distance > maximumLength)
                        return false;

                    Vector3 increment;
                    Vector3.Multiply(ref localRay.Direction, timeToZ, out increment);
                    Vector3.Add(ref increment, ref progressingOrigin, out progressingOrigin);
                }

            }


        }

        ///<summary>
        /// Gets the position of a vertex at the given indices in local space.
        ///</summary>
        ///<param name="i">Index in the first dimension.</param>
        ///<param name="j">Index in the second dimension.</param>
        ///<param name="v">Local space position at the given vertice.s</param>
        public void GetLocalPosition(int i, int j, out Vector3 v)
        {
#if !WINDOWS
            v = new Vector3();
#endif
            v.X = i;
            v.Y = heights[i, j];
            v.Z = j;
        }

        /// <summary>
        /// Gets the world space position of a vertex in the terrain at the given indices.
        /// </summary>
        ///<param name="i">Index in the first dimension.</param>
        ///<param name="j">Index in the second dimension.</param>
        /// <param name="transform">Transform to apply to the vertex.</param>
        /// <param name="position">Transformed position of the vertex at the given indices.</param>
        public void GetPosition(int i, int j, ref AffineTransform transform, out Vector3 position)
        {
            if (i <= 0)
                i = 0;
            else if (i >= heights.GetLength(0))
                i = heights.GetLength(0) - 1;
            if (j <= 0)
                j = 0;
            else if (j >= heights.GetLength(1))
                j = heights.GetLength(1) - 1;
#if !WINDOWS
            position = new Vector3();
#endif
            position.X = i;
            position.Y = heights[i, j];
            position.Z = j;
            AffineTransform.Transform(ref position, ref transform, out position);


        }


        /// <summary>
        /// Gets the world space normal at the given indices.
        /// </summary>
        ///<param name="i">Index in the first dimension.</param>
        ///<param name="j">Index in the second dimension.</param>
        /// <param name="transform">Transform to apply to the terrain while computing the normal.</param>
        /// <param name="normal">World space normal at the given indices.</param>
        public void GetNormal(int i, int j, ref AffineTransform transform, out Vector3 normal)
        {
            Vector3 top;
            Vector3 bottom;
            Vector3 right;
            Vector3 left;

            if (i <= 0)
                i = 0;
            else if (i >= heights.GetLength(0))
                i = heights.GetLength(0) - 1;
            if (j <= 0)
                j = 0;
            else if (j >= heights.GetLength(1))
                j = heights.GetLength(1) - 1;

            GetPosition(i, Math.Min(j + 1, heights.GetLength(1) - 1), ref transform, out top);
            GetPosition(i, Math.Max(j - 1, 0), ref transform, out bottom);
            GetPosition(Math.Min(i + 1, heights.GetLength(0) - 1), j, ref transform, out right);
            GetPosition(Math.Max(i - 1, 0), j, ref transform, out left);

            Vector3 temp;
            Vector3.Subtract(ref top, ref bottom, out temp);
            Vector3.Subtract(ref right, ref left, out normal);
            Vector3.Cross(ref temp, ref normal, out normal);

            normal.Normalize();
        }

        ///<summary>
        /// Gets overlapped triangles with the terrain shape with a bounding box in the local space of the shape.
        ///</summary>
        ///<param name="localSpaceBoundingBox">Bounding box in the local space of the terrain shape.</param>
        ///<param name="overlappedTriangles">Triangles whose bounding boxes overlap the input bounding box.</param>
        public bool GetOverlaps(BoundingBox localSpaceBoundingBox, RawList<TriangleMeshConvexContactManifold.TriangleIndices> overlappedTriangles)
        {
            int width = heights.GetLength(0);
            int minX = Math.Max((int)localSpaceBoundingBox.Min.X, 0);
            int minY = Math.Max((int)localSpaceBoundingBox.Min.Z, 0);
            int maxX = Math.Min((int)localSpaceBoundingBox.Max.X, width - 2);
            int maxY = Math.Min((int)localSpaceBoundingBox.Max.Z, heights.GetLength(1) - 2);
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    //Before adding a triangle to the list, make sure the object isn't too high or low from the quad.
                    float highest, lowest;
                    float y1 = heights[i, j];
                    float y2 = heights[i + 1, j];
                    float y3 = heights[i, j + 1];
                    float y4 = heights[i + 1, j + 1];

                    highest = y1;
                    lowest = y1;
                    if (y2 > highest)
                        highest = y2;
                    else if (y2 < lowest)
                        lowest = y2;
                    if (y3 > highest)
                        highest = y3;
                    else if (y3 < lowest)
                        lowest = y3;
                    if (y4 > highest)
                        highest = y4;
                    else if (y4 < lowest)
                        lowest = y4;


                    if (localSpaceBoundingBox.Max.Y < lowest ||
                        localSpaceBoundingBox.Min.Y > highest)
                        continue;

                    //Now the local bounding box is very likely intersecting those of the triangles.
                    //Add the triangles to the list.
                    var indices = new TriangleMeshConvexContactManifold.TriangleIndices();

                    //v3 v4
                    //v1 v2

                    if (quadTriangleOrganization == QuadTriangleOrganization.BottomLeftUpperRight)
                    {
                        //v1 v2 v3
                        indices.A = i + j * width;
                        indices.B = i + 1 + j * width;
                        indices.C = i + (j + 1) * width;
                        overlappedTriangles.Add(indices);

                        //v2 v4 v3
                        indices.A = i + 1 + j * width;
                        indices.B = i + 1 + (j + 1) * width;
                        indices.C = i + (j + 1) * width;
                        overlappedTriangles.Add(indices);
                    }
                    else //Bottom right, Upper left
                    {
                        //v1 v2 v4
                        indices.A = i + j * width;
                        indices.B = i + 1 + j * width;
                        indices.C = i + 1 + (j + 1) * width;
                        overlappedTriangles.Add(indices);

                        //v1 v4 v3
                        indices.A = i + j * width;
                        indices.B = i + 1 + (j + 1) * width;
                        indices.C = i + (j + 1) * width;
                        overlappedTriangles.Add(indices);
                    }

                }
            }
            return overlappedTriangles.count > 0;
        }

        ///<summary>
        /// Gets overlapped triangles with the terrain shape with a bounding box in the local space of the shape.
        ///</summary>
        ///<param name="localBoundingBox">Bounding box in the local space of the terrain shape.</param>
        ///<param name="overlappedElements">Indices of elements whose bounding boxes overlap the input bounding box.</param>
        public bool GetOverlaps(BoundingBox localBoundingBox, RawList<int> overlappedElements)
        {
            int width = heights.GetLength(0);
            int minX = Math.Max((int)localBoundingBox.Min.X, 0);
            int minY = Math.Max((int)localBoundingBox.Min.Z, 0);
            int maxX = Math.Min((int)localBoundingBox.Max.X, width - 2);
            int maxY = Math.Min((int)localBoundingBox.Max.Z, heights.GetLength(1) - 2);
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    //Before adding a triangle to the list, make sure the object isn't too high or low from the quad.
                    float highest, lowest;
                    float y1 = heights[i, j];
                    float y2 = heights[i + 1, j];
                    float y3 = heights[i, j + 1];
                    float y4 = heights[i + 1, j + 1];

                    highest = y1;
                    lowest = y1;
                    if (y2 > highest)
                        highest = y2;
                    else if (y2 < lowest)
                        lowest = y2;
                    if (y3 > highest)
                        highest = y3;
                    else if (y3 < lowest)
                        lowest = y3;
                    if (y4 > highest)
                        highest = y4;
                    else if (y4 < lowest)
                        lowest = y4;


                    if (localBoundingBox.Max.Y < lowest ||
                        localBoundingBox.Min.Y > highest)
                        continue;

                    //Now the local bounding box is very likely intersecting those of the triangles.
                    //Add the triangles to the list.
                    int quadIndex = (i + j * width) * 2;
                    overlappedElements.Add(quadIndex);
                    overlappedElements.Add(quadIndex + 1);


                }
            }
            return overlappedElements.count > 0;
        }

        ///<summary>
        /// Gets a world space triangle in the terrain at the given indices (as if it were a mesh).
        ///</summary>
        ///<param name="indices">Indices of the triangle.</param>
        ///<param name="transform">Transform to apply to the triangle vertices.</param>
        ///<param name="a">First vertex of the triangle.</param>
        ///<param name="b">Second vertex of the triangle.</param>
        ///<param name="c">Third vertex of the triangle.</param>
        public void GetTriangle(ref TriangleMeshConvexContactManifold.TriangleIndices indices, ref AffineTransform transform, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            //Reverse the encoded index:
            //index = i + width * j
            int width = heights.GetLength(0);
            int columnA = indices.A / width;
            int rowA = indices.A - columnA * width;
            int columnB = indices.B / width;
            int rowB = indices.B - columnB * width;
            int columnC = indices.C / width;
            int rowC = indices.C - columnC * width;
            GetPosition(rowA, columnA, ref transform, out a);
            GetPosition(rowB, columnB, ref transform, out b);
            GetPosition(rowC, columnC, ref transform, out c);
        }

        ///<summary>
        /// Gets a world space triangle in the terrain at the given triangle index.
        ///</summary>
        ///<param name="index">Index of the triangle.</param>
        ///<param name="transform">Transform to apply to the triangle vertices.</param>
        ///<param name="a">First vertex of the triangle.</param>
        ///<param name="b">Second vertex of the triangle.</param>
        ///<param name="c">Third vertex of the triangle.</param>
        public void GetTriangle(int index, ref AffineTransform transform, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            //Find the quad.
            int quadIndex = index / 2;
            bool isFirstTriangle = quadIndex * 2 == index;
            int column = quadIndex / heights.GetLength(0);
            int row = quadIndex - column * heights.GetLength(0);

            if (quadTriangleOrganization == CollisionShapes.QuadTriangleOrganization.BottomLeftUpperRight)
            {
                if (isFirstTriangle)
                {
                    GetPosition(row, column, ref transform, out a);
                    GetPosition(row + 1, column, ref transform, out b);
                    GetPosition(row, column + 1, ref transform, out c);
                }
                else
                {
                    GetPosition(row, column + 1, ref transform, out a);
                    GetPosition(row + 1, column + 1, ref transform, out b);
                    GetPosition(row + 1, column, ref transform, out c);
                }
            }
            else
            {
                //The quad is BottomRightUpperLeft.
                if (isFirstTriangle)
                {
                    GetPosition(row, column, ref transform, out a);
                    GetPosition(row + 1, column, ref transform, out b);
                    GetPosition(row + 1, column + 1, ref transform, out c);
                }
                else
                {
                    GetPosition(row, column, ref transform, out a);
                    GetPosition(row, column + 1, ref transform, out b);
                    GetPosition(row + 1, column + 1, ref transform, out c);
                }

            }
        }


    }

    /// <summary>
    /// Defines how a Terrain organizes triangles in its quads.
    /// </summary>
    public enum QuadTriangleOrganization
    {
        /// <summary>
        /// Triangle with a right angle at the (-i,-j) position and another at the (+i,+j) position.
        /// </summary>
        BottomLeftUpperRight,
        /// <summary>
        /// Triangle with a right angle at the (+i,-j) position and another at the high (-i,+j) position.
        /// </summary>
        BottomRightUpperLeft
    }
}
