using System;
using BEPUphysics.DeactivationManagement;
using BEPUphysics.Threading;
using BEPUphysics.Constraints;
using BEPUutilities;
using BEPUutilities.DataStructures;

namespace BEPUphysics.SolverSystems
{
    ///<summary>
    /// Iteratively solves solver updateables, converging to a solution for simulated joints and collision pair contact constraints.
    ///</summary>
    public class Solver : MultithreadedProcessingStage
    {
        RawList<SolverUpdateable> solverUpdateables = new RawList<SolverUpdateable>();
        internal int iterationLimit = 10;
        ///<summary>
        /// Gets or sets the maximum number of iterations the solver will attempt to use to solve the simulation's constraints.
        ///</summary>
        public int IterationLimit { get { return iterationLimit; } set { iterationLimit = Math.Max(value, 0); } }
        ///<summary>
        /// Gets the list of solver updateables in the solver.
        ///</summary>
        public ReadOnlyList<SolverUpdateable> SolverUpdateables
        {
            get
            {
                return new ReadOnlyList<SolverUpdateable>(solverUpdateables);
            }
        }
        protected internal TimeStepSettings timeStepSettings;
        ///<summary>
        /// Gets or sets the time step settings used by the solver.
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
            }
        }

        ///<summary>
        /// Gets or sets the deactivation manager used by the solver.
        /// When constraints are added and removed, the deactivation manager
        /// gains and loses simulation island connections that affect simulation islands
        /// and activity states.
        ///</summary>
        public DeactivationManager DeactivationManager { get; set; }

        /// <summary>
        /// Gets the permutation mapper used by the solver.
        /// </summary>
        public PermutationMapper PermutationMapper { get; private set; }

        ///<summary>
        /// Constructs a Solver.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings used by the solver.</param>
        ///<param name="deactivationManager">Deactivation manager used by the solver.</param>
        public Solver(TimeStepSettings timeStepSettings, DeactivationManager deactivationManager)
        {
            TimeStepSettings = timeStepSettings;
            DeactivationManager = deactivationManager;
            multithreadedPrestepDelegate = MultithreadedPrestep;
            multithreadedIterationDelegate = MultithreadedIteration;
            Enabled = true;
            PermutationMapper = new PermutationMapper();
        }
        ///<summary>
        /// Constructs a Solver.
        ///</summary>
        ///<param name="timeStepSettings">Time step settings used by the solver.</param>
        ///<param name="deactivationManager">Deactivation manager used by the solver.</param>
        /// <param name="threadManager">Thread manager used by the solver.</param>
        public Solver(TimeStepSettings timeStepSettings, DeactivationManager deactivationManager, IThreadManager threadManager)
            : this(timeStepSettings, deactivationManager)
        {
            ThreadManager = threadManager;
            AllowMultithreading = true;
        }

        ///<summary>
        /// Adds a solver updateable to the solver.
        ///</summary>
        ///<param name="item">Updateable to add.</param>
        ///<exception cref="ArgumentException">Thrown when the item already belongs to a solver.</exception>
        public void Add(SolverUpdateable item)
        {
            if (item.Solver == null)
            {
                item.Solver = this;
                item.solverIndex = solverUpdateables.Count;
                solverUpdateables.Add(item);
                DeactivationManager.Add(item.simulationIslandConnection);
                item.OnAdditionToSolver(this);
            }
            else
                throw new ArgumentException("Solver updateable already belongs to something; it can't be added.", "item");
        }
        ///<summary>
        /// Removes a solver updateable from the solver.
        ///</summary>
        ///<param name="item">Updateable to remove.</param>
        ///<exception cref="ArgumentException">Thrown when the item does not belong to the solver.</exception>
        public void Remove(SolverUpdateable item)
        {

            if (item.Solver == this)
            {

                item.Solver = null;
                solverUpdateables.Count--;
                if (item.solverIndex < solverUpdateables.Count)
                {
                    //The solver updateable isn't the last element, so put the last element in its place.
                    solverUpdateables.Elements[item.solverIndex] = solverUpdateables.Elements[solverUpdateables.Count];
                    //Update the replacement's solver index to its new location.
                    solverUpdateables.Elements[item.solverIndex].solverIndex = item.solverIndex;
                }
                solverUpdateables.Elements[solverUpdateables.Count] = null;


                DeactivationManager.Remove(item.simulationIslandConnection);
                item.OnRemovalFromSolver(this);
            }

            else
                throw new ArgumentException("Solver updateable doesn't belong to this solver; it can't be removed.", "item");

        }




        Action<int> multithreadedPrestepDelegate;
        void MultithreadedPrestep(int i)
        {
            var updateable = solverUpdateables.Elements[i];
            updateable.UpdateSolverActivity();
            if (updateable.isActiveInSolver)
            {
                updateable.SolverSettings.currentIterations = 0;
                updateable.SolverSettings.iterationsAtZeroImpulse = 0;
                updateable.Update(timeStepSettings.TimeStepDuration);

                updateable.EnterLock();
                try
                {
                    updateable.ExclusiveUpdate();
                }
                finally
                {
                    updateable.ExitLock();
                }
            }
        }


        Action<int> multithreadedIterationDelegate;
        void MultithreadedIteration(int i)
        {
            //'i' is currently an index into an implicit array of solver updateables that goes from 0 to solverUpdateables.count * iterationLimit.
            //It includes iterationLimit copies of each updateable.
            //Permute the entire set with duplicates.
            var updateable = solverUpdateables.Elements[PermutationMapper.GetMappedIndex(i, solverUpdateables.Count)];


            SolverSettings solverSettings = updateable.solverSettings;
            //Updateables only ever go from active to inactive during iterations,
            //so it's safe to check for activity before we do hard (synchronized) work.
            if (updateable.isActiveInSolver)
            {
                int incrementedIterations = -1;
                updateable.EnterLock();
                //This duplicate test protects against the possibility that the updateable went inactive between the first check and the lock.
                if (updateable.isActiveInSolver)
                {
                    if (updateable.SolveIteration() < solverSettings.minimumImpulse)
                    {
                        solverSettings.iterationsAtZeroImpulse++;
                        if (solverSettings.iterationsAtZeroImpulse > solverSettings.minimumIterationCount)
                            updateable.isActiveInSolver = false;
                    }
                    else
                    {
                        solverSettings.iterationsAtZeroImpulse = 0;
                    }

                    //Increment the iteration count.
                    incrementedIterations = solverSettings.currentIterations++;
                }
                updateable.ExitLock();
                //Since the updateables only ever go from active to inactive, it's safe to check outside of the lock.
                //Keeping this if statement out of the lock allows other waiters to get to work a few nanoseconds faster.
                if (incrementedIterations > iterationLimit ||
                    incrementedIterations > solverSettings.maximumIterationCount)
                {
                    updateable.isActiveInSolver = false;
                }

            }



        }

        protected override void UpdateMultithreaded()
        {
            ThreadManager.ForLoop(0, solverUpdateables.Count, multithreadedPrestepDelegate);
            ++PermutationMapper.PermutationIndex;
            ThreadManager.ForLoop(0, iterationLimit * solverUpdateables.Count, multithreadedIterationDelegate);
        }

        protected override void UpdateSingleThreaded()
        {

            int totalUpdateableCount = solverUpdateables.Count;
            for (int i = 0; i < totalUpdateableCount; i++)
            {
                UnsafePrestep(solverUpdateables.Elements[i]);
            }

            int totalCount = iterationLimit * totalUpdateableCount;
            ++PermutationMapper.PermutationIndex;
            for (int i = 0; i < totalCount; i++)
            {
                UnsafeSolveIteration(solverUpdateables.Elements[PermutationMapper.GetMappedIndex(i, totalUpdateableCount)]);
            }


        }

        protected internal void UnsafePrestep(SolverUpdateable updateable)
        {
            updateable.UpdateSolverActivity();
            if (updateable.isActiveInSolver)
            {
                SolverSettings solverSettings = updateable.solverSettings;
                solverSettings.currentIterations = 0;
                solverSettings.iterationsAtZeroImpulse = 0;
                updateable.Update(timeStepSettings.TimeStepDuration);
                updateable.ExclusiveUpdate();
            }
        }

        protected internal void UnsafeSolveIteration(SolverUpdateable updateable)
        {
            if (updateable.isActiveInSolver)
            {
                SolverSettings solverSettings = updateable.solverSettings;


                solverSettings.currentIterations++;
                if (solverSettings.currentIterations <= iterationLimit &&
                    solverSettings.currentIterations <= solverSettings.maximumIterationCount)
                {
                    if (updateable.SolveIteration() < solverSettings.minimumImpulse)
                    {
                        solverSettings.iterationsAtZeroImpulse++;
                        if (solverSettings.iterationsAtZeroImpulse > solverSettings.minimumIterationCount)
                            updateable.isActiveInSolver = false;

                    }
                    else
                    {
                        solverSettings.iterationsAtZeroImpulse = 0;
                    }
                }
                else
                {
                    updateable.isActiveInSolver = false;
                }

            }
        }


    }
}
