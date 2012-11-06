using System;
using BEPUphysics.Collidables;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using System.Collections.ObjectModel;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Superclass of manifolds which manage persistent contacts over multiple frames.
    ///</summary>
    public abstract class ContactManifold
    {

        protected RawList<int> contactIndicesToRemove;
        protected internal RawList<Contact> contacts;

        ///<summary>
        /// Gets the contacts in the manifold.
        ///</summary>
        public ReadOnlyList<Contact> Contacts
        {
            get
            {
                return new ReadOnlyList<Contact>(contacts);
            }
        }
        protected UnsafeResourcePool<Contact> unusedContacts;


        protected void RemoveQueuedContacts()
        {
            //TOREMOVE MUST BE SORTED LEAST TO GREATEST INDEX.
            for (int i = contactIndicesToRemove.count - 1; i >= 0; i--)
            {
                Remove(contactIndicesToRemove.Elements[i]);
            }
            contactIndicesToRemove.Clear();
        }

        protected virtual void Remove(int contactIndex)
        {
            Contact removing = contacts.Elements[contactIndex];
            contacts.FastRemoveAt(contactIndex);
            OnRemoved(removing);
            unusedContacts.GiveBack(removing);
        }

        protected virtual void Add(ref ContactData contactCandidate)
        {
            Contact adding = unusedContacts.Take();
            adding.Setup(ref contactCandidate);
            contacts.Add(adding);
            OnAdded(adding);
        }


        ///<summary>
        /// Fires when a contact is added.
        ///</summary>
        public event Action<Contact> ContactAdded;
        ///<summary>
        /// Fires when a contact is removed.
        ///</summary>
        public event Action<Contact> ContactRemoved;

        protected void OnAdded(Contact contact)
        {
            if (ContactAdded != null)
                ContactAdded(contact);
        }

        protected void OnRemoved(Contact contact)
        {
            if (ContactRemoved != null)
                ContactRemoved(contact);
        }

        ///<summary>
        /// Initializes the manifold.
        ///</summary>
        ///<param name="newCollidableA">First collidable.</param>
        ///<param name="newCollidableB">Second collidable.</param>
        public abstract void Initialize(Collidable newCollidableA, Collidable newCollidableB);


        ///<summary>
        /// Cleans up the manifold.
        ///</summary>
        public virtual void CleanUp()
        {
        }

        ///<summary>
        /// Updates the manifold.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public abstract void Update(float dt);

        /// <summary>
        /// Clears the contacts associated with this manifold.
        /// </summary>
        public virtual void ClearContacts()
        {
            for (int i = contacts.count - 1; i >= 0; i--)
            {
                Remove(i);
            }
        }

    }

}
