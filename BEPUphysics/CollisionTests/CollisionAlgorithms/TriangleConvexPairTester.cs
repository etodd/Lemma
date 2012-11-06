using System;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;
using BEPUphysics.Settings;
using BEPUphysics.DataStructures;
using System.Diagnostics;

namespace BEPUphysics.CollisionTests.CollisionAlgorithms
{
    ///<summary>
    /// Persistent tester that compares triangles against convex objects.
    ///</summary>
    public class TriangleConvexPairTester : TrianglePairTester
    {
        internal ConvexShape convex;

        internal CollisionState state = CollisionState.Plane;
        private const int EscapeAttemptPeriod = 10;
        int escapeAttempts;

        Vector3 localSeparatingAxis;

        //Relies on the triangle being located in the local space of the convex object.  The convex transform is used to transform the
        //contact points back from the convex's local space into world space.
        ///<summary>
        /// Generates a contact between the triangle and convex.
        ///</summary>
        ///<param name="contactList">Contact between the shapes, if any.</param>
        ///<returns>Whether or not the shapes are colliding.</returns>
        public override bool GenerateContactCandidate(out TinyStructList<ContactData> contactList)
        {
            switch (state)
            {
                case CollisionState.Plane:
                    return DoPlaneTest(out contactList);
                case CollisionState.ExternalSeparated:
                    return DoExternalSeparated(out contactList);
                case CollisionState.ExternalNear:
                    return DoExternalNear(out contactList);
                case CollisionState.Deep:
                    return DoDeepContact(out contactList);
                default:
                    contactList = new TinyStructList<ContactData>();
                    return false;
            }



        }


        private bool DoPlaneTest(out TinyStructList<ContactData> contactList)
        {


            //Find closest point between object and plane.
            Vector3 reverseNormal;
            Vector3 ab, ac;
            Vector3.Subtract(ref triangle.vB, ref triangle.vA, out ab);
            Vector3.Subtract(ref triangle.vC, ref triangle.vA, out ac);
            Vector3.Cross(ref ac, ref ab, out reverseNormal);
            //Convex position dot normal is ALWAYS zero.  The thing to look at is the plane's 'd'.
            //If the distance along the normal is positive, then the convex is 'behind' that normal.
            float dotA;
            Vector3.Dot(ref triangle.vA, ref reverseNormal, out dotA);

            contactList = new TinyStructList<ContactData>();
            switch (triangle.sidedness)
            {
                case TriangleSidedness.DoubleSided:
                    if (dotA < 0)
                    {
                        //The reverse normal is pointing towards the convex.
                        //It needs to point away from the convex so that the direction
                        //will get the proper extreme point.
                        Vector3.Negate(ref reverseNormal, out reverseNormal);
                        dotA = -dotA;
                    }
                    break;
                case TriangleSidedness.Clockwise:
                    //if (dotA < 0)
                    //{
                    //    //The reverse normal is pointing towards the convex.
                    //    return false;
                    //}
                    break;
                case TriangleSidedness.Counterclockwise:
                    //if (dotA > 0)
                    //{
                    //    //The reverse normal is pointing away from the convex.
                    //    return false;
                    //}

                    //The reverse normal is pointing towards the convex.
                    //It needs to point away from the convex so that the direction
                    //will get the proper extreme point.
                    Vector3.Negate(ref reverseNormal, out reverseNormal);
                    dotA = -dotA;
                    break;
            }
            Vector3 extremePoint;
            convex.GetLocalExtremePointWithoutMargin(ref reverseNormal, out extremePoint);


            //See if the extreme point is within the face or not.
            //It might seem like the easy "depth" test should come first, since a barycentric
            //calculation takes a bit more time.  However, transferring from plane to depth is 'rare' 
            //(like all transitions), and putting this test here is logically closer to its requirements'
            //computation.

            if (GetVoronoiRegion(ref extremePoint) != VoronoiRegion.ABC)
            {
                state = CollisionState.ExternalSeparated;
                return DoExternalSeparated(out contactList);
            }



            float dotE;
            Vector3.Dot(ref extremePoint, ref reverseNormal, out dotE);
            float t = (dotA - dotE) / reverseNormal.LengthSquared();



            Vector3 offset;
            Vector3.Multiply(ref reverseNormal, t, out offset);

            //Compare the distance from the plane to the convex object.
            float distanceSquared = offset.LengthSquared();

            float marginSum = triangle.collisionMargin + convex.collisionMargin;
            //TODO: Could just normalize early and avoid computing point plane before it's necessary.  
            //Exposes a sqrt but...
            if (t <= 0 || distanceSquared < marginSum * marginSum)
            {
                //The convex object is in the margin of the plane.
                //All that's left is to create the contact.


                var contact = new ContactData();
                //Displacement is from A to B.  point = A + t * AB, where t = marginA / margin.
                if (marginSum > Toolbox.Epsilon) //This can be zero! It would cause a NaN is unprotected.
                    Vector3.Multiply(ref offset, convex.collisionMargin / marginSum, out contact.Position); //t * AB
                else contact.Position = new Vector3();
                Vector3.Add(ref extremePoint, ref contact.Position, out contact.Position); //A + t * AB.

                float normalLength = reverseNormal.Length();
                Vector3.Divide(ref reverseNormal, normalLength, out contact.Normal);
                float distance = normalLength * t;



                contact.PenetrationDepth = marginSum - distance;

                if (contact.PenetrationDepth > marginSum)
                {
                    //Check to see if the inner sphere is touching the plane.
                    //This does not override other tests; there can be more than one contact from a single triangle.

                    ContactData alternateContact;
                    if (TryInnerSphereContact(out alternateContact))// && alternateContact.PenetrationDepth > contact.PenetrationDepth)
                    {
                        contactList.Add(ref alternateContact);
                    }

                    //The convex object is stuck deep in the plane!
                    //The most problematic case for this is when
                    //an object is right on top of a cliff.
                    //The lower, vertical triangle may occasionally detect
                    //a contact with the object, but would compute an extremely
                    //deep depth if the normal plane test was used.




                    //Verify that the depth is correct by trying another approach.
                    CollisionState previousState = state;
                    state = CollisionState.ExternalNear;
                    TinyStructList<ContactData> alternateContacts;
                    if (DoExternalNear(out alternateContacts))
                    {
                        alternateContacts.Get(0, out alternateContact);
                        if (alternateContact.PenetrationDepth + .01f < contact.PenetrationDepth) //Bias against the subtest's result, since the plane version will probably have a better position.
                        {
                            //It WAS a bad contact.
                            contactList.Add(ref alternateContact);
                            //DoDeepContact (which can be called from within DoExternalNear) can generate two contacts, but the second contact would just be an inner sphere (which we already generated).
                            //DoExternalNear can only generate one contact.  So we only need the first contact!
                            //TODO: This is a fairly fragile connection between the two stages.  Consider robustifying. (Also, the TryInnerSphereContact is done twice! This process is very rare for marginful pairs, though)
                        }
                        else
                        {
                            //Well, it really is just that deep.
                            contactList.Add(ref contact);
                            state = previousState;
                        }
                    }
                    else
                    {
                        //If the external near test finds that there was no collision at all, 
                        //just return to plane testing.  If the point turns up outside the face region
                        //next time, the system will adapt.
                        state = previousState;
                        return false;
                    }
                }
                else
                {
                    contactList.Add(ref contact);
                }
                return true;

            }
            return false;


        }




        private bool DoExternalSeparated(out TinyStructList<ContactData> contactList)
        {

            if (GJKToolbox.AreShapesIntersecting(convex, triangle, ref Toolbox.RigidIdentity, ref Toolbox.RigidIdentity, ref localSeparatingAxis))
            {
                state = CollisionState.ExternalNear;
                return DoExternalNear(out contactList);
            }
            TryToEscape();
            contactList = new TinyStructList<ContactData>();
            return false;
        }

        private bool DoExternalNear(out TinyStructList<ContactData> contactList)
        {

            Vector3 closestA, closestB;


            //Don't bother trying to do any clever caching.  The continually transforming simplex makes it very rarely useful.
            //TODO: Initialize the simplex of the GJK method using the 'true' center of the triangle.
            //If left unmodified, the simplex that is used in GJK will just be a point at 0,0,0, which of course is at the origin.
            //This causes an instant-out, always.  Not good!
            //By giving the contributing simplex the average centroid, it has a better guess.
            Vector3 triangleCentroid;
            Vector3.Add(ref triangle.vA, ref triangle.vB, out triangleCentroid);
            Vector3.Add(ref triangleCentroid, ref triangle.vC, out triangleCentroid);
            Vector3.Multiply(ref triangleCentroid, .33333333f, out triangleCentroid);

            var initialSimplex = new CachedSimplex { State = SimplexState.Point, LocalSimplexB = { A = triangleCentroid } };
            if (GJKToolbox.GetClosestPoints(convex, triangle, ref Toolbox.RigidIdentity, ref Toolbox.RigidIdentity, ref initialSimplex, out closestA, out closestB))
            {
                state = CollisionState.Deep;
                return DoDeepContact(out contactList);
            }
            Vector3 displacement;
            Vector3.Subtract(ref closestB, ref closestA, out displacement);
            float distanceSquared = displacement.LengthSquared();
            float margin = convex.collisionMargin + triangle.collisionMargin;

            contactList = new TinyStructList<ContactData>();
            if (distanceSquared < margin * margin)
            {
                //Try to generate a contact.
                var contact = new ContactData();

                //Determine if the normal points in the appropriate direction given the sidedness of the triangle.
                if (triangle.sidedness != TriangleSidedness.DoubleSided)
                {
                    Vector3 triangleNormal, ab, ac;
                    Vector3.Subtract(ref triangle.vB, ref triangle.vA, out ab);
                    Vector3.Subtract(ref triangle.vC, ref triangle.vA, out ac);
                    Vector3.Cross(ref ab, ref ac, out triangleNormal);
                    float dot;
                    Vector3.Dot(ref triangleNormal, ref displacement, out dot);
                    if (triangle.sidedness == TriangleSidedness.Clockwise && dot > 0)
                        return false;
                    if (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0)
                        return false;
                }


                //Displacement is from A to B.  point = A + t * AB, where t = marginA / margin.
                if (margin > Toolbox.Epsilon) //This can be zero! It would cause a NaN if unprotected.
                    Vector3.Multiply(ref displacement, convex.collisionMargin / margin, out contact.Position); //t * AB
                else contact.Position = new Vector3();
                Vector3.Add(ref closestA, ref contact.Position, out contact.Position); //A + t * AB.



                contact.Normal = displacement;
                float distance = (float)Math.Sqrt(distanceSquared);
                Vector3.Divide(ref contact.Normal, distance, out contact.Normal);
                contact.PenetrationDepth = margin - distance;



                contactList.Add(ref contact);
                TryToEscape(ref contact.Position);
                return true;

            }
            //Too far to make a contact- move back to separation.
            state = CollisionState.ExternalSeparated;
            return false;
        }

        private bool DoDeepContact(out TinyStructList<ContactData> contactList)
        {


            //Find the origin to triangle center offset.
            Vector3 center;
            Vector3.Add(ref triangle.vA, ref triangle.vB, out center);
            Vector3.Add(ref center, ref triangle.vC, out center);
            Vector3.Multiply(ref center, 1f / 3f, out center);

            ContactData contact;

            contactList = new TinyStructList<ContactData>();

            if (MPRToolbox.AreLocalShapesOverlapping(convex, triangle, ref center, ref Toolbox.RigidIdentity))
            {

                float dot;


                Vector3 triangleNormal, ab, ac;
                Vector3.Subtract(ref triangle.vB, ref triangle.vA, out ab);
                Vector3.Subtract(ref triangle.vC, ref triangle.vA, out ac);
                Vector3.Cross(ref ab, ref ac, out triangleNormal);
                float lengthSquared = triangleNormal.LengthSquared();
                if (lengthSquared < Toolbox.Epsilon * .01f)
                {
                    //Degenerate triangle! That's no good.
                    //Just use the direction pointing from A to B, "B" being the triangle.  That direction is center - origin, or just center.
                    MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref center, out contact.PenetrationDepth, out contact.Normal, out contact.Position);
                }
                else
                {
                    //Normalize the normal.
                    Vector3.Divide(ref triangleNormal, (float)Math.Sqrt(lengthSquared), out triangleNormal);


                    ////The first direction to check is one of the triangle's edge normals.  Choose the one that is most aligned with the offset from A to B.
                    ////Project the direction onto the triangle plane.
                    //Vector3.Dot(ref triangleNormal, ref center, out dot);
                    //Vector3 trianglePlaneDirection;
                    //Vector3.Multiply(ref triangleNormal, dot, out trianglePlaneDirection);
                    //Vector3.Subtract(ref trianglePlaneDirection, ref center, out trianglePlaneDirection);

                    ////To find out which edge to use, compute which region the direction is in.
                    ////This is done by constructing three planes which segment the triangle into three sub-triangles.

                    ////These planes are defined by A, origin, center; B, origin, center; C, origin, center.
                    ////The plane tests against the direction can be reordered to:
                    ////(center x direction) * A
                    ////(center x direction) * B
                    ////(center x direction) * C
                    //Vector3 OxD;
                    //Vector3.Cross(ref trianglePlaneDirection, ref center, out OxD);
                    //Vector3 p;

                    //float dotA, dotB, dotC;
                    //Vector3.Dot(ref triangle.vA, ref OxD, out dotA);
                    //Vector3.Dot(ref triangle.vB, ref OxD, out dotB);
                    //Vector3.Dot(ref triangle.vC, ref OxD, out dotC);

                    //if (dotA >= 0 && dotB <= 0)
                    //{
                    //    //Direction is in the AB edge zone.
                    //    //Compute the edge normal using AB x (AO x AB).
                    //    Vector3 AB, AO;
                    //    Vector3.Subtract(ref triangle.vB, ref triangle.vA, out AB);
                    //    Vector3.Subtract(ref center, ref triangle.vA, out AO);
                    //    Vector3.Cross(ref AO, ref AB, out p);
                    //    Vector3.Cross(ref AB, ref p, out trianglePlaneDirection);
                    //}
                    //else if (dotB >= 0 && dotC <= 0)
                    //{
                    //    //Direction is in the BC edge zone.
                    //    //Compute the edge normal using BC x (BO x BC).
                    //    Vector3 BC, BO;
                    //    Vector3.Subtract(ref triangle.vC, ref triangle.vB, out BC);
                    //    Vector3.Subtract(ref center, ref triangle.vB, out BO);
                    //    Vector3.Cross(ref BO, ref BC, out p);
                    //    Vector3.Cross(ref BC, ref p, out trianglePlaneDirection);

                    //}
                    //else // dotC > 0 && dotA < 0
                    //{
                    //    //Direction is in the CA edge zone.
                    //    //Compute the edge normal using CA x (CO x CA).
                    //    Vector3 CA, CO;
                    //    Vector3.Subtract(ref triangle.vA, ref triangle.vC, out CA);
                    //    Vector3.Subtract(ref center, ref triangle.vC, out CO);
                    //    Vector3.Cross(ref CO, ref CA, out p);
                    //    Vector3.Cross(ref CA, ref p, out trianglePlaneDirection);
                    //}



                    //dot = trianglePlaneDirection.LengthSquared();
                    //if (dot > Toolbox.Epsilon)
                    //{
                    //    Vector3.Divide(ref trianglePlaneDirection, (float)Math.Sqrt(dot), out trianglePlaneDirection);
                    //    MPRTesting.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref trianglePlaneDirection, out contact.PenetrationDepth, out contact.Normal);
                    //    //Check to see if the normal is facing in the proper direction, considering that this may not be a two-sided triangle.
                    //    Vector3.Dot(ref triangleNormal, ref contact.Normal, out dot);
                    //    if ((triangle.sidedness == TriangleSidedness.Clockwise && dot > 0) || (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0))
                    //    {
                    //        //Normal was facing the wrong way.
                    //        //Instead of ignoring it entirely, correct the direction to as close as it can get by removing any component parallel to the triangle normal.
                    //        Vector3 previousNormal = contact.Normal;
                    //        Vector3.Dot(ref contact.Normal, ref triangleNormal, out dot);

                    //        Vector3.Multiply(ref contact.Normal, dot, out p);
                    //        Vector3.Subtract(ref contact.Normal, ref p, out contact.Normal);
                    //        float length = contact.Normal.LengthSquared();
                    //        if (length > Toolbox.Epsilon)
                    //        {
                    //            //Renormalize the corrected normal.
                    //            Vector3.Divide(ref contact.Normal, (float)Math.Sqrt(length), out contact.Normal);
                    //            Vector3.Dot(ref contact.Normal, ref previousNormal, out dot);
                    //            contact.PenetrationDepth *= dot;
                    //        }
                    //        else
                    //        {
                    //            contact.PenetrationDepth = float.MaxValue;
                    //            contact.Normal = new Vector3();
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    contact.PenetrationDepth = float.MaxValue;
                    //    contact.Normal = new Vector3();
                    //}

                    //TODO: This tests all three edge axes with a full MPR raycast.  That's not really necessary; the correct edge normal should be discoverable, resulting in a single MPR raycast.

                    //Find the edge directions that will be tested with MPR.
                    Vector3 AO, BO, CO;
                    Vector3 AB, BC, CA;
                    Vector3.Subtract(ref center, ref triangle.vA, out AO);
                    Vector3.Subtract(ref center, ref triangle.vB, out BO);
                    Vector3.Subtract(ref center, ref triangle.vC, out CO);
                    Vector3.Subtract(ref triangle.vB, ref triangle.vA, out AB);
                    Vector3.Subtract(ref triangle.vC, ref triangle.vB, out BC);
                    Vector3.Subtract(ref triangle.vA, ref triangle.vC, out CA);


                    //We don't have to worry about degenerate triangles here because we've already handled that possibility above.
                    Vector3 ABnormal, BCnormal, CAnormal;

                    //Project the center onto the edge to find the direction from the center to the edge AB.
                    Vector3.Dot(ref AO, ref AB, out dot);
                    Vector3.Multiply(ref AB, dot / AB.LengthSquared(), out ABnormal);
                    Vector3.Subtract(ref AO, ref ABnormal, out ABnormal);
                    ABnormal.Normalize();

                    //Project the center onto the edge to find the direction from the center to the edge BC.
                    Vector3.Dot(ref BO, ref BC, out dot);
                    Vector3.Multiply(ref BC, dot / BC.LengthSquared(), out BCnormal);
                    Vector3.Subtract(ref BO, ref BCnormal, out BCnormal);
                    BCnormal.Normalize();

                    //Project the center onto the edge to find the direction from the center to the edge BC.
                    Vector3.Dot(ref CO, ref CA, out dot);
                    Vector3.Multiply(ref CA, dot / CA.LengthSquared(), out CAnormal);
                    Vector3.Subtract(ref CO, ref CAnormal, out CAnormal);
                    CAnormal.Normalize();


                    MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref ABnormal, out contact.PenetrationDepth, out contact.Normal);
                    //Check to see if the normal is facing in the proper direction, considering that this may not be a two-sided triangle.
                    Vector3.Dot(ref triangleNormal, ref contact.Normal, out dot);
                    if ((triangle.sidedness == TriangleSidedness.Clockwise && dot > 0) || (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0))
                    {
                        //Normal was facing the wrong way.
                        //Instead of ignoring it entirely, correct the direction to as close as it can get by removing any component parallel to the triangle normal.
                        Vector3 previousNormal = contact.Normal;
                        Vector3.Dot(ref contact.Normal, ref triangleNormal, out dot);

                        Vector3 p;
                        Vector3.Multiply(ref contact.Normal, dot, out p);
                        Vector3.Subtract(ref contact.Normal, ref p, out contact.Normal);
                        float length = contact.Normal.LengthSquared();
                        if (length > Toolbox.Epsilon)
                        {
                            //Renormalize the corrected normal.
                            Vector3.Divide(ref contact.Normal, (float)Math.Sqrt(length), out contact.Normal);
                            Vector3.Dot(ref contact.Normal, ref previousNormal, out dot);
                            contact.PenetrationDepth *= dot;
                        }
                        else
                        {
                            contact.PenetrationDepth = float.MaxValue;
                            contact.Normal = new Vector3();
                        }
                    }



                    Vector3 candidateNormal;
                    float candidateDepth;

                    MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref BCnormal, out candidateDepth, out candidateNormal);
                    //Check to see if the normal is facing in the proper direction, considering that this may not be a two-sided triangle.
                    Vector3.Dot(ref triangleNormal, ref candidateNormal, out dot);
                    if ((triangle.sidedness == TriangleSidedness.Clockwise && dot > 0) || (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0))
                    {
                        //Normal was facing the wrong way.
                        //Instead of ignoring it entirely, correct the direction to as close as it can get by removing any component parallel to the triangle normal.
                        Vector3 previousNormal = candidateNormal;
                        Vector3.Dot(ref candidateNormal, ref triangleNormal, out dot);

                        Vector3 p;
                        Vector3.Multiply(ref candidateNormal, dot, out p);
                        Vector3.Subtract(ref candidateNormal, ref p, out candidateNormal);
                        float length = candidateNormal.LengthSquared();
                        if (length > Toolbox.Epsilon)
                        {
                            //Renormalize the corrected normal.
                            Vector3.Divide(ref candidateNormal, (float)Math.Sqrt(length), out candidateNormal);
                            Vector3.Dot(ref candidateNormal, ref previousNormal, out dot);
                            candidateDepth *= dot;
                        }
                        else
                        {
                            contact.PenetrationDepth = float.MaxValue;
                            contact.Normal = new Vector3();
                        }
                    }
                    if (candidateDepth < contact.PenetrationDepth)
                    {
                        contact.Normal = candidateNormal;
                        contact.PenetrationDepth = candidateDepth;
                    }



                    MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref CAnormal, out candidateDepth, out candidateNormal);
                    //Check to see if the normal is facing in the proper direction, considering that this may not be a two-sided triangle.
                    Vector3.Dot(ref triangleNormal, ref candidateNormal, out dot);
                    if ((triangle.sidedness == TriangleSidedness.Clockwise && dot > 0) || (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0))
                    {
                        //Normal was facing the wrong way.
                        //Instead of ignoring it entirely, correct the direction to as close as it can get by removing any component parallel to the triangle normal.
                        Vector3 previousNormal = candidateNormal;
                        Vector3.Dot(ref candidateNormal, ref triangleNormal, out dot);

                        Vector3 p;
                        Vector3.Multiply(ref candidateNormal, dot, out p);
                        Vector3.Subtract(ref candidateNormal, ref p, out candidateNormal);
                        float length = candidateNormal.LengthSquared();
                        if (length > Toolbox.Epsilon)
                        {
                            //Renormalize the corrected normal.
                            Vector3.Divide(ref candidateNormal, (float)Math.Sqrt(length), out candidateNormal);
                            Vector3.Dot(ref candidateNormal, ref previousNormal, out dot);
                            candidateDepth *= dot;
                        }
                        else
                        {
                            contact.PenetrationDepth = float.MaxValue;
                            contact.Normal = new Vector3();
                        }
                    }
                    if (candidateDepth < contact.PenetrationDepth)
                    {
                        contact.Normal = candidateNormal;
                        contact.PenetrationDepth = candidateDepth;
                    }



                    //Try the depth along the positive triangle normal.

                    //If it's clockwise, this direction is unnecessary (the resulting normal would be invalidated by the onesidedness of the triangle).
                    if (triangle.sidedness != TriangleSidedness.Clockwise)
                    {
                        MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref triangleNormal, out candidateDepth, out candidateNormal);
                        if (candidateDepth < contact.PenetrationDepth)
                        {
                            contact.Normal = candidateNormal;
                            contact.PenetrationDepth = candidateDepth;
                        }
                    }

                    //Try the depth along the negative triangle normal.

                    //If it's counterclockwise, this direction is unnecessary (the resulting normal would be invalidated by the onesidedness of the triangle).
                    if (triangle.sidedness != TriangleSidedness.Counterclockwise)
                    {
                        Vector3.Negate(ref triangleNormal, out triangleNormal);
                        MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref triangleNormal, out candidateDepth, out candidateNormal);
                        if (candidateDepth < contact.PenetrationDepth)
                        {
                            contact.Normal = candidateNormal;
                            contact.PenetrationDepth = candidateDepth;
                        }
                    }




                }



                MPRToolbox.RefinePenetration(convex, triangle, ref Toolbox.RigidIdentity, contact.PenetrationDepth, ref contact.Normal, out contact.PenetrationDepth, out contact.Normal, out contact.Position);

                //It's possible for the normal to still face the 'wrong' direction according to one sided triangles.
                if (triangle.sidedness != TriangleSidedness.DoubleSided)
                {
                    Vector3.Dot(ref triangleNormal, ref contact.Normal, out dot);
                    if (dot < 0)
                    {
                        //Skip the add process.
                        goto InnerSphere;
                    }
                }


               contact.Id = -1;

                if (contact.PenetrationDepth < convex.collisionMargin + triangle.collisionMargin)
                {
                    state = CollisionState.ExternalNear; //If it's emerged from the deep contact, we can go back to using the preferred GJK method.
                }
                contactList.Add(ref contact);
            }

        InnerSphere:

            if (TryInnerSphereContact(out contact))
            {
                contactList.Add(ref contact);
            }
            if (contactList.count > 0)
                return true;

            state = CollisionState.ExternalSeparated;
            return false;












        }


        void TryToEscape()
        {
            if (++escapeAttempts == EscapeAttemptPeriod)
            {
                escapeAttempts = 0;
                state = CollisionState.Plane;
            }
        }

        void TryToEscape(ref Vector3 position)
        {
            if (++escapeAttempts == EscapeAttemptPeriod && GetVoronoiRegion(ref position) == VoronoiRegion.ABC)
            {
                escapeAttempts = 0;
                state = CollisionState.Plane;
            }
        }


        private bool TryInnerSphereContact(out ContactData contact)
        {
            Vector3 closestPoint;
            Toolbox.GetClosestPointOnTriangleToPoint(ref triangle.vA, ref triangle.vB, ref triangle.vC, ref Toolbox.ZeroVector, out closestPoint);
            float length = closestPoint.LengthSquared();
            float minimumRadius = convex.minimumRadius * (MotionSettings.CoreShapeScaling + .01f);
            if (length < minimumRadius * minimumRadius)
            {
                Vector3 triangleNormal, ab, ac;
                Vector3.Subtract(ref triangle.vB, ref triangle.vA, out ab);
                Vector3.Subtract(ref triangle.vC, ref triangle.vA, out ac);
                Vector3.Cross(ref ab, ref ac, out triangleNormal);
                float dot;
                Vector3.Dot(ref closestPoint, ref triangleNormal, out dot);
                if ((triangle.sidedness == TriangleSidedness.Clockwise && dot > 0) || (triangle.sidedness == TriangleSidedness.Counterclockwise && dot < 0))
                {
                    //Normal was facing the wrong way.
                    contact = new ContactData();
                    return false;
                }

                length = (float)Math.Sqrt(length);
                contact.Position = closestPoint;

                if (length > Toolbox.Epsilon) //Watch out for NaN's!
                {
                    Vector3.Divide(ref closestPoint, length, out contact.Normal);
                }
                else
                {
                    //The direction is undefined.  Use the triangle's normal.
                    //One sided triangles can only face in the appropriate direction.
                    float normalLength = triangleNormal.LengthSquared();
                    if (triangleNormal.LengthSquared() > Toolbox.Epsilon)
                    {
                        Vector3.Divide(ref triangleNormal, (float)Math.Sqrt(normalLength), out triangleNormal);
                        if (triangle.sidedness == TriangleSidedness.Clockwise)
                            contact.Normal = triangleNormal;
                        else
                            Vector3.Negate(ref triangleNormal, out contact.Normal);
                    }
                    else
                    {
                        //Degenerate triangle!
                        contact = new ContactData();
                        return false;
                    }
                }

                //Compute the actual depth of the contact.
                MPRToolbox.LocalSurfaceCast(convex, triangle, ref Toolbox.RigidIdentity, ref contact.Normal, out contact.PenetrationDepth, out triangleNormal); //Trash the 'corrected' normal.  We want to use the spherical normal.
                contact.Id = -1;
                return true;
            }
            contact = new ContactData();
            return false;
        }

        ///<summary>
        /// Determines what voronoi region a given point is in.
        ///</summary>
        ///<param name="p">Point to test.</param>
        ///<returns>Voronoi region containing the point.</returns>
        private VoronoiRegion GetVoronoiRegion(ref Vector3 p)
        {
            //The point we are comparing against the triangle is 0,0,0, so instead of storing an "A->P" vector,
            //just use -A.
            //Same for B->, C->P...

            Vector3 ab, ac, ap;
            Vector3.Subtract(ref triangle.vB, ref triangle.vA, out ab);
            Vector3.Subtract(ref triangle.vC, ref triangle.vA, out ac);
            Vector3.Subtract(ref p, ref triangle.vA, out ap);

            //Check to see if it's outside A.
            float APdotAB, APdotAC;
            Vector3.Dot(ref ap, ref ab, out APdotAB);
            Vector3.Dot(ref ap, ref ac, out APdotAC);
            if (APdotAC <= 0f && APdotAB <= 0)
            {
                //It is A!
                return VoronoiRegion.A;
            }

            //Check to see if it's outside B.
            float BPdotAB, BPdotAC;
            Vector3 bp;
            Vector3.Subtract(ref p, ref triangle.vB, out bp);
            Vector3.Dot(ref ab, ref bp, out BPdotAB);
            Vector3.Dot(ref ac, ref bp, out BPdotAC);
            if (BPdotAB >= 0f && BPdotAC <= BPdotAB)
            {
                //It is B!
                return VoronoiRegion.B;
            }

            //Check to see if it's outside AB.
            float vc = APdotAB * BPdotAC - BPdotAB * APdotAC;
            if (vc <= 0 && APdotAB > 0 && BPdotAB < 0) //Note > and < instead of => <=; avoids possibly division by zero
            {
                return VoronoiRegion.AB;
            }

            //Check to see if it's outside C.
            float CPdotAB, CPdotAC;
            Vector3 cp;
            Vector3.Subtract(ref p, ref triangle.vC, out cp);
            Vector3.Dot(ref ab, ref cp, out CPdotAB);
            Vector3.Dot(ref ac, ref cp, out CPdotAC);
            if (CPdotAC >= 0f && CPdotAB <= CPdotAC)
            {
                //It is C!
                return VoronoiRegion.C;
            }

            //Check if it's outside AC.    
            float vb = CPdotAB * APdotAC - APdotAB * CPdotAC;
            if (vb <= 0f && APdotAC > 0f && CPdotAC < 0f) //Note > instead of >= and < instead of <=; prevents bad denominator
            {
                return VoronoiRegion.AC;
            }

            //Check if it's outside BC.
            float va = BPdotAB * CPdotAC - CPdotAB * BPdotAC;
            if (va <= 0f && (BPdotAC - BPdotAB) > 0f && (CPdotAB - CPdotAC) > 0f)//Note > instead of >= and < instead of <=; prevents bad denominator
            {
                return VoronoiRegion.BC;
            }


            //On the face of the triangle.
            return VoronoiRegion.ABC;


        }

        ///<summary>
        /// Initializes the pair tester.
        ///</summary>
        ///<param name="convex">Convex shape to use.</param>
        ///<param name="triangle">Triangle shape to use.</param>
        public override void Initialize(ConvexShape convex, TriangleShape triangle)
        {
            this.convex = convex;
            this.triangle = triangle;
        }

        /// <summary>
        /// Cleans up the pair tester.
        /// </summary>
        public override void CleanUp()
        {
            triangle = null;
            convex = null;
            state = CollisionState.Plane;
            escapeAttempts = 0;
            localSeparatingAxis = new Vector3();
            Updated = false;
        }

        internal enum CollisionState
        {
            Plane,
            ExternalSeparated,
            ExternalNear,
            Deep
        }


        public override VoronoiRegion GetRegion(ref ContactData contact)
        {
            //Deep contact can produce non-triangle normals while still being within the triangle.
            //To solve this problem, find the voronoi region to which the contact belongs using its normal.
            //The voronoi region will be either the most extreme vertex, or the edge that includes
            //the first and second most extreme vertices.
            //If the normal dotted with an extreme edge direction is near 0, then it belongs to the edge.
            //Otherwise, it belongs to the vertex.
            //MPR tends to produce 'approximate' normals, though.
            //Use a fairly forgiving epsilon.
            float dotA, dotB, dotC;
            Vector3.Dot(ref triangle.vA, ref contact.Normal, out dotA);
            Vector3.Dot(ref triangle.vB, ref contact.Normal, out dotB);
            Vector3.Dot(ref triangle.vC, ref contact.Normal, out dotC);

            //Since normal points from convex to triangle always, reverse dot signs.
            dotA = -dotA;
            dotB = -dotB;
            dotC = -dotC;


            float faceEpsilon = .01f;
            const float edgeEpsilon = .01f;

            float edgeDot;
            Vector3 edgeDirection;
            if (dotA > dotB && dotA > dotC)
            {
                //A is extreme.
                if (dotB > dotC)
                {
                    //B is second most extreme.
                    if (Math.Abs(dotA - dotC) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vB, ref triangle.vA, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.AB;
                        else
                            return VoronoiRegion.A;
                    }
                }
                else
                {
                    //C is second most extreme.
                    if (Math.Abs(dotA - dotB) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vC, ref triangle.vA, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.AC;
                        else
                            return VoronoiRegion.A;
                    }
                }
            }
            else if (dotB > dotC)
            {
                //B is extreme.
                if (dotC > dotA)
                {
                    //C is second most extreme.
                    if (Math.Abs(dotB - dotA) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vC, ref triangle.vB, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.BC;
                        else
                            return VoronoiRegion.B;
                    }
                }
                else
                {
                    //A is second most extreme.
                    if (Math.Abs(dotB - dotC) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vA, ref triangle.vB, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.AB;
                        else
                            return VoronoiRegion.B;
                    }
                }
            }
            else
            {
                //C is extreme.
                if (dotA > dotB)
                {
                    //A is second most extreme.
                    if (Math.Abs(dotC - dotB) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vA, ref triangle.vC, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.AC;
                        else
                            return VoronoiRegion.C;
                    }
                }
                else
                {
                    //B is second most extreme.
                    if (Math.Abs(dotC - dotA) < faceEpsilon)
                    {
                        //The normal is basically a face normal.  This can happen at the edges occasionally.
                        return VoronoiRegion.ABC;
                    }
                    else
                    {
                        Vector3.Subtract(ref triangle.vB, ref triangle.vC, out edgeDirection);
                        Vector3.Dot(ref edgeDirection, ref contact.Normal, out edgeDot);
                        if (edgeDot * edgeDot < edgeDirection.LengthSquared() * edgeEpsilon)
                            return VoronoiRegion.BC;
                        else
                            return VoronoiRegion.C;
                    }
                }
            }

        }

        public override bool ShouldCorrectContactNormal
        {
            get
            {
                return state == CollisionState.Deep;
            }
        }

    }

}
