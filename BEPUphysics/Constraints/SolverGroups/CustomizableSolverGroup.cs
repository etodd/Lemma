namespace BEPUphysics.Constraints.SolverGroups
{
    /// <summary>
    /// Constraint made from other constraints.
    /// Putting constraints into a solver group can help with organization and, in some cases, performance.
    /// 
    /// If you have multiple constraints between the same two entities, putting the constraints into a 
    /// CustomizableSolverGroup can lower lock contention.
    /// 
    /// Be careful about overloading a single solvergroup; it should be kept relatively small to ensure that the multithreading loads stay balanced.
    /// </summary>
    public class CustomizableSolverGroup : SolverGroup
    {
        /// <summary>
        /// Adds a new solver updateable to the solver group.
        /// </summary>
        /// <param name="solverUpdateable">Solver updateable to add.</param>
        public new void Add(EntitySolverUpdateable solverUpdateable)
        {
            base.Add(solverUpdateable);
        }

        /// <summary>
        /// Removes a solver updateable from the solver group.
        /// </summary>
        /// <param name="solverUpdateable">Solver updateable to remove.</param>
        public new void Remove(EntitySolverUpdateable solverUpdateable)
        {
            base.Remove(solverUpdateable);
        }
    }
}