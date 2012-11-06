using Microsoft.Xna.Framework;

namespace BEPUphysics.CollisionTests.CollisionAlgorithms.GJK
{

    ///<summary>
    /// GJK simplex supporting ray-based tests.
    ///</summary>
    public struct RaySimplex
    {
        ///<summary>
        /// First vertex in the simplex.
        ///</summary>
        public Vector3 A;
        /// <summary>
        /// Second vertex in the simplex.
        /// </summary>
        public Vector3 B;
        /// <summary>
        /// Third vertex in the simplex.
        /// </summary>
        public Vector3 C;
        /// <summary>
        /// Fourth vertex in the simplex.
        /// </summary>
        public Vector3 D;
        /// <summary>
        /// Current state of the simplex.
        /// </summary>
        public SimplexState State;



        ///<summary>
        /// Gets the point on the simplex that is closest to the origin.
        ///</summary>
        ///<param name="simplex">Simplex to test.</param>
        ///<param name="point">Closest point on the simplex.</param>
        ///<returns>Whether or not the simplex contains the origin.</returns>
        public bool GetPointClosestToOrigin(ref RaySimplex simplex, out Vector3 point)
        {
            //This method finds the closest point on the simplex to the origin.
            //Barycentric coordinates are assigned to the MinimumNormCoordinates as necessary to perform the inclusion calculation.
            //If the simplex is a tetrahedron and found to be overlapping the origin, the function returns true to tell the caller to terminate.
            //Elements of the simplex that are not used to determine the point of minimum norm are removed from the simplex.

            switch (State)
            {

                case SimplexState.Point:
                    point = A;
                    break;
                case SimplexState.Segment:
                    GetPointOnSegmentClosestToOrigin(ref simplex, out point);
                    break;
                case SimplexState.Triangle:
                    GetPointOnTriangleClosestToOrigin(ref simplex, out point);
                    break;
                case SimplexState.Tetrahedron:
                    return GetPointOnTetrahedronClosestToOrigin(ref simplex, out point);
                default:
                    point = Toolbox.ZeroVector;
                    break;


            }
            return false;
        }


        ///<summary>
        /// Finds the point on the segment to the origin.
        ///</summary>
        ///<param name="simplex">Simplex to test.</param>
        ///<param name="point">Closest point.</param>
        public void GetPointOnSegmentClosestToOrigin(ref RaySimplex simplex, out Vector3 point)
        {
            Vector3 segmentDisplacement;
            Vector3.Subtract(ref B, ref A, out segmentDisplacement);

            float dotA;
            Vector3.Dot(ref segmentDisplacement, ref A, out dotA);
            if (dotA > 0)
            {
                //'Behind' segment.  This can't happen in a boolean version,
                //but with closest points warmstarting or raycasts, it will.
                simplex.State = SimplexState.Point;

                point = A;
                return;
            }
            float dotB;
            Vector3.Dot(ref segmentDisplacement, ref B, out dotB);
            if (dotB > 0)
            {
                //Inside segment.
                float V = -dotA / segmentDisplacement.LengthSquared();
                Vector3.Multiply(ref segmentDisplacement, V, out point);
                Vector3.Add(ref point, ref A, out point);
                return;

            }

            //It should be possible in the warmstarted closest point calculation/raycasting to be outside B.
            //It is not possible in a 'boolean' GJK, where it early outs as soon as a separating axis is found.

            //Outside B.
            //Remove current A; we're becoming a point.
            simplex.A = simplex.B;
            simplex.State = SimplexState.Point;

            point = A;

        }

        ///<summary>
        /// Gets the point on the triangle that is closest to the origin.
        ///</summary>
        ///<param name="simplex">Simplex to test.</param>
        ///<param name="point">Closest point to origin.</param>
        public void GetPointOnTriangleClosestToOrigin(ref RaySimplex simplex, out Vector3 point)
        {
            Vector3 ab, ac;
            Vector3.Subtract(ref B, ref A, out ab);
            Vector3.Subtract(ref C, ref A, out ac);
            //The point we are comparing against the triangle is 0,0,0, so instead of storing an "A->P" vector,
            //just use -A.
            //Same for B->P, C->P...

            //Check to see if it's outside A.
            //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside A.
            float AdotAB, AdotAC;
            Vector3.Dot(ref ab, ref A, out AdotAB);
            Vector3.Dot(ref ac, ref A, out AdotAC);
            AdotAB = -AdotAB;
            AdotAC = -AdotAC;
            if (AdotAC <= 0f && AdotAB <= 0)
            {
                //It is A!
                simplex.State = SimplexState.Point;
                point = A;
                return;
            }

            //Check to see if it's outside B.
            //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside B.
            float BdotAB, BdotAC;
            Vector3.Dot(ref ab, ref B, out BdotAB);
            Vector3.Dot(ref ac, ref B, out BdotAC);
            BdotAB = -BdotAB;
            BdotAC = -BdotAC;
            if (BdotAB >= 0f && BdotAC <= BdotAB)
            {
                //It is B!
                simplex.State = SimplexState.Point;
                simplex.A = simplex.B;

                point = B;
                return;
            }

            //Check to see if it's outside AB.
            float vc = AdotAB * BdotAC - BdotAB * AdotAC;
            if (vc <= 0 && AdotAB > 0 && BdotAB < 0)//Note > and < instead of => <=; avoids possibly division by zero
            {
                simplex.State = SimplexState.Segment;
                float V = AdotAB / (AdotAB - BdotAB);

                Vector3.Multiply(ref ab, V, out point);
                Vector3.Add(ref point, ref A, out point);
                return;
            }

            //Check to see if it's outside C.
            //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside C.
            float CdotAB, CdotAC;
            Vector3.Dot(ref ab, ref C, out CdotAB);
            Vector3.Dot(ref ac, ref C, out CdotAC);
            CdotAB = -CdotAB;
            CdotAC = -CdotAC;
            if (CdotAC >= 0f && CdotAB <= CdotAC)
            {
                //It is C!
                simplex.State = SimplexState.Point;
                simplex.A = simplex.C;
                point = A;
                return;
            }

            //Check if it's outside AC.            
            //float AdotAB, AdotAC;
            //Vector3.Dot(ref ab, ref A, out AdotAB);
            //Vector3.Dot(ref ac, ref A, out AdotAC);
            //AdotAB = -AdotAB;
            //AdotAC = -AdotAC;
            float vb = CdotAB * AdotAC - AdotAB * CdotAC;
            if (vb <= 0f && AdotAC > 0f && CdotAC < 0f)//Note > instead of >= and < instead of <=; prevents bad denominator
            {
                //Get rid of B.  Compress C into B.
                simplex.State = SimplexState.Segment;
                simplex.B = simplex.C;
                float V = AdotAC / (AdotAC - CdotAC);
                Vector3.Multiply(ref ac, V, out point);
                Vector3.Add(ref point, ref A, out point);
                return;
            }

            //Check if it's outside BC.
            //float BdotAB, BdotAC;
            //Vector3.Dot(ref ab, ref B, out BdotAB);
            //Vector3.Dot(ref ac, ref B, out BdotAC);
            //BdotAB = -BdotAB;
            //BdotAC = -BdotAC;
            float va = BdotAB * CdotAC - CdotAB * BdotAC;
            float d3d4;
            float d6d5;
            if (va <= 0f && (d3d4 = BdotAC - BdotAB) > 0f && (d6d5 = CdotAB - CdotAC) > 0f)//Note > instead of >= and < instead of <=; prevents bad denominator
            {
                //Throw away A.  C->A.
                //TODO: Does B->A, C->B work better?
                simplex.State = SimplexState.Segment;
                simplex.A = simplex.C;
                float U = d3d4 / (d3d4 + d6d5);

                Vector3 bc;
                Vector3.Subtract(ref C, ref B, out bc);
                Vector3.Multiply(ref bc, U, out point);
                Vector3.Add(ref point, ref B, out point);
                return;
            }


            //On the face of the triangle.
            float denom = 1f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            Vector3.Multiply(ref ab, v, out point);
            Vector3 acw;
            Vector3.Multiply(ref ac, w, out acw);
            Vector3.Add(ref A, ref point, out point);
            Vector3.Add(ref point, ref acw, out point);




        }

        ///<summary>
        /// Gets the point closest to the origin on the tetrahedron.
        ///</summary>
        ///<param name="simplex">Simplex to test.</param>
        ///<param name="point">Closest point.</param>
        ///<returns>Whether or not the tetrahedron encloses the origin.</returns>
        public bool GetPointOnTetrahedronClosestToOrigin(ref RaySimplex simplex, out Vector3 point)
        {

            //Thanks to the fact that D is new and that we know that the origin is within the extruded
            //triangular prism of ABC (and on the "D" side of ABC),
            //we can immediately ignore voronoi regions:
            //A, B, C, AC, AB, BC, ABC
            //and only consider:
            //D, DA, DB, DC, DAC, DCB, DBA

            //There is some overlap of calculations in this method, since DAC, DCB, and DBA are tested fully.
            
            //When this method is being called, we don't care about the state of 'this' simplex.  It's just a temporary shifted simplex.
            //The one that needs to be updated is the simplex being passed in.
            
            var minimumSimplex = new RaySimplex();
            point = new Vector3();
            float minimumDistance = float.MaxValue;


            RaySimplex candidate;
            float candidateDistance;
            Vector3 candidatePoint;
            if (TryTetrahedronTriangle(ref A, ref C, ref D,
                                       ref simplex.A, ref simplex.C, ref simplex.D,
                                       ref B, out candidate, out candidatePoint))
            {
                point = candidatePoint;
                minimumSimplex = candidate;
                minimumDistance = candidatePoint.LengthSquared();
            }

            if (TryTetrahedronTriangle(ref C, ref B, ref D,
                                       ref simplex.C, ref simplex.B, ref simplex.D,
                                       ref A, out candidate, out candidatePoint) &&
                (candidateDistance = candidatePoint.LengthSquared()) < minimumDistance)
            {
                point = candidatePoint;
                minimumSimplex = candidate;
                minimumDistance = candidateDistance;
            }

            if (TryTetrahedronTriangle(ref B, ref A, ref D,
                                       ref simplex.B, ref simplex.A, ref simplex.D,
                                       ref C, out candidate, out candidatePoint) &&
                (candidateDistance = candidatePoint.LengthSquared()) < minimumDistance)
            {
                point = candidatePoint;
                minimumSimplex = candidate;
                minimumDistance = candidateDistance;
            }

            if (TryTetrahedronTriangle(ref A, ref B, ref C,
                                       ref simplex.A, ref simplex.B, ref simplex.C,
                                       ref D, out candidate, out candidatePoint) &&
                (candidateDistance = candidatePoint.LengthSquared()) < minimumDistance)
            {
                point = candidatePoint;
                minimumSimplex = candidate;
                minimumDistance = candidateDistance;
            }


            if (minimumDistance < float.MaxValue)
            {
                simplex = minimumSimplex;
                return false;
            }
            return true;
        }


        private static bool TryTetrahedronTriangle(ref Vector3 A, ref Vector3 B, ref Vector3 C,
                                                   ref Vector3 simplexA, ref Vector3 simplexB, ref Vector3 simplexC,
                                                   ref Vector3 otherPoint, out RaySimplex simplex, out Vector3 point)
        {
            //Note that there may be some extra terms that can be removed from this process.
            //Some conditions could use less parameters, since it is known that the origin
            //is not 'behind' BC or AC.

            simplex = new RaySimplex();
            point = new Vector3();


            Vector3 ab, ac;
            Vector3.Subtract(ref B, ref A, out ab);
            Vector3.Subtract(ref C, ref A, out ac);
            Vector3 normal;
            Vector3.Cross(ref ab, ref ac, out normal);
            float AdotN, ADdotN;
            Vector3 AD;
            Vector3.Subtract(ref otherPoint, ref A, out AD);
            Vector3.Dot(ref A, ref normal, out AdotN);
            Vector3.Dot(ref AD, ref normal, out ADdotN);

            //If (-A * N) * (AD * N) < 0, D and O are on opposite sides of the triangle.
            if (AdotN * ADdotN >= 0)
            {
                //The point we are comparing against the triangle is 0,0,0, so instead of storing an "A->P" vector,
                //just use -A.
                //Same for B->, C->P...

                //Check to see if it's outside A.
                //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside A.
                float AdotAB, AdotAC;
                Vector3.Dot(ref ab, ref A, out AdotAB);
                Vector3.Dot(ref ac, ref A, out AdotAC);
                AdotAB = -AdotAB;
                AdotAC = -AdotAC;
                if (AdotAC <= 0f && AdotAB <= 0)
                {
                    //It is A!
                    simplex.State = SimplexState.Point;
                    simplex.A = simplexA;
                    point = A;
                    return true;
                }

                //Check to see if it's outside B.
                //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside B.
                float BdotAB, BdotAC;
                Vector3.Dot(ref ab, ref B, out BdotAB);
                Vector3.Dot(ref ac, ref B, out BdotAC);
                BdotAB = -BdotAB;
                BdotAC = -BdotAC;
                if (BdotAB >= 0f && BdotAC <= BdotAB)
                {
                    //It is B!
                    simplex.State = SimplexState.Point;
                    simplex.A = simplexB;
                    point = B;
                    return true;
                }

                //Check to see if it's outside AB.
                float vc = AdotAB * BdotAC - BdotAB * AdotAC;
                if (vc <= 0 && AdotAB > 0 && BdotAB < 0) //Note > and < instead of => <=; avoids possibly division by zero
                {
                    simplex.State = SimplexState.Segment;
                    simplex.A = simplexA;
                    simplex.B = simplexB;
                    float V = AdotAB / (AdotAB - BdotAB);

                    Vector3.Multiply(ref ab, V, out point);
                    Vector3.Add(ref point, ref A, out point);
                    return true;
                }

                //Check to see if it's outside C.
                //TODO: Note that in a boolean-style GJK, it shouldn't be possible to be outside C.
                float CdotAB, CdotAC;
                Vector3.Dot(ref ab, ref C, out CdotAB);
                Vector3.Dot(ref ac, ref C, out CdotAC);
                CdotAB = -CdotAB;
                CdotAC = -CdotAC;
                if (CdotAC >= 0f && CdotAB <= CdotAC)
                {
                    //It is C!
                    simplex.State = SimplexState.Point;
                    simplex.A = simplexC;
                    point = C;
                    return true;
                }

                //Check if it's outside AC.            
                //float AdotAB, AdotAC;
                //Vector3.Dot(ref ab, ref A, out AdotAB);
                //Vector3.Dot(ref ac, ref A, out AdotAC);
                //AdotAB = -AdotAB;
                //AdotAC = -AdotAC;
                float vb = CdotAB * AdotAC - AdotAB * CdotAC;
                if (vb <= 0f && AdotAC > 0f && CdotAC < 0f) //Note > instead of >= and < instead of <=; prevents bad denominator
                {
                    simplex.State = SimplexState.Segment;
                    simplex.A = simplexA;
                    simplex.B = simplexC;
                    float V = AdotAC / (AdotAC - CdotAC);
                    Vector3.Multiply(ref ac, V, out point);
                    Vector3.Add(ref point, ref A, out point);
                    return true;
                }

                //Check if it's outside BC.
                //float BdotAB, BdotAC;
                //Vector3.Dot(ref ab, ref B, out BdotAB);
                //Vector3.Dot(ref ac, ref B, out BdotAC);
                //BdotAB = -BdotAB;
                //BdotAC = -BdotAC;
                float va = BdotAB * CdotAC - CdotAB * BdotAC;
                float d3d4;
                float d6d5;
                if (va <= 0f && (d3d4 = BdotAC - BdotAB) > 0f && (d6d5 = CdotAB - CdotAC) > 0f)//Note > instead of >= and < instead of <=; prevents bad denominator
                {
                    simplex.State = SimplexState.Segment;
                    simplex.A = simplexB;
                    simplex.B = simplexC;
                    float V = d3d4 / (d3d4 + d6d5);

                    Vector3 bc;
                    Vector3.Subtract(ref C, ref B, out bc);
                    Vector3.Multiply(ref bc, V, out point);
                    Vector3.Add(ref point, ref B, out point);
                    return true;
                }


                //On the face of the triangle.
                simplex.State = SimplexState.Triangle;
                simplex.A = simplexA;
                simplex.B = simplexB;
                simplex.C = simplexC;
                float denom = 1f / (va + vb + vc);
                float w = vc * denom;
                float v = vb * denom;
                Vector3.Multiply(ref ab, v, out point);
                Vector3 acw;
                Vector3.Multiply(ref ac, w, out acw);
                Vector3.Add(ref A, ref point, out point);
                Vector3.Add(ref point, ref acw, out point);
                return true;
            }
            return false;
        }



        ///<summary>
        /// Adds a new point to the simplex.
        ///</summary>
        ///<param name="point">Point to add.</param>
        ///<param name="hitLocation">Current ray hit location.</param>
        ///<param name="shiftedSimplex">Simplex shifted with the hit location.</param>
        public void AddNewSimplexPoint(ref Vector3 point, ref Vector3 hitLocation, out RaySimplex shiftedSimplex)
        {
            shiftedSimplex = new RaySimplex();
            switch (State)
            {
                case SimplexState.Empty:
                    State = SimplexState.Point;
                    A = point;

                    Vector3.Subtract(ref hitLocation, ref A, out shiftedSimplex.A);
                    break;
                case SimplexState.Point:
                    State = SimplexState.Segment;
                    B = point;

                    Vector3.Subtract(ref hitLocation, ref A, out shiftedSimplex.A);
                    Vector3.Subtract(ref hitLocation, ref B, out shiftedSimplex.B);
                    break;
                case SimplexState.Segment:
                    State = SimplexState.Triangle;
                    C = point;

                    Vector3.Subtract(ref hitLocation, ref A, out shiftedSimplex.A);
                    Vector3.Subtract(ref hitLocation, ref B, out shiftedSimplex.B);
                    Vector3.Subtract(ref hitLocation, ref C, out shiftedSimplex.C);
                    break;
                case SimplexState.Triangle:
                    State = SimplexState.Tetrahedron;
                    D = point;

                    Vector3.Subtract(ref hitLocation, ref A, out shiftedSimplex.A);
                    Vector3.Subtract(ref hitLocation, ref B, out shiftedSimplex.B);
                    Vector3.Subtract(ref hitLocation, ref C, out shiftedSimplex.C);
                    Vector3.Subtract(ref hitLocation, ref D, out shiftedSimplex.D);
                    break;
            }
            shiftedSimplex.State = State;
        }

        /// <summary>
        /// Gets the error tolerance for the simplex.
        /// </summary>
        /// <param name="rayOrigin">Origin of the ray.</param>
        /// <returns>Error tolerance of the simplex.</returns>
        public float GetErrorTolerance(ref Vector3 rayOrigin)
        {
            switch (State)
            {
                case SimplexState.Point:
                    float distanceA;
                    Vector3.DistanceSquared(ref A, ref rayOrigin, out distanceA);
                    return distanceA;
                case SimplexState.Segment:
                    float distanceB;
                    Vector3.DistanceSquared(ref A, ref rayOrigin, out distanceA);
                    Vector3.DistanceSquared(ref B, ref rayOrigin, out distanceB);
                    return MathHelper.Max(distanceA, distanceB);
                case SimplexState.Triangle:
                    float distanceC;
                    Vector3.DistanceSquared(ref A, ref rayOrigin, out distanceA);
                    Vector3.DistanceSquared(ref B, ref rayOrigin, out distanceB);
                    Vector3.DistanceSquared(ref C, ref rayOrigin, out distanceC);
                    return MathHelper.Max(distanceA, MathHelper.Max(distanceB, distanceC));
                case SimplexState.Tetrahedron:
                    float distanceD;
                    Vector3.DistanceSquared(ref A, ref rayOrigin, out distanceA);
                    Vector3.DistanceSquared(ref B, ref rayOrigin, out distanceB);
                    Vector3.DistanceSquared(ref C, ref rayOrigin, out distanceC);
                    Vector3.DistanceSquared(ref D, ref rayOrigin, out distanceD);
                    return MathHelper.Max(distanceA, MathHelper.Max(distanceB, MathHelper.Max(distanceC, distanceD)));
            }
            return 0;
        }

    }

}
