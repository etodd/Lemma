using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.MathExtensions;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.ResourceManagement;
using System.Collections.Generic;
using System;
using BEPUphysics.Settings;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// Local space data associated with a mobile mesh.
    /// This contains a hierarchy and all the other heavy data needed
    /// by an MobileMesh.
    ///</summary>
    public class MobileMeshShape : EntityShape
    {
        private float meshCollisionMargin = CollisionDetectionSettings.DefaultMargin;
        /// <summary>
        /// Gets or sets the margin of the mobile mesh to use when colliding with other meshes.
        /// When colliding with non-mesh shapes, the mobile mesh has no margin.
        /// </summary>
        public float MeshCollisionMargin
        {
            get
            {
                return meshCollisionMargin;
            }
            set
            {
                if (value < 0)
                    throw new Exception("Mesh margin must be nonnegative.");
                meshCollisionMargin = value;
                OnShapeChanged();
            }
        }
        TriangleMesh triangleMesh;
        ///<summary>
        /// Gets or sets the TriangleMesh data structure used by this shape.
        ///</summary>
        public TriangleMesh TriangleMesh
        {
            get
            {
                return triangleMesh;
            }
        }

        /// <summary>
        /// Gets the transform used by the local mesh shape.
        /// </summary>
        public AffineTransform Transform
        {
            get
            {
                return ((TransformableMeshData)triangleMesh.Data).worldTransform;
            }
        }

        RawList<Vector3> surfaceVertices = new RawList<Vector3>();

        internal MobileMeshSolidity solidity = MobileMeshSolidity.DoubleSided;
        ///<summary>
        /// Gets the solidity of the mesh.
        ///</summary>
        public MobileMeshSolidity Solidity
        {
            get
            {
                return solidity;
            }
        }

        /// <summary>
        /// Gets or sets the sidedness of the shape.  This is a convenience property based on the Solidity property.
        /// If the shape is solid, this returns whatever sidedness is computed to make the triangles of the shape face outward.
        /// If the shape is solid, setting this property will change the sidedness that is used while the shape is solid.
        /// </summary>
        public TriangleSidedness Sidedness
        {
            get
            {
                switch (solidity)
                {
                    case MobileMeshSolidity.Clockwise:
                        return TriangleSidedness.Clockwise;
                    case MobileMeshSolidity.Counterclockwise:
                        return TriangleSidedness.Counterclockwise;
                    case MobileMeshSolidity.DoubleSided:
                        return TriangleSidedness.DoubleSided;
                    case MobileMeshSolidity.Solid:
                        return solidSidedness;

                }
                return TriangleSidedness.DoubleSided;
            }
            set
            {
                if (solidity == MobileMeshSolidity.Solid)
                    solidSidedness = value;
                else
                {
                    switch (value)
                    {
                        case TriangleSidedness.Clockwise:
                            solidity = MobileMeshSolidity.Clockwise;
                            break;
                        case TriangleSidedness.Counterclockwise:
                            solidity = MobileMeshSolidity.Counterclockwise;
                            break;
                        case TriangleSidedness.DoubleSided:
                            solidity = MobileMeshSolidity.DoubleSided;
                            break;
                    }
                }
            }
        }

        ///<summary>
        /// Constructs a new mobile mesh shape.
        ///</summary>
        ///<param name="vertices">Vertices of the mesh.</param>
        ///<param name="indices">Indices of the mesh.</param>
        ///<param name="localTransform">Local transform to apply to the shape.</param>
        ///<param name="solidity">Solidity state of the shape.</param>
        public MobileMeshShape(Vector3[] vertices, int[] indices, AffineTransform localTransform, MobileMeshSolidity solidity)
        {
            this.solidity = solidity;
            var data = new TransformableMeshData(vertices, indices, localTransform);
            ShapeDistributionInformation distributionInfo;
            ComputeShapeInformation(data, out distributionInfo);

            for (int i = 0; i < surfaceVertices.count; i++)
            {
                Vector3.Subtract(ref surfaceVertices.Elements[i], ref distributionInfo.Center, out surfaceVertices.Elements[i]);
            }
            triangleMesh = new TriangleMesh(data);

            ComputeSolidSidedness();
        }

        ///<summary>
        /// Constructs a new mobile mesh shape.
        ///</summary>
        ///<param name="vertices">Vertices of the mesh.</param>
        ///<param name="indices">Indices of the mesh.</param>
        ///<param name="localTransform">Local transform to apply to the shape.</param>
        ///<param name="solidity">Solidity state of the shape.</param>
        ///<param name="distributionInfo">Information computed about the shape during construction.</param>
        public MobileMeshShape(Vector3[] vertices, int[] indices, AffineTransform localTransform, MobileMeshSolidity solidity, out ShapeDistributionInformation distributionInfo)
        {
            this.solidity = solidity;
            var data = new TransformableMeshData(vertices, indices, localTransform);
            ComputeShapeInformation(data, out distributionInfo);

            for (int i = 0; i < surfaceVertices.count; i++)
            {
                Vector3.Subtract(ref surfaceVertices.Elements[i], ref distributionInfo.Center, out surfaceVertices.Elements[i]);
            }
            triangleMesh = new TriangleMesh(data);

            ComputeSolidSidedness();
            //ComputeBoundingHull();
        }

        /// <summary>
        /// Sidedness required if the mesh is in solid mode.
        /// If the windings were reversed or double sided,
        /// the solidity would fight against shell contacts,
        /// leading to very bad jittering.
        /// </summary>
        internal TriangleSidedness solidSidedness;


        /// <summary>
        /// Tests to see if a ray's origin is contained within the mesh.
        /// If it is, the hit location is found.
        /// If it isn't, the hit location is still valid if a hit occurred.
        /// If the origin isn't inside and there was no hit, the hit has a T value of float.MaxValue.
        /// </summary>
        /// <param name="ray">Ray in the local space of the shape to test.</param>
        /// <param name="hit">The first hit against the mesh, if any.</param>
        /// <returns>Whether or not the ray origin was in the mesh.</returns>
        public bool IsLocalRayOriginInMesh(ref Ray ray, out RayHit hit)
        {
            var overlapList = Resources.GetIntList();
            hit = new RayHit();
            hit.T = float.MaxValue;
            if (triangleMesh.Tree.GetOverlaps(ray, overlapList))
            {
                bool minimumClockwise = false;
                for (int i = 0; i < overlapList.Count; i++)
                {
                    Vector3 vA, vB, vC;
                    triangleMesh.Data.GetTriangle(overlapList[i], out vA, out vB, out vC);
                    bool hitClockwise;
                    RayHit tempHit;
                    if (Toolbox.FindRayTriangleIntersection(ref ray, float.MaxValue, ref vA, ref vB, ref vC, out hitClockwise, out tempHit) &&
                        tempHit.T < hit.T)
                    {
                        hit = tempHit;
                        minimumClockwise = hitClockwise;
                    }
                }
                Resources.GiveBack(overlapList);

                //If the mesh is hit from behind by the ray on the first hit, then the ray is inside.
                return hit.T < float.MaxValue && ((solidSidedness == TriangleSidedness.Clockwise && !minimumClockwise) || (solidSidedness == TriangleSidedness.Counterclockwise && minimumClockwise));
            }
            Resources.GiveBack(overlapList);
            return false;

        }

        /// <summary>
        /// The difference in t parameters in a ray cast under which two hits are considered to be redundant.
        /// </summary>
        public static float MeshHitUniquenessThreshold = .0001f;

        internal bool IsHitUnique(RawList<RayHit> hits, ref RayHit hit)
        {
            for (int i = 0; i < hits.count; i++)
            {
                if (Math.Abs(hits.Elements[i].T - hit.T) < MeshHitUniquenessThreshold)
                    return false;
            }
            hits.Add(hit);
            return true;
        }

        void ComputeSolidSidedness()
        {
            //Raycast against the mesh.
            //If there's an even number of hits, then the ray start point is outside.
            //If there's an odd number of hits, then the ray start point is inside.

            //If the start is outside, then take the earliest toi hit and calibrate sidedness based on it.
            //If the start is inside, then take the latest toi hit and calibrate sidedness based on it.

            //This test assumes consistent winding across the entire mesh as well as a closed surface.
            //If those assumptions are not correct, then the raycast cannot determine inclusion or exclusion,
            //or there exists no calibration that will work across the entire surface.

            //Pick a ray direction that goes to a random location on the mesh.  
            //A vertex would work, but targeting the middle of a triangle avoids some edge cases.
            var ray = new Ray();
            Vector3 vA, vB, vC;
            triangleMesh.Data.GetTriangle(((triangleMesh.Data.indices.Length / 3) / 2) * 3, out vA, out vB, out vC);
            ray.Direction = (vA + vB + vC) / 3;
            ray.Direction.Normalize();

            solidSidedness = ComputeSolidSidednessHelper(ray);
            //TODO: Positions need to be valid for the verifying directions to work properly.
            ////Find another direction and test it to corroborate the first test.
            //Ray alternateRay;
            //alternateRay.Position = ray.Position;
            //Vector3.Cross(ref ray.Direction, ref Toolbox.UpVector, out alternateRay.Direction);
            //float lengthSquared = alternateRay.Direction.LengthSquared();
            //if (lengthSquared < Toolbox.Epsilon)
            //{
            //    Vector3.Cross(ref ray.Direction, ref Toolbox.RightVector, out alternateRay.Direction);
            //    lengthSquared = alternateRay.Direction.LengthSquared();
            //}
            //Vector3.Divide(ref alternateRay.Direction, (float)Math.Sqrt(lengthSquared), out alternateRay.Direction);
            //var sidednessCandidate2 = ComputeSolidSidednessHelper(alternateRay);
            //if (sidednessCandidate == sidednessCandidate2)
            //{
            //    //The two tests agreed! It's very likely that the sidedness is, in fact, in this direction.
            //    solidSidedness = sidednessCandidate;
            //}
            //else
            //{
            //    //The two tests disagreed.  Tiebreaker!
            //    Vector3.Cross(ref alternateRay.Direction, ref ray.Direction, out alternateRay.Direction);
            //    solidSidedness = ComputeSolidSidednessHelper(alternateRay);
            //}
        }

        TriangleSidedness ComputeSolidSidednessHelper(Ray ray)
        {
            TriangleSidedness toReturn;
            var hitList = Resources.GetIntList();
            if (triangleMesh.Tree.GetOverlaps(ray, hitList))
            {
                Vector3 vA, vB, vC;
                var hits = Resources.GetRayHitList();
                //Identify the first and last hits.
                int minimum = 0;
                int maximum = 0;
                float minimumT = float.MaxValue;
                float maximumT = -1;
                for (int i = 0; i < hitList.Count; i++)
                {
                    triangleMesh.Data.GetTriangle(hitList[i], out vA, out vB, out vC);
                    RayHit hit;
                    if (Toolbox.FindRayTriangleIntersection(ref ray, float.MaxValue, TriangleSidedness.DoubleSided, ref vA, ref vB, ref vC, out hit) &&
                        IsHitUnique(hits, ref hit))
                    {
                        if (hit.T < minimumT)
                        {
                            minimumT = hit.T;
                            minimum = hitList[i];
                        }
                        if (hit.T > maximumT)
                        {
                            maximumT = hit.T;
                            maximum = hitList[i];
                        }
                    }
                }

                if (hits.count % 2 == 0)
                {
                    //Since we were outside, the first hit triangle should be calibrated
                    //such that it faces towards us.

                    triangleMesh.Data.GetTriangle(minimum, out vA, out vB, out vC);
                    var normal = Vector3.Cross(vA - vB, vA - vC);
                    if (Vector3.Dot(normal, ray.Direction) < 0)
                        toReturn = TriangleSidedness.Clockwise;
                    else
                        toReturn = TriangleSidedness.Counterclockwise;
                }
                else
                {
                    //Since we were inside, the last hit triangle should be calibrated
                    //such that it faces away from us.

                    triangleMesh.Data.GetTriangle(maximum, out vA, out vB, out vC);
                    var normal = Vector3.Cross(vA - vB, vA - vC);
                    if (Vector3.Dot(normal, ray.Direction) < 0)
                        toReturn = TriangleSidedness.Counterclockwise;
                    else
                        toReturn = TriangleSidedness.Clockwise;
                }

                Resources.GiveBack(hits);

            }
            else
                toReturn = TriangleSidedness.DoubleSided; //This is a problem...
            Resources.GiveBack(hitList);
            return toReturn;
        }

        void ComputeShapeInformation(TransformableMeshData data, out ShapeDistributionInformation shapeInformation)
        {
            //Compute the surface vertices of the shape.
            surfaceVertices.Clear();
            try
            {
                ConvexHullHelper.GetConvexHull(data.vertices, surfaceVertices);
                for (int i = 0; i < surfaceVertices.count; i++)
                {
                    AffineTransform.Transform(ref surfaceVertices.Elements[i], ref data.worldTransform, out surfaceVertices.Elements[i]);
                }
            }
            catch
            {
                surfaceVertices.Clear();
                //If the convex hull failed, then the point set has no volume.  A mobile mesh is allowed to have zero volume, however.
                //In this case, compute the bounding box of all points.
                BoundingBox box = new BoundingBox();
                for (int i = 0; i < data.vertices.Length; i++)
                {
                    Vector3 v;
                    data.GetVertexPosition(i, out v);
                    if (v.X > box.Max.X)
                        box.Max.X = v.X;
                    if (v.X < box.Min.X)
                        box.Min.X = v.X;
                    if (v.Y > box.Max.Y)
                        box.Max.Y = v.Y;
                    if (v.Y < box.Min.Y)
                        box.Min.Y = v.Y;
                    if (v.Z > box.Max.Z)
                        box.Max.Z = v.Z;
                    if (v.Z < box.Min.Z)
                        box.Min.Z = v.Z;
                }
                //Add the corners.  This will overestimate the size of the surface a bit.
                surfaceVertices.Add(box.Min);
                surfaceVertices.Add(box.Max);
                surfaceVertices.Add(new Vector3(box.Min.X, box.Min.Y, box.Max.Z));
                surfaceVertices.Add(new Vector3(box.Min.X, box.Max.Y, box.Min.Z));
                surfaceVertices.Add(new Vector3(box.Max.X, box.Min.Y, box.Min.Z));
                surfaceVertices.Add(new Vector3(box.Min.X, box.Max.Y, box.Max.Z));
                surfaceVertices.Add(new Vector3(box.Max.X, box.Max.Y, box.Min.Z));
                surfaceVertices.Add(new Vector3(box.Max.X, box.Min.Y, box.Max.Z));
            }
            shapeInformation.Center = new Vector3();

            if (solidity == MobileMeshSolidity.Solid)
            {

                //The following inertia tensor calculation assumes a closed mesh.

                shapeInformation.Volume = 0;
                for (int i = 0; i < data.indices.Length; i += 3)
                {
                    Vector3 v2, v3, v4;
                    data.GetTriangle(i, out v2, out v3, out v4);

                    //Determinant is 6 * volume.  It's signed, though; this is because the mesh isn't necessarily convex nor centered on the origin.
                    float tetrahedronVolume = v2.X * (v3.Y * v4.Z - v3.Z * v4.Y) -
                                              v3.X * (v2.Y * v4.Z - v2.Z * v4.Y) +
                                              v4.X * (v2.Y * v3.Z - v2.Z * v3.Y);

                    shapeInformation.Volume += tetrahedronVolume;
                    shapeInformation.Center += tetrahedronVolume * (v2 + v3 + v4);
                }
                shapeInformation.Center /= shapeInformation.Volume * 4;
                shapeInformation.Volume /= 6;
                shapeInformation.Volume = Math.Abs(shapeInformation.Volume);

                data.worldTransform.Translation -= shapeInformation.Center;

                //Source: Explicit Exact Formulas for the 3-D Tetrahedron Inertia Tensor in Terms of its Vertex Coordinates
                //http://www.scipub.org/fulltext/jms2/jms2118-11.pdf
                //x1, x2, x3, x4 are origin, triangle1, triangle2, triangle3
                //Looking to find inertia tensor matrix of the form
                // [  a  -b' -c' ]
                // [ -b'  b  -a' ]
                // [ -c' -a'  c  ]
                float a = 0, b = 0, c = 0, ao = 0, bo = 0, co = 0;

                float totalWeight = 0;
                for (int i = 0; i < data.indices.Length; i += 3)
                {
                    Vector3 v2, v3, v4;
                    data.GetTriangle(i, out v2, out v3, out v4);

                    //Determinant is 6 * volume.  It's signed, though; this is because the mesh isn't necessarily convex nor centered on the origin.
                    float tetrahedronVolume = v2.X * (v3.Y * v4.Z - v3.Z * v4.Y) -
                                              v3.X * (v2.Y * v4.Z - v2.Z * v4.Y) +
                                              v4.X * (v2.Y * v3.Z - v2.Z * v3.Y);

                    totalWeight += tetrahedronVolume;

                    a += tetrahedronVolume * (v2.Y * v2.Y + v2.Y * v3.Y + v3.Y * v3.Y + v2.Y * v4.Y + v3.Y * v4.Y + v4.Y * v4.Y +
                                              v2.Z * v2.Z + v2.Z * v3.Z + v3.Z * v3.Z + v2.Z * v4.Z + v3.Z * v4.Z + v4.Z * v4.Z);
                    b += tetrahedronVolume * (v2.X * v2.X + v2.X * v3.X + v3.X * v3.X + v2.X * v4.X + v3.X * v4.X + v4.X * v4.X +
                                              v2.Z * v2.Z + v2.Z * v3.Z + v3.Z * v3.Z + v2.Z * v4.Z + v3.Z * v4.Z + v4.Z * v4.Z);
                    c += tetrahedronVolume * (v2.X * v2.X + v2.X * v3.X + v3.X * v3.X + v2.X * v4.X + v3.X * v4.X + v4.X * v4.X +
                                              v2.Y * v2.Y + v2.Y * v3.Y + v3.Y * v3.Y + v2.Y * v4.Y + v3.Y * v4.Y + v4.Y * v4.Y);
                    ao += tetrahedronVolume * (2 * v2.Y * v2.Z + v3.Y * v2.Z + v4.Y * v2.Z + v2.Y * v3.Z + 2 * v3.Y * v3.Z + v4.Y * v3.Z + v2.Y * v4.Z + v3.Y * v4.Z + 2 * v4.Y * v4.Z);
                    bo += tetrahedronVolume * (2 * v2.X * v2.Z + v3.X * v2.Z + v4.X * v2.Z + v2.X * v3.Z + 2 * v3.X * v3.Z + v4.X * v3.Z + v2.X * v4.Z + v3.X * v4.Z + 2 * v4.X * v4.Z);
                    co += tetrahedronVolume * (2 * v2.X * v2.Y + v3.X * v2.Y + v4.X * v2.Y + v2.X * v3.Y + 2 * v3.X * v3.Y + v4.X * v3.Y + v2.X * v4.Y + v3.X * v4.Y + 2 * v4.X * v4.Y);
                }
                float density = 1 / totalWeight;
                float diagonalFactor = density / 10;
                float offFactor = -density / 20;
                a *= diagonalFactor;
                b *= diagonalFactor;
                c *= diagonalFactor;
                ao *= offFactor;
                bo *= offFactor;
                co *= offFactor;
                shapeInformation.VolumeDistribution = new Matrix3X3(a, bo, co,
                                                                    bo, b, ao,
                                                                    co, ao, c);


            }
            else
            {
                shapeInformation.Center = new Vector3();
                float totalWeight = 0;
                for (int i = 0; i < data.indices.Length; i += 3)
                { //Configure the inertia tensor to be local.
                    Vector3 vA, vB, vC;
                    data.GetTriangle(i, out vA, out vB, out vC);
                    Vector3 vAvB;
                    Vector3 vAvC;
                    Vector3.Subtract(ref vB, ref vA, out vAvB);
                    Vector3.Subtract(ref vC, ref vA, out vAvC);
                    Vector3 cross;
                    Vector3.Cross(ref vAvB, ref vAvC, out cross);
                    float weight = cross.Length();
                    totalWeight += weight;

                    shapeInformation.Center += weight * (vA + vB + vC) / 3;


                }
                shapeInformation.Center /= totalWeight;
                shapeInformation.Volume = 0;


                data.worldTransform.Translation -= shapeInformation.Center;

                shapeInformation.VolumeDistribution = new Matrix3X3();
                for (int i = 0; i < data.indices.Length; i += 3)
                { //Configure the inertia tensor to be local.
                    Vector3 vA, vB, vC;
                    data.GetTriangle(i, out vA, out vB, out vC);
                    Vector3 vAvB;
                    Vector3 vAvC;
                    Vector3.Subtract(ref vB, ref vA, out vAvB);
                    Vector3.Subtract(ref vC, ref vA, out vAvC);
                    Vector3 cross;
                    Vector3.Cross(ref vAvB, ref vAvC, out cross);
                    float weight = cross.Length();
                    totalWeight += weight;

                    Matrix3X3 innerProduct;
                    Matrix3X3.CreateScale(vA.LengthSquared(), out innerProduct);
                    Matrix3X3 outerProduct;
                    Matrix3X3.CreateOuterProduct(ref vA, ref vA, out outerProduct);
                    Matrix3X3 contribution;
                    Matrix3X3.Subtract(ref innerProduct, ref outerProduct, out contribution);
                    Matrix3X3.Multiply(ref contribution, weight, out contribution);
                    Matrix3X3.Add(ref shapeInformation.VolumeDistribution, ref contribution, out shapeInformation.VolumeDistribution);

                    Matrix3X3.CreateScale(vB.LengthSquared(), out innerProduct);
                    Matrix3X3.CreateOuterProduct(ref vB, ref vB, out outerProduct);
                    Matrix3X3.Subtract(ref innerProduct, ref outerProduct, out outerProduct);
                    Matrix3X3.Multiply(ref contribution, weight, out contribution);
                    Matrix3X3.Add(ref shapeInformation.VolumeDistribution, ref contribution, out shapeInformation.VolumeDistribution);

                    Matrix3X3.CreateScale(vC.LengthSquared(), out innerProduct);
                    Matrix3X3.CreateOuterProduct(ref vC, ref vC, out outerProduct);
                    Matrix3X3.Subtract(ref innerProduct, ref outerProduct, out contribution);
                    Matrix3X3.Multiply(ref contribution, weight, out contribution);
                    Matrix3X3.Add(ref shapeInformation.VolumeDistribution, ref contribution, out shapeInformation.VolumeDistribution);

                }
                Matrix3X3.Multiply(ref shapeInformation.VolumeDistribution, 1 / (6 * totalWeight), out shapeInformation.VolumeDistribution);
            }

            ////Configure the inertia tensor to be local.
            //Vector3 finalOffset = shapeInformation.Center;
            //Matrix3X3 finalInnerProduct;
            //Matrix3X3.CreateScale(finalOffset.LengthSquared(), out finalInnerProduct);
            //Matrix3X3 finalOuterProduct;
            //Matrix3X3.CreateOuterProduct(ref finalOffset, ref finalOffset, out finalOuterProduct);

            //Matrix3X3 finalContribution;
            //Matrix3X3.Subtract(ref finalInnerProduct, ref finalOuterProduct, out finalContribution);

            //Matrix3X3.Subtract(ref shapeInformation.VolumeDistribution, ref finalContribution, out shapeInformation.VolumeDistribution);
        }

        ///// <summary>
        ///// Defines two planes that bound the mesh shape in local space.
        ///// </summary>
        //struct Extent
        //{
        //    internal Vector3 Direction;
        //    internal float Minimum;
        //    internal float Maximum;

        //    internal void Clamp(ref Vector3 v)
        //    {
        //        float dot;
        //        Vector3.Dot(ref v, ref Direction, out dot);
        //        float difference;
        //        if (dot < Minimum)
        //        {
        //            difference = dot - Minimum;
        //        }
        //        else if (dot > Maximum)
        //        {
        //            difference = dot - Maximum;
        //        }
        //        else return;

        //        //Subtract the component of v which is parallel to the normal.
        //        v.X -= difference * Direction.X;
        //        v.Y -= difference * Direction.Y;
        //        v.Z -= difference * Direction.Z;
        //    }

        //}

        //RawList<Extent> extents = new RawList<Extent>();

        //void ComputeBoundingHull()
        //{
        //    //TODO:
        //    //While we have computed a convex hull of the shape already, we don't really
        //    //need the full tightness of the convex hull.
        //    extents.Add(new Extent() { Direction = new Vector3(1, 0, 0) });
        //    extents.Add(new Extent() { Direction = new Vector3(0, 1, 0) });
        //    extents.Add(new Extent() { Direction = new Vector3(0, 0, 1) });
        //    //extents.Add(new Extent() { Direction = new Vector3(1, 1, 0) });
        //    //extents.Add(new Extent() { Direction = new Vector3(-1, 1, 0) });
        //    //extents.Add(new Extent() { Direction = new Vector3(0, 1, 1) });
        //    //extents.Add(new Extent() { Direction = new Vector3(0, 1, -1) });
        //    extents.Add(new Extent() { Direction = Vector3.Normalize(new Vector3(1, 0, 1)) });
        //    extents.Add(new Extent() { Direction = Vector3.Normalize(new Vector3(1, 0, -1)) });
        //    //Add more extents for a tighter volume

        //    //Initialize the max and mins.
        //    for (int i = 0; i < extents.count; i++)
        //    {
        //        extents.Elements[i].Minimum = float.MaxValue;
        //        extents.Elements[i].Maximum = -float.MaxValue;
        //    }

        //    for (int i = 0; i < triangleMesh.Data.vertices.Length; i++)
        //    {
        //        Vector3 v;
        //        triangleMesh.Data.GetVertexPosition(i, out v);
        //        for (int j = 0; j < extents.count; j++)
        //        {
        //            float dot;
        //            Vector3.Dot(ref v, ref extents.Elements[j].Direction, out dot);
        //            if (dot < extents.Elements[j].Minimum)
        //                extents.Elements[j].Minimum = dot;
        //            if (dot > extents.Elements[j].Maximum)
        //                extents.Elements[j].Maximum = dot;
        //        }
        //    }
        //}

        private void GetBoundingBox(ref Matrix3X3 o, out BoundingBox boundingBox)
        {
#if !WINDOWS
            boundingBox = new BoundingBox();
#endif
            //Sample the local directions from the matrix, implicitly transposed.
            var rightDirection = new Vector3(o.M11, o.M21, o.M31);
            var upDirection = new Vector3(o.M12, o.M22, o.M32);
            var backDirection = new Vector3(o.M13, o.M23, o.M33);

            int right = 0, left = 0, up = 0, down = 0, backward = 0, forward = 0;
            float minX = float.MaxValue, maxX = -float.MaxValue, minY = float.MaxValue, maxY = -float.MaxValue, minZ = float.MaxValue, maxZ = -float.MaxValue;

            for (int i = 0; i < surfaceVertices.count; i++)
            {
                float dotX, dotY, dotZ;
                Vector3.Dot(ref rightDirection, ref surfaceVertices.Elements[i], out dotX);
                Vector3.Dot(ref upDirection, ref surfaceVertices.Elements[i], out dotY);
                Vector3.Dot(ref backDirection, ref surfaceVertices.Elements[i], out dotZ);
                if (dotX < minX)
                {
                    minX = dotX;
                    left = i;
                }
                if (dotX > maxX)
                {
                    maxX = dotX;
                    right = i;
                }

                if (dotY < minY)
                {
                    minY = dotY;
                    down = i;
                }
                if (dotY > maxY)
                {
                    maxY = dotY;
                    up = i;
                }

                if (dotZ < minZ)
                {
                    minZ = dotZ;
                    forward = i;
                }
                if (dotZ > maxZ)
                {
                    maxZ = dotZ;
                    backward = i;
                }

            }

            //Incorporate the collision margin.
            Vector3.Multiply(ref rightDirection, meshCollisionMargin / (float)Math.Sqrt(rightDirection.Length()), out rightDirection);
            Vector3.Multiply(ref upDirection, meshCollisionMargin / (float)Math.Sqrt(upDirection.Length()), out upDirection);
            Vector3.Multiply(ref backDirection, meshCollisionMargin / (float)Math.Sqrt(backDirection.Length()), out backDirection);

            var rightElement = surfaceVertices.Elements[right];
            var leftElement = surfaceVertices.Elements[left];
            var upElement = surfaceVertices.Elements[up];
            var downElement = surfaceVertices.Elements[down];
            var backwardElement = surfaceVertices.Elements[backward];
            var forwardElement = surfaceVertices.Elements[forward];
            Vector3.Add(ref rightElement, ref rightDirection, out rightElement);
            Vector3.Subtract(ref leftElement, ref rightDirection, out leftElement);
            Vector3.Add(ref upElement, ref upDirection, out upElement);
            Vector3.Subtract(ref downElement, ref upDirection, out downElement);
            Vector3.Add(ref backwardElement, ref backDirection, out backwardElement);
            Vector3.Subtract(ref forwardElement, ref backDirection, out forwardElement);

            //TODO: This could be optimized.  Unnecessary transformation information gets computed.
            Vector3 vMinX, vMaxX, vMinY, vMaxY, vMinZ, vMaxZ;
            Matrix3X3.Transform(ref rightElement, ref o, out vMaxX);
            Matrix3X3.Transform(ref leftElement, ref o, out vMinX);
            Matrix3X3.Transform(ref upElement, ref o, out vMaxY);
            Matrix3X3.Transform(ref downElement, ref o, out vMinY);
            Matrix3X3.Transform(ref backwardElement, ref o, out vMaxZ);
            Matrix3X3.Transform(ref forwardElement, ref o, out vMinZ);


            boundingBox.Max.X = vMaxX.X;
            boundingBox.Max.Y = vMaxY.Y;
            boundingBox.Max.Z = vMaxZ.Z;

            boundingBox.Min.X = vMinX.X;
            boundingBox.Min.Y = vMinY.Y;
            boundingBox.Min.Z = vMinZ.Z;
        }

        ///<summary>
        /// Computes the bounding box of the transformed mesh shape.
        ///</summary>
        ///<param name="shapeTransform">Transform to apply to the shape during the bounding box calculation.</param>
        ///<param name="boundingBox">Bounding box containing the transformed mesh shape.</param>
        public void GetBoundingBox(ref RigidTransform shapeTransform, out BoundingBox boundingBox)
        {
            ////TODO: Could use an approximate bounding volume.  Would be cheaper at runtime and use less memory, though the box would be bigger.
            //Matrix3X3 o;
            //Matrix3X3.CreateFromQuaternion(ref shapeTransform.Orientation, out o);
            ////Sample the local directions from the orientation matrix, implicitly transposed.
            //Vector3 right = new Vector3(o.M11 * 100000, o.M21 * 100000, o.M31 * 100000);
            //Vector3 up = new Vector3(o.M12 * 100000, o.M22 * 100000, o.M32 * 100000);
            //Vector3 backward = new Vector3(o.M13 * 100000, o.M23 * 100000, o.M33 * 100000);
            //Vector3 left, down, forward;
            //Vector3.Negate(ref right, out left);
            //Vector3.Negate(ref up, out down);
            //Vector3.Negate(ref backward, out forward);
            //for (int i = 0; i < extents.count; i++)
            //{
            //    extents.Elements[i].Clamp(ref right);
            //    extents.Elements[i].Clamp(ref left);
            //    extents.Elements[i].Clamp(ref up);
            //    extents.Elements[i].Clamp(ref down);
            //    extents.Elements[i].Clamp(ref backward);
            //    extents.Elements[i].Clamp(ref forward);
            //}

            //Matrix3X3.Transform(ref right, ref o, out right);
            //Matrix3X3.Transform(ref left, ref o, out left);
            //Matrix3X3.Transform(ref down, ref o, out down);
            //Matrix3X3.Transform(ref up, ref o, out up);
            //Matrix3X3.Transform(ref forward, ref o, out forward);
            //Matrix3X3.Transform(ref backward, ref o, out backward);


            //boundingBox.Max.X = shapeTransform.Position.X + right.X;
            //boundingBox.Max.Y = shapeTransform.Position.Y + up.Y;
            //boundingBox.Max.Z = shapeTransform.Position.Z + backward.Z;

            //boundingBox.Min.X = shapeTransform.Position.X + left.X;
            //boundingBox.Min.Y = shapeTransform.Position.Y + down.Y;
            //boundingBox.Min.Z = shapeTransform.Position.Z + forward.Z;


            Matrix3X3 o;
            Matrix3X3.CreateFromQuaternion(ref shapeTransform.Orientation, out o);
            GetBoundingBox(ref o, out boundingBox);


            boundingBox.Max.X += shapeTransform.Position.X;
            boundingBox.Max.Y += shapeTransform.Position.Y;
            boundingBox.Max.Z += shapeTransform.Position.Z;

            boundingBox.Min.X += shapeTransform.Position.X;
            boundingBox.Min.Y += shapeTransform.Position.Y;
            boundingBox.Min.Z += shapeTransform.Position.Z;

        }


        /// <summary>
        /// Gets the bounding box of the mesh transformed first into world space, and then into the local space of another affine transform.
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

            GetBoundingBox(ref transform.LinearTransform, out boundingBox);
            boundingBox.Max.X += transform.Translation.X;
            boundingBox.Max.Y += transform.Translation.Y;
            boundingBox.Max.Z += transform.Translation.Z;

            boundingBox.Min.X += transform.Translation.X;
            boundingBox.Min.Y += transform.Translation.Y;
            boundingBox.Min.Z += transform.Translation.Z;

        }

        /// <summary>
        /// Gets the bounding box of the mesh transformed first into world space, and then into the local space of another affine transform.
        /// </summary>
        /// <param name="shapeTransform">Transform to use to put the shape into world space.</param>
        /// <param name="spaceTransform">Used as the frame of reference to compute the bounding box.
        /// In effect, the shape is transformed by the inverse of the space transform to compute its bounding box in local space.</param>
        /// <param name="sweep">World space sweep direction to transform and add to the bounding box.</param>
        /// <param name="boundingBox">Bounding box in the local space.</param>
        public void GetSweptLocalBoundingBox(ref RigidTransform shapeTransform, ref AffineTransform spaceTransform, ref Vector3 sweep, out BoundingBox boundingBox)
        {
            GetLocalBoundingBox(ref shapeTransform, ref spaceTransform, out boundingBox);
            Vector3 expansion;
            Matrix3X3.TransformTranspose(ref sweep, ref spaceTransform.LinearTransform, out expansion);
            Toolbox.ExpandBoundingBox(ref boundingBox, ref expansion);
        }


        /// <summary>
        /// Computes the volume, center of mass, and volume distribution of the shape.
        /// </summary>
        /// <param name="shapeInfo">Data about the shape.</param>
        public override void ComputeDistributionInformation(out ShapeDistributionInformation shapeInfo)
        {
            ComputeShapeInformation(this.TriangleMesh.Data as TransformableMeshData, out shapeInfo);
        }

        public override Collidables.MobileCollidables.EntityCollidable GetCollidableInstance()
        {
            return new MobileMeshCollidable(this);
        }



      
    }

    ///<summary>
    /// Solidity of a triangle or mesh.
    /// A triangle can be double sided, or allow one of its sides to let interacting objects through.
    /// The entire mesh can be made solid, which means objects on the interior still generate contacts even if there aren't any triangles to hit.
    /// Solidity requires the mesh to be closed.
    ///</summary>
    public enum MobileMeshSolidity
    {
        /// <summary>
        /// The mesh will interact with objects coming from both directions.
        /// </summary>
        DoubleSided,
        /// <summary>
        /// The mesh will interact with objects from which the winding of the triangles appears to be clockwise.
        /// </summary>
        Clockwise,
        /// <summary>
        /// The mesh will interact with objects from which the winding of the triangles appears to be counterclockwise.
        /// </summary>
        Counterclockwise,
        /// <summary>
        /// The mesh will treat objects inside of its concave shell as if the mesh had volume.  Mesh must be closed for this to work properly.
        /// </summary>
        Solid
    }
}
