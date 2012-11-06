using System.Collections.Generic;
using BEPUphysics.Constraints;
using System.Collections.ObjectModel;
using BEPUphysics.DeactivationManagement;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;

namespace BEPUphysics.SolverSystems
{
    ///<summary>
    /// Superclass of all objects that live in the solver.
    ///</summary>
    public abstract class SolverUpdateable : ISimulationIslandConnectionOwner, ISpaceObject
    {
        internal int solverIndex;

        protected internal Solver solver;
        ///<summary>
        /// Gets the solver to which the solver updateable belongs.
        ///</summary>
        public virtual Solver Solver
        {
            get
            {
                return solver;
            }
            internal set
            {
                solver = value;
            }
        }

        protected SolverUpdateable()
        {
            //Initialize the connection.
            //It will usually be overridden and end up floating on back to the resource pool.
            simulationIslandConnection = Resources.GetSimulationIslandConnection();
            simulationIslandConnection.Owner = this;
        }

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public abstract void Update(float dt);

        //Will be locked if necessary by the solver.
        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public abstract void ExclusiveUpdate();

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public abstract float SolveIteration();

        protected internal SolverSettings solverSettings = new SolverSettings();
        ///<summary>
        /// Gets the solver settings that manage how the solver updates.
        ///</summary>
        public SolverSettings SolverSettings
        {
            get { return solverSettings; }
        }

        protected internal bool isActive = true;
        /// <summary>
        /// Gets or sets whether or not this solver updateable is active.
        /// 
        /// When set to false, this solver updateable will be idle and its 
        /// isActiveInSolver field will always be false.
        /// 
        /// When set to true, the solver updateable will run normally and update if
        /// the type's activity conditions allow it.
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        protected internal bool isActiveInSolver = true;
        /// <summary>
        /// Gets whether or not the space's solver should try to solve this object.
        /// Depends on conditions specific to each solver updateable type and whether or not
        /// it has completed its computations early.  Recomputed each frame.
        /// </summary>
        public bool IsActiveInSolver
        {
            get
            {
                return isActiveInSolver;
            }
        }

        /// <summary>
        /// Attempts to acquire a lock on the solver updateable.
        /// This allows operations that need exclusive access to the solver updateable's members.
        /// If it is contested, it aborts the attempt.
        /// </summary>
        /// <returns>Whether or not the lock could be acquired.</returns>
        public abstract bool TryEnterLock();
        /// <summary>
        /// Acquires a lock on the solver updateable.
        /// This allows operations that need exclusive access to the solver updateable's members.
        /// </summary>
        public abstract void EnterLock();
        /// <summary>
        /// Releases the lock on the solver updateable.
        /// </summary>
        public abstract void ExitLock();


        /// <summary>
        /// Updates the activity state of the solver updateable based on its members.
        /// </summary>
        public virtual void UpdateSolverActivity()
        {
            if (isActive)
            {
                //This is a simulation island connection.  We already know that all connected objects share the
                //same simulation island (or don't have one, in the case of kinematics).  All we have to do is test to see if that island is active!
                for (int i = 0; i < simulationIslandConnection.entries.count; i++)
                {
                    var island = simulationIslandConnection.entries.Elements[i].Member.SimulationIsland;
                    if (island != null && island.isActive)
                    {
                        isActiveInSolver = true;
                        return;
                    }
                }
            }
            isActiveInSolver = false;
        }



        protected internal ISpace space;
        ISpace ISpaceObject.Space
        {
            get
            {
                return space;
            }
            set
            {
                space = value;
            }
        }

        /// <summary>
        /// Called after the object is added to a space.
        /// </summary>
        /// <param name="newSpace"></param>
        public virtual void OnAdditionToSpace(ISpace newSpace)
        {
        }

        /// <summary>
        /// Called before an object is removed from its space.
        /// </summary>
        public virtual void OnRemovalFromSpace(ISpace oldSpace)
        {
        }

        ///<summary>
        /// Called when the updateable is added to a solver.
        ///</summary>
        ///<param name="newSolver">Solver to which the updateable was added.</param>
        public virtual void OnAdditionToSolver(Solver newSolver)
        {
        }

        /// <summary>
        /// Called when the updateable is removed from its solver.
        /// </summary>
        /// <param name="oldSolver">Solver from which the updateable was removed.</param>
        public virtual void OnRemovalFromSolver(Solver oldSolver)
        {
        }

        /// <summary>
        /// Gets or sets the user data associated with this object.
        /// </summary>
        public object Tag { get; set; }


        protected internal SimulationIslandConnection simulationIslandConnection;

        /// <summary>
        /// Gets the simulation island connection associated with this updateable.
        /// </summary>
        public SimulationIslandConnection SimulationIslandConnection
        {
            get { return simulationIslandConnection; }
        }
    }
}
