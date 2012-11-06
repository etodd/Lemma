using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.Settings;

namespace BEPUphysics.CollisionTests.CollisionAlgorithms.GJK
{
    ///<summary>
    /// Helper class containing various tests based on GJK.
    ///</summary>
    public static class GJKToolbox
    {
        /// <summary>
        /// Maximum number of iterations the GJK algorithm will do.  If the iterations exceed this number, the system will immediately quit and return whatever information it has at the time.
        /// </summary>
        public static int MaximumGJKIterations = 15;
        /// <summary>
        /// Defines how many iterations are required to consider a GJK attempt to be 'probably stuck' and proceed with protective measures.
        /// </summary>
        public static int HighGJKIterations = 8;

        ///<summary>
        /// Tests if the pair is intersecting.
        ///</summary>
        ///<param name="shapeA">First shape of the pair.</param>
        ///<param name="shapeB">Second shape of the pair.</param>
        ///<param name="transformA">Transform to apply to the first shape.</param>
        ///<param name="transformB">Transform to apply to the second shape.</param>
        ///<returns>Whether or not the shapes are intersecting.</returns>
        public static bool AreShapesIntersecting(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB)
        {
            //Zero isn't a very good guess!  But it's a cheap guess.
            Vector3 separatingAxis = Toolbox.ZeroVector;
            return AreShapesIntersecting(shapeA, shapeB, ref transformA, ref transformB, ref separatingAxis);
        }

        ///<summary>
        /// Tests if the pair is intersecting.
        ///</summary>
        ///<param name="shapeA">First shape of the pair.</param>
        ///<param name="shapeB">Second shape of the pair.</param>
        ///<param name="transformA">Transform to apply to the first shape.</param>
        ///<param name="transformB">Transform to apply to the second shape.</param>
        ///<param name="localSeparatingAxis">Warmstartable separating axis used by the method to quickly early-out if possible.  Updated to the latest separating axis after each run.</param>
        ///<returns>Whether or not the objects were intersecting.</returns>
        public static bool AreShapesIntersecting(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB,
                                                 ref Vector3 localSeparatingAxis)
        {
            RigidTransform localtransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localtransformB);

            //Warm start the simplex.
            var simplex = new SimpleSimplex();
            Vector3 extremePoint;
            MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref localSeparatingAxis, ref localtransformB, out extremePoint);
            simplex.AddNewSimplexPoint(ref extremePoint);

            Vector3 closestPoint;
            int count = 0;
            while (count++ < MaximumGJKIterations)
            {
                if (simplex.GetPointClosestToOrigin(out closestPoint) || //Also reduces the simplex.
                    closestPoint.LengthSquared() <= simplex.GetErrorTolerance() * Toolbox.BigEpsilon)
                {
                    //Intersecting, or so close to it that it will be difficult/expensive to figure out the separation.
                    return true;
                }

                //Use the closest point as a direction.
                Vector3 direction;
                Vector3.Negate(ref closestPoint, out direction);
                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref direction, ref localtransformB, out extremePoint);
                //Since this is a boolean test, we don't need to refine the simplex if it becomes apparent that we cannot reach the origin.
                //If the most extreme point at any given time does not go past the origin, then we can quit immediately.
                float dot;
                Vector3.Dot(ref extremePoint, ref closestPoint, out dot); //extreme point dotted against the direction pointing backwards towards the CSO. 
                if (dot > 0)
                {
                    // If it's positive, that means that the direction pointing towards the origin produced an extreme point 'in front of' the origin, eliminating the possibility of any intersection.
                    localSeparatingAxis = direction;
                    return false;
                }

                simplex.AddNewSimplexPoint(ref extremePoint);


            }
            return false;
        }

        ///<summary>
        /// Gets the closest points between the shapes.
        ///</summary>
        ///<param name="shapeA">First shape of the pair.</param>
        ///<param name="shapeB">Second shape of the pair.</param>
        ///<param name="transformA">Transform to apply to the first shape.</param>
        ///<param name="transformB">Transform to apply to the second shape.</param>
        ///<param name="closestPointA">Closest point on the first shape to the second shape.</param>
        ///<param name="closestPointB">Closest point on the second shape to the first shape.</param>
        ///<returns>Whether or not the objects were intersecting.  If they are intersecting, then the closest points cannot be identified.</returns>
        public static bool GetClosestPoints(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB,
                                            out Vector3 closestPointA, out Vector3 closestPointB)
        {
            //The cached simplex stores locations that are local to the shapes.  A fairly decent initial state is between the centroids of the objects.
            //In local space, the centroids are at the origins.

            RigidTransform localtransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localtransformB);

            var simplex = new CachedSimplex {State = SimplexState.Point};
                // new CachedSimplex(shapeA, shapeB, ref localtransformB);
            bool toReturn = GetClosestPoints(shapeA, shapeB, ref localtransformB, ref simplex, out closestPointA, out closestPointB);

            RigidTransform.Transform(ref closestPointA, ref transformA, out closestPointA);
            RigidTransform.Transform(ref closestPointB, ref transformA, out closestPointB);
            return toReturn;
        }

        ///<summary>
        /// Gets the closest points between the shapes.
        ///</summary>
        ///<param name="shapeA">First shape of the pair.</param>
        ///<param name="shapeB">Second shape of the pair.</param>
        ///<param name="transformA">Transform to apply to the first shape.</param>
        ///<param name="transformB">Transform to apply to the second shape.</param>
        /// <param name="cachedSimplex">Simplex from a previous updated used to warmstart the current attempt.  Updated after each run.</param>
        ///<param name="closestPointA">Closest point on the first shape to the second shape.</param>
        ///<param name="closestPointB">Closest point on the second shape to the first shape.</param>
        ///<returns>Whether or not the objects were intersecting.  If they are intersecting, then the closest points cannot be identified.</returns>
        public static bool GetClosestPoints(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform transformA, ref RigidTransform transformB,
                                            ref CachedSimplex cachedSimplex, out Vector3 closestPointA, out Vector3 closestPointB)
        {
            RigidTransform localtransformB;
            MinkowskiToolbox.GetLocalTransform(ref transformA, ref transformB, out localtransformB);

            bool toReturn = GetClosestPoints(shapeA, shapeB, ref localtransformB, ref cachedSimplex, out closestPointA, out closestPointB);

            RigidTransform.Transform(ref closestPointA, ref transformA, out closestPointA);
            RigidTransform.Transform(ref closestPointB, ref transformA, out closestPointB);
            return toReturn;
        }

        private static bool GetClosestPoints(ConvexShape shapeA, ConvexShape shapeB, ref RigidTransform localTransformB,
                                             ref CachedSimplex cachedSimplex, out Vector3 localClosestPointA, out Vector3 localClosestPointB)
        {

            var simplex = new PairSimplex(ref cachedSimplex, ref localTransformB);

            Vector3 closestPoint;
            int count = 0;
            while (true)
            {
                if (simplex.GetPointClosestToOrigin(out closestPoint) || //Also reduces the simplex and computes barycentric coordinates if necessary. 
                    closestPoint.LengthSquared() <= Toolbox.Epsilon * simplex.errorTolerance)
                {
                    //Intersecting.
                    localClosestPointA = Toolbox.ZeroVector;
                    localClosestPointB = Toolbox.ZeroVector;

                    simplex.UpdateCachedSimplex(ref cachedSimplex);
                    return true;
                }

                if (++count > MaximumGJKIterations)
                    break; //Must break BEFORE a new vertex is added if we're over the iteration limit.  This guarantees final simplex is not a tetrahedron.

                if (simplex.GetNewSimplexPoint(shapeA, shapeB, count, ref closestPoint))
                {
                    //No progress towards origin, not intersecting.
                    break;
                }

            }
            //Compute closest points from the contributing simplexes and barycentric coordinates
            simplex.GetClosestPoints(out localClosestPointA, out localClosestPointB);
            //simplex.VerifyContributions();
            //if (Vector3.Distance(localClosestPointA - localClosestPointB, closestPoint) > .00001f)
            //    Debug.WriteLine("break.");
            simplex.UpdateCachedSimplex(ref cachedSimplex);
            return false;
        }

        //TODO: Consider changing the termination epsilons on these casts.  Epsilon * Modifier is okay, but there might be better options.

        ///<summary>
        /// Tests a ray against a convex shape.
        ///</summary>
        ///<param name="ray">Ray to test against the shape.</param>
        ///<param name="shape">Shape to test.</param>
        ///<param name="shapeTransform">Transform to apply to the shape for the test.</param>
        ///<param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="hit">Hit data of the ray cast, if any.</param>
        ///<returns>Whether or not the ray hit the shape.</returns>
        public static bool RayCast(Ray ray, ConvexShape shape, ref RigidTransform shapeTransform, float maximumLength,
                                   out RayHit hit)
        {
            //Transform the ray into the object's local space.
            Vector3.Subtract(ref ray.Position, ref shapeTransform.Position, out ray.Position);
            Quaternion conjugate;
            Quaternion.Conjugate(ref shapeTransform.Orientation, out conjugate);
            Vector3.Transform(ref ray.Position, ref conjugate, out ray.Position);
            Vector3.Transform(ref ray.Direction, ref conjugate, out ray.Direction);

            Vector3 w, p;
            hit.T = 0;
            hit.Location = ray.Position;
            hit.Normal = Toolbox.ZeroVector;
            Vector3 v = hit.Location;

            RaySimplex simplex = new RaySimplex();

            float vw, vdir;
            int count = 0;
            //This epsilon has a significant impact on performance and accuracy.  Changing it to use BigEpsilon instead increases speed by around 30-40% usually, but jigging is more evident.
            while (v.LengthSquared() >= Toolbox.Epsilon * simplex.GetErrorTolerance(ref ray.Position))
            {
                if (++count > MaximumGJKIterations)
                {
                    //It's taken too long to find a hit.  Numerical problems are probable; quit.
                    hit = new RayHit();
                    return false;
                }

                shape.GetLocalExtremePoint(v, out p);

                Vector3.Subtract(ref hit.Location, ref p, out w);
                Vector3.Dot(ref v, ref w, out vw);
                if (vw > 0)
                {
                    Vector3.Dot(ref v, ref ray.Direction, out vdir);
                    if (vdir >= 0)
                    {
                        hit = new RayHit();
                        return false;
                    }
                    hit.T = hit.T - vw / vdir;
                    if (hit.T > maximumLength)
                    {
                        //If we've gone beyond where the ray can reach, there's obviously no hit.
                        hit = new RayHit();
                        return false;
                    }
                    //Shift the ray up.
                    Vector3.Multiply(ref ray.Direction, hit.T, out hit.Location);
                    Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                    hit.Normal = v;
                }

                RaySimplex shiftedSimplex;
                simplex.AddNewSimplexPoint(ref p, ref hit.Location, out shiftedSimplex);

                shiftedSimplex.GetPointClosestToOrigin(ref simplex, out v);

            }
            //Transform the hit data into world space.
            Vector3.Transform(ref hit.Normal, ref shapeTransform.Orientation, out hit.Normal);
            Vector3.Transform(ref hit.Location, ref shapeTransform.Orientation, out hit.Location);
            Vector3.Add(ref hit.Location, ref shapeTransform.Position, out hit.Location);

            return true;
        }

        ///<summary>
        /// Sweeps a shape against another shape using a given sweep vector.
        ///</summary>
        ///<param name="sweptShape">Shape to sweep.</param>
        ///<param name="target">Shape being swept against.</param>
        ///<param name="sweep">Sweep vector for the sweptShape.</param>
        ///<param name="startingSweptTransform">Starting transform of the sweptShape.</param>
        ///<param name="targetTransform">Transform to apply to the target shape.</param>
        ///<param name="hit">Hit data of the sweep test, if any.</param>
        ///<returns>Whether or not the swept shape hit the other shape.</returns>
        public static bool ConvexCast(ConvexShape sweptShape, ConvexShape target, ref Vector3 sweep, ref RigidTransform startingSweptTransform, ref RigidTransform targetTransform,
                                  out RayHit hit)
        {
            return ConvexCast(sweptShape, target, ref sweep, ref Toolbox.ZeroVector, ref startingSweptTransform, ref targetTransform, out hit);
        }

        ///<summary>
        /// Sweeps two shapes against another.
        ///</summary>
        ///<param name="shapeA">First shape being swept.</param>
        ///<param name="shapeB">Second shape being swept.</param>
        ///<param name="sweepA">Sweep vector for the first shape.</param>
        ///<param name="sweepB">Sweep vector for the second shape.</param>
        ///<param name="transformA">Transform to apply to the first shape.</param>
        ///<param name="transformB">Transform to apply to the second shape.</param>
        ///<param name="hit">Hit data of the sweep test, if any.</param>
        ///<returns>Whether or not the swept shapes hit each other..</returns>
        public static bool ConvexCast(ConvexShape shapeA, ConvexShape shapeB, ref Vector3 sweepA, ref Vector3 sweepB, ref RigidTransform transformA, ref RigidTransform transformB,
                                  out RayHit hit)
        {
            //Put the velocity into shapeA's local space.
            Vector3 velocityWorld;
            Vector3.Subtract(ref sweepB, ref sweepA, out velocityWorld);
            Quaternion conjugateOrientationA;
            Quaternion.Conjugate(ref transformA.Orientation, out conjugateOrientationA);
            Vector3 rayDirection;
            Vector3.Transform(ref velocityWorld, ref conjugateOrientationA, out rayDirection);
            //Transform b into a's local space.
            RigidTransform localTransformB;
            Quaternion.Concatenate(ref transformB.Orientation, ref conjugateOrientationA, out localTransformB.Orientation);
            Vector3.Subtract(ref transformB.Position, ref transformA.Position, out localTransformB.Position);
            Vector3.Transform(ref localTransformB.Position, ref conjugateOrientationA, out localTransformB.Position);
            

            Vector3 w, p;
            hit.T = 0;
            hit.Location = Vector3.Zero; //The ray starts at the origin.
            hit.Normal = Toolbox.ZeroVector;
            Vector3 v = hit.Location;

            RaySimplex simplex = new RaySimplex();

 
            float vw, vdir;
            int count = 0;
            do
            {
                

                if (++count > MaximumGJKIterations)
                {
                    //It's taken too long to find a hit.  Numerical problems are probable; quit.
                    hit = new RayHit();
                    return false;
                }

                MinkowskiToolbox.GetLocalMinkowskiExtremePoint(shapeA, shapeB, ref v, ref localTransformB, out p);

                Vector3.Subtract(ref hit.Location, ref p, out w);
                Vector3.Dot(ref v, ref w, out vw);
                if (vw > 0)
                {
                    Vector3.Dot(ref v, ref rayDirection, out vdir);
                    if (vdir >= 0)
                    {
                        hit = new RayHit();
                        return false;
                    }
                    hit.T = hit.T - vw / vdir;
                    if (hit.T > 1)
                    {
                        //If we've gone beyond where the ray can reach, there's obviously no hit.
                        hit = new RayHit();
                        return false;
                    }
                    //Shift the ray up.
                    Vector3.Multiply(ref rayDirection, hit.T, out hit.Location);
                    //The ray origin is the origin!  Don't need to add any ray position.
                    hit.Normal = v;
                }

                RaySimplex shiftedSimplex;
                simplex.AddNewSimplexPoint(ref p, ref hit.Location, out shiftedSimplex);

                shiftedSimplex.GetPointClosestToOrigin(ref simplex, out v);

                //Could measure the progress of the ray.  If it's too little, could early out.
                //Not used by default since it's biased towards precision over performance.

            } while (v.LengthSquared() >= Toolbox.Epsilon * simplex.GetErrorTolerance(ref Toolbox.ZeroVector));
            //This epsilon has a significant impact on performance and accuracy.  Changing it to use BigEpsilon instead increases speed by around 30-40% usually, but jigging is more evident.
            //Transform the hit data into world space.
            Vector3.Transform(ref hit.Normal, ref transformA.Orientation, out hit.Normal);
            Vector3.Multiply(ref velocityWorld, hit.T, out hit.Location);
            Vector3.Add(ref hit.Location, ref transformA.Position, out hit.Location);
            return true;
        }


        ///<summary>
        /// Casts a fat (sphere expanded) ray against the shape.
        ///</summary>
        ///<param name="ray">Ray to test against the shape.</param>
        ///<param name="radius">Radius of the ray.</param>
        ///<param name="shape">Shape to test against.</param>
        ///<param name="shapeTransform">Transform to apply to the shape for the test.</param>
        ///<param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="hit">Hit data of the sphere cast, if any.</param>
        ///<returns>Whether or not the sphere cast hit the shape.</returns>
        public static bool SphereCast(Ray ray, float radius, ConvexShape shape, ref RigidTransform shapeTransform, float maximumLength,
                                   out RayHit hit)
        {
            //Transform the ray into the object's local space.
            Vector3.Subtract(ref ray.Position, ref shapeTransform.Position, out ray.Position);
            Quaternion conjugate;
            Quaternion.Conjugate(ref shapeTransform.Orientation, out conjugate);
            Vector3.Transform(ref ray.Position, ref conjugate, out ray.Position);
            Vector3.Transform(ref ray.Direction, ref conjugate, out ray.Direction);

            Vector3 w, p;
            hit.T = 0;
            hit.Location = ray.Position;
            hit.Normal = Toolbox.ZeroVector;
            Vector3 v = hit.Location;

            RaySimplex simplex = new RaySimplex();

            float vw, vdir;
            int count = 0;

            //This epsilon has a significant impact on performance and accuracy.  Changing it to use BigEpsilon instead increases speed by around 30-40% usually, but jigging is more evident.
            while (v.LengthSquared() >= Toolbox.Epsilon * simplex.GetErrorTolerance(ref ray.Position))
            {
                if (++count > MaximumGJKIterations)
                {
                    //It's taken too long to find a hit.  Numerical problems are probable; quit.
                    hit = new RayHit();
                    return false;
                }

                shape.GetLocalExtremePointWithoutMargin(ref v, out p);
                Vector3 contribution;
                MinkowskiToolbox.ExpandMinkowskiSum(shape.collisionMargin, radius, ref v, out contribution);
                Vector3.Add(ref p, ref contribution, out p);

                Vector3.Subtract(ref hit.Location, ref p, out w);
                Vector3.Dot(ref v, ref w, out vw);
                if (vw > 0)
                {
                    Vector3.Dot(ref v, ref ray.Direction, out vdir);
                    hit.T = hit.T - vw / vdir;
                    if (vdir >= 0)
                    {
                        //We would have to back up!
                        return false;
                    }
                    if (hit.T > maximumLength)
                    {
                        //If we've gone beyond where the ray can reach, there's obviously no hit.
                        return false;
                    }
                    //Shift the ray up.
                    Vector3.Multiply(ref ray.Direction, hit.T, out hit.Location);
                    Vector3.Add(ref hit.Location, ref ray.Position, out hit.Location);
                    hit.Normal = v;
                }

                RaySimplex shiftedSimplex;
                simplex.AddNewSimplexPoint(ref p, ref hit.Location, out shiftedSimplex);

                shiftedSimplex.GetPointClosestToOrigin(ref simplex, out v);

            }
            //Transform the hit data into world space.
            Vector3.Transform(ref hit.Normal, ref shapeTransform.Orientation, out hit.Normal);
            Vector3.Transform(ref hit.Location, ref shapeTransform.Orientation, out hit.Location);
            Vector3.Add(ref hit.Location, ref shapeTransform.Position, out hit.Location);

            return true;
        }

        ///<summary>
        /// Casts a fat (sphere expanded) ray against the shape.  If the raycast appears to be stuck in the shape, the cast will be attempted
        /// with a smaller ray (scaled by the MotionSettings.CoreShapeScaling each time).
        ///</summary>
        ///<param name="ray">Ray to test against the shape.</param>
        ///<param name="radius">Radius of the ray.</param>
        ///<param name="target">Shape to test against.</param>
        ///<param name="shapeTransform">Transform to apply to the shape for the test.</param>
        ///<param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="hit">Hit data of the sphere cast, if any.</param>
        ///<returns>Whether or not the sphere cast hit the shape.</returns>
        public static bool CCDSphereCast(Ray ray, float radius, ConvexShape target, ref RigidTransform shapeTransform, float maximumLength,
                                   out RayHit hit)
        {
            int iterations = 0;
            while (true)
            {
                if (GJKToolbox.SphereCast(ray, radius, target, ref shapeTransform, maximumLength, out hit) &&
                    hit.T > 0)
                {
                    //The ray cast isn't embedded in the shape, and it's less than maximum length away!
                    return true;
                }
                if (hit.T > maximumLength || hit.T < 0)
                    return false; //Failure showed it was too far, or behind.

                radius *= MotionSettings.CoreShapeScaling;
                iterations++;
                if (iterations > 3) //Limit could be configurable.
                {
                    //It's iterated too much, let's just do a last ditch attempt using a raycast and hope that can help.
                    return GJKToolbox.RayCast(ray, target, ref shapeTransform, maximumLength, out hit) && hit.T > 0;
                        
                }
            }
        }
    }


}
