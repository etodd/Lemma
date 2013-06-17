using BEPUutilities;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUutilities.DataStructures;

namespace BEPUphysics.BroadPhaseEntries.Events
{

    ///<summary>
    /// Event manager for collidables (things which can create contact points).
    ///</summary>
    ///<typeparam name="T">Some Collidable subclass.</typeparam>
    public class ContactEventManager<T> : EntryEventManager<T>, IContactEventTriggerer where T : Collidable
    {

        #region Events

        /// <summary>
        /// Fires when the entity stops touching another entity.
        /// </summary>
        public event CollisionEndedEventHandler<T> CollisionEnded
        {
            add
            {
                InternalCollisionEnded += value;
                AddToEventfuls();
            }
            remove
            {
                InternalCollisionEnded -= value;
                VerifyEventStatus();
            }
        }

        /// <summary>
        /// Fires when the entity stops touching another entity.
        /// Unlike the CollisionEnded event, this event will run inline instead of at the end of the space's update.
        /// Some operations are unsupported while the engine is updating, and be especially careful if internal multithreading is enabled.
        /// </summary>
        public event CollisionEndingEventHandler<T> CollisionEnding;

        /// <summary>
        /// Fires when a pair is updated and there are contact points in it.
        /// </summary>
        public event PairTouchedEventHandler<T> PairTouched
        {
            add
            {
                InternalPairTouched += value;
                AddToEventfuls();
            }
            remove
            {
                InternalPairTouched -= value;
                VerifyEventStatus();
            }
        }

        /// <summary>
        /// Fires when a pair is updated and there are contact points in it.
        /// Unlike the PairTouched event, this event will run inline instead of at the end of the space's update.
        /// Some operations are unsupported while the engine is updating, and be especially careful if internal multithreading is enabled.
        /// </summary>
        public event PairTouchingEventHandler<T> PairTouching;

        /// <summary>
        /// Fires when this entity gains a contact point with another entity.
        /// </summary>
        public event ContactCreatedEventHandler<T> ContactCreated
        {
            add
            {
                InternalContactCreated += value;
                AddToEventfuls();
            }
            remove
            {
                InternalContactCreated -= value;
                VerifyEventStatus();
            }
        }

        /// <summary>
        /// Fires when this entity loses a contact point with another entity.
        /// </summary>
        public event ContactRemovedEventHandler<T> ContactRemoved
        {
            add
            {
                InternalContactRemoved += value;
                AddToEventfuls();
            }
            remove
            {
                InternalContactRemoved -= value;
                VerifyEventStatus();
            }
        }

        /// <summary>
        /// Fires when this entity gains a contact point with another entity.
        /// Unlike the ContactCreated event, this event will run inline instead of at the end of the space's update.
        /// Some operations are unsupported while the engine is updating, and be especially careful if internal multithreading is enabled.
        /// </summary>
        public event CreatingContactEventHandler<T> CreatingContact;

        /// <summary>
        /// Fires when a collision first occurs.
        /// Unlike the InitialCollisionDetected event, this event will run inline instead of at the end of the space's update.
        /// Some operations are unsupported while the engine is updating, and be especially careful if internal multithreading is enabled.
        /// </summary>
        public event DetectingInitialCollisionEventHandler<T> DetectingInitialCollision;

        /// <summary>
        /// Fires when a collision first occurs.
        /// </summary>
        public event InitialCollisionDetectedEventHandler<T> InitialCollisionDetected
        {
            add
            {
                InternalInitialCollisionDetected += value;
                AddToEventfuls();
            }
            remove
            {
                InternalInitialCollisionDetected -= value;
                VerifyEventStatus();
            }
        }

        /// <summary>
        /// Fires when this entity loses a contact point with another entity.
        /// Unlike the ContactRemoved event, this event will run inline instead of at the end of the space's update.
        /// Some operations are unsupported while the engine is updating, and be especially careful if internal multithreading is enabled.
        /// </summary>
        public event RemovingContactEventHandler<T> RemovingContact;

        private event CollisionEndedEventHandler<T> InternalCollisionEnded;
        private event PairTouchedEventHandler<T> InternalPairTouched;
        private event ContactCreatedEventHandler<T> InternalContactCreated;
        private event ContactRemovedEventHandler<T> InternalContactRemoved;
        private event InitialCollisionDetectedEventHandler<T> InternalInitialCollisionDetected;

        #endregion

        #region Supporting members

        protected override bool EventsAreInactive()
        {
            return InternalCollisionEnded == null &&
                   InternalPairTouched == null &&
                   InternalContactCreated == null &&
                   InternalContactRemoved == null &&
                   InternalInitialCollisionDetected == null &&
                   base.EventsAreInactive();
        }

        readonly ConcurrentDeque<EventStorageContactCreated> eventStorageContactCreated = new ConcurrentDeque<EventStorageContactCreated>(0);
        readonly ConcurrentDeque<EventStorageInitialCollisionDetected> eventStorageInitialCollisionDetected = new ConcurrentDeque<EventStorageInitialCollisionDetected>(0);
        readonly ConcurrentDeque<EventStorageContactRemoved> eventStorageContactRemoved = new ConcurrentDeque<EventStorageContactRemoved>(0);
        readonly ConcurrentDeque<EventStorageCollisionEnded> eventStorageCollisionEnded = new ConcurrentDeque<EventStorageCollisionEnded>(0);
        readonly ConcurrentDeque<EventStoragePairTouched> eventStoragePairTouched = new ConcurrentDeque<EventStoragePairTouched>(0);

        protected override void DispatchEvents()
        {
            //Note: Deferred event creation should be performed sequentially with dispatching.
            //This means a event creation from this creator cannot occur ASYNCHRONOUSLY while DispatchEvents is running.

            //Note: If the deferred event handler is removed during the execution of the engine, the handler may be null.
            //In this situation, ignore the event.
            //This is not a particularly clean behavior, but it's better than just crashing.
            EventStorageContactCreated contactCreated;
            while (eventStorageContactCreated.TryUnsafeDequeueFirst(out contactCreated))
                if (InternalContactCreated != null)
                    InternalContactCreated(owner, contactCreated.other, contactCreated.pair, contactCreated.contactData);

            EventStorageInitialCollisionDetected initialCollisionDetected;
            while (eventStorageInitialCollisionDetected.TryUnsafeDequeueFirst(out initialCollisionDetected))
                if (InternalInitialCollisionDetected != null)
                    InternalInitialCollisionDetected(owner, initialCollisionDetected.other, initialCollisionDetected.pair);

            EventStorageContactRemoved contactRemoved;
            while (eventStorageContactRemoved.TryUnsafeDequeueFirst(out contactRemoved))
                if (InternalContactRemoved != null)
                    InternalContactRemoved(owner, contactRemoved.other, contactRemoved.pair, contactRemoved.contactData);

            EventStorageCollisionEnded collisionEnded;
            while (eventStorageCollisionEnded.TryUnsafeDequeueFirst(out collisionEnded))
                if (InternalCollisionEnded != null)
                    InternalCollisionEnded(owner, collisionEnded.other, collisionEnded.pair);

            EventStoragePairTouched collisionPairTouched;
            while (eventStoragePairTouched.TryUnsafeDequeueFirst(out collisionPairTouched))
                if (InternalPairTouched != null)
                    InternalPairTouched(owner, collisionPairTouched.other, collisionPairTouched.pair);

            base.DispatchEvents();
        }

        public void OnCollisionEnded(Collidable other, CollidablePairHandler collisionPair)
        {
            if (InternalCollisionEnded != null)
                eventStorageCollisionEnded.Enqueue(new EventStorageCollisionEnded(other, collisionPair));
            if (CollisionEnding != null)
                CollisionEnding(owner, other, collisionPair);
        }

        public void OnPairTouching(Collidable other, CollidablePairHandler collisionPair)
        {
            if (InternalPairTouched != null)
                eventStoragePairTouched.Enqueue(new EventStoragePairTouched(other, collisionPair));
            if (PairTouching != null)
                PairTouching(owner, other, collisionPair);
        }

        public void OnContactCreated(Collidable other, CollidablePairHandler collisionPair, Contact contact)
        {
            if (InternalContactCreated != null)
            {
                ContactData contactData;
                contactData.Position = contact.Position;
                contactData.Normal = contact.Normal;
                contactData.PenetrationDepth = contact.PenetrationDepth;
                contactData.Id = contact.Id;
                eventStorageContactCreated.Enqueue(new EventStorageContactCreated(other, collisionPair, ref contactData));
            }
            if (CreatingContact != null)
                CreatingContact(owner, other, collisionPair, contact);
        }

        public void OnContactRemoved(Collidable other, CollidablePairHandler collisionPair, Contact contact)
        {
            if (InternalContactRemoved != null)
            {
                ContactData contactData;
                contactData.Position = contact.Position;
                contactData.Normal = contact.Normal;
                contactData.PenetrationDepth = contact.PenetrationDepth;
                contactData.Id = contact.Id;
                eventStorageContactRemoved.Enqueue(new EventStorageContactRemoved(other, collisionPair, ref contactData));
            }
            if (RemovingContact != null)
                RemovingContact(owner, other, collisionPair, contact);
        }

        public void OnInitialCollisionDetected(Collidable other, CollidablePairHandler collisionPair)
        {
            if (InternalInitialCollisionDetected != null)
                eventStorageInitialCollisionDetected.Enqueue(new EventStorageInitialCollisionDetected(other, collisionPair));
            if (DetectingInitialCollision != null)
                DetectingInitialCollision(owner, other, collisionPair);
        }

        ///<summary>
        /// Removes all event hooks from the event manager.
        ///</summary>
        public override void RemoveAllEvents()
        {
            InternalCollisionEnded = null;
            InternalPairTouched = null;
            InternalContactCreated = null;
            InternalContactRemoved = null;
            InternalInitialCollisionDetected = null;

            CollisionEnding = null;
            DetectingInitialCollision = null;
            CreatingContact = null;
            RemovingContact = null;
            PairTouching = null;

            base.RemoveAllEvents();
        }

        #endregion



    }




}
