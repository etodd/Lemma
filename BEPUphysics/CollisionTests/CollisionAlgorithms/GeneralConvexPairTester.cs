using System;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionTests.CollisionAlgorithms
{
    ///<summary>
    /// Tests convex shapes against other convex shapes for contact generation.
    ///</summary>
    public class GeneralConvexPairTester
    {
        //TODO: warmstarted calculations like those within this tester will carry over bad information if the shape of an object is changed.
        //Need to notify the system to take appropriate action when a shape changes...

        ///<summary>
        /// Whether or not to use simplex caching in general case convex-convex collisions.
        /// This will improve performance in simulations relying on the general case system, 
        /// but may decrease quality of behavior for curved shapes.
        ///</summary>
        public static bool UseSimplexCaching;
        private CollisionState state = CollisionState.Separated;
        private CollisionState previousState = CollisionState.Separated;

        Vector3 localSeparatingAxis;
        CachedSimplex cachedSimplex;

        protected internal ConvexCollidable collidableA;
        protected internal ConvexCollidable collidableB;

        ///<summary>
        /// Gets the first collidable in the pair.
        ///</summary>
        public ConvexCollidable CollidableA
        {
            get
            {
                return collidableA;
            }
        }
        ///<summary>
        /// Gets the second collidable in the pair.
        ///</summary>
        public ConvexCollidable CollidableB
        {
            get
            {
                return collidableB;
            }
        }


        ///<summary>
        /// Generates a contact between the objects, if possible.
        ///</summary>
        ///<param name="contact">Contact created between the pair, if possible.</param>
        ///<returns>Whether or not the objects were colliding.</returns>
        public bool GenerateContactCandidate(out ContactData contact)
        {
            //Generate contacts.  This will just find one closest point using general supportmapping based systems like MPR and GJK.

            //The collision system moves through a state machine depending on the latest collision generation result.
            //At first, assume that the pair is completely separating.  This is almost always the correct guess for new pairs.
            //An extremely fast, warm-startable boolean GJK test can be performed.  If it returns with nonintersection, we can quit and do nothing.
            //If the initial boolean GJK test finds intersection, move onto a shallow contact test.
            //The shallow contact test is a different kind of GJK test that finds the closest points between the shape pair.  It's not as speedy as the boolean version.
            //The algorithm is run between the marginless versions of the shapes, so that the closest points will form a contact somewhere in the space separating the cores.
            //If the closest point system finds no intersection and returns the closest points, the state is changed to ShallowContact.
            //If the closest point system finds intersection of the core shapes, then the state is changed to DeepContact, and MPR is run to determine contact information.
            //The system tries to escape from deep contact to shallow contact, and from shallow contact to separated whenever possible.

            //Here's the state flow:
            //On Separated: BooleanGJK
            //  -Intersecting -> Go to ShallowContact.
            //  -Nonintersecting -> Do nothing.
            //On ShallowContact: ClosestPointsGJK
            //  -Intersecting -> Go to DeepContact.
            //  -Nonintersecting: Go to Separated (without test) if squared distance > margin squared, otherwise use closest points to make contact.
            //On DeepContact: MPR
            //  -Intersecting -> Go to ShallowContact if penetration depth < margin
            //  -Nonintersecting -> This case is rare, but not impossible.  Go to Separated (without test).

            previousState = state;
            switch (state)
            {
                case CollisionState.Separated:
                    if (GJKToolbox.AreShapesIntersecting(collidableA.Shape, collidableB.Shape, ref collidableA.worldTransform, ref collidableB.worldTransform, ref localSeparatingAxis))
                    {
                        state = CollisionState.ShallowContact;
                        return DoShallowContact(out contact);
                    }
                    contact = new ContactData();
                    return false;
                case CollisionState.ShallowContact:
                    return DoShallowContact(out contact);
                case CollisionState.DeepContact:
                    return DoDeepContact(out contact);
            }

            contact = new ContactData();
            return false;
        }

        private bool DoShallowContact(out ContactData contact)
        {
            Vector3 closestA, closestB;

            //RigidTransform transform = RigidTransform.Identity;
            //Vector3 closestAnew, closestBnew;
            //CachedSimplex cachedTest = cachedSimplex;
            //bool intersecting = GJKToolbox.GetClosestPoints(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform, ref cachedTest, out closestAnew, out closestBnew);

            ////bool otherIntersecting = OldGJKVerifier.GetClosestPointsBetweenObjects(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform, 0, 0, out closestA, out closestB);
            //bool otherIntersecting = GJKToolbox.GetClosestPoints(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform, out closestA, out closestB);

            //Vector3 closestAold, closestBold;
            //bool oldIntersecting = OldGJKVerifier.GetClosestPointsBetweenObjects(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform, 0, 0, out closestAold, out closestBold);

            //if (otherIntersecting != intersecting || (!otherIntersecting && !intersecting &&
            //    Vector3.DistanceSquared(closestAnew, closestBnew) - Vector3.DistanceSquared(closestA, closestB) > .0001f &&
            //    (Vector3.DistanceSquared(closestA, closestAnew) > .0001f ||
            //    Vector3.DistanceSquared(closestB, closestBnew) > .0001f)))// ||
            //    //Math.Abs(Vector3.Dot(closestB - closestA, closestBnew - closestAnew) - Vector3.Dot(closestB - closestA, closestB - closestA)) > Toolbox.Epsilon)))
            //    Debug.WriteLine("Break.");

            //Vector3 sub;
            //Vector3.Subtract(ref closestA, ref closestB, out sub);
            //if (sub.LengthSquared() < Toolbox.Epsilon)

            bool intersecting;
            if (UseSimplexCaching)
                intersecting = GJKToolbox.GetClosestPoints(collidableA.Shape, collidableB.Shape, ref collidableA.worldTransform, ref collidableB.worldTransform, ref cachedSimplex, out closestA, out closestB);
            else
            {
                //The initialization of the pair creates a pretty decent simplex to start from.
                //Just don't try to update it.
                CachedSimplex preInitializedSimplex = cachedSimplex;
                intersecting = GJKToolbox.GetClosestPoints(collidableA.Shape, collidableB.Shape, ref collidableA.worldTransform, ref collidableB.worldTransform, ref preInitializedSimplex, out closestA, out closestB);
            }

            Vector3 displacement;
            Vector3.Subtract(ref closestB, ref closestA, out displacement);
            if (intersecting)
            //if (OldGJKVerifier.GetClosestPointsBetweenObjects(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform, 0, 0, out closestA, out closestB))
            {
                state = CollisionState.DeepContact;
                return DoDeepContact(out contact);
            }

            localDirection = displacement; //Use this as the direction for future deep contacts.
            float distanceSquared = displacement.LengthSquared();
            float margin = collidableA.Shape.collisionMargin + collidableB.Shape.collisionMargin;


            if (distanceSquared < margin * margin)
            {
                //Generate a contact.
                contact = new ContactData();
                //Displacement is from A to B.  point = A + t * AB, where t = marginA / margin.
                if (margin > Toolbox.Epsilon) //Avoid a NaN!
                    Vector3.Multiply(ref displacement, collidableA.Shape.collisionMargin / margin, out contact.Position); //t * AB
                else
                    contact.Position = new Vector3();

                Vector3.Add(ref closestA, ref contact.Position, out contact.Position); //A + t * AB.


                contact.Normal = displacement;
                float distance = (float)Math.Sqrt(distanceSquared);
                Vector3.Divide(ref contact.Normal, distance, out contact.Normal);
                contact.PenetrationDepth = margin - distance;
                return true;

            }
            //Too shallow to make a contact- move back to separation.
            state = CollisionState.Separated;
            contact = new ContactData();
            return false;
        }

        Vector3 localDirection;
        private bool DoDeepContact(out ContactData contact)
        {
           
            #region Informed search
            if (previousState == CollisionState.Separated) //If it was shallow before, then its closest points will be used to find the normal.
            {
                //It's overlapping! Find the relative velocity at the point relative to the two objects.  The point is still in local space!
                //Vector3 velocityA;
                //Vector3.Cross(ref contact.Position, ref collidableA.entity.angularVelocity, out velocityA);
                //Vector3.Add(ref velocityA, ref collidableA.entity.linearVelocity, out velocityA);
                //Vector3 velocityB;
                //Vector3.Subtract(ref contact.Position, ref localTransformB.Position, out velocityB);
                //Vector3.Cross(ref velocityB, ref collidableB.entity.angularVelocity, out velocityB);
                //Vector3.Add(ref velocityB, ref collidableB.entity.linearVelocity, out velocityB);
                ////The velocity is negated because the direction so point backwards along the velocity.
                //Vector3.Subtract(ref velocityA, ref velocityB, out localDirection);

                //The above takes into account angular velocity, but linear velocity alone is a lot more stable and does the job just fine.
                if (collidableA.entity != null && collidableB.entity != null)
                    Vector3.Subtract(ref collidableA.entity.linearVelocity, ref collidableB.entity.linearVelocity, out localDirection);
                else
                    localDirection = localSeparatingAxis;

                if (localDirection.LengthSquared() < Toolbox.Epsilon)
                {
                    localDirection = Vector3.Up;
                }

            }
            if (MPRToolbox.GetContact(collidableA.Shape, collidableB.Shape, ref collidableA.worldTransform, ref collidableB.worldTransform, ref localDirection, out contact))
            {
                if (contact.PenetrationDepth < collidableA.Shape.collisionMargin + collidableB.Shape.collisionMargin)
                    state = CollisionState.ShallowContact;
                return true;
            }
            //This is rare, but could happen.
            state = CollisionState.Separated;
            return false;

            //if (MPRTesting.GetLocalOverlapPosition(collidableA.Shape, collidableB.Shape, ref localTransformB, out contact.Position))
            //{


            //    //First, try to use the heuristically found direction.  This comes from either the GJK shallow contact separating axis or from the relative velocity.
            //    Vector3 rayCastDirection;
            //    float lengthSquared = localDirection.LengthSquared();
            //    if (lengthSquared > Toolbox.Epsilon)
            //    {
            //        Vector3.Divide(ref localDirection, (float)Math.Sqrt(lengthSquared), out rayCastDirection);// (Vector3.Normalize(localDirection) + Vector3.Normalize(collidableB.worldTransform.Position - collidableA.worldTransform.Position)) / 2;
            //        MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref rayCastDirection, out contact.PenetrationDepth, out contact.Normal);
            //    }
            //    else
            //    {
            //        contact.PenetrationDepth = float.MaxValue;
            //        contact.Normal = Toolbox.UpVector;
            //    }
            //    //Try the offset between the origins as a second option.  Sometimes this is a better choice than the relative velocity.
            //    //TODO: Could use the position-finding MPR iteration to find the A-B direction hit by continuing even after the origin has been found (optimization).
            //    Vector3 normalCandidate;
            //    float depthCandidate;
            //    lengthSquared = localTransformB.Position.LengthSquared();
            //    if (lengthSquared > Toolbox.Epsilon)
            //    {
            //        Vector3.Divide(ref localTransformB.Position, (float)Math.Sqrt(lengthSquared), out rayCastDirection);
            //        MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref rayCastDirection, out depthCandidate, out normalCandidate);
            //        if (depthCandidate < contact.PenetrationDepth)
            //        {
            //            contact.Normal = normalCandidate;
            //        }
            //    }


            //    //Correct the penetration depth.
            //    MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref contact.Normal, out contact.PenetrationDepth, out rayCastDirection);


            //    ////The local casting can optionally continue.  Eventually, it will converge to the local minimum.
            //    //while (true)
            //    //{
            //    //    MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref contact.Normal, out depthCandidate, out normalCandidate);
            //    //    if (contact.PenetrationDepth - depthCandidate <= Toolbox.BigEpsilon)
            //    //        break;

            //    //    contact.PenetrationDepth = depthCandidate;
            //    //    contact.Normal = normalCandidate;
            //    //}

            //    contact.Id = -1;
            //    //we're still in local space! transform it all back.
            //    Matrix3X3 orientation;
            //    Matrix3X3.CreateFromQuaternion(ref collidableA.worldTransform.Orientation, out orientation);
            //    Matrix3X3.Transform(ref contact.Normal, ref orientation, out contact.Normal);
            //    //Vector3.Negate(ref contact.Normal, out contact.Normal);
            //    Matrix3X3.Transform(ref contact.Position, ref orientation, out contact.Position);
            //    Vector3.Add(ref contact.Position, ref collidableA.worldTransform.Position, out contact.Position);
            //    if (contact.PenetrationDepth < collidableA.Shape.collisionMargin + collidableB.Shape.collisionMargin)
            //        state = CollisionState.ShallowContact;
            //    return true;
            //}

            ////This is rare, but could happen.
            //state = CollisionState.Separated;
            //contact = new ContactData();
            //return false;
            #endregion

            #region Testing
            //RigidTransform localTransformB;
            //MinkowskiToolbox.GetLocalTransform(ref collidableA.worldTransform, ref collidableB.worldTransform, out localTransformB); 
            //contact.Id = -1;
            //if (MPRTesting.GetLocalOverlapPosition(collidableA.Shape, collidableB.Shape, ref localTransformB, out contact.Position))
            //{
            //    Vector3 rayCastDirection = localTransformB.Position;
            //    MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref rayCastDirection, out contact.PenetrationDepth, out contact.Normal);
            //    MPRTesting.LocalSurfaceCast(collidableA.Shape, collidableB.Shape, ref localTransformB, ref contact.Normal, out contact.PenetrationDepth, out rayCastDirection);
            //    RigidTransform.Transform(ref contact.Position, ref collidableA.worldTransform, out contact.Position);
            //    Vector3.Transform(ref contact.Normal, ref collidableA.worldTransform.Orientation, out contact.Normal);
            //    return true;
            //}
            //contact.Normal = new Vector3();
            //contact.PenetrationDepth = 0;
            //return false;
            #endregion

            #region v0.15.2 and before
            //if (MPRToolbox.AreObjectsColliding(collidableA.Shape, collidableB.Shape, ref collidableA.worldTransform, ref collidableB.worldTransform, out contact))
            //{
            //    if (contact.PenetrationDepth < collidableA.Shape.collisionMargin + collidableB.Shape.collisionMargin)
            //        state = CollisionState.ShallowContact; //If it's emerged from the deep contact, we can go back to using the preferred GJK method.
            //    return true;
            //}
            ////This is rare, but could happen.
            //state = CollisionState.Separated;
            //return false;
            #endregion

        }

        ///<summary>
        /// Initializes the pair tester.
        ///</summary>
        ///<param name="shapeA">First shape in the pair.</param>
        ///<param name="shapeB">Second shape in the pair.</param>
        public void Initialize(Collidable shapeA, Collidable shapeB)
        {
            collidableA = (ConvexCollidable)shapeA;
            collidableB = (ConvexCollidable)shapeB;
            cachedSimplex = new CachedSimplex { State = SimplexState.Point };// new CachedSimplex(informationA.Shape, informationB.Shape, ref informationA.worldTransform, ref informationB.worldTransform);
        }

        ///<summary>
        /// Cleans up the pair tester.
        ///</summary>
        public void CleanUp()
        {
            state = CollisionState.Separated;
            previousState = CollisionState.Separated;
            cachedSimplex = new CachedSimplex();
            localSeparatingAxis = new Vector3();
            collidableA = null;
            collidableB = null;
        }


        enum CollisionState
        {
            Separated,
            ShallowContact,
            DeepContact
        }


    }

}
