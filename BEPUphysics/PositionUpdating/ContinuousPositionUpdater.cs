using System;
using System.Collections.Generic;
using BEPUphysics.Settings;
using BEPUphysics.Threading;
using BEPUutilities;
using BEPUutilities.DataStructures;

namespace BEPUphysics.PositionUpdating
{
    ///<summary>
    /// Updates objects according to the position update mode.
    /// This allows continuous objects to avoid missing collisions.
    ///</summary>
    public class ContinuousPositionUpdater : PositionUpdater
    {
        RawList<IPositionUpdateable> discreteUpdateables = new RawList<IPositionUpdateable>();
        RawList<ICCDPositionUpdateable> passiveUpdateables = new RawList<ICCDPositionUpdateable>();
        RawList<ICCDPositionUpdateable> continuousUpdateables = new RawList<ICCDPositionUpdateable>();

        ///<summary>
        /// Number of objects in a list required to use multithreading.
        ///</summary>
        public static int MultithreadingThreshold = 100;

        ///<summary>
        /// Constructs the position updater.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        public ContinuousPositionUpdater(TimeStepSettings timeStepSettings)
            : base(timeStepSettings)
        {
            preUpdate = PreUpdate;
            updateTimeOfImpact = UpdateTimeOfImpact;
            updateContinuous = UpdateContinuousItem;
        }


        ///<summary>
        /// Constructs the position updater.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings to use.</param>
        /// <param name="threadManager">Thread manager to use.</param>
        public ContinuousPositionUpdater(TimeStepSettings timeStepSettings, IThreadManager threadManager)
            : base(timeStepSettings, threadManager)
        {
            preUpdate = PreUpdate;
            updateTimeOfImpact = UpdateTimeOfImpact;
            updateContinuous = UpdateContinuousItem;
        }

        Action<int> preUpdate;
        void PreUpdate(int i)
        {
            if (i >= discreteUpdateables.Count)
            {
                i -= discreteUpdateables.Count;
                if (i >= passiveUpdateables.Count)
                {
                    i -= passiveUpdateables.Count;
                    //It's a continuous updateable.
                    if (continuousUpdateables.Elements[i].IsActive)
                        continuousUpdateables.Elements[i].PreUpdatePosition(timeStepSettings.TimeStepDuration);
                }
                else
                {
                    //It's a passive updateable.
                    if (passiveUpdateables.Elements[i].IsActive)
                        passiveUpdateables.Elements[i].PreUpdatePosition(timeStepSettings.TimeStepDuration);
                }
            }
            else
            {
                //It's a discrete updateable.
                if (discreteUpdateables.Elements[i].IsActive)
                    discreteUpdateables.Elements[i].PreUpdatePosition(timeStepSettings.TimeStepDuration);
            }
        }

        Action<int> updateTimeOfImpact;
        void UpdateTimeOfImpact(int i)
        {
            //This should always execute, even if the updateable is not active.  This is because
            //a CCD object may be in a pair where the CCD object is resting, and another incoming object
            //is awake.
            continuousUpdateables.Elements[i].UpdateTimesOfImpact(timeStepSettings.TimeStepDuration);
        }

        Action<int> updateContinuous;
        void UpdateContinuousItem(int i)
        {
            if (i < passiveUpdateables.Count)
            {
                if (passiveUpdateables.Elements[i].IsActive)
                    passiveUpdateables.Elements[i].UpdatePositionContinuously(timeStepSettings.TimeStepDuration);
            }
            else
            {
                if (continuousUpdateables.Elements[i - passiveUpdateables.Count].IsActive)
                    continuousUpdateables.Elements[i - passiveUpdateables.Count].UpdatePositionContinuously(timeStepSettings.TimeStepDuration);
            }
        }

        protected override void UpdateMultithreaded()
        {

            //Go through the list of all updateables which do not permit motion clamping.
            //Since these do not care about CCD, just update them as if they were discrete.
            //In addition, go through the remaining non-discrete objects and perform their prestep.
            //This usually involves updating their angular motion, but not their linear motion.
            int count = discreteUpdateables.Count + passiveUpdateables.Count + continuousUpdateables.Count;
            ThreadManager.ForLoop(0, count, preUpdate);

            //Now go through the list of all full CCD objects.  These are responsible
            //for determining the TOI of collision pairs, if existent.
            if (continuousUpdateables.Count > MultithreadingThreshold)
                ThreadManager.ForLoop(0, continuousUpdateables.Count, updateTimeOfImpact);
            else
                for (int i = 0; i < continuousUpdateables.Count; i++)
                    UpdateTimeOfImpact(i);

            //The TOI's are now computed, so we can integrate all of the CCD or allow-motionclampers 
            //to their new positions.
            count = passiveUpdateables.Count + continuousUpdateables.Count;
            if (count > MultithreadingThreshold)
                ThreadManager.ForLoop(0, count, updateContinuous);
            else
                for (int i = 0; i < count; i++)
                    UpdateContinuousItem(i);

            //The above process is the same as the UpdateSingleThreaded version, but 
            //it doesn't always use multithreading.  Sometimes, a simulation can have
            //very few continuous objects.  In this case, there's no point in having the 
            //multithreaded overhead.

        }

        protected override void UpdateSingleThreaded()
        {
            //Go through the list of all updateables which do not permit motion clamping.
            //Since these do not care about CCD, just update them as if they were discrete.
            //In addition, go through the remaining non-discrete objects and perform their prestep.
            //This usually involves updating their angular motion, but not their linear motion.
            int count = discreteUpdateables.Count + passiveUpdateables.Count + continuousUpdateables.Count;
            for (int i = 0; i < count; i++)
                PreUpdate(i);

            //Now go through the list of all full CCD objects.  These are responsible
            //for determining the TOI of collision pairs, if existent.
            for (int i = 0; i < continuousUpdateables.Count; i++)
                UpdateTimeOfImpact(i);

            //The TOI's are now computed, so we can integrate all of the CCD or allow-motionclampers 
            //to their new positions.
            count = passiveUpdateables.Count + continuousUpdateables.Count;
            for (int i = 0; i < count; i++)
                UpdateContinuousItem(i);
        }

        ///<summary>
        /// Notifies the position updater that an updateable has changed state.
        ///</summary>
        ///<param name="updateable">Updateable with changed state.</param>
        ///<param name="previousMode">Previous state the updateable was in.</param>
        public void UpdateableModeChanged(ICCDPositionUpdateable updateable, PositionUpdateMode previousMode)
        {
            switch (previousMode)
            {
                case PositionUpdateMode.Discrete:
                    discreteUpdateables.Remove(updateable);
                    break;
                case PositionUpdateMode.Passive:
                    passiveUpdateables.Remove(updateable);
                    break;
                case PositionUpdateMode.Continuous:
                    continuousUpdateables.Remove(updateable);
                    break;
            }

            switch (updateable.PositionUpdateMode)
            {
                case PositionUpdateMode.Discrete:
                    discreteUpdateables.Add(updateable);
                    break;
                case PositionUpdateMode.Passive:
                    passiveUpdateables.Add(updateable);
                    break;
                case PositionUpdateMode.Continuous:
                    continuousUpdateables.Add(updateable);
                    break;
            }
        }


        ///<summary>
        /// Adds an object to the position updater.
        ///</summary>
        ///<param name="updateable">Updateable to add.</param>
        ///<exception cref="Exception">Thrown if the updateable already belongs to a position updater.</exception>
        public override void Add(IPositionUpdateable updateable)
        {
            if (updateable.PositionUpdater == null)
            {
                updateable.PositionUpdater = this;
                var ccdUpdateable = updateable as ICCDPositionUpdateable;
                if (ccdUpdateable != null)
                {
                    switch (ccdUpdateable.PositionUpdateMode)
                    {
                        case PositionUpdateMode.Discrete:
                            discreteUpdateables.Add(updateable);
                            break;
                        case PositionUpdateMode.Passive:
                            passiveUpdateables.Add(ccdUpdateable);
                            break;
                        case PositionUpdateMode.Continuous:
                            continuousUpdateables.Add(ccdUpdateable);
                            break;
                    }
                }
                else
                    discreteUpdateables.Add(updateable);
            }
            else
            {
                throw new ArgumentException("Cannot add object to Integrator; it already belongs to one.");
            }
        }


        ///<summary>
        /// Removes an updateable from the updater.
        ///</summary>
        ///<param name="updateable">Item to remove.</param>
        ///<exception cref="Exception">Thrown if the updater does not own the updateable.</exception>
        public override void Remove(IPositionUpdateable updateable)
        {
            if (updateable.PositionUpdater == this)
            {
                updateable.PositionUpdater = null;
                var ccdUpdateable = updateable as ICCDPositionUpdateable;
                if (ccdUpdateable != null)
                {
                    switch (ccdUpdateable.PositionUpdateMode)
                    {
                        case PositionUpdateMode.Discrete:
                            discreteUpdateables.Remove(updateable);
                            break;
                        case PositionUpdateMode.Passive:
                            passiveUpdateables.Remove(ccdUpdateable);
                            break;
                        case PositionUpdateMode.Continuous:
                            continuousUpdateables.Remove(ccdUpdateable);
                            break;
                    }
                }
                else
                    discreteUpdateables.Remove(updateable);
            }
            else
                throw new ArgumentException("Cannot remove object from this Integrator.  The object doesn't belong to it.");
        }
    }
}
