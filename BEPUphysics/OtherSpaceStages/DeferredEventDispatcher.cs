using System;
using System.Collections.Generic;

namespace BEPUphysics.OtherSpaceStages
{
    ///<summary>
    /// Manages the deferred events spawned by IDeferredEventCreators and dispatches them on update.
    ///</summary>
    public class DeferredEventDispatcher : ProcessingStage
    {
        private List<IDeferredEventCreator> activeEventCreators = new List<IDeferredEventCreator>();

        ///<summary>
        /// Constructs the dispatcher.
        ///</summary>
        public DeferredEventDispatcher()
        {
            Enabled = true;
        }

        ///<summary>
        /// Adds an event creator.
        ///</summary>
        ///<param name="creator">Creator to add.</param>
        ///<exception cref="ArgumentException">Thrown when the creator is already managed by a dispatcher.</exception>
        public void AddEventCreator(IDeferredEventCreator creator)
        {
            if (creator.DeferredEventDispatcher == null)
            {
                creator.DeferredEventDispatcher = this;
                //If it already has events attached, add it to the active event creators list.
                //Otherwise, don't bother adding it until it has some.
                //It is up to the creator to notify the dispatcher of the change.
                if (creator.IsActive)
                    activeEventCreators.Add(creator);
            }
            else
                throw new ArgumentException("The event creator is already managed by a dispatcher; it cannot be added.", "creator");
        }

        /// <summary>
        /// Removes an event creator.
        /// </summary>
        /// <param name="creator">Creator to remove.</param>
        public void RemoveEventCreator(IDeferredEventCreator creator)
        {
            if (creator.DeferredEventDispatcher == this)
            {
                creator.DeferredEventDispatcher = null;
                if (creator.IsActive)
                    activeEventCreators.Remove(creator);
            }
            else
                throw new ArgumentException("The event creator is managed by a different dispatcher; it cannot be removed.", "creator");
        }

        ///<summary>
        /// Notifies the dispatcher that the event activity of a creator has changed.
        ///</summary>
        ///<param name="creator">Cretor whose activity has changed.</param>
        ///<exception cref="ArgumentException">Thrown when the event creator's state hasn't changed.</exception>
        public void CreatorActivityChanged(IDeferredEventCreator creator)
        {
            //This is a pretty rarely called method.  It's okay to do a little extra verification at the cost of performance.
            if (creator.IsActive)
                if (!activeEventCreators.Contains(creator))
                    activeEventCreators.Add(creator);
                else
                    throw new ArgumentException("The event creator was already active in the dispatcher; make sure the CreatorActivityChanged function is only called when the state actually changes.", "creator");
            else
                if (!activeEventCreators.Remove(creator))
                    throw new ArgumentException("The event creator not active in the dispatcher; make sure the CreatorActivityChanged function is only called when the state actually changes.", "creator");
        }



        protected override void UpdateStage()
        {
            for (int i = activeEventCreators.Count - 1; i >= 0; i--)
            {
                activeEventCreators[i].DispatchEvents();
                // Since it can attempt to remove or add any number of creators during the handler, all we can do is make sure 
                // that our current index is still valid.  We may re-dispatch some event creators, but assuming they follow the
                // proper pattern for deferred event creators, it will be okay.  The first execution cleans out all of the 
                // deferred events that have been queued up.  The second execution will try, but the queues will be empty.
                // This is a fairly acceptable result for a rare case.
                if (i > activeEventCreators.Count)
                    i = activeEventCreators.Count;

            }
        }

    }
}
