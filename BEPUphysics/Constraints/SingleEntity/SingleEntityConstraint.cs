using BEPUphysics.Entities;
using BEPUphysics.DataStructures;

namespace BEPUphysics.Constraints.SingleEntity
{
    /// <summary>
    /// Abstract superclass of constraints which control a single entity.
    /// </summary>
    public abstract class SingleEntityConstraint : EntitySolverUpdateable
    {
        /// <summary>
        /// Number of frames so far at effectively zero corrective impulse.
        /// Set to zero during every preStep(float dt) call and incremented by checkForEarlyOutIterations(Vector3 impulse).
        /// </summary>
        protected int iterationsAtZeroImpulse;

        /// <summary>
        /// Entity affected by the constraint.
        /// </summary>
        protected internal Entity entity;

        /// <summary>
        /// Gets or sets the entity affected by the constraint.
        /// </summary>
        public virtual Entity Entity
        {
            get { return entity; }
            set
            {
                //TODO: Should this clear accumulated impulses?
                //For constraints too...
                entity = value;
                OnInvolvedEntitiesChanged();
            }
        }


        /// <summary>
        /// Adds entities associated with the solver item to the involved entities list.
        /// Ensure that sortInvolvedEntities() is called at the end of the function.
        /// This allows the non-batched multithreading system to lock properly.
        /// </summary>
        protected internal override void CollectInvolvedEntities(RawList<Entity> outputInvolvedEntities)
        {
            if (entity != null) //sometimes, the entity is set to null to 'deactivate' it.  Don't add null to the involved entities list.
                outputInvolvedEntities.Add(entity);
        }

    }
}