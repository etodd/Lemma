using System;
using System.Collections.Generic;
using BEPUphysics.Threading;

namespace BEPUphysics.PositionUpdating
{
    ///<summary>
    /// Discrete position updater.  Similar to the ContinuousPositionUpdater, but
    /// ignores the continuous state and just updates everything as if it were discrete.
    ///</summary>
    public class DiscretePositionUpdater : PositionUpdater
    {
        List<IPositionUpdateable> integrables = new List<IPositionUpdateable>();

        ///<summary>
        /// Constructs the discrete position updater.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public DiscretePositionUpdater(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
            loopBody = UpdateIntegrable;
        }

        ///<summary>
        /// Constructs the discrete position updater.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public DiscretePositionUpdater(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
            loopBody = UpdateIntegrable;
        }

        Action<int> loopBody;
        void UpdateIntegrable(int i)
        {
            if (integrables[i].IsActive)
                integrables[i].PreUpdatePosition(timeStepSettings.TimeStepDuration);
        }

        protected override void UpdateMultithreaded()
        {
            ThreadManager.ForLoop(0, integrables.Count, loopBody);
        }

        protected override void UpdateSingleThreaded()
        {
            for (int i = 0; i < integrables.Count; i++)
                UpdateIntegrable(i);
        }


        ///<summary>
        /// Adds an updateable to the updater.
        ///</summary>
        ///<param name="updateable">Item to add.</param>
        ///<exception cref="Exception">Thrown if the updateable already belongs to an updater.</exception>
        public override void Add(IPositionUpdateable updateable)
        {
            if (updateable.PositionUpdater == null)
            {
                updateable.PositionUpdater = this;
                integrables.Add(updateable);
            }
            else
            {
                throw new Exception("Cannot add object to position updater; it already belongs to one.");
            }
        }

        /// <summary>
        /// Removes an updateable from the updater.
        /// </summary>
        /// <param name="updateable">Updateable to remove.</param>
        public override void Remove(IPositionUpdateable updateable)
        {
            if (updateable.PositionUpdater == this)
            {
                updateable.PositionUpdater = null;
                integrables.Remove(updateable);
            }
            else
                throw new Exception("Cannot remove object from this position updater.  The object doesn't belong to it.");
        }
    }
}
