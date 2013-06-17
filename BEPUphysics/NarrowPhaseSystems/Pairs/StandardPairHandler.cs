using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.Materials;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a standard pair handler that has a direct manifold and constraint.
    ///</summary>
    public abstract class StandardPairHandler : CollidablePairHandler
    {

        /// <summary>
        /// Gets the contact manifold used by the pair handler.
        /// </summary>
        public abstract ContactManifold ContactManifold { get; }
        /// <summary>
        /// Gets the contact constraint usd by the pair handler.
        /// </summary>
        public abstract ContactManifoldConstraint ContactConstraint { get; }

        ///<summary>
        /// Constructs a pair handler.
        ///</summary>
        protected StandardPairHandler()
        {
            //Child type constructors construct manifold and constraint first.
            ContactManifold.ContactAdded += OnContactAdded;
            ContactManifold.ContactRemoved += OnContactRemoved;
        }

        protected override void OnContactAdded(Contact contact)
        {
            ContactConstraint.AddContact(contact);
            base.OnContactAdded(contact);

        }

        protected override void OnContactRemoved(Contact contact)
        {
            ContactConstraint.RemoveContact(contact);
            base.OnContactRemoved(contact);

        }



        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            //Child initialization is responsible for setting up the entries.

            ContactManifold.Initialize(CollidableA, CollidableB);
            ContactConstraint.Initialize(EntityA, EntityB, this);

            base.Initialize(entryA, entryB);



        }

        ///<summary>
        /// Forces an update of the pair's material properties.
        ///</summary>
        public override void UpdateMaterialProperties(Material a, Material b)
        {
            ContactConstraint.UpdateMaterialProperties(
                a ?? (EntityA == null ? null : EntityA.material),
                b ?? (EntityB == null ? null : EntityB.material));
        }

        /// <summary>
        /// Updates the material interaction properties of the pair handler's constraint.
        /// </summary>
        /// <param name="properties">Properties to use.</param>
        public override void UpdateMaterialProperties(InteractionProperties properties)
        {
            ContactConstraint.MaterialInteraction = properties;
        }




        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {
            //Deal with the remaining contacts.
            for (int i = ContactManifold.contacts.Count - 1; i >= 0; i--)
            {
                OnContactRemoved(ContactManifold.contacts[i]);
            }

            //If the constraint is still in the solver, then request to have it removed.
            if (ContactConstraint.solver != null)
            {
                ContactConstraint.pair = null; //Setting the pair to null tells the constraint that it's going to be orphaned.  It will be cleaned up on removal.
                if (Parent != null)
                    Parent.RemoveSolverUpdateable(ContactConstraint);
                else if (NarrowPhase != null)
                    NarrowPhase.NotifyUpdateableRemoved(ContactConstraint);
            }
            else
            {
                ContactConstraint.CleanUpReferences();//The constraint isn't in the solver, so we can safely clean it up directly.
                //Even though it's not in the solver, we still may need to notify the parent to remove it.
                if (Parent != null && ContactConstraint.SolverGroup != null)
                    Parent.RemoveSolverUpdateable(ContactConstraint);
            }

            ContactConstraint.CleanUp();


            base.CleanUp();

            ContactManifold.CleanUp();


            //Child cleanup is responsible for cleaning up direct references to the involved collidables.
        }



        ///<summary>
        /// Updates the pair handler.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void UpdateCollision(float dt)
        {
            //Cache some properties.
            var a = CollidableA;
            var b = CollidableB;
            var triggerA = a.EventTriggerer;
            var triggerB = b.EventTriggerer;

            if (!suppressEvents)
            {
                triggerA.OnPairUpdated(b, this);
                triggerB.OnPairUpdated(a, this);
            }

            ContactManifold.Update(dt);

            if (ContactManifold.contacts.Count > 0)
            {
                if (!suppressEvents)
                {
                    triggerA.OnPairTouching(b, this);
                    triggerB.OnPairTouching(a, this);
                }

                if (previousContactCount == 0)
                {
                    //New collision.

                    //Add a solver item.
                    if (Parent != null)
                        Parent.AddSolverUpdateable(ContactConstraint);
                    else if (NarrowPhase != null)
                        NarrowPhase.NotifyUpdateableAdded(ContactConstraint);

                    //And notify the pair members.
                    if (!suppressEvents)
                    {
                        triggerA.OnInitialCollisionDetected(b, this);
                        triggerB.OnInitialCollisionDetected(a, this);
                    }
                }
            }
            else if (previousContactCount > 0)
            {
                //Just exited collision.

                //Remove the solver item.
                if (Parent != null)
                    Parent.RemoveSolverUpdateable(ContactConstraint);
                else if (NarrowPhase != null)
                    NarrowPhase.NotifyUpdateableRemoved(ContactConstraint);

                if (!suppressEvents)
                {
                    triggerA.OnCollisionEnded(b, this);
                    triggerB.OnCollisionEnded(a, this);
                }
            }

            previousContactCount = ContactManifold.contacts.Count;

        }


        /// <summary>
        /// Gets the number of contacts associated with this pair handler.
        /// </summary>
        protected internal override int ContactCount
        {
            get { return ContactManifold.contacts.Count; }
        }

        /// <summary>
        /// Clears the contacts associated with this pair handler.
        /// </summary>
        public override void ClearContacts()
        {
            if (previousContactCount > 0)
            {
                //Just exited collision.

                //Remove the solver item.
                if (Parent != null)
                    Parent.RemoveSolverUpdateable(ContactConstraint);
                else if (NarrowPhase != null)
                    NarrowPhase.NotifyUpdateableRemoved(ContactConstraint);

                if (!suppressEvents)
                {
                    var a = CollidableA;
                    var b = CollidableB;
                    a.EventTriggerer.OnCollisionEnded(b, this);
                    b.EventTriggerer.OnCollisionEnded(a, this);
                }
            }
            ContactManifold.ClearContacts();
            base.ClearContacts();
        }

    }

}
