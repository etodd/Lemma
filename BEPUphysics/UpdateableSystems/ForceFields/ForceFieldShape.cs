using System.Collections.Generic;
using BEPUphysics.Entities;
using BEPUphysics.DataStructures;

namespace BEPUphysics.UpdateableSystems.ForceFields
{
    /// <summary>
    /// Superclass of force field shapes that test whether or not entities are affected by a forcefield.
    /// </summary>
    public abstract class ForceFieldShape
    {
        /// <summary>
        /// Force field associated with this shape.
        /// </summary>
        public ForceField ForceField { get; internal set; }


        /// <summary>
        /// Uses an efficient query to see what entities may be affected.
        /// Usually uses a broadphase bounding box query.
        /// </summary>
        /// <returns>Possibly affected entities.</returns>
        public abstract IList<Entity> GetPossiblyAffectedEntities();

        /// <summary>
        /// Performs a narrow-phase test to see if an entity is affected by the force field.
        /// </summary>
        /// <param name="entity">Entity to test.</param>
        /// <returns>Whether or not the entity is affected.</returns>
        public abstract bool IsEntityAffected(Entity entity);
    }
}