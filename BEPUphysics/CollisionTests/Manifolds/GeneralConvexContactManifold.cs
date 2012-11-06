using System;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.Settings;
using BEPUphysics.ResourceManagement;
using System.Diagnostics;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contacts for two convex collidables.
    ///</summary>
    public class GeneralConvexContactManifold : ContactManifold
    {
        RawValueList<ContactSupplementData> supplementData = new RawValueList<ContactSupplementData>(4);
        GeneralConvexPairTester pairTester;

        ///<summary>
        /// Gets the pair tester used by the manifold to do testing.
        ///</summary>
        public GeneralConvexPairTester PairTester
        {
            get
            {
                return pairTester;
            }

        }

        protected ConvexCollidable collidableA, collidableB;

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

        /// <summary>
        /// Gets the second collidable in the pair.
        /// </summary>
        public ConvexCollidable CollidableB
        {
            get
            {
                return collidableB;
            }
        }

        ///<summary>
        /// Constructs a new convex-convex manifold.
        ///</summary>
        public GeneralConvexContactManifold()
        {
            contacts = new RawList<Contact>(4);
            unusedContacts = new UnsafeResourcePool<Contact>(4);
            contactIndicesToRemove = new RawList<int>(4);
            pairTester = new GeneralConvexPairTester();
        }

        ///<summary>
        /// Updates the manifold.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {
            //First, refresh all existing contacts.  This is an incremental manifold.
            ContactRefresher.ContactRefresh(contacts, supplementData, ref collidableA.worldTransform, ref collidableB.worldTransform, contactIndicesToRemove);
            RemoveQueuedContacts();


            //Now, generate a contact between the two shapes.
            ContactData contact;
            if (pairTester.GenerateContactCandidate(out contact))
            {

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
                        //Won't overflow the manifold, so just toss it in.
                        Add(ref contact);
                    }
                }
            }
            else
            {
                //No collision, clean out the manifold.
                for (int i = contacts.count - 1; i >= 0; i--)
                {
                    Remove(i);
                }
            }


        }

        protected override void Add(ref ContactData contactCandidate)
        {
            ContactSupplementData supplement;
            supplement.BasePenetrationDepth = contactCandidate.PenetrationDepth;
            //The closest point method computes the local space versions before transforming to world... consider cutting out the middle man
            RigidTransform.TransformByInverse(ref contactCandidate.Position, ref collidableA.worldTransform, out supplement.LocalOffsetA);
            RigidTransform.TransformByInverse(ref contactCandidate.Position, ref collidableB.worldTransform, out supplement.LocalOffsetB);
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
            for (int i = 0; i < contacts.count; i++)
            {
                float distanceSquared;
                Vector3.DistanceSquared(ref contacts.Elements[i].Position, ref contactCandidate.Position, out distanceSquared);
                if (distanceSquared < CollisionDetectionSettings.ContactMinimumSeparationDistanceSquared)
                {
                    //Update the existing 'redundant' contact with the new information.
                    //This works out because the new contact is the deepest contact according to the previous collision detection iteration.
                    contacts.Elements[i].Normal = contactCandidate.Normal;
                    contacts.Elements[i].Position = contactCandidate.Position;
                    contacts.Elements[i].PenetrationDepth = contactCandidate.PenetrationDepth;
                    supplementData.Elements[i].BasePenetrationDepth = contactCandidate.PenetrationDepth;
                    RigidTransform.TransformByInverse(ref contactCandidate.Position, ref collidableA.worldTransform, out supplementData.Elements[i].LocalOffsetA);
                    RigidTransform.TransformByInverse(ref contactCandidate.Position, ref collidableB.worldTransform, out supplementData.Elements[i].LocalOffsetB);
                    return false;
                }
            }
            return true;
        }

        ///<summary>
        /// Initializes the manifold.
        ///</summary>
        ///<param name="newCollidableA">First collidable.</param>
        ///<param name="newCollidableB">Second collidable.</param>
        public override void Initialize(Collidable newCollidableA, Collidable newCollidableB)
        {
            collidableA = newCollidableA as ConvexCollidable;
            collidableB = newCollidableB as ConvexCollidable;
            pairTester.Initialize(newCollidableA, newCollidableB);


            if (collidableA == null || collidableB == null)
            {
                throw new Exception("Inappropriate types used to initialize pair tester.");
            }
        }

        ///<summary>
        /// Cleans up the manifold.
        ///</summary>
        public override void CleanUp()
        {
            supplementData.Clear();
            contacts.Clear();
            collidableA = null;
            collidableB = null;
            pairTester.CleanUp();
            base.CleanUp();
        }

    }
}
