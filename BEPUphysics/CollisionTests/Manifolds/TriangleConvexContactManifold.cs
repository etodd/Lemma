using System;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.Settings;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contacts between a triangle and convex.
    ///</summary>
    public class TriangleConvexContactManifold : ContactManifold
    {
        RawValueList<ContactSupplementData> supplementData = new RawValueList<ContactSupplementData>(4);
        TriangleConvexPairTester pairTester;
        TriangleShape localTriangleShape = new TriangleShape();

        ///<summary>
        /// Gets the pair tester used by the manifold.
        ///</summary>
        public TriangleConvexPairTester PairTester
        {
            get
            {
                return pairTester;
            }

        }

        protected ConvexCollidable convex;
        protected ConvexCollidable<TriangleShape> triangle;

        ///<summary>
        /// Gets the convex associated with the pair.
        ///</summary>
        public ConvexCollidable Convex
        {
            get
            {
                return convex;
            }
        }

        ///<summary>
        /// Gets the triangle associated with the pair.
        ///</summary>
        public ConvexCollidable<TriangleShape> Triangle
        {
            get
            {
                return triangle;
            }
        }

        ///<summary>
        /// Constructs a new manifold.
        ///</summary>
        public TriangleConvexContactManifold()
        {
            contacts = new RawList<Contact>(4);
            unusedContacts = new UnsafeResourcePool<Contact>(4);
            contactIndicesToRemove = new RawList<int>(4);
            pairTester = new TriangleConvexPairTester();
        }

        public override void Update(float dt)
        {
            //First, refresh all existing contacts.  This is an incremental manifold.
            ContactRefresher.ContactRefresh(contacts, supplementData, ref convex.worldTransform, ref triangle.worldTransform, contactIndicesToRemove);
            RemoveQueuedContacts();


            //Compute the local triangle vertices.
            //TODO: this could be quicker and cleaner.
            localTriangleShape.collisionMargin = triangle.Shape.collisionMargin;
            localTriangleShape.sidedness = triangle.Shape.sidedness;
            Matrix3X3 orientation;
            Matrix3X3.CreateFromQuaternion(ref triangle.worldTransform.Orientation, out orientation);
            Matrix3X3.Transform(ref triangle.Shape.vA, ref orientation, out localTriangleShape.vA);
            Matrix3X3.Transform(ref triangle.Shape.vB, ref orientation, out localTriangleShape.vB);
            Matrix3X3.Transform(ref triangle.Shape.vC, ref orientation, out localTriangleShape.vC);
            Vector3.Add(ref localTriangleShape.vA, ref triangle.worldTransform.Position, out localTriangleShape.vA);
            Vector3.Add(ref localTriangleShape.vB, ref triangle.worldTransform.Position, out localTriangleShape.vB);
            Vector3.Add(ref localTriangleShape.vC, ref triangle.worldTransform.Position, out localTriangleShape.vC);

            Vector3.Subtract(ref localTriangleShape.vA, ref convex.worldTransform.Position, out localTriangleShape.vA);
            Vector3.Subtract(ref localTriangleShape.vB, ref convex.worldTransform.Position, out localTriangleShape.vB);
            Vector3.Subtract(ref localTriangleShape.vC, ref convex.worldTransform.Position, out localTriangleShape.vC);
            Matrix3X3.CreateFromQuaternion(ref convex.worldTransform.Orientation, out orientation);
            Matrix3X3.TransformTranspose(ref localTriangleShape.vA, ref orientation, out localTriangleShape.vA);
            Matrix3X3.TransformTranspose(ref localTriangleShape.vB, ref orientation, out localTriangleShape.vB);
            Matrix3X3.TransformTranspose(ref localTriangleShape.vC, ref orientation, out localTriangleShape.vC);

            //Now, generate a contact between the two shapes.
            ContactData contact;
            TinyStructList<ContactData> contactList;
            if (pairTester.GenerateContactCandidate(out contactList))
            {
                for (int i = 0; i < contactList.count; i++)
                {
                    contactList.Get(i, out contact);
                    //Put the contact into world space.
                    Matrix3X3.Transform(ref contact.Position, ref orientation, out contact.Position);
                    Vector3.Add(ref contact.Position, ref convex.worldTransform.Position, out contact.Position);
                    Matrix3X3.Transform(ref contact.Normal, ref orientation, out contact.Normal);
                    //Check if the contact is unique before proceeding.
                    if (IsContactUnique(ref contact))
                    {
                        //Check if adding the new contact would overflow the manifold.
                        if (contacts.count == 4)
                        {
                            //Adding that contact would overflow the manifold.  Reduce to the best subset.
                            bool addCandidate;
                            ContactReducer.ReduceContacts(contacts, ref contact, contactIndicesToRemove, out addCandidate);
                            RemoveQueuedContacts();
                            if (addCandidate)
                                Add(ref contact);
                        }
                        else
                        {
                            //Won't overflow the manifold, so just toss it in PROVIDED that it isn't too close to something else.
                            Add(ref contact);
                        }
                    }
                }
            }
            else
            {
                //Clear out the contacts, it's separated.
                for (int i = contacts.count - 1; i >= 0; i--)
                    Remove(i);
            }

        }

        protected override void Add(ref ContactData contactCandidate)
        {
            ContactSupplementData supplement;
            supplement.BasePenetrationDepth = contactCandidate.PenetrationDepth;
            //The closest point method computes the local space versions before transforming to world... consider cutting out the middle man
            RigidTransform.TransformByInverse(ref contactCandidate.Position, ref convex.worldTransform, out supplement.LocalOffsetA);
            RigidTransform.TransformByInverse(ref contactCandidate.Position, ref triangle.worldTransform, out supplement.LocalOffsetB);
            supplementData.Add(ref supplement);
            base.Add(ref contactCandidate);
        }

        protected override void Remove(int contactIndex)
        {
            supplementData.RemoveAt(contactIndex);
            base.Remove(contactIndex);
        }


        private bool IsContactUnique(ref ContactData contactCandidate)
        {

            float distanceSquared;
            for (int i = 0; i < contacts.count; i++)
            {
                Vector3.DistanceSquared(ref contacts.Elements[i].Position, ref contactCandidate.Position, out distanceSquared);
                if (distanceSquared < CollisionDetectionSettings.ContactMinimumSeparationDistanceSquared)
                {
                    //Update the existing 'redundant' contact with the new information.
                    //This works out because the new contact is the deepest contact according to the previous collision detection iteration.
                    contacts.Elements[i].Normal = contactCandidate.Normal;
                    contacts.Elements[i].Position = contactCandidate.Position;
                    contacts.Elements[i].PenetrationDepth = contactCandidate.PenetrationDepth;
                    supplementData.Elements[i].BasePenetrationDepth = contactCandidate.PenetrationDepth;
                    RigidTransform.TransformByInverse(ref contactCandidate.Position, ref convex.worldTransform, out supplementData.Elements[i].LocalOffsetA);
                    RigidTransform.TransformByInverse(ref contactCandidate.Position, ref triangle.worldTransform, out supplementData.Elements[i].LocalOffsetB);
                    return false;
                }
            }
            return true;
        }

        public override void Initialize(Collidable newCollidableA, Collidable newCollidableB)
        {
            convex = newCollidableA as ConvexCollidable;
            triangle = newCollidableB as ConvexCollidable<TriangleShape>;


            if (convex == null || triangle == null)
            {
                convex = newCollidableB as ConvexCollidable;
                triangle = newCollidableA as ConvexCollidable<TriangleShape>;
                if (convex == null || triangle == null)
                    throw new Exception("Inappropriate types used to initialize contact manifold.");
            }

            pairTester.Initialize(convex.Shape, localTriangleShape);
        }

        public override void CleanUp()
        {
            supplementData.Clear();
            contacts.Clear();
            convex = null;
            triangle = null;
            pairTester.CleanUp();
            base.CleanUp();
        }


    }
}
