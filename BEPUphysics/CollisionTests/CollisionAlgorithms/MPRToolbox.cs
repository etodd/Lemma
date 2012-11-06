using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace BEPUphysics.CollisionTests.CollisionAlgorithms
{
    /// <summary>
    /// Contains a variety of queries and computation methods that make use of minkowski portal refinement.
    /// </summary>
    public static class MPRToolbox
    {
        //TODO: Lots of nasty code repeat.
        //The common phases of portal construction, correction, and refinement are implemented with subtle differences.
        //With some effort, they could be shoved together.

        /// <summary>
        /// Number of iterations that the MPR system will run in its inner loop before giving up and returning with failure.
        /// </summary>
        public static int InnerIterationLimit = 15;
        /// <summary>
        /// Number of iterations that the MPR system will run in its outer loop before giving up and moving on to its inner loop.
        /// </summary>
        public static int OuterIterationLimit = 15;

        private static float surfaceEpsilon = 1e-7f;
        /// <summary>
        /// Gets or sets how close surface-finding based MPR methods have to get before exiting.
        /// Defaults to 1e-7.
        /// </summary>
        public static float SurfaceEpsilon
        {
            get
            {
                return surfaceEpsilon;
            }
            set
            {
                if (value > 0)
                    surfaceEpsilon = value;
                else throw new Exception("Epsilon must be positive.");

            }
        }

        private static float depthRefinementEpsilon = 1e-4f;
        /// <summary>
        /// Gets or sets how close the penetration depth refinement system should converge before quitting.
        /// Making this smaller can help more precisely find a local minimum at the cost of performance.
        /// The change will likely only be visible on curved shapes, since polytopes will converge extremely rapidly to a precise local minimum.
        /// Defaults to 1e-4.
        /// </summary>
        public static float DepthRefinementEpsilon
        {
            get
            {
                return depthRefinementEpsilon;
            }
            set
            {
                if (value > 0)
                    depthRefinementEpsilon = value;
                else throw new Exception("Epsilon must be positive.");

            }
        }

        private static float rayCastSurfaceEpsilon = 1e-9f;
        /// <summary>
        /// Gets or sets how close surface-finding ray casts have to get before exiting.
        /// Defaults to 1e-9.
        /// </summary>
        public static float RayCastSurfaceEpsilon
        {
            get
            {
                return rayCastSurfaceEpsilon;
            }
            set
            {
                if(value > 0)
                    rayCastSurfaceEpsilon = value;
                else
                    throw new Exception("Epsilon must be positive.");
            }
        }

        private static int maximumDepthRefinementIterations = 3;
        /// <summary>
        /// Gets or sets the maximum number of iterations to use to reach the local penetration depth minimum when using the RefinePenetration function.
        /// Increasing this allows the system to work longer to find local penetration minima.
        /// The change will likely only be visible on curved shapes, since polytopes will converge extremely rapidly to a precise local minimum.
        /// Defaults to 3.
        /// </summary>
        public static int MaximumDepthRefinementIterations
        {
            get
            {
                return maximumDepthRefinementIterations;
            }
            set
            {
                if (value > 0)
                    maximumDepthRefinementIterations = value;
                else throw new Exception("Iteration count must be positive.");
            }
        }

        /// <summary>
        /// Gets a world space point in the overlapped volume between two shapes.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="transformA">Transformation to apply to the first shape.</param>
        /// <param name="transformB">Transformation to apply to the second shape.</param>
        /// <param name="position">Position within the overlapped volume of the two shapes, if any.</param>
        /// <returns>Whether or not the two shapes overlap.</returns>
        public static bool GetOverlapPosition(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB, out Vector3 position)
        {
            RigidTransform localTransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localTransformB);
            bool toReturn = GetLocalOverlapPosition(shapeA, shapeB, ref localTransformB, out position);
            RigidTransform.Transform(ref position, ref transformA, out position);
            return toReturn;

        }


        /// <summary>
        /// Gets a point in the overlapped volume between two shapes in shape A's local space.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="position">Position within the overlapped volume of the two shapes in shape A's local space, if any.</param>
        /// <returns>Whether or not the two shapes overlap.</returns>
        public static bool GetLocalOverlapPosition(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, out Vector3 position)
        {
            return GetLocalOverlapPosition(shapeA, shapeB, ref localTransformB.Position, ref localTransformB, out position);
        }
        
        internal static bool GetLocalOverlapPosition(ConvexShape shapeA, ConvexShape shapeB, ref Vector3 originRay, ref RigidTransform localTransformB, out Vector3 position)
        {
            //Compute the origin ray.  This points from a point known to be inside the minkowski sum to the origin.
            //The centers of the shapes are used to create the interior point.

            //It's possible that the two objects' centers are overlapping, or very very close to it.  In this case, 
            //they are obviously colliding and we can immediately exit.
            if (originRay.LengthSquared() < Toolbox.Epsilon)
            {
                position = new Vector3();
                //DEBUGlastPosition = position;
                return true;
            }

            Vector3 v0;
            Vector3.Negate(ref originRay, out v0); //Since we're in A's local space, A-B is just -B.



            //Now that the origin ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = originRay;
            Vector3 v1;
            Vector3 v1A, v1B; //extreme point contributions from each shape.  Used later to compute contact position; could be used to cache simplex too.
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1A, out v1B, out v1);

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2;
            Vector3 v2A, v2B;
            Vector3.Cross(ref v1, ref v0, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!
                //If the origin is further along this direction than the extreme point, then there is no intersection.
                //If the origin is within this extreme point, then there is an intersection.
                float dot;
                Vector3.Dot(ref v1, ref originRay, out dot);
                if (dot < 0)
                {
                    //Origin is outside.
                    position = new Vector3();
                    return false;
                }
                //Origin is inside.
                //Compute barycentric coordinates along simplex (segment).
                float dotv0;
                //Dot > 0, so dotv0 starts out negative.
                Vector3.Dot(ref v0, ref originRay, out dotv0);
                float barycentricCoordinate = -dotv0 / (dot - dotv0);
                //Vector3.Subtract(ref v1A, ref v0A, out offset); //'v0a' is just the zero vector, so there's no need to calculate the offset.
                Vector3.Multiply(ref v1A, barycentricCoordinate, out position);
                return true;
            }
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2A, out v2B, out v2);

            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Subtract(ref v1, ref v0, out temp1);
            Vector3.Subtract(ref v2, ref v0, out temp2);
            Vector3.Cross(ref temp1, ref temp2, out n);


            Vector3 v3A, v3B, v3;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3A, out v3B, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                    break;
                count++;
                //By now, the simplex is a tetrahedron, but it is not known whether or not the origin ray found earlier actually passes through the portal
                //defined by v1, v2, v3.

                // If the origin is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                float dot;
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    v2A = v3A;
                    v2B = v3B;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v1, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }

                // If the origin is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    v1A = v3A;
                    v1B = v3B;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v2, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            while (true)
            {
                //Test the origin against the plane defined by v1, v2, v3.  If it's inside, we're done.
                //Compute the outward facing normal.
                Vector3.Subtract(ref v3, ref v2, out temp1);
                Vector3.Subtract(ref v1, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);
                float dot;
                Vector3.Dot(ref n, ref v1, out dot);
                if (dot >= 0)
                {
                    Vector3 temp3;
                    //Compute the barycentric coordinates of the origin.
                    //This is done by computing the scaled volume (parallelepiped) of the tetrahedra 
                    //formed by each triangle of the v0v1v2v3 tetrahedron and the origin.

                    //TODO: consider a different approach using T parameter or something.
                    Vector3.Subtract(ref v1, ref v0, out temp1);
                    Vector3.Subtract(ref v2, ref v0, out temp2);
                    Vector3.Subtract(ref v3, ref v0, out temp3);

                    Vector3 cross;
                    Vector3.Cross(ref temp1, ref temp2, out cross);
                    float v0v1v2v3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0v1v2v3volume);

                    Vector3.Cross(ref v1, ref v2, out cross);
                    float ov1v2v3volume;
                    Vector3.Dot(ref cross, ref v3, out ov1v2v3volume);

                    Vector3.Cross(ref originRay, ref temp2, out cross);
                    float v0ov2v3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0ov2v3volume);

                    Vector3.Cross(ref temp1, ref originRay, out cross);
                    float v0v1ov3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0v1ov3volume);

                    if (v0v1v2v3volume > Toolbox.Epsilon * .01f)
                    {
                        float inverseTotalVolume = 1 / v0v1v2v3volume;
                        float v0Weight = ov1v2v3volume * inverseTotalVolume;
                        float v1Weight = v0ov2v3volume * inverseTotalVolume;
                        float v2Weight = v0v1ov3volume * inverseTotalVolume;
                        float v3Weight = 1 - v0Weight - v1Weight - v2Weight;
                        position = v1Weight * v1A + v2Weight * v2A + v3Weight * v3A;
                    }
                    else
                    {
                        position = new Vector3();
                    }
                    //DEBUGlastPosition = position;
                    return true;
                }

                //We haven't yet found the origin.  Find the support point in the portal's outward facing direction.
                Vector3 v4, v4A, v4B;
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4A, out v4B, out v4);
                //If the origin is further along the direction than the extreme point, it's not inside the shape.
                float dot2;
                Vector3.Dot(ref v4, ref n, out dot2);
                if (dot2 < 0)
                {
                    //The origin is outside!
                    position = new Vector3();
                    return false;
                }

                //If the plane which generated the normal is very close to the extreme point, then we're at the surface
                //and we have not found the origin; it's either just BARELY inside, or it is outside.  Assume it's outside.
                if (dot2 - dot < surfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    position = new Vector3();
                    //DEBUGlastPosition = position;
                    return false;
                }
                count++;

                //Still haven't exited, so refine the portal.
                //Test origin against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)
                Vector3.Cross(ref v4, ref v0, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                        v1A = v4A;
                        v1B = v4B;
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                        v3A = v4A;
                        v3B = v4B;
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                        v2A = v4A;
                        v2B = v4B;
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                        v1A = v4A;
                        v1B = v4B;
                    }
                }

                //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
                //    Debug.WriteLine("Break.");

            }


        }



        /// <summary>
        /// Determines if two shapes are colliding.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape of the pair.</param>
        /// <param name="transformA">Transformation to apply to shape A.</param>
        /// <param name="transformB">Transformation to apply to shape B.</param>
        /// <returns>Whether or not the shapes are overlapping.</returns>
        public static bool AreShapesOverlapping(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB)
        {
            RigidTransform localTransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localTransformB);
            return AreLocalShapesOverlapping(shapeA, shapeB, ref localTransformB);

        }

        /// <summary>
        /// Determines if two shapes are colliding.  Shape B is positioned relative to shape A.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape of the pair.</param>
        /// <param name="localTransformB">Relative transform of shape B to shape A.</param>
        /// <returns>Whether or not the shapes are overlapping.</returns>
        public static bool AreLocalShapesOverlapping(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB)
        {
            return AreLocalShapesOverlapping(shapeA, shapeB, ref localTransformB.Position, ref localTransformB);
        }

        /// <summary>
        /// Determines if two shapes are colliding.  Shape B is positioned relative to shape A.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape of the pair.</param>
        /// <param name="originRay">Direction in which to cast the overlap ray.  Necessary when an object's origin is not contained in its geometry.</param>
        /// <param name="localTransformB">Relative transform of shape B to shape A.</param>
        /// <returns>Whether or not the shapes are overlapping.</returns>
        internal static bool AreLocalShapesOverlapping(ConvexShape shapeA, ConvexShape shapeB, ref Vector3 originRay, ref RigidTransform localTransformB)
        {
            //Compute the origin ray.  This points from a point known to be inside the minkowski sum to the origin.
            //The centers of the shapes are used to create the interior point.

            //It's possible that the two objects' centers are overlapping, or very very close to it.  In this case, 
            //they are obviously colliding and we can immediately exit.
            if (originRay.LengthSquared() < Toolbox.Epsilon)
            {
                return true;
            }

            Vector3 v0;
            Vector3.Negate(ref originRay, out v0); //Since we're in A's local space, A-B is just -B.



            //Now that the origin ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = originRay;
            Vector3 v1;
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1);

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2;
            Vector3.Cross(ref v1, ref v0, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!
                //If the origin is further along this direction than the extreme point, then there is no intersection.
                //If the origin is within this extreme point, then there is an intersection.
                float dot;
                Vector3.Dot(ref v1, ref originRay, out dot);
                if (dot < 0)
                {
                    //Origin is outside.
                    return false;
                }
                //Origin is inside.
                return true;
            }
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2);

            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Subtract(ref v1, ref v0, out temp1);
            Vector3.Subtract(ref v2, ref v0, out temp2);
            Vector3.Cross(ref temp1, ref temp2, out n);


            Vector3 v3;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                    break;
                count++;
                //By now, the simplex is a tetrahedron, but it is not known whether or not the origin ray found earlier actually passes through the portal
                //defined by v1, v2, v3.

                // If the origin is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                float dot;
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v1, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }

                // If the origin is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v2, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            while (true)
            {
                //Test the origin against the plane defined by v1, v2, v3.  If it's inside, we're done.
                //Compute the outward facing normal.
                Vector3.Subtract(ref v3, ref v2, out temp1);
                Vector3.Subtract(ref v1, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);
                float dot;
                Vector3.Dot(ref n, ref v1, out dot);
                if (dot >= 0)
                {
                    return true;
                }

                //We haven't yet found the origin.  Find the support point in the portal's outward facing direction.
                Vector3 v4;
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4);
                //If the origin is further along the direction than the extreme point, it's not inside the shape.
                float dot2;
                Vector3.Dot(ref v4, ref n, out dot2);
                if (dot2 < 0)
                {
                    //The origin is outside!
                    return false;
                }

                //If the plane which generated the normal is very close to the extreme point, then we're at the surface
                //and we have not found the origin; it's either just BARELY inside, or it is outside.  Assume it's outside.
                if (dot2 - dot < surfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    return false;
                }
                count++;

                //Still haven't exited, so refine the portal.
                //Test origin against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)
                Vector3.Cross(ref v4, ref v0, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                    }
                }

                //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
                //    Debug.WriteLine("Break.");

            }


        }

        /// <summary>
        /// Casts a ray from the origin in the given direction at the surface of the minkowski difference.
        /// Assumes that the origin is within the minkowski difference.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="localTransformB">Transformation of shape B relative to shape A.</param>
        /// <param name="direction">Direction to cast the ray.</param>
        /// <param name="t">Length along the direction vector that the impact was found.</param>
        /// <param name="normal">Normal of the impact at the surface of the convex.</param>
        public static void LocalSurfaceCast(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, ref Vector3 direction, out float t, out Vector3 normal)
        {
            // Local surface cast is very similar to regular MPR.  However, instead of starting at an interior point and targeting the origin,
            // the ray starts at the origin (a point known to be in both shapeA and shapeB), and just goes towards the direction until the surface
            // is found.  The portal (v1, v2, v3) at termination defines the surface normal, and the distance from the origin to the portal along the direction is used as the 't' result.


            //'v0' is no longer explicitly tracked since it is simply the origin.


            //Now that the origin ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = direction;
            Vector3 v1;
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1);
            //v1 could be zero in some degenerate cases.
            //if (v1.LengthSquared() < Toolbox.Epsilon)
            //{
            //    t = 0;
            //    normal = n;
            //    return;
            //}

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2;
            Vector3.Cross(ref direction, ref v1, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!

                float rayLengthSquared = direction.LengthSquared();
                if (rayLengthSquared > Toolbox.Epsilon * .01f)
                    Vector3.Divide(ref direction, (float)Math.Sqrt(rayLengthSquared), out normal);
                else
                    normal = new Vector3();

                float rate;
                Vector3.Dot(ref  normal, ref direction, out rate);
                float distance;
                Vector3.Dot(ref  normal, ref v1, out distance);
                if (rate > 0)
                    t = distance / rate;
                else
                    t = 0;
                return;
            }
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2);




            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Cross(ref v1, ref v2, out n);

            //It's possible that v1 and v2 were constructed in such a way that 'n' is not properly calibrated
            //relative to the direction vector.
            float dot;
            Vector3.Dot(ref n, ref direction, out dot);
            if (dot > 0)
            {
                //It's not properly calibrated.  Flip the winding (and the previously calculated normal).
                Vector3.Negate(ref n, out n);
                temp1 = v1;
                v1 = v2;
                v2 = temp1;
            }

            Vector3 v3;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                {
                    //Can't enclose the origin! That's a bit odd; something is wrong.
                    t = float.MaxValue;
                    normal = Toolbox.UpVector;
                    return;
                }
                count++;

                //By now, the simplex is a tetrahedron, but it is not known whether or not the ray actually passes through the portal
                //defined by v1, v2, v3.

                // If the direction is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                Vector3.Dot(ref temp1, ref direction, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v1, ref v3, out n);
                    continue;
                }

                // If the direction is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref direction, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v2, ref v3, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref Toolbox.ZeroVector, ref v1, ref v2, ref v3, ref direction))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            count = 0;
            while (true)
            {
                //Compute the outward facing normal.
                Vector3.Subtract(ref v1, ref v2, out temp1);
                Vector3.Subtract(ref v3, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);


                //Keep working towards the surface.  Find the next extreme point.
                Vector3 v4;
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4);


                //If the plane which generated the normal is very close to the extreme point, then we're at the surface.
                Vector3.Dot(ref n, ref v1, out dot);
                float supportDot;
                Vector3.Dot(ref v4, ref n, out supportDot);

                if (supportDot - dot < surfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    //normal = n;
                    //float normalLengthInverse = 1 / normal.Length();
                    //Vector3.Multiply(ref normal, normalLengthInverse, out normal);
                    ////Find the distance from the origin to the plane.
                    //t = dot * normalLengthInverse;

                    float lengthSquared = n.LengthSquared();
                    if (lengthSquared > Toolbox.Epsilon * .01f)
                    {
                        Vector3.Divide(ref n, (float)Math.Sqrt(lengthSquared), out normal);

                        //The plane is very close to the surface, and the ray is known to pass through it.
                        //dot is the rate.
                        Vector3.Dot(ref normal, ref direction, out dot);
                        //supportDot is the distance to the plane.
                        Vector3.Dot(ref normal, ref v1, out supportDot);
                        if (dot > 0)
                            t = supportDot / dot;
                        else
                            t = 0;
                    }
                    else
                    {
                        normal = Vector3.Up;
                        t = 0;
                    }
                    ////DEBUG STUFF:

                    //DEBUGlastRayT = t;
                    //DEBUGlastRayDirection = direction;
                    //DEBUGlastDepth = t;
                    //DEBUGlastNormal = normal;
                    //DEBUGlastV1 = v1;
                    //DEBUGlastV2 = v2;
                    //DEBUGlastV3 = v3;
                    return;
                }

                //Still haven't exited, so refine the portal.
                //Test direction against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)



                //This may look a little weird at first.
                //'inside' here means 'on the positive side of the plane.'
                //There are three total planes being tested, one for each of v1, v2, and v3.
                //The planes are created from consistently wound vertices, so it's possible to determine
                //where the ray passes through the portal based upon its relationship to two of the three planes.
                //The third vertex which is found to be opposite the face which contains the ray is replaced with the extreme point.

                //This v4 x direction is just a minor reordering of a scalar triple product: (v1 x v4) * direction.
                //It eliminates the need for extra cross products for the inner if.
                Vector3.Cross(ref v4, ref direction, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                    }
                }

                count++;

                //Here's an unoptimized equivalent without the scalar triple product reorder.
                #region Equivalent refinement
                //Vector3.Cross(ref v1, ref v4, out temp1);
                //Vector3.Dot(ref temp1, ref direction, out dot);
                //if (dot > 0)
                //{
                //    Vector3.Cross(ref v2, ref v4, out temp2);
                //    Vector3.Dot(ref temp2, ref direction, out dot);
                //    if (dot > 0)
                //    {
                //        //Inside v1, v4, v0 and inside v2, v4, v0
                //        v1 = v4;
                //    }
                //    else
                //    {
                //        //Inside v1, v4, v0 and outside v2, v4, v0
                //        v3 = v4;
                //    }
                //}
                //else
                //{
                //    Vector3.Cross(ref v3, ref v4, out temp2);
                //    Vector3.Dot(ref temp2, ref direction, out dot);
                //    if (dot > 0)
                //    {
                //        //Outside v1, v4, v0 and inside v3, v4, v0
                //        v2 = v4;
                //    }
                //    else
                //    {
                //        //Outside v1, v4, v0 and outside v3, v4, v0
                //        v1 = v4;
                //    }
                //}
                #endregion

                //if (!VerifySimplex(ref Toolbox.ZeroVector, ref v1, ref v2, ref v3, ref direction))
                //    Debug.WriteLine("Break.");
            }
        }

        /// <summary>
        /// Casts a ray from the origin in the given direction at the surface of the minkowski difference.
        /// Assumes that the origin is within the minkowski difference.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="localTransformB">Transformation of shape B relative to shape A.</param>
        /// <param name="direction">Direction to cast the ray.</param>
        /// <param name="t">Length along the direction vector that the impact was found.</param>
        /// <param name="normal">Normal of the impact at the surface of the convex.</param>
        /// <param name="position">Location of the ray cast hit on the surface of A.</param>
        public static void LocalSurfaceCast(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, ref Vector3 direction, out float t, out Vector3 normal, out Vector3 position)
        {
            // Local surface cast is very similar to regular MPR.  However, instead of starting at an interior point and targeting the origin,
            // the ray starts at the origin (a point known to be in both shapeA and shapeB), and just goes towards the direction until the surface
            // is found.  The portal (v1, v2, v3) at termination defines the surface normal, and the distance from the origin to the portal along the direction is used as the 't' result.


            //'v0' is no longer explicitly tracked since it is simply the origin.


            //Now that the origin ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = direction;
            Vector3 v1, v1A;
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1A, out v1);
            //v1 could be zero in some degenerate cases.
            //if (v1.LengthSquared() < Toolbox.Epsilon)
            //{
            //    t = 0;
            //    normal = n;
            //    return;
            //}

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2, v2A;
            Vector3.Cross(ref direction, ref v1, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!

                float rayLengthSquared = direction.LengthSquared();
                if (rayLengthSquared > Toolbox.Epsilon * .01f)
                    Vector3.Divide(ref direction, (float)Math.Sqrt(rayLengthSquared), out normal);
                else
                    normal = new Vector3();

                float rate;
                Vector3.Dot(ref  normal, ref direction, out rate);
                float distance;
                Vector3.Dot(ref  normal, ref v1, out distance);
                if (rate > 0)
                    t = distance / rate;
                else
                    t = 0;
                position = v1A;
                return;
            }
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2A, out v2);




            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Cross(ref v1, ref v2, out n);

            //It's possible that v1 and v2 were constructed in such a way that 'n' is not properly calibrated
            //relative to the direction vector.
            float dot;
            Vector3.Dot(ref n, ref direction, out dot);
            if (dot > 0)
            {
                //It's not properly calibrated.  Flip the winding (and the previously calculated normal).
                Vector3.Negate(ref n, out n);
                temp1 = v1;
                v1 = v2;
                v2 = temp1;

                temp1 = v1A;
                v1A = v2A;
                v2A = temp1;
            }

            Vector3 v3, v3A;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3A, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                {
                    //Can't enclose the origin! That's a bit odd; something is wrong.
                    t = float.MaxValue;
                    normal = Toolbox.UpVector;
                    position = new Vector3();
                    return;
                }
                count++;

                //By now, the simplex is a tetrahedron, but it is not known whether or not the ray actually passes through the portal
                //defined by v1, v2, v3.

                // If the direction is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                Vector3.Dot(ref temp1, ref direction, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    v2A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v1, ref v3, out n);
                    continue;
                }

                // If the direction is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref direction, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    v1A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v2, ref v3, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref Toolbox.ZeroVector, ref v1, ref v2, ref v3, ref direction))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            count = 0;
            while (true)
            {
                //Compute the outward facing normal.
                Vector3.Subtract(ref v1, ref v2, out temp1);
                Vector3.Subtract(ref v3, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);


                //Keep working towards the surface.  Find the next extreme point.
                Vector3 v4, v4A;
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4A, out v4);


                //If the plane which generated the normal is very close to the extreme point, then we're at the surface.
                Vector3.Dot(ref n, ref v1, out dot);
                float supportDot;
                Vector3.Dot(ref v4, ref n, out supportDot);

                if (supportDot - dot < surfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    //normal = n;
                    //float normalLengthInverse = 1 / normal.Length();
                    //Vector3.Multiply(ref normal, normalLengthInverse, out normal);
                    ////Find the distance from the origin to the plane.
                    //t = dot * normalLengthInverse;

                    float lengthSquared = n.LengthSquared();
                    if (lengthSquared > Toolbox.Epsilon * .01f)
                    {
                        Vector3.Divide(ref n, (float)Math.Sqrt(lengthSquared), out normal);

                        //The plane is very close to the surface, and the ray is known to pass through it.
                        //dot is the rate.
                        Vector3.Dot(ref normal, ref direction, out dot);
                        //supportDot is the distance to the plane.
                        Vector3.Dot(ref normal, ref v1, out supportDot);
                        if (dot > 0)
                            t = supportDot / dot;
                        else
                            t = 0;
                    }
                    else
                    {
                        normal = Vector3.Up;
                        t = 0;
                    }

                    float v1Weight, v2Weight, v3Weight;
                    Vector3.Multiply(ref direction, t, out position);

                    Toolbox.GetBarycentricCoordinates(ref position, ref v1, ref v2, ref v3, out v1Weight, out v2Weight, out v3Weight);
                    Vector3.Multiply(ref v1A, v1Weight, out position);
                    Vector3 temp;
                    Vector3.Multiply(ref v2A, v2Weight, out temp);
                    Vector3.Add(ref temp, ref position, out position);
                    Vector3.Multiply(ref v3A, v3Weight, out temp);
                    Vector3.Add(ref temp, ref position, out position);
                    ////DEBUG STUFF:

                    //DEBUGlastRayT = t;
                    //DEBUGlastRayDirection = direction;
                    //DEBUGlastDepth = t;
                    //DEBUGlastNormal = normal;
                    //DEBUGlastV1 = v1;
                    //DEBUGlastV2 = v2;
                    //DEBUGlastV3 = v3;
                    return;
                }

                //Still haven't exited, so refine the portal.
                //Test direction against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)



                //This may look a little weird at first.
                //'inside' here means 'on the positive side of the plane.'
                //There are three total planes being tested, one for each of v1, v2, and v3.
                //The planes are created from consistently wound vertices, so it's possible to determine
                //where the ray passes through the portal based upon its relationship to two of the three planes.
                //The third vertex which is found to be opposite the face which contains the ray is replaced with the extreme point.

                //This v4 x direction is just a minor reordering of a scalar triple product: (v1 x v4) * direction.
                //It eliminates the need for extra cross products for the inner if.
                Vector3.Cross(ref v4, ref direction, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                        v1A = v4A;
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                        v3A = v4A;
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                        v2A = v4A;
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                        v1A = v4A;
                    }
                }

                count++;

                //Here's an unoptimized equivalent without the scalar triple product reorder.
                #region Equivalent refinement
                //Vector3.Cross(ref v1, ref v4, out temp1);
                //Vector3.Dot(ref temp1, ref direction, out dot);
                //if (dot > 0)
                //{
                //    Vector3.Cross(ref v2, ref v4, out temp2);
                //    Vector3.Dot(ref temp2, ref direction, out dot);
                //    if (dot > 0)
                //    {
                //        //Inside v1, v4, v0 and inside v2, v4, v0
                //        v1 = v4;
                //    }
                //    else
                //    {
                //        //Inside v1, v4, v0 and outside v2, v4, v0
                //        v3 = v4;
                //    }
                //}
                //else
                //{
                //    Vector3.Cross(ref v3, ref v4, out temp2);
                //    Vector3.Dot(ref temp2, ref direction, out dot);
                //    if (dot > 0)
                //    {
                //        //Outside v1, v4, v0 and inside v3, v4, v0
                //        v2 = v4;
                //    }
                //    else
                //    {
                //        //Outside v1, v4, v0 and outside v3, v4, v0
                //        v1 = v4;
                //    }
                //}
                #endregion

                //if (!VerifySimplex(ref Toolbox.ZeroVector, ref v1, ref v2, ref v3, ref direction))
                //    Debug.WriteLine("Break.");
            }
        }

        static bool VerifySimplex(ref Vector3 v0, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 direction)
        {


            //v1, v0, v3
            Vector3 cross = Vector3.Cross(v0 - v1, v3 - v1);
            float planeProduct1 = Vector3.Dot(cross, direction);
            //v3, v0, v2
            cross = Vector3.Cross(v0 - v3, v2 - v3);
            float planeProduct2 = Vector3.Dot(cross, direction);
            //v2, v0, v1
            cross = Vector3.Cross(v0 - v2, v1 - v2);
            float planeProduct3 = Vector3.Dot(cross, direction);
            return (planeProduct1 <= 0 && planeProduct2 <= 0 && planeProduct3 <= 0) ||
                (planeProduct1 >= 0 && planeProduct2 >= 0 && planeProduct3 >= 0);
        }

        /// <summary>
        /// Gets a contact point between two convex shapes.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="transformA">Transformation to apply to the first shape.</param>
        /// <param name="transformB">Transformation to apply to the second shape.</param>
        /// <param name="penetrationAxis">Axis along which to first test the penetration depth.</param>
        /// <param name="contact">Contact data between the two shapes, if any.</param>
        /// <returns>Whether or not the shapes overlap.</returns>
        public static bool GetContact(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB, ref Vector3 penetrationAxis, out ContactData contact)
        {
            RigidTransform localTransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localTransformB);
            if (MPRToolbox.AreLocalShapesOverlapping(shapeA, shapeB, ref localTransformB))
            {
                //First, try to use the heuristically found direction.  This comes from either the GJK shallow contact separating axis or from the relative velocity.
                Vector3 rayCastDirection;
                float lengthSquared = penetrationAxis.LengthSquared();
                if (lengthSquared > Toolbox.Epsilon)
                {
                    Vector3.Divide(ref penetrationAxis, (float)Math.Sqrt(lengthSquared), out rayCastDirection);// (Vector3.Normalize(localDirection) + Vector3.Normalize(collidableB.worldTransform.Position - collidableA.worldTransform.Position)) / 2;
                    MPRToolbox.LocalSurfaceCast(shapeA, shapeB, ref localTransformB, ref rayCastDirection, out contact.PenetrationDepth, out contact.Normal);
                }
                else
                {
                    contact.PenetrationDepth = float.MaxValue;
                    contact.Normal = Toolbox.UpVector;
                }
                //Try the offset between the origins as a second option.  Sometimes this is a better choice than the relative velocity.
                //TODO: Could use the position-finding MPR iteration to find the A-B direction hit by continuing even after the origin has been found (optimization).
                Vector3 normalCandidate;
                float depthCandidate;
                lengthSquared = localTransformB.Position.LengthSquared();
                if (lengthSquared > Toolbox.Epsilon)
                {
                    Vector3.Divide(ref localTransformB.Position, (float)Math.Sqrt(lengthSquared), out rayCastDirection);
                    MPRToolbox.LocalSurfaceCast(shapeA, shapeB, ref localTransformB, ref rayCastDirection, out depthCandidate, out normalCandidate);
                    if (depthCandidate < contact.PenetrationDepth)
                    {
                        contact.Normal = normalCandidate;
                        contact.PenetrationDepth = depthCandidate;
                    }
                }

                //if (contact.PenetrationDepth > 1)
                //    Debug.WriteLine("Break.");

                //Correct the penetration depth.
                RefinePenetration(shapeA, shapeB, ref localTransformB, contact.PenetrationDepth, ref contact.Normal, out contact.PenetrationDepth, out contact.Normal, out contact.Position);

                ////Correct the penetration depth.
                //MPRTesting.LocalSurfaceCast(shapeA, shapeB, ref localTransformB, ref contact.Normal, out contact.PenetrationDepth, out rayCastDirection);


                ////The local casting can optionally continue.  Eventually, it will converge to the local minimum.
                //while (true)
                //{
                //    MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref contact.Normal, out depthCandidate, out normalCandidate);
                //    if (contact.PenetrationDepth - depthCandidate <= Toolbox.BigEpsilon)
                //        break;

                //    contact.PenetrationDepth = depthCandidate;
                //    contact.Normal = normalCandidate;
                //}

                contact.Id = -1;
                //we're still in local space! transform it all back.
                Matrix3X3 orientation;
                Matrix3X3.CreateFromQuaternion(ref transformA.Orientation, out orientation);
                Matrix3X3.Transform(ref contact.Normal, ref orientation, out contact.Normal);
                //Vector3.Negate(ref contact.Normal, out contact.Normal);
                Matrix3X3.Transform(ref contact.Position, ref orientation, out contact.Position);
                Vector3.Add(ref contact.Position, ref transformA.Position, out contact.Position);
                return true;
            }
            contact = new ContactData();
            return false;
        }

        /// <summary>
        /// Incrementally refines the penetration depth and normal towards the local minimum.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="localTransformB">Transformation of shape B relative to shape A.</param>
        /// <param name="initialDepth">Initial depth estimate.</param>
        /// <param name="initialNormal">Initial normal estimate.</param>
        /// <param name="penetrationDepth">Refined penetration depth.</param>
        /// <param name="refinedNormal">Refined normal.</param>
        /// <param name="position">Refined position.</param>
        public static void RefinePenetration(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, float initialDepth, ref Vector3 initialNormal, out float penetrationDepth, out Vector3 refinedNormal, out Vector3 position)
        {
            //The local casting can optionally continue.  Eventually, it will converge to the local minimum.
            int optimizingCount = 0;
            refinedNormal = initialNormal;
            penetrationDepth = initialDepth;
            float candidateDepth;
            Vector3 candidateNormal;
      
            while (true)
            {

                MPRToolbox.LocalSurfaceCast(shapeA, shapeB, ref localTransformB, ref refinedNormal, out candidateDepth, out candidateNormal, out position);
                if (penetrationDepth - candidateDepth <= depthRefinementEpsilon ||
                    ++optimizingCount >= maximumDepthRefinementIterations)
                {
                    //If we've reached the end due to convergence, the normal will be extremely close to correct (if not 100% correct).
                    //The candidateDepth computed is the previous contact normal's depth.
                    //The reason why the previous normal is kept is that the last raycast computed the depth for that normal, not the new normal.
                    penetrationDepth = candidateDepth;
                    break;
                }

                penetrationDepth = candidateDepth;
                refinedNormal = candidateNormal;

            }
        }

        #region Sweeping

        /// <summary>
        /// Sweeps the shapes against each other and finds a point, time, and normal of impact.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="sweepA">Sweep direction and amount to apply to the first shape.</param>
        /// <param name="sweepB">Sweep direction and amount to apply to the second shape.</param>
        /// <param name="transformA">Initial transform to apply to the first shape.</param>
        /// <param name="transformB">Initial transform to apply to the second shape.</param>
        /// <param name="hit">Hit data between the two shapes, if any.</param>
        /// <returns>Whether or not the swept shapes hit each other.</returns>
        public static bool Sweep(ConvexShape shapeA, ConvexShape shapeB, ref Vector3 sweepA, ref Vector3 sweepB, ref RigidTransform transformA, ref RigidTransform transformB, out RayHit hit)
        {
            //Put the relative velocity into shapeA's local space.
            Vector3 velocityWorld;
            //note the order of subtraction.  It 'should' be B-A, but the ray direction the algorithm works with is actually OPPOSITE.
            Vector3.Subtract(ref sweepA, ref sweepB, out velocityWorld);
            Quaternion conjugateOrientationA;
            Quaternion.Conjugate(ref transformA.Orientation, out conjugateOrientationA);
            Vector3 localDirection;
            Vector3.Transform(ref velocityWorld, ref conjugateOrientationA, out localDirection);

            #region Preparation and verification

            //Sweeping two objects against each other is very similar to the local surface cast.
            //The ray starts at the origin and goes in the sweep direction.
            //However, unlike the local surface cast, the origin may start outside of the minkowski difference.
            //Additionally, the method can early out if the length traversed by the ray is found to be longer than the maximum length, or if the origin is found to be outside the minkowski difference.

            //The support points of the minkowski difference are also modified.  By default, the minkowski difference should very rarely contain the origin.
            //Sweep tests aren't very useful if the objects are intersecting!
            //However, in order for the local surface cast to actually find a proper result (assuming there is a hit at all), the origin must be inside the minkowski difference.
            //So, expand the minkowski difference using the sweep direction with magnitude sufficient to fully include the plane defined by the origin and the sweep direction.
            //If there's going to be a hit, then the origin will be within this expanded shape.



            //If the sweep direction is found to be negative, the ray can be thought of as pointing away from the shape.
            //However, the minkowski difference may contain the shape.  In this case, the time of impact is zero.

            //If the swept (with sweep = 0 in case of incorrect direction) minkowski difference does not contain the shape, then the raycast cannot begin and we also know that the shapes will not intersect.

            //If the sweep amount is nonnegative and the minkowski difference contains the shape, then the normal raycasting process can continue.
            //Perform the usual local raycast, but use the swept minkowski difference.
            //Once the surface is found, the final t parameter of the ray is equal to the sweep distance minus the local raycast computed t parameter.



            RigidTransform localTransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localTransformB);


            //First: Compute the sweep amount along the sweep direction.
            //This sweep amount needs to expand the minkowski difference to fully intersect the plane defined by the sweep direction and origin.

            float rayLengthSquared = localDirection.LengthSquared();
            float sweepLength;
            if (rayLengthSquared > Toolbox.Epsilon * .01f)
            {
                Vector3.Dot(ref localTransformB.Position, ref localDirection, out sweepLength);
                sweepLength /= rayLengthSquared;
                //Scale the sweep length by the margins.  Divide by the length to pull the margin into terms of the length of the ray.
                sweepLength += (shapeA.maximumRadius + shapeB.maximumRadius) / (float)Math.Sqrt(rayLengthSquared);
            }
            else
            {
                rayLengthSquared = 0;
                sweepLength = 0;
            }
            //If the sweep direction is found to be negative, the ray can be thought of as pointing away from the shape.
            //Do not sweep backward.
            bool negativeLength;
            if (negativeLength = sweepLength < 0)
                sweepLength = 0;



            Vector3 sweep;
            Vector3.Multiply(ref localDirection, sweepLength, out sweep);
            //Check to see if the origin is contained within the swept shape.
            if (!AreSweptShapesIntersecting(shapeA, shapeB, ref sweep, ref localTransformB, out hit.Location)) //Computes a hit location to be used if the early-outs due to being in contact.
            {
                //The origin is not contained within the sweep volume.  The raycast definitely misses.
                hit.T = float.MaxValue;
                hit.Normal = new Vector3();
                hit.Location = new Vector3();
                return false;
            }
            if (negativeLength)
            {
                //The origin is contained, but we shouldn't continue.
                //The ray is facing backwards.  The time of impact would be 0.
                hit.T = 0;
                Vector3.Normalize(ref localDirection, out hit.Normal);
                Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);
                //hit.Location = hit.T * localDirection;
                Vector3.Transform(ref hit.Location, ref transformA.Orientation, out hit.Location);
                Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);
                hit.Location += sweepA * hit.T;
                return true;
            }
            #endregion

            if (LocalSweepCast(shapeA, shapeB, sweepLength, rayLengthSquared, ref localDirection, ref sweep, ref localTransformB, out hit))
            {
                //Compute the actual hit location on the minkowski surface.
                Vector3 minkowskiRayHit = -hit.T * localDirection;
                //TODO: This uses MPR to identify a witness point on shape A.
                //It's a very roundabout way to do it.  There should be a much simpler/faster way to compute the witness point directly, or with a little sampling. 
                GetLocalPosition(shapeA, shapeB, ref localTransformB, ref minkowskiRayHit, out hit.Location);
                //The hit location is still in local space, so transform it into world space using A's transform.
                RigidTransform.Transform(ref hit.Location, ref transformA, out hit.Location);
                Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);
                //Push the world space hit location relative to object A along A's sweep direction.
                Vector3 temp;
                Vector3.Multiply(ref sweepA, hit.T, out temp);
                Vector3.Add(ref temp, ref hit.Location, out hit.Location);
                return true;
            }
            return false;

        }

        private static bool LocalSweepCast(ConvexShape shapeA, ConvexShape shapeB, float sweepLength, float rayLengthSquared, ref Vector3 localDirection, ref Vector3 sweep, ref RigidTransform localTransformB, out RayHit hit)
        {
            //By now, the ray is known to be within the swept shape and facing the right direction for a normal raycast.

            //First guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = localDirection;
            Vector3 v1, v1A;
            GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v1A, out v1);
            //v1 could be zero in some degenerate cases.
            //if (v1.LengthSquared() < Toolbox.Epsilon)
            //{
            //    hit.T = 0;
            //    Vector3.Normalize(ref n, out hit.Normal);
            //    Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);
            //    //hit.Location = hit.T * localDirection;
            //    Vector3.Transform(ref hit.Location, ref transformA.Orientation, out hit.Location);
            //    Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);
            //    hit.Location += sweepA * hit.T;
            //    return true;
            //}

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2, v2A;
            Vector3.Cross(ref localDirection, ref v1, out n);
            hit.Location = new Vector3();
            if (n.LengthSquared() < Toolbox.Epsilon * .01f)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!

                if (rayLengthSquared > Toolbox.Epsilon * .01f)
                    Vector3.Divide(ref localDirection, (float)Math.Sqrt(rayLengthSquared), out hit.Normal);
                else
                    hit.Normal = new Vector3();

                float rate;
                Vector3.Dot(ref  hit.Normal, ref localDirection, out rate);
                float distance;
                Vector3.Dot(ref  hit.Normal, ref v1, out distance);
                if (rate > 0)
                    hit.T = sweepLength - distance / rate;
                else
                    hit.T = sweepLength;

                if (hit.T < 0)
                    hit.T = 0;

                //Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);
                ////hit.Location = hit.T * localDirection;
                //Vector3.Transform(ref hit.Location, ref transformA.Orientation, out hit.Location);
                //Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);
                //hit.Location += sweepA * hit.T;
                return hit.T <= 1;


            }
            GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v2A, out v2);




            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Cross(ref v1, ref v2, out n);

            //It's possible that v1 and v2 were constructed in such a way that 'n' is not properly calibrated
            //relative to the direction vector.
            float dot;
            Vector3.Dot(ref n, ref localDirection, out dot);
            if (dot > 0)
            {
                //It's not properly calibrated.  Flip the winding (and the previously calculated normal).
                Vector3.Negate(ref n, out n);
                temp1 = v1;
                v1 = v2;
                v2 = temp1;
                temp1 = v1A;
                v1A = v2A;
                v2A = temp1;
            }

            Vector3 v3, v3A;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v3A, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                {
                    //Can't enclose the origin! That's a bit odd.  Something is wrong; the preparation for this raycast
                    //guarantees that the origin is enclosed.  Could be a numerical problem.
                    hit.T = float.MaxValue;
                    hit.Normal = new Vector3();
                    hit.Location = new Vector3();
                    return false;
                }
                count++;

                //By now, the simplex is a tetrahedron, but it is not known whether or not the ray actually passes through the portal
                //defined by v1, v2, v3.

                // If the direction is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                Vector3.Dot(ref temp1, ref localDirection, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    v2A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v1, ref v3, out n);
                    continue;
                }

                // If the direction is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref localDirection, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    v1A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v2, ref v3, out n);
                    continue;
                }
                break;
            }


            // Refine the portal.
            count = 0;
            while (true)
            {
                //Compute the outward facing normal.
                Vector3.Subtract(ref v1, ref v2, out temp1);
                Vector3.Subtract(ref v3, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);


                //Keep working towards the surface.  Find the next extreme point.
                Vector3 v4, v4A;
                GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v4A, out v4);


                //If the plane which generated the normal is very close to the extreme point, then we're at the surface.
                Vector3.Dot(ref n, ref v1, out dot);
                float supportDot;
                Vector3.Dot(ref v4, ref n, out supportDot);

                if (supportDot - dot < rayCastSurfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    //The portal is now on the surface.  The algorithm can now compute the TOI and exit.
                    float lengthSquared = n.LengthSquared();
                    if (lengthSquared > Toolbox.Epsilon * .00001f)
                    {
                        Vector3.Divide(ref n, (float)Math.Sqrt(lengthSquared), out hit.Normal);

                        //The plane is very close to the surface, and the ray is known to pass through it.
                        //dot is the rate.
                        Vector3.Dot(ref  hit.Normal, ref localDirection, out dot);
                        //supportDot is the distance to the plane.
                        Vector3.Dot(ref  hit.Normal, ref v1, out supportDot);


                        hit.T = sweepLength - supportDot / dot;
                    }
                    else
                    {
                        Vector3.Normalize(ref localDirection, out hit.Normal);
                        hit.T = sweepLength;
                    }
                    //Sometimes, when the objects are intersecting, the T parameter can be negative.
                    //In this case, just go with t = 0.
                    if (hit.T < 0)
                        hit.T = 0;


                    //Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);

                    //Vector3.Transform(ref hit.Location, ref transformA.Orientation, out hit.Location);
                    //Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);
                    //hit.Location += sweepA * (hit.T);

                    //Compute the barycentric coordinates of the ray hit location.
                    //Vector3 mdHitLocation = t * localDirection;
                    //float v1Weight, v2Weight, v3Weight;
                    //Toolbox.GetBarycentricCoordinates(ref mdHitLocation, ref v1, ref v2, ref v3, out v1Weight, out v2Weight, out v3Weight);
                    //hit.Location = v1Weight * v1A + v2Weight * v2A + v3Weight * v3A;
                    //hit.Location += sweepA * hit.T;

                    //Vector3.Transform(ref hit.Location, ref transformA.Orientation, out hit.Location);
                    //Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);

                    return hit.T <= 1;
                }

                //Still haven't exited, so refine the portal.
                //Test direction against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)


                //This may look a little weird at first.
                //'inside' here means 'on the positive side of the plane.'
                //There are three total planes being tested, one for each of v1, v2, and v3.
                //The planes are created from consistently wound vertices, so it's possible to determine
                //where the ray passes through the portal based upon its relationship to two of the three planes.
                //The third vertex which is found to be opposite the face which contains the ray is replaced with the extreme point.

                //This v4 x direction is just a minor reordering of a scalar triple product: (v1 x v4) * direction.
                //It eliminates the need for extra cross products for the inner if.
                Vector3.Cross(ref v4, ref localDirection, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                        v1A = v4A;
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                        v3A = v4A;
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                        v2A = v4A;
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                        v1A = v4A;
                    }
                }

                count++;

            }
        }


        /// <summary>
        /// Computes the position of the minkowski point in the local space of A.
        /// This assumes that the minkowski point is contained in A-B.
        /// </summary>
        /// <param name="shapeA">First shape to test.</param>
        /// <param name="shapeB">Second shape to test.</param>
        /// <param name="localTransformB">Transform of shape B in the local space of A.</param>
        /// <param name="minkowskiPosition">Position in minkowski space to pull into the local space of A.</param>
        /// <param name="position">Position of the minkowski space point in the local space of A.</param>
        internal static void GetLocalPosition(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, ref Vector3 minkowskiPosition, out Vector3 position)
        {
            //Compute the ray.  This points from a point known to be inside the minkowski sum to the test minkowski position;
            //The centers of the shapes are used to create the interior point.
            Vector3 rayDirection;
            Vector3.Add(ref minkowskiPosition, ref localTransformB.Position, out rayDirection);

            //It's possible that the point is extremely close to the A-B center.  In this case, early out.
            if (rayDirection.LengthSquared() < Toolbox.Epsilon)
            {
                //0,0,0 is the contributing position from object A if it overlaps with the A-B minkowski center.
                //A-B center is fromed from the center position of A minus the center position of B.  We're in A's local space.
                position = new Vector3();
                //DEBUGlastPosition = position;
                return;
            }

            Vector3 v0;
            Vector3.Negate(ref localTransformB.Position, out v0); //Since we're in A's local space, A-B is just -B.



            //Now that the ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = rayDirection;
            Vector3 v1;
            Vector3 v1A, v1B; //extreme point contributions from each shape.  Used later to compute contact position; could be used to cache simplex too.
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1A, out v1B, out v1);

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2;
            Vector3 v2A, v2B;
            Vector3.Cross(ref v1, ref v0, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!
                //If the origin is further along this direction than the extreme point, then there is no intersection.
                //If the origin is within this extreme point, then there is an intersection.
                //For this test, we already guarantee that the point is extremely close to the A-B shape or inside of it, so don't bother
                //trying to return false.
                float dot = Vector3.Dot(v1 - minkowskiPosition, rayDirection);
                //Vector3.Dot(ref v1, ref rayDirection, out dot);
                //if (dot < 0) //if we were trying to return false here (in a IsPointContained style test), then the '0' should actually be a dot between the minkowskiPoint and rayDirection (simplified by a subtraction and then a dot).
                //{
                //    //Origin is outside.
                //    position = new Vector3();
                //    return false;
                //}
                //Origin is inside.
                //Compute barycentric coordinates along simplex (segment).
                float dotv0 = Vector3.Dot(v0 - minkowskiPosition, rayDirection);
                //Dot > 0, so dotv0 starts out negative.
                //Vector3.Dot(ref v0, ref rayDirection, out dotv0);
                float barycentricCoordinate = -dotv0 / (dot - dotv0);
                //Vector3.Subtract(ref v1A, ref v0A, out offset); //'v0a' is just the zero vector, so there's no need to calculate the offset.
                Vector3.Multiply(ref v1A, barycentricCoordinate, out position);
                Vector3 offset;
                Vector3.Subtract(ref v1B, ref localTransformB.Position, out offset);
                return;
            }
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2A, out v2B, out v2);

            Vector3 temp;
            Vector3 v0v1, v0v2;
            //Set n for the first iteration.
            Vector3.Subtract(ref v1, ref v0, out v0v1);
            Vector3.Subtract(ref v2, ref v0, out v0v2);
            Vector3.Cross(ref v0v1, ref v0v2, out n);

            Vector3 pointToV0;
            Vector3.Subtract(ref v0, ref minkowskiPosition, out pointToV0);
            Vector3 v3A, v3B, v3;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3A, out v3B, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                    break;
                count++;
                //By now, the simplex is a tetrahedron, but it is not known whether or not the origin ray found earlier actually passes through the portal
                //defined by v1, v2, v3.

                // If the origin is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3 v0v3;
                Vector3.Subtract(ref v1, ref v0, out v0v1);
                Vector3.Subtract(ref v3, ref v0, out v0v3);
                Vector3.Cross(ref v0v1, ref v0v3, out temp);
                float dot;
                Vector3.Dot(ref temp, ref pointToV0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    v2A = v3A;
                    v2B = v3B;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v0v1, ref v0v3, out n);
                    continue;
                }

                // If the origin is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Subtract(ref v2, ref v0, out v0v2);
                Vector3.Cross(ref v0v3, ref v0v2, out temp);
                Vector3.Dot(ref temp, ref pointToV0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    v1A = v3A;
                    v1B = v3B;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Cross(ref v0v2, ref v0v3, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref rayDirection))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            while (true)
            {
                //Test the origin against the plane defined by v1, v2, v3.  If it's inside, we're done.
                //Compute the outward facing normal.
                Vector3 v2v3, v2v1;
                Vector3.Subtract(ref v3, ref v2, out v2v3);
                Vector3.Subtract(ref v1, ref v2, out v2v1);
                Vector3.Cross(ref v2v3, ref v2v1, out n);
                float dot;
                Vector3 pointToV1;
                Vector3.Subtract(ref v1, ref minkowskiPosition, out pointToV1);
                Vector3.Dot(ref pointToV1, ref n, out dot);
                //Because this method is intended for use with surface collisions, rely on the surface push case.
                //This will not significantly harm performance, but will simplify the termination condition.
                //if (dot >= 0)
                //{
                //    Vector3 temp3;
                //    //Compute the barycentric coordinates of the origin.
                //    //This is done by computing the scaled volume (parallelepiped) of the tetrahedra 
                //    //formed by each triangle of the v0v1v2v3 tetrahedron and the origin.

                //    //TODO: consider a different approach using T parameter or something.
                //    Vector3.Subtract(ref v1, ref v0, out temp1);
                //    Vector3.Subtract(ref v2, ref v0, out temp2);
                //    Vector3.Subtract(ref v3, ref v0, out temp3);

                //    Vector3 cross;
                //    Vector3.Cross(ref temp1, ref temp2, out cross);
                //    float v0v1v2v3volume;
                //    Vector3.Dot(ref cross, ref temp3, out v0v1v2v3volume);

                //    Vector3.Cross(ref v1, ref v2, out cross);
                //    float ov1v2v3volume;
                //    Vector3.Dot(ref cross, ref v3, out ov1v2v3volume);

                //    Vector3.Cross(ref rayDirection, ref temp2, out cross);
                //    float v0ov2v3volume;
                //    Vector3.Dot(ref cross, ref temp3, out v0ov2v3volume);

                //    Vector3.Cross(ref temp1, ref rayDirection, out cross);
                //    float v0v1ov3volume;
                //    Vector3.Dot(ref cross, ref temp3, out v0v1ov3volume);

                //    if (v0v1v2v3volume > Toolbox.Epsilon * .01f)
                //    {
                //        float inverseTotalVolume = 1 / v0v1v2v3volume;
                //        float v0Weight = ov1v2v3volume * inverseTotalVolume;
                //        float v1Weight = v0ov2v3volume * inverseTotalVolume;
                //        float v2Weight = v0v1ov3volume * inverseTotalVolume;
                //        float v3Weight = 1 - v0Weight - v1Weight - v2Weight;
                //        position = v1Weight * v1A + v2Weight * v2A + v3Weight * v3A;
                //    }
                //    else
                //    {
                //        position = new Vector3();
                //    }
                //    //DEBUGlastPosition = position;
                //    return;
                //}

                //We haven't yet found the origin.  Find the support point in the portal's outward facing direction.
                Vector3 v4, v4A, v4B;
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4A, out v4B, out v4);
                //If the origin is further along the direction than the extreme point, it's not inside the shape.
                float dot2;
                Vector3 pointToV4;
                Vector3.Subtract(ref v4, ref minkowskiPosition, out pointToV4);
                Vector3.Dot(ref pointToV4, ref n, out dot2);
                //if (dot2 < 0) //We're not concerned with this test!  We already have guarantees that the shape contains or very nearly contains the minkowski point.
                //{
                //    //The origin is outside!
                //    position = new Vector3();
                //    return false;
                //}

                //If the plane which generated the normal is very close to the extreme point, then we're at the surface
                //and we have not found the origin; it's either just BARELY inside, or it is outside.  Assume it's outside.
                if (dot2 - dot < rayCastSurfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    //We found the surface.  Technically, we did not find the minkowski point yet, but it must be really close based on the guarantees
                    //required by this method.
                    //The ray intersection with the plane defined by our final portal should be extremely close to the actual minkowski point.
                    //In fact, it is probably close enough such that the barycentric coordinates can be computed using the minkowski point directly!
                    float weight1, weight2, weight3;
                    Toolbox.GetBarycentricCoordinates(ref minkowskiPosition, ref v1, ref v2, ref v3, out weight1, out weight2, out weight3);
                    Vector3.Multiply(ref v1A, weight1, out position);
                    Vector3.Multiply(ref v2A, weight2, out v2A);
                    Vector3.Multiply(ref v3A, weight3, out v3A);
                    Vector3.Add(ref v2A, ref position, out position);
                    Vector3.Add(ref v3A, ref position, out position);

                    return;
                }
                count++;

                //Still haven't exited, so refine the portal.
                //Test minkowskiPoint against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)
                Vector3.Cross(ref pointToV4, ref pointToV0, out temp);
                Vector3.Dot(ref pointToV1, ref temp, out dot);
                if (dot >= 0) //Dot v0 x v4 against v1, and dot it against 
                {
                    Vector3 pointToV2;
                    Vector3.Subtract(ref v2, ref minkowskiPosition, out pointToV2);
                    Vector3.Dot(ref pointToV2, ref temp, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                        v1A = v4A;
                        v1B = v4B;
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                        v3A = v4A;
                        v3B = v4B;
                    }
                }
                else
                {
                    Vector3 pointToV3;
                    Vector3.Subtract(ref v3, ref minkowskiPosition, out pointToV3);
                    Vector3.Dot(ref pointToV3, ref temp, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                        v2A = v4A;
                        v2B = v4B;
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                        v1A = v4A;
                        v1B = v4B;
                    }
                }

                //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref rayDirection))
                //    Debug.WriteLine("Break.");

            }


        }

        /// <summary>
        /// Determines if two shapes are intersecting.
        /// </summary>
        /// <param name="shapeA">First shape in the pair.</param>
        /// <param name="shapeB">Second shape in the pair.</param>
        /// <param name="sweep">Sweep direction and magnitude.</param>
        /// <param name="localTransformB">Transformation of shape B in the local space of A.</param>
        /// <param name="position">Position of the minkowski difference origin in the local space of A, if the swept volumes intersect.</param>
        /// <returns>Whether the swept shapes intersect.</returns>
        public static bool AreSweptShapesIntersecting(ConvexShape shapeA, ConvexShape shapeB, ref Vector3 sweep, ref RigidTransform localTransformB, out Vector3 position)
        {
            //It's possible that the two objects' centers are overlapping, or very very close to it.  In this case, 
            //they are obviously colliding and we can immediately exit.
            if (localTransformB.Position.LengthSquared() < Toolbox.Epsilon)
            {
                position = new Vector3();
                return true;
            }

            Vector3 v0;
            Vector3.Negate(ref localTransformB.Position, out v0); //Since we're in A's local space, A-B is just -B.



            //Now that the origin ray is known, create a portal through which the ray passes.
            //To do this, first guess a portal.
            //This implementation is similar to that of the original XenoCollide.
            //'n' will be the direction used to find supports throughout the algorithm.
            Vector3 n = localTransformB.Position;
            Vector3 v1;
            Vector3 v1A; //extreme point contributions from each shape.  Used later to compute contact position; could be used to cache simplex too.
            //MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v1A, out v1B, out v1);
            GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v1A, out v1);

            //Find another extreme point in a direction perpendicular to the previous.
            Vector3 v2;
            Vector3 v2A;
            Vector3.Cross(ref v1, ref v0, out n);
            if (n.LengthSquared() < Toolbox.Epsilon)
            {
                //v1 and v0 could be parallel.
                //This isn't a bad thing- it means the direction is exactly aligned with the extreme point offset.
                //In other words, if the raycast is followed out to the surface, it will arrive at the extreme point!
                //If the origin is further along this direction than the extreme point, then there is no intersection.
                //If the origin is within this extreme point, then there is an intersection.
                float dot;
                Vector3.Dot(ref v1, ref localTransformB.Position, out dot);
                if (dot < 0)
                {
                    //Origin is outside.
                    position = new Vector3();
                    return false;
                }
                //Origin is inside.
                //Compute barycentric coordinates along simplex (segment).
                float dotv0;
                //Dot > 0, so dotv0 starts out negative.
                Vector3.Dot(ref v0, ref localTransformB.Position, out dotv0);
                float barycentricCoordinate = -dotv0 / (dot - dotv0);
                //Vector3.Subtract(ref v1A, ref v0A, out offset); //'v0a' is just the zero vector, so there's no need to calculate the offset.
                Vector3.Multiply(ref v1A, barycentricCoordinate, out position);
                return true;
            }
            //MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v2A, out v2B, out v2);
            GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v2A, out v2);

            Vector3 temp1, temp2;
            //Set n for the first iteration.
            Vector3.Subtract(ref v1, ref v0, out temp1);
            Vector3.Subtract(ref v2, ref v0, out temp2);
            Vector3.Cross(ref temp1, ref temp2, out n);


            Vector3 v3A, v3;
            int count = 0;
            while (true)
            {
                //Find a final extreme point using the normal of the plane defined by v0, v1, v2.
                //MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v3A, out v3B, out v3);
                GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v3A, out v3);

                if (count > MPRToolbox.OuterIterationLimit)
                    break;
                count++;
                //By now, the simplex is a tetrahedron, but it is not known whether or not the origin ray found earlier actually passes through the portal
                //defined by v1, v2, v3.

                // If the origin is outside the plane defined by v1,v0,v3, then the portal is invalid.
                Vector3.Cross(ref v1, ref v3, out temp1);
                float dot;
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v2) with the new extreme point.
                    v2 = v3;
                    v2A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v1, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }

                // If the origin is outside the plane defined by v3,v0,v2, then the portal is invalid.
                Vector3.Cross(ref v3, ref v2, out temp1);
                Vector3.Dot(ref temp1, ref v0, out dot);
                if (dot < 0)
                {
                    //Replace the point that was on the inside of the plane (v1) with the new extreme point.
                    v1 = v3;
                    v1A = v3A;
                    // Calculate the normal of the plane that will be used to find a new extreme point.
                    Vector3.Subtract(ref v2, ref v0, out temp1);
                    Vector3.Subtract(ref v3, ref v0, out temp2);
                    Vector3.Cross(ref temp1, ref temp2, out n);
                    continue;
                }
                break;
            }

            //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
            //    Debug.WriteLine("Break.");


            // Refine the portal.
            while (true)
            {
                //Test the origin against the plane defined by v1, v2, v3.  If it's inside, we're done.
                //Compute the outward facing normal.
                Vector3.Subtract(ref v3, ref v2, out temp1);
                Vector3.Subtract(ref v1, ref v2, out temp2);
                Vector3.Cross(ref temp1, ref temp2, out n);
                float dot;
                Vector3.Dot(ref n, ref v1, out dot);
                if (dot >= 0)
                {
                    Vector3 temp3;
                    //Compute the barycentric coordinates of the origin.
                    //This is done by computing the scaled volume (parallelepiped) of the tetrahedra 
                    //formed by each triangle of the v0v1v2v3 tetrahedron and the origin.

                    //TODO: consider a different approach using T parameter or something.
                    Vector3.Subtract(ref v1, ref v0, out temp1);
                    Vector3.Subtract(ref v2, ref v0, out temp2);
                    Vector3.Subtract(ref v3, ref v0, out temp3);

                    Vector3 cross;
                    Vector3.Cross(ref temp1, ref temp2, out cross);
                    float v0v1v2v3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0v1v2v3volume);

                    Vector3.Cross(ref v1, ref v2, out cross);
                    float ov1v2v3volume;
                    Vector3.Dot(ref cross, ref v3, out ov1v2v3volume);

                    Vector3.Cross(ref localTransformB.Position, ref temp2, out cross);
                    float v0ov2v3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0ov2v3volume);

                    Vector3.Cross(ref temp1, ref localTransformB.Position, out cross);
                    float v0v1ov3volume;
                    Vector3.Dot(ref cross, ref temp3, out v0v1ov3volume);


                    float inverseTotalVolume = 1 / v0v1v2v3volume;
                    float v0Weight = ov1v2v3volume * inverseTotalVolume;
                    float v1Weight = v0ov2v3volume * inverseTotalVolume;
                    float v2Weight = v0v1ov3volume * inverseTotalVolume;
                    float v3Weight = 1 - v0Weight - v1Weight - v2Weight;
                    position = v1Weight * v1A + v2Weight * v2A + v3Weight * v3A;
                    //DEBUGlastPosition = position;
                    return true;
                }

                //We haven't yet found the origin.  Find the support point in the portal's outward facing direction.
                Vector3 v4, v4A;
                //MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref n, ref localTransformB, out v4A, out v4B, out v4); 
                GetSweptExtremePoint(shapeA, shapeB, ref localTransformB, ref sweep, ref n, out v4A, out v4);

                //If the origin is further along the direction than the extreme point, it's not inside the shape.
                float dot2;
                Vector3.Dot(ref v4, ref n, out dot2);
                if (dot2 < 0)
                {
                    //The origin is outside!
                    position = new Vector3();
                    return false;
                }

                //If the plane which generated the normal is very close to the extreme point, then we're at the surface
                //and we have not found the origin; it's either just BARELY inside, or it is outside.  Assume it's outside.
                if (dot2 - dot < surfaceEpsilon || count > MPRToolbox.InnerIterationLimit) // TODO: Could use a dynamic epsilon for possibly better behavior.
                {
                    position = new Vector3();
                    //DEBUGlastPosition = position;
                    return false;
                }
                count++;

                //Still haven't exited, so refine the portal.
                //Test origin against the three planes that separate the new portal candidates: (v1,v4,v0) (v2,v4,v0) (v3,v4,v0)
                Vector3.Cross(ref v4, ref v0, out temp1);
                Vector3.Dot(ref v1, ref temp1, out dot);
                if (dot >= 0)
                {
                    Vector3.Dot(ref v2, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v1 = v4; // Inside v1 & inside v2 ==> eliminate v1
                        v1A = v4A;
                    }
                    else
                    {
                        v3 = v4; // Inside v1 & outside v2 ==> eliminate v3
                        v3A = v4A;
                    }
                }
                else
                {
                    Vector3.Dot(ref v3, ref temp1, out dot);
                    if (dot >= 0)
                    {
                        v2 = v4; // Outside v1 & inside v3 ==> eliminate v2
                        v2A = v4A;
                    }
                    else
                    {
                        v1 = v4; // Outside v1 & outside v3 ==> eliminate v1
                        v1A = v4A;
                    }
                }

                //if (!VerifySimplex(ref v0, ref v1, ref v2, ref v3, ref localTransformB.Position))
                //    Debug.WriteLine("Break.");

            }


        }


        static void GetSweptExtremePoint(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, ref Vector3 sweep, ref Vector3 extremePointDirection, out Vector3 extremePoint)
        {
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref extremePointDirection, ref localTransformB, out extremePoint);
            float dot;
            Vector3.Dot(ref extremePointDirection, ref sweep, out dot);
            if (dot > 0)
                Vector3.Add(ref extremePoint, ref sweep, out extremePoint);
        }


        static void GetSweptExtremePoint(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB, ref Vector3 sweep, ref Vector3 extremePointDirection, out Vector3 extremePointA, out Vector3 extremePoint)
        {
            Vector3 b;
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref extremePointDirection, ref localTransformB, out extremePointA, out b, out extremePoint);
            float dot;
            Vector3.Dot(ref extremePointDirection, ref sweep, out dot);
            if (dot > 0)
            {
                Vector3.Add(ref extremePoint, ref sweep, out extremePoint);
            }
        }

        #endregion
    }
}
