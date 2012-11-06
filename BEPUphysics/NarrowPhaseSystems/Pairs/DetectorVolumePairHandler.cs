using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using System;
using BEPUphysics.Collidables;
using BEPUphysics.UpdateableSystems;
using BEPUphysics.Collidables.MobileCollidables;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Superclass of pairs between collidables that generate contact points.
    ///</summary>
    public abstract class DetectorVolumePairHandler : NarrowPhasePair
    {
        /// <summary>
        /// Gets the detector volume associated with the pair.
        /// </summary>
        public DetectorVolume DetectorVolume { get; private set; }
        /// <summary>
        /// Gets the entity collidable associated with the pair.
        /// </summary>
        public abstract EntityCollidable Collidable { get; }

        /// <summary>
        /// Gets whether or not the collidable was touching the detector volume during the previous frame.
        /// </summary>
        public bool WasTouching
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets whether or not the collidable was fully contained within the detector volume during the previous frame.
        /// </summary>
        public bool WasContaining
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets whether or not the collidable is touching the detector volume.
        /// </summary>
        public bool Touching
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets whether or not the collidable is fully contained within the detector volume.
        /// </summary>
        public bool Containing
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the parent of this pair handler, if any.
        /// </summary>
        public IDetectorVolumePairHandlerParent Parent
        {
            get;
            internal set;
        }

        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            //Child initialization is responsible for setting up the collidable.

            DetectorVolume = entryA as DetectorVolume;
            if (DetectorVolume == null)
            {
                DetectorVolume = entryB as DetectorVolume;
                if (DetectorVolume == null)
                    throw new Exception("Incorrect types used to initialize detector volume pair.");
            }

        }

        ///<summary>
        /// Called when the pair handler is added to the narrow phase.
        ///</summary>
        protected internal override void OnAddedToNarrowPhase()
        {
            DetectorVolume.pairs.Add(Collidable.entity, this);
        }


        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {
            //Fire off some events if needed! Note the order; we should stop containing before we stop touching. 
            if (Parent == null)
            {
                if (Containing)
                {
                    DetectorVolume.StoppedContaining(this);
                }
                if (Touching)
                {
                    DetectorVolume.StoppedTouching(this);
                }
            }
            Containing = false;
            Touching = false;
            WasContaining = false;
            WasTouching = false;


            DetectorVolume.pairs.Remove(Collidable.entity);


            broadPhaseOverlap = new BroadPhaseOverlap();

            DetectorVolume = null;

            Parent = null;
            //Child cleanup is responsible for cleaning up direct references to the involved collidables.


        }

        protected void NotifyDetectorVolumeOfChanges()
        {
            //Don't notify the detector volume if we have a parent.  The parent will analyze our state.
            if (Parent == null)
            {
                //Beware the order!
                //Starts touching -> starts containing
                if (!WasTouching && Touching)
                {
                    DetectorVolume.BeganTouching(this);
                }
                if (!WasContaining && Containing)
                {
                    DetectorVolume.BeganContaining(this);
                }
                //Stops containing -> stops touching
                if (WasContaining && !Containing)
                {
                    DetectorVolume.StoppedContaining(this);
                }
                if (WasTouching && !Touching)
                {
                    DetectorVolume.StoppedTouching(this);
                }
            }

        }

    }
}
