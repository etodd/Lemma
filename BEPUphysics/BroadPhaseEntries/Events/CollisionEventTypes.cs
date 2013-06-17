using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace BEPUphysics.BroadPhaseEntries.Events
{
    //TODO: Contravariance isn't supported on all platforms...

    /// <summary>
    /// Handles any special logic when two objects' bounding boxes overlap as determined by the broadphase system.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    public delegate void PairCreatedEventHandler<T>(T sender, BroadPhaseEntry other, NarrowPhasePair pair);

    /// <summary>
    /// Handles any special logic when two objects' bounding boxes overlap as determined by the broadphase system.
    /// Unlike PairCreatedEventHandler, this will be called as soon as a pair is created instead of at the end of the frame.
    /// This allows the pair's data to be adjusted prior to any usage, but some actions are not supported due to the execution stage.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    public delegate void CreatingPairEventHandler<T>(T sender, BroadPhaseEntry other, NarrowPhasePair pair);

    /// <summary>
    /// Handles any special logic when two objects' bounding boxes cease to overlap as determined by the broadphase system.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">The entry formerly interacting with the sender via the deleted pair.</param>
    public delegate void PairRemovedEventHandler<T>(T sender, BroadPhaseEntry other);

    /// <summary>
    /// Handles any special logic when two objects' bounding boxes cease to overlap as determined by the broadphase system.
    /// Unlike PairRemovedEventHandler, this will trigger at the time of pair removal instead of at the end of the space's update.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">The entry formerly interacting with the sender via the deleted pair.</param>
    public delegate void RemovingPairEventHandler<T>(T sender, BroadPhaseEntry other);

    /// <summary>
    /// Handles any special logic when two bodies are touching and generate a contact point.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    /// <param name="contact">Created contact data.</param>
    public delegate void ContactCreatedEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair, ContactData contact);

    /// <summary>
    /// Handles any special logic when two bodies are touching and generate a contact point.
    /// Unlike ContactCreatedEventHandler, this will trigger at the time of contact generation instead of at the end of the space's update.
    /// This allows the contact's data to be adjusted prior to usage in the velocity solver, 
    /// but other actions such as altering the owning space's pair or entry listings are unsafe.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    /// <param name="contact">Newly generated contact point between the pair's two bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; contacts can quickly return to the resource pool.</param>
    public delegate void CreatingContactEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair, Contact contact);

    /// <summary>
    /// Handles any special logic when two bodies initally collide and generate a contact point.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    public delegate void InitialCollisionDetectedEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    /// <summary>
    /// Handles any special logic when two bodies initally collide and generate a contact point.
    /// Unlike InitialCollisionDetectedEventHandler, this will trigger at the time of contact creation instead of at the end of the space's update.
    /// Performing operations outside of the scope of the pair is unsafe.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    public delegate void DetectingInitialCollisionEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    /// <summary>
    /// Handles any special logic when a contact point between two bodies is removed.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies and data about the removed contact.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    /// <param name="contact">Removed contact data.</param>
    public delegate void ContactRemovedEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair, ContactData contact);

    /// <summary>
    /// Handles any special logic when a contact point between two bodies is removed.
    /// Unlike ContactRemovedEventHandler, this will trigger at the time of contact removal instead of at the end of the space's update.
    /// Performing operations outside of the scope of the controller is unsafe.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies and data about the removed contact.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    /// <param name="contact">Contact between the two entries.  This reference cannot be safely kept outside of the scope of the handler;
    /// it will be immediately returned to the resource pool after the event handler completes.</param>
    public delegate void RemovingContactEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair, Contact contact);

    /// <summary>
    /// Handles any special logic when two bodies go from a touching state to a separated state.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair overseeing the collision.  Note that this instance may be invalid if the entries' bounding boxes no longer overlap.</param>
    public delegate void CollisionEndedEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    /// <summary>
    /// Handles any special logic when two bodies go from a touching state to a separated state.
    /// Unlike CollisionEndedEventHandler, this will trigger at the time of contact removal instead of at the end of the space's update.
    /// Performing operations outside of the scope of the controller is unsafe.
    /// </summary>
    /// <param name="sender">Entry sending the event.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair presiding over the interaction of the two involved bodies.
    /// This reference cannot be safely kept outside of the scope of the handler; pairs can quickly return to the resource pool.</param>
    public delegate void CollisionEndingEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    /// <summary>
    /// Handles any special logic to perform at the end of a pair's UpdateContactManifold method.
    /// This is called every single update regardless if the process was quit early or did not complete due to interaction rules.
    /// </summary>
    /// <param name="sender">Entry involved in the pair monitored for events.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair that was updated.</param>
    public delegate void PairUpdatedEventHandler<T>(T sender, BroadPhaseEntry other, NarrowPhasePair pair);

    /// <summary>
    /// Handles any special logic to perform at the end of a pair's UpdateContactManifold method.
    /// This is called every single update regardless if the process was quit early or did not complete due to interaction rules.
    /// Unlike PairUpdatedEventHandler, this is called at the time of the collision detection update rather than at the end of the space's update.
    /// Other entries' information may not be up to date, and operations acting on data outside of the character controller may be unsafe.
    /// </summary>
    /// <param name="sender">Entry involved in the pair monitored for events.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair that was updated.</param>
    public delegate void PairUpdatingEventHandler<T>(T sender, BroadPhaseEntry other, NarrowPhasePair pair);

    /// <summary>
    /// Handles any special logic to perform at the end of a pair's UpdateContactManifold method if the two objects are colliding.
    /// This is called every single update regardless if the process was quit early or did not complete due to interaction rules.
    /// </summary>
    /// <param name="sender">Entry involved in the pair monitored for events.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair that was updated.</param>
    public delegate void PairTouchedEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    /// <summary>
    /// Handles any special logic to perform at the end of a pair's UpdateContactManifold method if the two objects are colliding.
    /// This is called every single update regardless if the process was quit early or did not complete due to interaction rules.
    /// Unlike PairTouchedEventHandler, this is called at the time of the collision detection update rather than at the end of the space's update.
    /// Other entries' information may not be up to date, and operations acting on data outside of the character controller may be unsafe.
    /// </summary>
    /// <param name="sender">Entry involved in the pair monitored for events.</param>
    /// <param name="other">Other entry within the pair opposing the monitored entry.</param>
    /// <param name="pair">Pair that was updated.</param>
    public delegate void PairTouchingEventHandler<T>(T sender, Collidable other, CollidablePairHandler pair);

    //Storage for deferred event dispatching
    internal struct EventStoragePairCreated
    {
        internal NarrowPhasePair pair;
        internal BroadPhaseEntry other;

        internal EventStoragePairCreated(BroadPhaseEntry other, NarrowPhasePair pair)
        {
            this.other = other;
            this.pair = pair;
        }
    }

    internal struct EventStoragePairRemoved
    {
        internal BroadPhaseEntry other;

        internal EventStoragePairRemoved(BroadPhaseEntry other)
        {
            this.other = other;
        }
    }

    internal struct EventStorageContactCreated
    {
        internal CollidablePairHandler pair;
        internal ContactData contactData;
        internal Collidable other;


        internal EventStorageContactCreated(Collidable other, CollidablePairHandler pair, ref ContactData contactData)
        {
            this.other = other;
            this.pair = pair;
            this.contactData = contactData;
        }
    }

    internal struct EventStorageInitialCollisionDetected
    {
        internal CollidablePairHandler pair;
        internal Collidable other;

        internal EventStorageInitialCollisionDetected(Collidable other, CollidablePairHandler pair)
        {
            this.pair = pair;
            this.other = other;
        }
    }

    internal struct EventStorageContactRemoved
    {
        internal CollidablePairHandler pair;
        internal ContactData contactData;
        internal Collidable other;

        internal EventStorageContactRemoved(Collidable other, CollidablePairHandler pair, ref ContactData contactData)
        {
            this.other = other;
            this.pair = pair;
            this.contactData = contactData;
        }
    }

    internal struct EventStorageCollisionEnded
    {
        internal CollidablePairHandler pair;
        internal Collidable other;

        internal EventStorageCollisionEnded(Collidable other, CollidablePairHandler pair)
        {
            this.other = other;
            this.pair = pair;
        }
    }

    internal struct EventStoragePairUpdated
    {
        internal NarrowPhasePair pair;
        internal BroadPhaseEntry other;

        internal EventStoragePairUpdated(BroadPhaseEntry other, NarrowPhasePair pair)
        {
            this.other = other;
            this.pair = pair;
        }
    }

    internal struct EventStoragePairTouched
    {
        internal CollidablePairHandler pair;
        internal Collidable other;

        internal EventStoragePairTouched(Collidable other, CollidablePairHandler pair)
        {
            this.other = other;
            this.pair = pair;
        }
    }
}
