using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionTests;

namespace BEPUphysics.Collidables.Events
{
    /// <summary>
    /// Manages triggers for events in an ContactEventManager.
    /// </summary>
    public interface IContactEventTriggerer : IEntryEventTriggerer
    {
        /// <summary>
        /// Fires collision ending events.
        /// </summary>
        /// <param name="other">Other collidable involved in the pair.</param>
        /// <param name="collisionPair">Collidable pair handler that manages the two objects.</param>
        void OnCollisionEnded(Collidable other, CollidablePairHandler collisionPair);

        /// <summary>
        /// Fires pair touching events.
        /// </summary>
        /// <param name="other">Other collidable involved in the pair.</param>
        /// <param name="collisionPair">Collidable pair handler that manages the two objects.</param>
        void OnPairTouching(Collidable other, CollidablePairHandler collisionPair);

        /// <summary>
        /// Fires contact creating events.
        /// </summary>
        /// <param name="other">Other collidable involved in the pair.</param>
        /// <param name="collisionPair">Collidable pair handler that manages the two objects.</param>
        /// <param name="contact">Contact point of collision.</param>
        void OnContactCreated(Collidable other, CollidablePairHandler collisionPair, Contact contact);

        /// <summary>
        /// Fires contact removal events.
        /// </summary>
        /// <param name="other">Other collidable involved in the pair.</param>
        /// <param name="collisionPair">Collidable pair handler that manages the two objects.</param>
        /// <param name="contact">Contact point of collision.</param>
        void OnContactRemoved(Collidable other, CollidablePairHandler collisionPair, Contact contact);

        /// <summary>
        /// Fires initial collision detected events.
        /// </summary>
        /// <param name="other">Other collidable involved in the pair.</param>
        /// <param name="collisionPair">Collidable pair handler that manages the two objects.</param>
        void OnInitialCollisionDetected(Collidable other, CollidablePairHandler collisionPair);
    }
}
