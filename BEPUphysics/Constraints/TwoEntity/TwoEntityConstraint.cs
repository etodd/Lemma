using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;

namespace BEPUphysics.Constraints.TwoEntity
{
    /// <summary>
    /// Abstract superclass of constraints involving two bodies.
    /// </summary>
    public abstract class TwoEntityConstraint : EntitySolverUpdateable
    {
        /// <summary>
        /// Entity that constraints connect to when they are given a null connection.
        /// </summary>
        public static readonly Entity WorldEntity = new Sphere(Vector3.Zero, 0);

        /// <summary>
        /// First connection to the constraint.
        /// </summary>
        protected internal Entity connectionA;


        /// <summary>
        /// Second connection to the constraint.
        /// </summary>
        protected internal Entity connectionB;


        /// <summary>
        /// Gets or sets the first connection to the constraint.
        /// </summary>
        public Entity ConnectionA
        {
            get
            {
                //if (myConnectionA == nullSphere)
                //    return null;
                return connectionA;
            }
            set
            {
                connectionA = value ?? WorldEntity;
                OnInvolvedEntitiesChanged();
            }
        }

        /// <summary>
        /// Gets or sets the second connection to the constraint.
        /// </summary>
        public Entity ConnectionB
        {
            get
            {
                //if (myConnectionB == nullSphere)
                //    return null;
                return connectionB;
            }
            set
            {
                connectionB = value ?? WorldEntity;
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
            if (connectionA != null && connectionA != WorldEntity)
                outputInvolvedEntities.Add(connectionA);

            if (connectionB != null && connectionB != WorldEntity)
                outputInvolvedEntities.Add(connectionB);
        }


    }
}