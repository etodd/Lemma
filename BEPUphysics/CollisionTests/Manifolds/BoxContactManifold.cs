using System;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using System.Diagnostics;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contact data between two boxes.
    ///</summary>
    public class BoxContactManifold : ContactManifold
    {
        protected ConvexCollidable<BoxShape> boxA, boxB;

        ///<summary>
        /// Gets the first collidable in the pair.
        ///</summary>
        public ConvexCollidable<BoxShape> CollidableA
        {
            get
            {
                return boxA;
            }
        }

        /// <summary>
        /// Gets the second collidable in the pair.
        /// </summary>
        public ConvexCollidable<BoxShape> CollidableB
        {
            get
            {
                return boxB;
            }
        }

        ///<summary>
        /// Constructs a new manifold.
        ///</summary>
        public BoxContactManifold()
        {
            contacts = new RawList<Contact>(4);
            unusedContacts = new UnsafeResourcePool<Contact>(4);
            contactIndicesToRemove = new RawList<int>(4);
        }

#if ALLOWUNSAFE
        ///<summary>
        /// Updates the manifold.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {

            //Now, generate a contact between the two shapes.
            float distance;
            Vector3 axis;
            BoxContactDataCache manifold;
            if (BoxBoxCollider.AreBoxesColliding(boxA.Shape, boxB.Shape, ref boxA.worldTransform, ref boxB.worldTransform, out distance, out axis, out manifold))
            {
                unsafe
                {
                    BoxContactData* manifoldPointer = &manifold.D1;
                    Vector3.Negate(ref axis, out axis);
                    var toRemove = new TinyList<int>();
                    for (int i = 0; i < contacts.count; i++)
                    {
                        bool found = false;
                        for (int j = manifold.Count - 1; j >= 0; j--)
                        {
                            if (contacts.Elements[i].Id == manifoldPointer[j].Id)
                            {
                                found = true;
                                //Update contact...
                                contacts.Elements[i].Position = manifoldPointer[j].Position;
                                contacts.Elements[i].PenetrationDepth = -manifoldPointer[j].Depth;
                                contacts.Elements[i].Normal = axis;
                                //Remove manifold entry
                                manifold.RemoveAt(j);
                                break;
                            }
                        }
                        if (!found)
                        {//No match found
                            toRemove.Add(i);
                        }
                    }


                    //toRemove is sorted by increasing index.  Go backwards along it so that the indices are valid all the way through.
                    for (int i = toRemove.Count - 1; i >= 0; i--)
                        Remove(toRemove[i]);

                    //Add new contacts.
                    for (int i = 0; i < manifold.Count; i++)
                    {
                        var newContact = new ContactData
                                             {
                                                 Position = manifoldPointer[i].Position,
                                                 PenetrationDepth = -manifoldPointer[i].Depth,
                                                 Normal = axis,
                                                 Id = manifoldPointer[i].Id
                                             };

                        Add(ref newContact);
                    }
                }
            }
            else
            {
                //Not colliding, so get rid of it.
                for (int i = contacts.count - 1; i >= 0; i--)
                {
                    Remove(i);
                }
            }
        }
#else
        public override void Update(float dt)
        {

            //Now, generate a contact between the two shapes.
            float distance;
            Vector3 axis;
            var manifold = new TinyStructList<BoxContactData>();
            if (BoxBoxCollider.AreBoxesColliding(boxA.Shape, boxB.Shape, ref boxA.worldTransform, ref boxB.worldTransform, out distance, out axis, out manifold))
            {
                Vector3.Negate(ref axis, out axis);
                TinyList<int> toRemove = new TinyList<int>();
                BoxContactData data;
                for (int i = 0; i < contacts.count; i++)
                {
                    bool found = false;
                    for (int j = manifold.Count - 1; j >= 0; j--)
                    {
                        manifold.Get(j, out data);
                        if (contacts.Elements[i].Id == data.Id)
                        {
                            found = true;
                            //Update contact...
                            contacts.Elements[i].Position = data.Position;
                            contacts.Elements[i].PenetrationDepth = -data.Depth;
                            contacts.Elements[i].Normal = axis;
                            //Remove manifold entry
                            manifold.RemoveAt(j);
                            break;
                        }
                    }
                    if (!found)
                    {//No match found
                        toRemove.Add(i);
                    }
                }

                ////Go through the indices to remove.
                ////For each one, replace the removal index with a contact in the new manifold.
                //int removalIndex;
                //for (removalIndex = toRemove.count - 1; removalIndex >= 0 && manifold.count > 0; removalIndex--)
                //{
                //    int indexToReplace = toRemove[removalIndex];
                //    toRemove.RemoveAt(removalIndex);
                //    manifold.Get(manifold.count - 1, out data);
                //    //Update contact...
                //    contacts.Elements[indexToReplace].Position = data.Position;
                //    contacts.Elements[indexToReplace].PenetrationDepth = -data.Depth;
                //    contacts.Elements[indexToReplace].Normal = axis;
                //    contacts.Elements[indexToReplace].Id = data.Id;
                //    //Remove manifold entry
                //    manifold.RemoveAt(manifold.count - 1);

                //}

                //Alright, we ran out of contacts to replace (if, in fact, toRemove isn't empty now).  Just remove the remainder.
                //toRemove is sorted by increasing index.  Go backwards along it so that the indices are valid all the way through.
                for (int i = toRemove.Count - 1; i >= 0; i--)
                    Remove(toRemove[i]);

                //Add new contacts.
                for (int i = 0; i < manifold.Count; i++)
                {
                    manifold.Get(i, out data);
                    ContactData newContact = new ContactData();
                    newContact.Position = data.Position;
                    newContact.PenetrationDepth = -data.Depth;
                    newContact.Normal = axis;
                    newContact.Id = data.Id;

                    Add(ref newContact);
                }
            }
            else
            {
                //Not colliding, so get rid of it.
                for (int i = contacts.count - 1; i >= 0; i--)
                {
                    Remove(i);
                }
            }
        }
#endif


        ///<summary>
        /// Initializes the manifold.
        ///</summary>
        ///<param name="newCollidableA">First collidable.</param>
        ///<param name="newCollidableB">Second collidable.</param>
        ///<exception cref="Exception">Thrown when the collidables being used are not of the proper type.</exception>
        public override void Initialize(Collidable newCollidableA, Collidable newCollidableB)
        {
            boxA = (ConvexCollidable<BoxShape>)newCollidableA;
            boxB = (ConvexCollidable<BoxShape>)newCollidableB;


            if (boxA == null || boxB == null)
            {
                throw new Exception("Inappropriate types used to initialize pair tester.");
            }
        }

        ///<summary>
        /// Cleans up the manifold.
        ///</summary>
        public override void CleanUp()
        {
            contacts.Clear();
            boxA = null;
            boxB = null;
            base.CleanUp();
        }


    }
}
