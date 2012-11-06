using System;
using System.Collections.Generic;
using BEPUphysics.Entities;
using BEPUphysics.DataStructures;
using BEPUphysics.UpdateableSystems;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Simulates wheeled vehicles using a variety of constraints and shape casts.
    /// </summary>
    public class Vehicle : CombinedUpdateable, IDuringForcesUpdateable, IBeforeNarrowPhaseUpdateable, IEndOfTimeStepUpdateable, IEndOfFrameUpdateable
    {
        //TODO: The vehicle uses wheel 'fake constraints' that were made prior to the changes to the constraint system that allow for customizable solver settings.
        //It would be convenient to use a SolverGroup to handle the wheel constraints, since the functionality is nearly the same.

        private readonly List<Wheel> wheels = new List<Wheel>();

        private Entity body;
        internal List<Entity> previousSupports = new List<Entity>();

        /// <summary>
        /// Constructs a vehicle.
        /// </summary>
        /// <param name="shape">Body of the vehicle.</param>
        public Vehicle(Entity shape)
        {
            IsUpdatedSequentially = false;
            Body = shape;
            Body.activityInformation.IsAlwaysActive = true;
            //The body is always active, so don't bother with stabilization either.
            //Stabilization can introduce artifacts as well.
            body.activityInformation.AllowStabilization = false;
        }

        /// <summary>
        /// Constructs a vehicle.
        /// </summary>
        /// <param name="shape">Body of the vehicle.</param>
        /// <param name="wheelList">List of wheels of the vehicle.</param>
        public Vehicle(Entity shape, IEnumerable<Wheel> wheelList)
        {
            IsUpdatedSequentially = false;
            Body = shape;
            Body.activityInformation.IsAlwaysActive = true;     
            //The body is always active, so don't bother with stabilization either.
            //Stabilization can introduce artifacts as well.
            body.activityInformation.AllowStabilization = false;
            foreach (Wheel wheel in wheelList)
            {
                AddWheel(wheel);
            }
        }

        /// <summary>
        /// Gets or sets the entity representing the shape of the car.
        /// </summary>
        public Entity Body
        {
            get { return body; }
            set
            {
                body = value;
                OnInvolvedEntitiesChanged();
            }
        }


        /// <summary>
        /// Number of wheels with supports.
        /// </summary>
        public int SupportedWheelCount
        {
            get
            {
                int toReturn = 0;
                foreach (Wheel wheel in Wheels)
                {
                    if (wheel.HasSupport)
                        toReturn++;
                }
                return toReturn;
            }
        }

        /// <summary>
        /// Gets the list of wheels supporting the vehicle.
        /// </summary>
        public List<Wheel> Wheels
        {
            get { return wheels; }
        }


        /// <summary>
        /// Sets up the vehicle's information when being added to the space.
        /// Called automatically when the space adds the vehicle.
        /// </summary>
        /// <param name="newSpace">New owning space.</param>
        public override void OnAdditionToSpace(ISpace newSpace)
        {
            newSpace.Add(body);
            foreach (Wheel wheel in Wheels)
            {
                wheel.OnAdditionToSpace(newSpace);
            }
        }

        /// <summary>
        /// Sets up the vehicle's information when being added to the space.
        /// Called automatically when the space adds the vehicle.
        /// </summary>
        public override void OnRemovalFromSpace(ISpace oldSpace)
        {
            foreach (Wheel wheel in Wheels)
            {
                wheel.OnRemovalFromSpace(oldSpace);
            }
            oldSpace.Remove(Body);
        }

        /// <summary>
        /// Performs the end-of-frame update component.
        /// </summary>
        /// <param name="dt">Time since last frame in simulation seconds.</param>
        void IEndOfFrameUpdateable.Update(float dt)
        {
            //Graphics should be updated at the end of each frame.
            foreach (Wheel wheel in Wheels)
            {
                wheel.UpdateAtEndOfFrame(dt);
            }
        }

        /// <summary>
        /// Performs the end-of-update update component.
        /// </summary>
        /// <param name="dt">Time since last frame in simulation seconds.</param>
        void IEndOfTimeStepUpdateable.Update(float dt)
        {
            //Graphics should be updated at the end of each frame.
            foreach (Wheel wheel in Wheels)
            {
                wheel.UpdateAtEndOfUpdate(dt);
            }
        }

        void IBeforeNarrowPhaseUpdateable.Update(float dt)
        {
            //After broadphase, test for supports.
            foreach (Wheel wheel in wheels)
            {
                wheel.FindSupport();
            }
            OnInvolvedEntitiesChanged();
        }

        void IDuringForcesUpdateable.Update(float dt)
        {
            foreach (Wheel wheel in wheels)
            {
                wheel.UpdateDuringForces(dt);
            }
        }

        /// <summary>
        /// Adds a wheel to the vehicle.
        /// </summary>
        /// <param name="wheel">WheelTest to add.</param>
        public void AddWheel(Wheel wheel)
        {
            if (wheel.vehicle == null)
            {
                Wheels.Add(wheel);
                wheel.OnAddedToVehicle(this);
            }
            else
                throw new InvalidOperationException("Can't add a wheel to a vehicle if it already belongs to a vehicle.");
        }

        /// <summary>
        /// Removes a wheel from the vehicle.
        /// </summary>
        /// <param name="wheel">WheelTest to remove.</param>
        public void RemoveWheel(Wheel wheel)
        {
            if (wheel.vehicle == this)
            {
                wheel.OnRemovedFromVehicle();
                Wheels.Remove(wheel);
            }
            else
                throw new InvalidOperationException("Can't remove a wheel from a vehicle that does not own it.");
        }


        /// <summary>
        /// Updates the vehicle.
        /// Called automatically when needed by the owning Space.
        /// </summary>
        public override float SolveIteration()
        {
            int numActive = 0;
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.isActiveInSolver)
                    if (!wheel.ApplyImpulse())
                        wheel.isActiveInSolver = false;
                    else
                        numActive++;
            }
            if (numActive == 0)
                isActiveInSolver = false;
            return solverSettings.minimumImpulse + 1; //We take care of ourselves.
        }

        /// <summary>
        /// Adds entities associated with the solver item to the involved entities list.
        /// Ensure that sortInvolvedEntities() is called at the end of the function.
        /// This allows the non-batched multithreading system to lock properly.
        /// </summary>
        protected internal override void CollectInvolvedEntities(RawList<Entity> outputInvolvedEntities)
        {
            outputInvolvedEntities.Add(Body);
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.supportingEntity != null && !outputInvolvedEntities.Contains(wheel.supportingEntity))
                    outputInvolvedEntities.Add(wheel.supportingEntity);
            }
        }

        /// <summary>
        /// Computes information required during the later update.
        /// Called once before the iteration loop.
        /// </summary>
        /// <param name="dt">Time since previous frame in simulation seconds.</param>
        public override void Update(float dt)
        {
            //TODO: to help balance multithreading, what if each wheel were its own SolverUpdateable
            //(no more CombinedUpdateable, basically)
            //This might be okay, but chances are if each was totally isolated, the 'enter exit'
            //of the monitor would be more expensive than just going in all at once and leaving at once.
            //Maybe a SolverGroup instead of CombinedUpdateable, though.

            //Update the wheel 'constraints.'
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.isActiveInSolver)
                    wheel.PreStep(dt);
            }
        }

        /// <summary>
        /// Performs any pre-solve iteration work that needs exclusive
        /// access to the members of the solver updateable.
        /// Usually, this is used for applying warmstarting impulses.
        /// </summary>
        public override void ExclusiveUpdate()
        {
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.isActiveInSolver)
                    wheel.ExclusiveUpdate();
            }
        }

        /// <summary>
        /// Updates the activity state of the wheel constraints.
        /// </summary>
        public override void UpdateSolverActivity()
        {
            if (isActive)
            {
                isActiveInSolver = false;
                if (body.activityInformation.IsActive)
                {
                    foreach (Wheel wheel in Wheels)
                    {
                        wheel.UpdateSolverActivity();
                        isActiveInSolver = isActiveInSolver || wheel.isActiveInSolver;
                    }
                }
            }
            else
                isActiveInSolver = false;
        }

    }
}