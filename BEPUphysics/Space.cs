using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.BroadPhaseSystems.Hierarchies;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.DeactivationManagement;
using BEPUphysics.Entities;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.OtherSpaceStages;
using BEPUphysics.PositionUpdating;
using BEPUphysics.SolverSystems;
using BEPUphysics.Threading;
using BEPUphysics.NarrowPhaseSystems;
using BEPUphysics.UpdateableSystems;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using System.Collections.ObjectModel;
using BEPUphysics.BroadPhaseSystems.SortAndSweep;
using BEPUphysics.DataStructures;
using BEPUphysics.MathExtensions;

namespace BEPUphysics
{
    ///<summary>
    /// Main simulation class of BEPUphysics.  Contains various updating stages addition/removal methods for getting objects into the simulation.
    ///</summary>
    public class Space : ISpace, IDisposable
    {
        private TimeStepSettings timeStepSettings;
        ///<summary>
        /// Gets or sets the time step settings used by the space.
        ///</summary>
        public TimeStepSettings TimeStepSettings
        {
            get
            {
                return timeStepSettings;
            }
            set
            {
                timeStepSettings = value;
                DeactivationManager.TimeStepSettings = value;
                ForceUpdater.TimeStepSettings = value;
                BoundingBoxUpdater.TimeStepSettings = value;
                Solver.TimeStepSettings = value;
                PositionUpdater.TimeStepSettings = value;

            }
        }

        IThreadManager threadManager;
        ///<summary>
        /// Gets or sets the thread manager used by the space.
        ///</summary>
        public IThreadManager ThreadManager
        {
            get
            {
                return threadManager;
            }
            set
            {
                threadManager = value;
                DeactivationManager.ThreadManager = value;
                ForceUpdater.ThreadManager = value;
                BoundingBoxUpdater.ThreadManager = value;
                BroadPhase.ThreadManager = value;
                NarrowPhase.ThreadManager = value;
                Solver.ThreadManager = value;
                PositionUpdater.ThreadManager = value;
                DuringForcesUpdateables.ThreadManager = value;
                BeforeNarrowPhaseUpdateables.ThreadManager = value;
                EndOfTimeStepUpdateables.ThreadManager = value;
                EndOfFrameUpdateables.ThreadManager = value;
            }
        }

        ///<summary>
        /// Gets or sets the space object buffer used by the space.
        /// The space object buffer allows objects to be safely asynchronously
        /// added to and removed from the space.
        ///</summary>
        public SpaceObjectBuffer SpaceObjectBuffer { get; set; }
        ///<summary>
        /// Gets or sets the entity state write buffer used by the space.
        /// The write buffer contains buffered writes to entity states that are
        /// flushed each frame when the buffer is updated.
        ///</summary>
        public EntityStateWriteBuffer EntityStateWriteBuffer { get; set; }
        ///<summary>
        /// Gets or sets the deactivation manager used by the space.
        /// The deactivation manager controls the activity state objects, putting them
        /// to sleep and managing the connections between objects and simulation islands.
        ///</summary>
        public DeactivationManager DeactivationManager { get; set; }
        ///<summary>
        /// Gets or sets the force updater used by the space.
        /// The force updater applies forces to all dynamic objects in the space each frame.
        ///</summary>
        public ForceUpdater ForceUpdater { get; set; }
        ///<summary>
        /// Gets or sets the bounding box updater used by the space.
        /// The bounding box updater updates the bounding box of mobile collidables each frame.
        ///</summary>
        public BoundingBoxUpdater BoundingBoxUpdater { get; set; }
        private BroadPhase broadPhase;
        /// <summary>
        /// Gets or sets the broad phase used by the space.
        /// The broad phase finds overlaps between broad phase entries and passes
        /// them off to the narrow phase for processing.
        /// </summary>
        public BroadPhase BroadPhase
        {
            get
            {
                return broadPhase;
            }
            set
            {
                broadPhase = value;
                if (NarrowPhase != null)
                    if (value != null)
                    {
                        NarrowPhase.BroadPhaseOverlaps = broadPhase.Overlaps;
                    }
                    else
                    {
                        NarrowPhase.BroadPhaseOverlaps = null;
                    }
            }
        }
        ///<summary>
        /// Gets or sets the narrow phase used by the space.
        /// The narrow phase uses overlaps found by the broad phase
        /// to create pair handlers.  Those pair handlers can go on to 
        /// create things like contacts and constraints.
        ///</summary>
        public NarrowPhase NarrowPhase { get; set; }
        ///<summary>
        /// Gets or sets the solver used by the space.
        /// The solver iteratively finds a solution to the constraints in the simulation.
        ///</summary>
        public Solver Solver { get; set; }
        ///<summary>
        /// Gets or sets the position updater used by the space.
        /// The position updater moves everything around each frame.
        ///</summary>
        public PositionUpdater PositionUpdater { get; set; }
        ///<summary>
        /// Gets or sets the buffered states manager used by the space.
        /// The buffered states manager keeps track of read buffered entity states
        /// and also interpolated states based on the time remaining from internal
        /// time steps.
        ///</summary>
        public BufferedStatesManager BufferedStates { get; set; }
        ///<summary>
        /// Gets or sets the deferred event dispatcher used by the space.
        /// The event dispatcher gathers up deferred events created
        /// over the course of a timestep and dispatches them sequentially at the end.
        ///</summary>
        public DeferredEventDispatcher DeferredEventDispatcher { get; set; }

        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update during force application.
        ///</summary>
        public DuringForcesUpdateableManager DuringForcesUpdateables { get; set; }
        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update before the narrow phase.
        ///</summary>
        public BeforeNarrowPhaseUpdateableManager BeforeNarrowPhaseUpdateables { get; set; }
        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update before the solver
        ///</summary>
        public BeforeSolverUpdateableManager BeforeSolverUpdateables { get; set; }
        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update right before the position update phase.
        ///</summary>
        public BeforePositionUpdateUpdateableManager BeforePositionUpdateUpdateables { get; set; }
        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update at the end of a timestep.
        ///</summary>
        public EndOfTimeStepUpdateableManager EndOfTimeStepUpdateables { get; set; }
        ///<summary>
        /// Gets or sets the updateable manager that handles updateables that update at the end of a frame.
        ///</summary>
        public EndOfFrameUpdateableManager EndOfFrameUpdateables { get; set; }


        ///<summary>
        /// Gets the list of entities in the space.
        ///</summary>
        public ReadOnlyList<Entity> Entities
        {
            get { return BufferedStates.Entities; }
        }

        ///<summary>
        /// Constructs a new space for things to live in.
        /// Uses the SpecializedThreadManager.
        ///</summary>
        public Space()
            : this(new SpecializedThreadManager())
        {
        }

        ///<summary>
        /// Constructs a new space for things to live in.
        ///</summary>
        ///<param name="threadManager">Thread manager to use with the space.</param>
        public Space(IThreadManager threadManager)
        {
            timeStepSettings = new TimeStepSettings();

            this.threadManager = threadManager;

            SpaceObjectBuffer = new SpaceObjectBuffer(this);
            EntityStateWriteBuffer = new EntityStateWriteBuffer();
            DeactivationManager = new DeactivationManager(TimeStepSettings, ThreadManager);
            ForceUpdater = new ForceUpdater(TimeStepSettings, ThreadManager);
            BoundingBoxUpdater = new BoundingBoxUpdater(TimeStepSettings, ThreadManager);
            BroadPhase = new DynamicHierarchy(ThreadManager);
            NarrowPhase = new NarrowPhase(TimeStepSettings, BroadPhase.Overlaps, ThreadManager);
            Solver = new Solver(TimeStepSettings, DeactivationManager, ThreadManager);
            NarrowPhase.Solver = Solver;
            PositionUpdater = new ContinuousPositionUpdater(TimeStepSettings, ThreadManager);
            BufferedStates = new BufferedStatesManager(ThreadManager);
            DeferredEventDispatcher = new DeferredEventDispatcher();

            DuringForcesUpdateables = new DuringForcesUpdateableManager(timeStepSettings, ThreadManager);
            BeforeNarrowPhaseUpdateables = new BeforeNarrowPhaseUpdateableManager(timeStepSettings, ThreadManager);
            BeforeSolverUpdateables = new BeforeSolverUpdateableManager(timeStepSettings, ThreadManager);
            BeforePositionUpdateUpdateables = new BeforePositionUpdateUpdateableManager(timeStepSettings, ThreadManager);
            EndOfTimeStepUpdateables = new EndOfTimeStepUpdateableManager(timeStepSettings, ThreadManager);
            EndOfFrameUpdateables = new EndOfFrameUpdateableManager(timeStepSettings, ThreadManager);

        }


        ///<summary>
        /// Adds a space object to the simulation.
        ///</summary>
        ///<param name="spaceObject">Space object to add.</param>
        public void Add(ISpaceObject spaceObject)
        {
            if (spaceObject.Space != null)
                throw new ArgumentException("The object belongs to some Space already; cannot add it again.");
            spaceObject.Space = this;

            SimulationIslandMember simulationIslandMember = spaceObject as SimulationIslandMember;
            if (simulationIslandMember != null)
            {
                DeactivationManager.Add(simulationIslandMember);
            }

            ISimulationIslandMemberOwner simulationIslandMemberOwner = spaceObject as ISimulationIslandMemberOwner;
            if (simulationIslandMemberOwner != null)
            {
                DeactivationManager.Add(simulationIslandMemberOwner.ActivityInformation);
            }

            //Go through each stage, adding the space object to it if necessary.
            IForceUpdateable velocityUpdateable = spaceObject as IForceUpdateable;
            if (velocityUpdateable != null)
            {
                ForceUpdater.Add(velocityUpdateable);
            }

            MobileCollidable boundingBoxUpdateable = spaceObject as MobileCollidable;
            if (boundingBoxUpdateable != null)
            {
                BoundingBoxUpdater.Add(boundingBoxUpdateable);
            }

            BroadPhaseEntry broadPhaseEntry = spaceObject as BroadPhaseEntry;
            if (broadPhaseEntry != null)
            {
                BroadPhase.Add(broadPhaseEntry);
            }

            //Entites own collision proxies, but are not entries themselves.
            IBroadPhaseEntryOwner broadPhaseEntryOwner = spaceObject as IBroadPhaseEntryOwner;
            if (broadPhaseEntryOwner != null)
            {
                BroadPhase.Add(broadPhaseEntryOwner.Entry);
                boundingBoxUpdateable = broadPhaseEntryOwner.Entry as MobileCollidable;
                if (boundingBoxUpdateable != null)
                {
                    BoundingBoxUpdater.Add(boundingBoxUpdateable);
                }
            }

            SolverUpdateable solverUpdateable = spaceObject as SolverUpdateable;
            if (solverUpdateable != null)
            {
                Solver.Add(solverUpdateable);
            }

            IPositionUpdateable integrable = spaceObject as IPositionUpdateable;
            if (integrable != null)
            {
                PositionUpdater.Add(integrable);
            }

            Entity entity = spaceObject as Entity;
            if (entity != null)
            {
                BufferedStates.Add(entity);
            }

            IDeferredEventCreator deferredEventCreator = spaceObject as IDeferredEventCreator;
            if (deferredEventCreator != null)
            {
                DeferredEventDispatcher.AddEventCreator(deferredEventCreator);
            }

            IDeferredEventCreatorOwner deferredEventCreatorOwner = spaceObject as IDeferredEventCreatorOwner;
            if (deferredEventCreatorOwner != null)
            {
                DeferredEventDispatcher.AddEventCreator(deferredEventCreatorOwner.EventCreator);
            }

            //Updateable stages.
            IDuringForcesUpdateable duringForcesUpdateable = spaceObject as IDuringForcesUpdateable;
            if (duringForcesUpdateable != null)
            {
                DuringForcesUpdateables.Add(duringForcesUpdateable);
            }

            IBeforeNarrowPhaseUpdateable beforeNarrowPhaseUpdateable = spaceObject as IBeforeNarrowPhaseUpdateable;
            if (beforeNarrowPhaseUpdateable != null)
            {
                BeforeNarrowPhaseUpdateables.Add(beforeNarrowPhaseUpdateable);
            }

            IBeforeSolverUpdateable beforeSolverUpdateable = spaceObject as IBeforeSolverUpdateable;
            if (beforeSolverUpdateable != null)
            {
                BeforeSolverUpdateables.Add(beforeSolverUpdateable);
            }

            IBeforePositionUpdateUpdateable beforePositionUpdateUpdateable = spaceObject as IBeforePositionUpdateUpdateable;
            if (beforePositionUpdateUpdateable != null)
            {
                BeforePositionUpdateUpdateables.Add(beforePositionUpdateUpdateable);
            }

            IEndOfTimeStepUpdateable endOfStepUpdateable = spaceObject as IEndOfTimeStepUpdateable;
            if (endOfStepUpdateable != null)
            {
                EndOfTimeStepUpdateables.Add(endOfStepUpdateable);
            }

            IEndOfFrameUpdateable endOfFrameUpdateable = spaceObject as IEndOfFrameUpdateable;
            if (endOfFrameUpdateable != null)
            {
                EndOfFrameUpdateables.Add(endOfFrameUpdateable);
            }

            spaceObject.OnAdditionToSpace(this);
        }

        ///<summary>
        /// Removes a space object from the simulation.
        ///</summary>
        ///<param name="spaceObject">Space object to remove.</param>
        public void Remove(ISpaceObject spaceObject)
        {
            if (spaceObject.Space != this)
                throw new ArgumentException("The object does not belong to this space; cannot remove it.");

            SimulationIslandMember simulationIslandMember = spaceObject as SimulationIslandMember;
            if (simulationIslandMember != null)
            {
                DeactivationManager.Remove(simulationIslandMember);
            }

            ISimulationIslandMemberOwner simulationIslandMemberOwner = spaceObject as ISimulationIslandMemberOwner;
            if (simulationIslandMemberOwner != null)
            {
                DeactivationManager.Remove(simulationIslandMemberOwner.ActivityInformation);
            }

            //Go through each stage, removing the space object from it if necessary.
            IForceUpdateable velocityUpdateable = spaceObject as IForceUpdateable;
            if (velocityUpdateable != null)
            {
                ForceUpdater.Remove(velocityUpdateable);
            }

            MobileCollidable boundingBoxUpdateable = spaceObject as MobileCollidable;
            if (boundingBoxUpdateable != null)
            {
                BoundingBoxUpdater.Remove(boundingBoxUpdateable);
            }

            BroadPhaseEntry broadPhaseEntry = spaceObject as BroadPhaseEntry;
            if (broadPhaseEntry != null)
            {
                BroadPhase.Remove(broadPhaseEntry);
            }

            //Entites own collision proxies, but are not entries themselves.
            IBroadPhaseEntryOwner broadPhaseEntryOwner = spaceObject as IBroadPhaseEntryOwner;
            if (broadPhaseEntryOwner != null)
            {
                BroadPhase.Remove(broadPhaseEntryOwner.Entry);
                boundingBoxUpdateable = broadPhaseEntryOwner.Entry as MobileCollidable;
                if (boundingBoxUpdateable != null)
                {
                    BoundingBoxUpdater.Remove(boundingBoxUpdateable);
                }
            }

            SolverUpdateable solverUpdateable = spaceObject as SolverUpdateable;
            if (solverUpdateable != null)
            {
                Solver.Remove(solverUpdateable);
            }

            IPositionUpdateable integrable = spaceObject as IPositionUpdateable;
            if (integrable != null)
            {
                PositionUpdater.Remove(integrable);
            }

            Entity entity = spaceObject as Entity;
            if (entity != null)
            {
                BufferedStates.Remove(entity);
            }

            IDeferredEventCreator deferredEventCreator = spaceObject as IDeferredEventCreator;
            if (deferredEventCreator != null)
            {
                DeferredEventDispatcher.RemoveEventCreator(deferredEventCreator);
            }

            IDeferredEventCreatorOwner deferredEventCreatorOwner = spaceObject as IDeferredEventCreatorOwner;
            if (deferredEventCreatorOwner != null)
            {
                DeferredEventDispatcher.RemoveEventCreator(deferredEventCreatorOwner.EventCreator);
            }

            //Updateable stages.
            IDuringForcesUpdateable duringForcesUpdateable = spaceObject as IDuringForcesUpdateable;
            if (duringForcesUpdateable != null)
            {
                DuringForcesUpdateables.Remove(duringForcesUpdateable);
            }

            IBeforeNarrowPhaseUpdateable beforeNarrowPhaseUpdateable = spaceObject as IBeforeNarrowPhaseUpdateable;
            if (beforeNarrowPhaseUpdateable != null)
            {
                BeforeNarrowPhaseUpdateables.Remove(beforeNarrowPhaseUpdateable);
            }

            IBeforeSolverUpdateable beforeSolverUpdateable = spaceObject as IBeforeSolverUpdateable;
            if (beforeSolverUpdateable != null)
            {
                BeforeSolverUpdateables.Remove(beforeSolverUpdateable);
            }


            IBeforePositionUpdateUpdateable beforePositionUpdateUpdateable = spaceObject as IBeforePositionUpdateUpdateable;
            if (beforePositionUpdateUpdateable != null)
            {
                BeforePositionUpdateUpdateables.Remove(beforePositionUpdateUpdateable);
            }

            IEndOfTimeStepUpdateable endOfStepUpdateable = spaceObject as IEndOfTimeStepUpdateable;
            if (endOfStepUpdateable != null)
            {
                EndOfTimeStepUpdateables.Remove(endOfStepUpdateable);
            }

            IEndOfFrameUpdateable endOfFrameUpdateable = spaceObject as IEndOfFrameUpdateable;
            if (endOfFrameUpdateable != null)
            {
                EndOfFrameUpdateables.Remove(endOfFrameUpdateable);
            }

            spaceObject.Space = null;
            spaceObject.OnRemovalFromSpace(this);
        }

        void DoTimeStep()
        {

            SpaceObjectBuffer.Update();
            EntityStateWriteBuffer.Update();
            DeactivationManager.Update();
            ForceUpdater.Update();
            DuringForcesUpdateables.Update();
            BoundingBoxUpdater.Update();
            BroadPhase.Update();
            BeforeNarrowPhaseUpdateables.Update();
            NarrowPhase.Update();
            BeforeSolverUpdateables.Update();
            Solver.Update();
            BeforePositionUpdateUpdateables.Update();
            PositionUpdater.Update();
            BufferedStates.ReadBuffers.Update();
            DeferredEventDispatcher.Update();
            EndOfTimeStepUpdateables.Update();


        }

        ///<summary>
        /// Performs a single timestep.
        ///</summary>
        public void Update()
        {
            DoTimeStep();
            EndOfFrameUpdateables.Update();
        }

        /// <summary>
        /// Performs as many timesteps as necessary to get as close to the elapsed time as possible.
        /// </summary>
        /// <param name="dt">Elapsed time from the previous frame.</param>
        public void Update(float dt)
        {
            TimeStepSettings.AccumulatedTime += dt;
            for (int i = 0; i < TimeStepSettings.MaximumTimeStepsPerFrame; i++)
            {
                if (TimeStepSettings.AccumulatedTime >= TimeStepSettings.TimeStepDuration)
                {
                    TimeStepSettings.AccumulatedTime -= TimeStepSettings.TimeStepDuration;
                    DoTimeStep();
                }
                else
                {
                    break;
                }
            }

            BufferedStates.InterpolatedStates.BlendAmount = TimeStepSettings.AccumulatedTime / TimeStepSettings.TimeStepDuration;
            BufferedStates.InterpolatedStates.Update();
            EndOfFrameUpdateables.Update();
        }

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, out RayCastResult result)
        {
            return RayCast(ray, float.MaxValue, out result);
        }

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, Func<BroadPhaseEntry, bool> filter, out RayCastResult result)
        {
            return RayCast(ray, float.MaxValue, filter, out result);
        }

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, float maximumLength, out RayCastResult result)
        {
            var resultsList = Resources.GetRayCastResultList();
            bool didHit = RayCast(ray, maximumLength, resultsList);
            result = resultsList.Elements[0];
            for (int i = 1; i < resultsList.count; i++)
            {
                RayCastResult candidate = resultsList.Elements[i];
                if (candidate.HitData.T < result.HitData.T)
                    result = candidate;
            }
            Resources.GiveBack(resultsList);

            return didHit;
        }

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, float maximumLength, Func<BroadPhaseEntry, bool> filter, out RayCastResult result)
        {
            var resultsList = Resources.GetRayCastResultList();
            bool didHit = RayCast(ray, maximumLength, filter, resultsList);
            result = resultsList.Elements[0];
            for (int i = 1; i < resultsList.count; i++)
            {
                RayCastResult candidate = resultsList.Elements[i];
                if (candidate.HitData.T < result.HitData.T)
                    result = candidate;
            }
            Resources.GiveBack(resultsList);

            return didHit;
        }

        /// <summary>
        /// Tests a ray against the space, possibly returning multiple hits.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="outputRayCastResults">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, float maximumLength, IList<RayCastResult> outputRayCastResults)
        {
            var outputIntersections = Resources.GetBroadPhaseEntryList();
            if (BroadPhase.QueryAccelerator.RayCast(ray, maximumLength, outputIntersections))
            {

                for (int i = 0; i < outputIntersections.count; i++)
                {
                    RayHit rayHit;
                    BroadPhaseEntry candidate = outputIntersections.Elements[i];
                    if (candidate.RayCast(ray, maximumLength, out rayHit))
                    {
                        outputRayCastResults.Add(new RayCastResult(rayHit, candidate));
                    }
                }
            }
            Resources.GiveBack(outputIntersections);
            return outputRayCastResults.Count > 0;
        }

        /// <summary>
        /// Tests a ray against the space, possibly returning multiple hits.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="outputRayCastResults">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        public bool RayCast(Ray ray, float maximumLength, Func<BroadPhaseEntry, bool> filter, IList<RayCastResult> outputRayCastResults)
        {
            var outputIntersections = Resources.GetBroadPhaseEntryList();
            if (BroadPhase.QueryAccelerator.RayCast(ray, maximumLength, outputIntersections))
            {

                for (int i = 0; i < outputIntersections.count; i++)
                {
                    RayHit rayHit;
                    BroadPhaseEntry candidate = outputIntersections.Elements[i];
                    if (candidate.RayCast(ray, maximumLength, filter, out rayHit))
                    {
                        outputRayCastResults.Add(new RayCastResult(rayHit, candidate));
                    }
                }
            }
            Resources.GiveBack(outputIntersections);
            return outputRayCastResults.Count > 0;
        }


        bool disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                ThreadManager.Dispose();
            }
        }
    }


}
