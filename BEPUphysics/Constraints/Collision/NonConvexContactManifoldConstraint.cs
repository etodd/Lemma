using BEPUphysics.CollisionTests;
using System.Collections.ObjectModel;
using BEPUphysics.ResourceManagement;
using BEPUphysics.DataStructures;
using System.Collections.Generic;

namespace BEPUphysics.Constraints.Collision
{
    ///<summary>
    /// Collision constraint for non-convex manifolds.  These manifolds are usually used in cases
    /// where the contacts are coming from multiple objects or from non-convex objects.  The normals
    /// will likely face more than one direction.
    ///</summary>
    public class NonConvexContactManifoldConstraint : ContactManifoldConstraint
    {
        //Unlike the convex manifold constraint, this constraint enforces no requirements
        //on the contact data.  The collisions can form a nonconvex patch.  They can have differing normals.
        //This is required for proper collision handling on large structures

        //The solver group is composed of multiple constraints.
        //One pentration constraint for each contact.
        //One friction constraint for each contact.

        internal RawList<ContactPenetrationConstraint> penetrationConstraints;
        ///<summary>
        /// Gets the penetration constraints in the manifold.
        ///</summary>
        public ReadOnlyList<ContactPenetrationConstraint> ContactPenetrationConstraints
        {
            get
            {
                return new ReadOnlyList<ContactPenetrationConstraint>(penetrationConstraints);
            }
        }

        Stack<ContactPenetrationConstraint> penetrationConstraintPool = new Stack<ContactPenetrationConstraint>(4);

        internal RawList<ContactFrictionConstraint> frictionConstraints;
        ///<summary>
        /// Gets the friction constraints in the manifold.
        ///</summary>
        public ReadOnlyList<ContactFrictionConstraint> ContactFrictionConstraints
        {
            get
            {
                return new ReadOnlyList<ContactFrictionConstraint>(frictionConstraints);
            }
        }

        Stack<ContactFrictionConstraint> frictionConstraintPool = new Stack<ContactFrictionConstraint>(4);


        ///<summary>
        /// Constructs a new nonconvex manifold constraint.
        ///</summary>
        public NonConvexContactManifoldConstraint()
        {
            //All of the constraints are always in the solver group.  Some of them are just deactivated sometimes.
            //This reduces some bookkeeping complications.


            penetrationConstraints = new RawList<ContactPenetrationConstraint>(4);
            frictionConstraints = new RawList<ContactFrictionConstraint>(4);

            for (int i = 0; i < 4; i++)
            {
                var penetrationConstraint = new ContactPenetrationConstraint();
                penetrationConstraintPool.Push(penetrationConstraint);
                Add(penetrationConstraint);

                var frictionConstraint = new ContactFrictionConstraint();
                frictionConstraintPool.Push(frictionConstraint);
                Add(frictionConstraint);
            }
            
        }


        ///<summary>
        /// Cleans up the constraint.
        ///</summary>
        public override void CleanUp()
        {
            //Deactivate any remaining constraints.
            for (int i = penetrationConstraints.count - 1; i >= 0; i--)
            {
                var penetrationConstraint = penetrationConstraints.Elements[i];
                penetrationConstraint.CleanUp();
                penetrationConstraints.RemoveAt(i);
                penetrationConstraintPool.Push(penetrationConstraint);
            }

            for (int i = frictionConstraints.count - 1; i >= 0; i--)
            {
                var frictionConstraint = frictionConstraints.Elements[i];
                frictionConstraint.CleanUp();
                frictionConstraints.RemoveAt(i);
                frictionConstraintPool.Push(frictionConstraint);
            }


        }


 




        //TODO: PROBLEM IS that the add contact/remove contact, when they go from 0 -> !0 or !0 -> 0, the whole constraint is added/removed from the solver.
        //The Added/Removed contact methods here will run ambiguously before or after they are removed from the solver.
        //That ambiguous order doesn't really matter though, since everything that these add/remove methods do is local to this solver object and its children.
        //It doesn't go out and modify any external values on referenced entities.  That only happens when it's added or removed from the solver by whatever owns this object!

        //To avoid ANY ambiguity, some third party is now responsible for adding and removing contacts from this.

        ///<summary>
        /// Adds a contact to be managed by the constraint.
        ///</summary>
        ///<param name="contact">Contact to add.</param>
        public override void AddContact(Contact contact)
        {
            var penetrationConstraint = penetrationConstraintPool.Pop();
            penetrationConstraint.Setup(this, contact);
            penetrationConstraints.Add(penetrationConstraint);

            var frictionConstraint = frictionConstraintPool.Pop();
            frictionConstraint.Setup(this, penetrationConstraint);
            frictionConstraints.Add(frictionConstraint);

        }

        ///<summary>
        /// Removes a contact from the constraint.
        ///</summary>
        ///<param name="contact">Contact to remove.</param>
        public override void RemoveContact(Contact contact)
        {

            ContactPenetrationConstraint penetrationConstraint = null;
            for (int i = 0; i < penetrationConstraints.count; i++)
            {
                if ((penetrationConstraint = penetrationConstraints.Elements[i]).contact == contact)
                {
                    penetrationConstraint.CleanUp();
                    penetrationConstraints.RemoveAt(i);
                    penetrationConstraintPool.Push(penetrationConstraint);
                    break;
                }
            }
            for (int i = frictionConstraints.count - 1; i >= 0; i--)
            {
                ContactFrictionConstraint frictionConstraint = frictionConstraints[i];
                if (frictionConstraint.PenetrationConstraint == penetrationConstraint)
                {
                    frictionConstraint.CleanUp();
                    frictionConstraints.RemoveAt(i);
                    frictionConstraintPool.Push(frictionConstraint);
                    break;
                }
            }

        }




        //NOTE: Even though the order of addition to the solver group ensures penetration constraints come first, the
        //order of penetration constraints themselves matters in terms of determinism!
        //Consider what happens when penetration constraints are added and removed.  They cycle through a stack,
        //so the penetration constraints in the solver group's listing have inconsistent ordering.  Reloading the simulation
        //doesn't reset the penetration constraint pools, so suddenly everything is nonrepeatable, even single threaded.

        //By having the update use the order defined by contact addition/removal, determinism is maintained (so long as contact addition/removal is deterministic!)

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public sealed override void Update(float dt)
        {
            for (int i = 0; i < penetrationConstraints.count; i++)
                UpdateUpdateable(penetrationConstraints.Elements[i], dt);
            for (int i = 0; i < frictionConstraints.count; i++)
                UpdateUpdateable(frictionConstraints.Elements[i], dt);
        }


        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public sealed override void ExclusiveUpdate()
        {
            for (int i = 0; i < penetrationConstraints.count; i++)
                ExclusiveUpdateUpdateable(penetrationConstraints.Elements[i]);
            for (int i = 0; i < frictionConstraints.count; i++)
                ExclusiveUpdateUpdateable(frictionConstraints.Elements[i]);
        }


        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public sealed override float SolveIteration()
        {
            int activeConstraints = 0;
            for (int i = 0; i < penetrationConstraints.count; i++)
                SolveUpdateable(penetrationConstraints.Elements[i], ref activeConstraints);
            for (int i = 0; i < frictionConstraints.count; i++)
                SolveUpdateable(frictionConstraints.Elements[i], ref activeConstraints);
            isActiveInSolver = activeConstraints > 0;
            return solverSettings.minimumImpulse + 1; //Never let the system deactivate due to low impulses; solver group takes care of itself.
        }
    }
}
