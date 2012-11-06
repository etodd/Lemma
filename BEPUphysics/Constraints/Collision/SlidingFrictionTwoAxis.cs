using System;
using BEPUphysics.Entities;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.Settings;

namespace BEPUphysics.Constraints.Collision
{
    /// <summary>
    /// Computes the forces to slow down and stop sliding motion between two entities when centralized friction is active.
    /// </summary>
    public class SlidingFrictionTwoAxis : EntitySolverUpdateable
    {
        private ConvexContactManifoldConstraint contactManifoldConstraint;
        ///<summary>
        /// Gets the contact manifold constraint that owns this constraint.
        ///</summary>
        public ConvexContactManifoldConstraint ContactManifoldConstraint
        {
            get
            {
                return contactManifoldConstraint;
            }
        }
        internal Vector2 accumulatedImpulse;
        internal Matrix2X3 angularA, angularB;
        private int contactCount;
        private float friction;
        internal Matrix2X3 linearA;
        private Entity entityA, entityB;
        private bool entityADynamic, entityBDynamic;
        private Vector3 ra, rb;
        private Matrix2X2 velocityToImpulse;


        /// <summary>
        /// Gets the first direction in which the friction force acts.
        /// This is one of two directions that are perpendicular to each other and the normal of a collision between two entities.
        /// </summary>
        public Vector3 FrictionDirectionX
        {
            get { return new Vector3(linearA.M11, linearA.M12, linearA.M13); }
        }

        /// <summary>
        /// Gets the second direction in which the friction force acts.
        /// This is one of two directions that are perpendicular to each other and the normal of a collision between two entities.
        /// </summary>
        public Vector3 FrictionDirectionY
        {
            get { return new Vector3(linearA.M21, linearA.M22, linearA.M23); }
        }

        /// <summary>
        /// Gets the total impulse applied by sliding friction in the last time step.
        /// The X component of this vector is the force applied along the frictionDirectionX,
        /// while the Y component is the force applied along the frictionDirectionY.
        /// </summary>
        public Vector2 TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        ///<summary>
        /// Gets the tangential relative velocity between the associated entities at the contact point.
        ///</summary>
        public Vector2 RelativeVelocity
        {
            get
            {
                //Compute relative velocity
                //Explicit version:
                //Vector2 dot;
                //Matrix2x3.Transform(ref parentA.myInternalLinearVelocity, ref linearA, out lambda);
                //Matrix2x3.Transform(ref parentB.myInternalLinearVelocity, ref linearA, out dot);
                //lambda.X -= dot.X; lambda.Y -= dot.Y;
                //Matrix2x3.Transform(ref parentA.myInternalAngularVelocity, ref angularA, out dot);
                //lambda.X += dot.X; lambda.Y += dot.Y;
                //Matrix2x3.Transform(ref parentB.myInternalAngularVelocity, ref angularB, out dot);
                //lambda.X += dot.X; lambda.Y += dot.Y;

                //Inline version:
                //lambda.X = linearA.M11 * parentA.myInternalLinearVelocity.X + linearA.M12 * parentA.myInternalLinearVelocity.Y + linearA.M13 * parentA.myInternalLinearVelocity.Z -
                //           linearA.M11 * parentB.myInternalLinearVelocity.X - linearA.M12 * parentB.myInternalLinearVelocity.Y - linearA.M13 * parentB.myInternalLinearVelocity.Z +
                //           angularA.M11 * parentA.myInternalAngularVelocity.X + angularA.M12 * parentA.myInternalAngularVelocity.Y + angularA.M13 * parentA.myInternalAngularVelocity.Z +
                //           angularB.M11 * parentB.myInternalAngularVelocity.X + angularB.M12 * parentB.myInternalAngularVelocity.Y + angularB.M13 * parentB.myInternalAngularVelocity.Z;
                //lambda.Y = linearA.M21 * parentA.myInternalLinearVelocity.X + linearA.M22 * parentA.myInternalLinearVelocity.Y + linearA.M23 * parentA.myInternalLinearVelocity.Z -
                //           linearA.M21 * parentB.myInternalLinearVelocity.X - linearA.M22 * parentB.myInternalLinearVelocity.Y - linearA.M23 * parentB.myInternalLinearVelocity.Z +
                //           angularA.M21 * parentA.myInternalAngularVelocity.X + angularA.M22 * parentA.myInternalAngularVelocity.Y + angularA.M23 * parentA.myInternalAngularVelocity.Z +
                //           angularB.M21 * parentB.myInternalAngularVelocity.X + angularB.M22 * parentB.myInternalAngularVelocity.Y + angularB.M23 * parentB.myInternalAngularVelocity.Z;

                //Re-using information version:
                //TODO: va + wa x ra - vb - wb x rb, dotted against each axis, is it faster?
                float dvx = 0, dvy = 0, dvz = 0;
                if (entityA != null)
                {
                    dvx = entityA.linearVelocity.X + (entityA.angularVelocity.Y * ra.Z) - (entityA.angularVelocity.Z * ra.Y);
                    dvy = entityA.linearVelocity.Y + (entityA.angularVelocity.Z * ra.X) - (entityA.angularVelocity.X * ra.Z);
                    dvz = entityA.linearVelocity.Z + (entityA.angularVelocity.X * ra.Y) - (entityA.angularVelocity.Y * ra.X);
                }
                if (entityB != null)
                {
                    dvx += -entityB.linearVelocity.X - (entityB.angularVelocity.Y * rb.Z) + (entityB.angularVelocity.Z * rb.Y);
                    dvy += -entityB.linearVelocity.Y - (entityB.angularVelocity.Z * rb.X) + (entityB.angularVelocity.X * rb.Z);
                    dvz += -entityB.linearVelocity.Z - (entityB.angularVelocity.X * rb.Y) + (entityB.angularVelocity.Y * rb.X);
                }

                //float dvx = entityA.linearVelocity.X + (entityA.angularVelocity.Y * ra.Z) - (entityA.angularVelocity.Z * ra.Y)
                //            - entityB.linearVelocity.X - (entityB.angularVelocity.Y * rb.Z) + (entityB.angularVelocity.Z * rb.Y);

                //float dvy = entityA.linearVelocity.Y + (entityA.angularVelocity.Z * ra.X) - (entityA.angularVelocity.X * ra.Z)
                //            - entityB.linearVelocity.Y - (entityB.angularVelocity.Z * rb.X) + (entityB.angularVelocity.X * rb.Z);

                //float dvz = entityA.linearVelocity.Z + (entityA.angularVelocity.X * ra.Y) - (entityA.angularVelocity.Y * ra.X)
                //            - entityB.linearVelocity.Z - (entityB.angularVelocity.X * rb.Y) + (entityB.angularVelocity.Y * rb.X);

#if !WINDOWS
                Vector2 lambda = new Vector2();
#else
                Vector2 lambda;
#endif
                lambda.X = dvx * linearA.M11 + dvy * linearA.M12 + dvz * linearA.M13;
                lambda.Y = dvx * linearA.M21 + dvy * linearA.M22 + dvz * linearA.M23;
                return lambda;

                //Using XNA Cross product instead of inline
                //Vector3 wara, wbrb;
                //Vector3.Cross(ref parentA.myInternalAngularVelocity, ref Ra, out wara);
                //Vector3.Cross(ref parentB.myInternalAngularVelocity, ref Rb, out wbrb);

                //float dvx, dvy, dvz;
                //dvx = wara.X + parentA.myInternalLinearVelocity.X - wbrb.X - parentB.myInternalLinearVelocity.X;
                //dvy = wara.Y + parentA.myInternalLinearVelocity.Y - wbrb.Y - parentB.myInternalLinearVelocity.Y;
                //dvz = wara.Z + parentA.myInternalLinearVelocity.Z - wbrb.Z - parentB.myInternalLinearVelocity.Z;

                //lambda.X = dvx * linearA.M11 + dvy * linearA.M12 + dvz * linearA.M13;
                //lambda.Y = dvx * linearA.M21 + dvy * linearA.M22 + dvz * linearA.M23;
            }
        }


        ///<summary>
        /// Constructs a new sliding friction constraint.
        ///</summary>
        public SlidingFrictionTwoAxis()
        {
            isActive = false;
        }

        /// <summary>
        /// Computes one iteration of the constraint to meet the solver updateable's goal.
        /// </summary>
        /// <returns>The rough applied impulse magnitude.</returns>
        public override float SolveIteration()
        {

            Vector2 lambda = RelativeVelocity;

            //Convert to impulse
            //Matrix2x2.Transform(ref lambda, ref velocityToImpulse, out lambda);
            float x = lambda.X;
            lambda.X = x * velocityToImpulse.M11 + lambda.Y * velocityToImpulse.M21;
            lambda.Y = x * velocityToImpulse.M12 + lambda.Y * velocityToImpulse.M22;

            //Accumulate and clamp
            Vector2 previousAccumulatedImpulse = accumulatedImpulse;
            accumulatedImpulse.X += lambda.X;
            accumulatedImpulse.Y += lambda.Y;
            float length = accumulatedImpulse.LengthSquared();
            float maximumFrictionForce = 0;
            for (int i = 0; i < contactCount; i++)
            {
                maximumFrictionForce += contactManifoldConstraint.penetrationConstraints.Elements[i].accumulatedImpulse;
            }
            maximumFrictionForce *= friction;
            if (length > maximumFrictionForce * maximumFrictionForce)
            {
                length = maximumFrictionForce / (float)Math.Sqrt(length);
                accumulatedImpulse.X *= length;
                accumulatedImpulse.Y *= length;
            }
            lambda.X = accumulatedImpulse.X - previousAccumulatedImpulse.X;
            lambda.Y = accumulatedImpulse.Y - previousAccumulatedImpulse.Y;
            //Single Axis clamp
            //float maximumFrictionForce = 0;
            //for (int i = 0; i < contactCount; i++)
            //{
            //    maximumFrictionForce += pair.contacts[i].penetrationConstraint.accumulatedImpulse;
            //}
            //maximumFrictionForce *= friction;
            //float previousAccumulatedImpulse = accumulatedImpulse.X;
            //accumulatedImpulse.X = MathHelper.Clamp(accumulatedImpulse.X + lambda.X, -maximumFrictionForce, maximumFrictionForce);
            //lambda.X = accumulatedImpulse.X - previousAccumulatedImpulse;
            //previousAccumulatedImpulse = accumulatedImpulse.Y;
            //accumulatedImpulse.Y = MathHelper.Clamp(accumulatedImpulse.Y + lambda.Y, -maximumFrictionForce, maximumFrictionForce);
            //lambda.Y = accumulatedImpulse.Y - previousAccumulatedImpulse;

            //Apply impulse
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            //Matrix2x3.Transform(ref lambda, ref linearA, out linear);
            linear.X = lambda.X * linearA.M11 + lambda.Y * linearA.M21;
            linear.Y = lambda.X * linearA.M12 + lambda.Y * linearA.M22;
            linear.Z = lambda.X * linearA.M13 + lambda.Y * linearA.M23;
            if (entityADynamic)
            {
                //Matrix2x3.Transform(ref lambda, ref angularA, out angular);
                angular.X = lambda.X * angularA.M11 + lambda.Y * angularA.M21;
                angular.Y = lambda.X * angularA.M12 + lambda.Y * angularA.M22;
                angular.Z = lambda.X * angularA.M13 + lambda.Y * angularA.M23;
                entityA.ApplyLinearImpulse(ref linear);
                entityA.ApplyAngularImpulse(ref angular);
            }
            if (entityBDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                //Matrix2x3.Transform(ref lambda, ref angularB, out angular);
                angular.X = lambda.X * angularB.M11 + lambda.Y * angularB.M21;
                angular.Y = lambda.X * angularB.M12 + lambda.Y * angularB.M22;
                angular.Z = lambda.X * angularB.M13 + lambda.Y * angularB.M23;
                entityB.ApplyLinearImpulse(ref linear);
                entityB.ApplyAngularImpulse(ref angular);
            }


            return Math.Abs(lambda.X) + Math.Abs(lambda.Y);
        }

        internal Vector3 manifoldCenter, relativeVelocity;

        ///<summary>
        /// Performs the frame's configuration step.
        ///</summary>
        ///<param name="dt">Timestep duration.</param>
        public override void Update(float dt)
        {

            entityADynamic = entityA != null && entityA.isDynamic;
            entityBDynamic = entityB != null && entityB.isDynamic;

            contactCount = contactManifoldConstraint.penetrationConstraints.count;
            switch (contactCount)
            {
                case 1:
                    manifoldCenter = contactManifoldConstraint.penetrationConstraints.Elements[0].contact.Position;
                    break;
                case 2:
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[0].contact.Position,
                                ref contactManifoldConstraint.penetrationConstraints.Elements[1].contact.Position,
                                out manifoldCenter);
                    manifoldCenter.X *= .5f;
                    manifoldCenter.Y *= .5f;
                    manifoldCenter.Z *= .5f;
                    break;
                case 3:
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[0].contact.Position,
                                ref contactManifoldConstraint.penetrationConstraints.Elements[1].contact.Position,
                                out manifoldCenter);
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[2].contact.Position,
                                ref manifoldCenter,
                                out manifoldCenter);
                    manifoldCenter.X *= .333333333f;
                    manifoldCenter.Y *= .333333333f;
                    manifoldCenter.Z *= .333333333f;
                    break;
                case 4:
                    //This isn't actually the center of the manifold.  Is it good enough?  Sure seems like it.
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[0].contact.Position,
                                ref contactManifoldConstraint.penetrationConstraints.Elements[1].contact.Position,
                                out manifoldCenter);
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[2].contact.Position,
                                ref manifoldCenter,
                                out manifoldCenter);
                    Vector3.Add(ref contactManifoldConstraint.penetrationConstraints.Elements[3].contact.Position,
                                ref manifoldCenter,
                                out manifoldCenter);
                    manifoldCenter.X *= .25f;
                    manifoldCenter.Y *= .25f;
                    manifoldCenter.Z *= .25f;
                    break;
                default:
                    manifoldCenter = Toolbox.NoVector;
                    break;
            }

            //Compute the three dimensional relative velocity at the point.


            Vector3 velocityA, velocityB;
            if (entityA != null)
            {
                Vector3.Subtract(ref manifoldCenter, ref entityA.position, out ra);
                Vector3.Cross(ref entityA.angularVelocity, ref ra, out velocityA);
                Vector3.Add(ref velocityA, ref entityA.linearVelocity, out velocityA);
            }
            else
                velocityA = new Vector3();
            if (entityB != null)
            {
                Vector3.Subtract(ref manifoldCenter, ref entityB.position, out rb);
                Vector3.Cross(ref entityB.angularVelocity, ref rb, out velocityB);
                Vector3.Add(ref velocityB, ref entityB.linearVelocity, out velocityB);
            }
            else
                velocityB = new Vector3();
            Vector3.Subtract(ref velocityA, ref velocityB, out relativeVelocity);

            //Get rid of the normal velocity.
            Vector3 normal = contactManifoldConstraint.penetrationConstraints.Elements[0].contact.Normal;
            float normalVelocityScalar = normal.X * relativeVelocity.X + normal.Y * relativeVelocity.Y + normal.Z * relativeVelocity.Z;
            relativeVelocity.X -= normalVelocityScalar * normal.X;
            relativeVelocity.Y -= normalVelocityScalar * normal.Y;
            relativeVelocity.Z -= normalVelocityScalar * normal.Z;

            //Create the jacobian entry and decide the friction coefficient.
            float length = relativeVelocity.LengthSquared();
            if (length > Toolbox.Epsilon)
            {
                length = (float)Math.Sqrt(length);
                float inverseLength = 1 / length;
                linearA.M11 = relativeVelocity.X * inverseLength;
                linearA.M12 = relativeVelocity.Y * inverseLength;
                linearA.M13 = relativeVelocity.Z * inverseLength;


                friction = length > CollisionResponseSettings.StaticFrictionVelocityThreshold ?
                           contactManifoldConstraint.materialInteraction.KineticFriction :
                           contactManifoldConstraint.materialInteraction.StaticFriction;
            }
            else
            {
                friction = contactManifoldConstraint.materialInteraction.StaticFriction;

                //If there was no velocity, try using the previous frame's jacobian... if it exists.
                //Reusing an old one is okay since jacobians are cleared when a contact is initialized.
                if (!(linearA.M11 != 0 || linearA.M12 != 0 || linearA.M13 != 0))
                {
                    //Otherwise, just redo it all.
                    //Create arbitrary axes.
                    Vector3 axis1;
                    Vector3.Cross(ref normal, ref Toolbox.RightVector, out axis1);
                    length = axis1.LengthSquared();
                    if (length > Toolbox.Epsilon)
                    {
                        length = (float)Math.Sqrt(length);
                        float inverseLength = 1 / length;
                        linearA.M11 = axis1.X * inverseLength;
                        linearA.M12 = axis1.Y * inverseLength;
                        linearA.M13 = axis1.Z * inverseLength;
                    }
                    else
                    {
                        Vector3.Cross(ref normal, ref Toolbox.UpVector, out axis1);
                        axis1.Normalize();
                        linearA.M11 = axis1.X;
                        linearA.M12 = axis1.Y;
                        linearA.M13 = axis1.Z;
                    }
                }
            }

            //Second axis is first axis x normal
            linearA.M21 = (linearA.M12 * normal.Z) - (linearA.M13 * normal.Y);
            linearA.M22 = (linearA.M13 * normal.X) - (linearA.M11 * normal.Z);
            linearA.M23 = (linearA.M11 * normal.Y) - (linearA.M12 * normal.X);


            //Compute angular jacobians
            if (entityA != null)
            {
                //angularA 1 =  ra x linear axis 1
                angularA.M11 = (ra.Y * linearA.M13) - (ra.Z * linearA.M12);
                angularA.M12 = (ra.Z * linearA.M11) - (ra.X * linearA.M13);
                angularA.M13 = (ra.X * linearA.M12) - (ra.Y * linearA.M11);

                //angularA 2 =  ra x linear axis 2
                angularA.M21 = (ra.Y * linearA.M23) - (ra.Z * linearA.M22);
                angularA.M22 = (ra.Z * linearA.M21) - (ra.X * linearA.M23);
                angularA.M23 = (ra.X * linearA.M22) - (ra.Y * linearA.M21);
            }

            //angularB 1 =  linear axis 1 x rb
            if (entityB != null)
            {
                angularB.M11 = (linearA.M12 * rb.Z) - (linearA.M13 * rb.Y);
                angularB.M12 = (linearA.M13 * rb.X) - (linearA.M11 * rb.Z);
                angularB.M13 = (linearA.M11 * rb.Y) - (linearA.M12 * rb.X);

                //angularB 2 =  linear axis 2 x rb
                angularB.M21 = (linearA.M22 * rb.Z) - (linearA.M23 * rb.Y);
                angularB.M22 = (linearA.M23 * rb.X) - (linearA.M21 * rb.Z);
                angularB.M23 = (linearA.M21 * rb.Y) - (linearA.M22 * rb.X);
            }
            //Compute inverse effective mass matrix
            Matrix2X2 entryA, entryB;

            //these are the transformed coordinates
            Matrix2X3 transform;
            Matrix3X2 transpose;
            if (entityADynamic)
            {
                Matrix2X3.Multiply(ref angularA, ref entityA.inertiaTensorInverse, out transform);
                Matrix2X3.Transpose(ref angularA, out transpose);
                Matrix2X2.Multiply(ref transform, ref transpose, out entryA);
                entryA.M11 += entityA.inverseMass;
                entryA.M22 += entityA.inverseMass;
            }
            else
            {
                entryA = new Matrix2X2();
            }

            if (entityBDynamic)
            {
                Matrix2X3.Multiply(ref angularB, ref entityB.inertiaTensorInverse, out transform);
                Matrix2X3.Transpose(ref angularB, out transpose);
                Matrix2X2.Multiply(ref transform, ref transpose, out entryB);
                entryB.M11 += entityB.inverseMass;
                entryB.M22 += entityB.inverseMass;
            }
            else
            {
                entryB = new Matrix2X2();
            }

            velocityToImpulse.M11 = -entryA.M11 - entryB.M11;
            velocityToImpulse.M12 = -entryA.M12 - entryB.M12;
            velocityToImpulse.M21 = -entryA.M21 - entryB.M21;
            velocityToImpulse.M22 = -entryA.M22 - entryB.M22;
            Matrix2X2.Invert(ref velocityToImpulse, out velocityToImpulse);


        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {

            //Warm starting
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            //Matrix2x3.Transform(ref lambda, ref linearA, out linear);
            linear.X = accumulatedImpulse.X * linearA.M11 + accumulatedImpulse.Y * linearA.M21;
            linear.Y = accumulatedImpulse.X * linearA.M12 + accumulatedImpulse.Y * linearA.M22;
            linear.Z = accumulatedImpulse.X * linearA.M13 + accumulatedImpulse.Y * linearA.M23;
            if (entityADynamic)
            {
                //Matrix2x3.Transform(ref lambda, ref angularA, out angular);
                angular.X = accumulatedImpulse.X * angularA.M11 + accumulatedImpulse.Y * angularA.M21;
                angular.Y = accumulatedImpulse.X * angularA.M12 + accumulatedImpulse.Y * angularA.M22;
                angular.Z = accumulatedImpulse.X * angularA.M13 + accumulatedImpulse.Y * angularA.M23;
                entityA.ApplyLinearImpulse(ref linear);
                entityA.ApplyAngularImpulse(ref angular);
            }
            if (entityBDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                //Matrix2x3.Transform(ref lambda, ref angularB, out angular);
                angular.X = accumulatedImpulse.X * angularB.M11 + accumulatedImpulse.Y * angularB.M21;
                angular.Y = accumulatedImpulse.X * angularB.M12 + accumulatedImpulse.Y * angularB.M22;
                angular.Z = accumulatedImpulse.X * angularB.M13 + accumulatedImpulse.Y * angularB.M23;
                entityB.ApplyLinearImpulse(ref linear);
                entityB.ApplyAngularImpulse(ref angular);
            }
        }

        internal void Setup(ConvexContactManifoldConstraint contactManifoldConstraint)
        {
            this.contactManifoldConstraint = contactManifoldConstraint;
            isActive = true;

            linearA = new Matrix2X3();

            entityA = contactManifoldConstraint.EntityA;
            entityB = contactManifoldConstraint.EntityB;
        }

        internal void CleanUp()
        {
            accumulatedImpulse = new Vector2();
            contactManifoldConstraint = null;
            entityA = null;
            entityB = null;
            isActive = false;
        }

        protected internal override void CollectInvolvedEntities(DataStructures.RawList<Entity> outputInvolvedEntities)
        {
            //This should never really have to be called.
            if (entityA != null)
                outputInvolvedEntities.Add(entityA);
            if (entityB != null)
                outputInvolvedEntities.Add(entityB);
        }
    }
}