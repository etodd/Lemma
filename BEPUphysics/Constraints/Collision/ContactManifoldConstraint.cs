using BEPUphysics.Constraints.SolverGroups;
using BEPUphysics.DataStructures;
using BEPUphysics.Entities;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Materials;
using BEPUphysics.SolverSystems;

namespace BEPUphysics.Constraints.Collision
{
    ///<summary>
    /// Superclass of collision constraints that include multiple contact subconstraints.
    ///</summary>
    public abstract class ContactManifoldConstraint : SolverGroup
    {

        internal InteractionProperties materialInteraction;
        ///<summary>
        /// Gets or sets the material-blended properties used by this constraint.
        ///</summary>
        public InteractionProperties MaterialInteraction
        {
            get
            {
                return materialInteraction;
            }
            set
            {
                materialInteraction = value;
            }
        }

        protected Entity entityA;
        ///<summary>
        /// Gets the first entity associated with the manifold.
        ///</summary>
        public Entity EntityA { get { return entityA; } }
        protected Entity entityB;
        ///<summary>
        /// Gets the second entity associated with the manifold.
        ///</summary>
        public Entity EntityB { get { return entityB; } }

        protected internal CollidablePairHandler pair;

        ///<summary>
        /// Gets the pair handler owning this constraint.
        ///</summary>
        public CollidablePairHandler Pair
        {
            get
            {
                return pair;
            }

        }

        protected internal override void OnInvolvedEntitiesChanged()
        {
            //The default implementation of this method is pretty complicated.
            //This is a special constraint that has certain guarantees that allow a simpler method to be used.
            //This updates the involved entities and connected members lists, but does not update references.
            //It does not have to update references because this 'constraint' can never change entities while belonging to a solver.
            //It doesn't even need to notify the parent solvergroup.
            CollectInvolvedEntities();
        }
        protected internal override void CollectInvolvedEntities(RawList<Entity> outputInvolvedEntities)
        {
            //The default implementation for solver groups looks at every single subconstraint.
            //That's not necessary for these special constraints.
            if (entityA != null)
                outputInvolvedEntities.Add(entityA);
            if (entityB != null)
                outputInvolvedEntities.Add(entityB);
        }

        ///<summary>
        /// Adds a contact to be managed by the constraint.
        ///</summary>
        ///<param name="contact">Contact to add.</param>
        public abstract void AddContact(Contact contact);

        ///<summary>
        /// Removes a contact from the constraint.
        ///</summary>
        ///<param name="contact">Contact to remove.</param>
        public abstract void RemoveContact(Contact contact);

        ///<summary>
        /// Initializes the constraint.
        ///</summary>
        ///<param name="a">First entity of the pair.</param>
        ///<param name="b">Second entity of the pair.</param>
        ///<param name="newPair">Pair owning this constraint.</param>
        public virtual void Initialize(Entity a, Entity b, CollidablePairHandler newPair)
        {
            //This should only be called before the constraint has been added to the solver.
            entityA = a;
            entityB = b;
            pair = newPair;
            OnInvolvedEntitiesChanged();
        }

        ///<summary>
        /// Cleans up the constraint.
        ///</summary>
        public abstract void CleanUp();

        protected internal void CleanUpReferences()
        {
            entityA = null;
            entityB = null;

            OnInvolvedEntitiesChanged();
        }


        /// <summary>
        /// Called when the updateable is removed from its solver.
        /// </summary>
        /// <param name="oldSolver">Solver from which the updateable was removed.</param>
        public override void OnRemovalFromSolver(Solver oldSolver)
        {
            //This should only be called after the constraint has been removed from the solver.
            if (pair == null)
            {
                CleanUpReferences();
            }
        }

        /// <summary>
        /// Sets the activity state of the constraint based on the activity state of its connections.
        /// Called automatically by the space owning a constaint.  If a constraint is a sub-constraint that hasn't been directly added to the space,
        /// this may need to be called alongside the preStep from within the parent constraint.
        /// </summary>
        public override void UpdateSolverActivity()
        {

            if (isActive)
            {
                var aValid = entityA != null && entityA.isDynamic;
                isActiveInSolver = pair.BroadPhaseOverlap.collisionRule < CollisionRule.NoSolver &&
                                   ((entityA != null && entityA.isDynamic && entityA.activityInformation.IsActive) || //At least one of the objects must be an active dynamic entity.
                                   (entityB != null && entityB.isDynamic && entityB.activityInformation.IsActive));
                for (int i = 0; i < solverUpdateables.count; i++)
                {
                    solverUpdateables.Elements[i].isActiveInSolver = solverUpdateables.Elements[i].isActive && isActiveInSolver;
                }
            }
            else
                isActiveInSolver = false;
            

        }

        ///<summary>
        /// Updates the material properties associated with the constraint.
        ///</summary>
        ///<param name="materialA">Material associated with the first entity of the pair.</param>
        ///<param name="materialB">Material associated with the second entity of the pair.</param>
        public void UpdateMaterialProperties(Material materialA, Material materialB)
        {
            if (materialA != null && materialB != null)
                MaterialManager.GetInteractionProperties(materialA, materialB, out materialInteraction);
            else if (materialA == null && materialB != null)
            {
                materialInteraction.KineticFriction = materialB.kineticFriction;
                materialInteraction.StaticFriction = materialB.staticFriction;
                materialInteraction.Bounciness = materialB.bounciness;
            }
            else if (materialA != null)
            {
                materialInteraction.KineticFriction = materialA.kineticFriction;
                materialInteraction.StaticFriction = materialA.staticFriction;
                materialInteraction.Bounciness = materialA.bounciness;
            }
            else
            {
                materialInteraction.KineticFriction = 0;
                materialInteraction.StaticFriction = 0;
                materialInteraction.Bounciness = 0;
            }
        }

    }
}
