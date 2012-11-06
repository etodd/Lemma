using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.MathExtensions;
using BEPUphysics.Materials;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Uses a raycast as the shape of a wheel.
    /// </summary>
    public class RaycastWheelShape : WheelShape
    {
        private float graphicalRadius;

        /// <summary>
        /// Creates a new raycast based wheel shape.
        /// </summary>
        /// <param name="graphicalRadius">Graphical radius of the wheel.
        /// This is not used for simulation.  It is only used in
        /// determining aesthetic properties of a vehicle wheel,
        /// like position and orientation.</param>
        /// <param name="localGraphicTransform">Local graphic transform of the wheel shape.
        /// This transform is applied first when creating the shape's worldTransform.</param>
        public RaycastWheelShape(float graphicalRadius, Matrix localGraphicTransform)
        {
            Radius = graphicalRadius;
            LocalGraphicTransform = localGraphicTransform;
        }

        /// <summary>
        /// Gets or sets the graphical radius of the wheel.
        /// This is not used for simulation.  It is only used in
        /// determining aesthetic properties of a vehicle wheel,
        /// like position and orientation.
        /// </summary>
        public override sealed float Radius
        {
            get { return graphicalRadius; }
            set { graphicalRadius = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Updates the wheel's world transform for graphics.
        /// Called automatically by the owning wheel at the end of each frame.
        /// If the engine is updating asynchronously, you can call this inside of a space read buffer lock
        /// and update the wheel transforms safely.
        /// </summary>
        public override void UpdateWorldTransform()
        {
#if !WINDOWS
            Vector3 newPosition = new Vector3();
#else
            Vector3 newPosition;
#endif
            Vector3 worldAttachmentPoint;
            Vector3 localAttach;
            Vector3.Add(ref wheel.suspension.localAttachmentPoint, ref wheel.vehicle.Body.CollisionInformation.localPosition, out localAttach);
            worldTransform = Matrix3X3.ToMatrix4X4(wheel.vehicle.Body.BufferedStates.InterpolatedStates.OrientationMatrix);

            Vector3.TransformNormal(ref localAttach, ref worldTransform, out worldAttachmentPoint);
            worldAttachmentPoint += wheel.vehicle.Body.BufferedStates.InterpolatedStates.Position;

            Vector3 worldDirection;
            Vector3.Transform(ref wheel.suspension.localDirection, ref worldTransform, out worldDirection);

            float length = wheel.suspension.currentLength - graphicalRadius;
            newPosition.X = worldAttachmentPoint.X + worldDirection.X * length;
            newPosition.Y = worldAttachmentPoint.Y + worldDirection.Y * length;
            newPosition.Z = worldAttachmentPoint.Z + worldDirection.Z * length;

            Matrix spinTransform;

            Vector3 localSpinAxis = Vector3.Cross(wheel.localForwardDirection, wheel.suspension.localDirection);
            Matrix.CreateFromAxisAngle(ref localSpinAxis, spinAngle, out spinTransform);


            Matrix localTurnTransform;
            Matrix.Multiply(ref localGraphicTransform, ref spinTransform, out localTurnTransform);
            Matrix.Multiply(ref localTurnTransform, ref steeringTransform, out localTurnTransform);
            //Matrix.Multiply(ref localTurnTransform, ref spinTransform, out localTurnTransform);
            Matrix.Multiply(ref localTurnTransform, ref worldTransform, out worldTransform);
            worldTransform.Translation = newPosition;
        }

        /// <summary>
        /// Finds a supporting entity, the contact location, and the contact normal.
        /// </summary>
        /// <param name="location">Contact point between the wheel and the support.</param>
        /// <param name="normal">Contact normal between the wheel and the support.</param>
        /// <param name="suspensionLength">Length of the suspension at the contact.</param>
        /// <param name="supportingCollidable">Collidable supporting the wheel, if any.</param>
        /// <param name="entity">Supporting object.</param>
        /// <param name="material">Material of the wheel.</param>
        /// <returns>Whether or not any support was found.</returns>
        protected internal override bool FindSupport(out Vector3 location, out Vector3 normal, out float suspensionLength, out Collidable supportingCollidable, out Entity entity, out Material material)
        {
            suspensionLength = float.MaxValue;
            location = Toolbox.NoVector;
            supportingCollidable = null;
            entity = null;
            normal = Toolbox.NoVector;
            material = null;

            Collidable testCollidable;
            RayHit rayHit;

            bool hit = false;

            for (int i = 0; i < detector.CollisionInformation.pairs.Count; i++)
            {
                var pair = detector.CollisionInformation.pairs[i];
                testCollidable = (pair.BroadPhaseOverlap.entryA == detector.CollisionInformation ? pair.BroadPhaseOverlap.entryB : pair.BroadPhaseOverlap.entryA) as Collidable;
                if (testCollidable != null)
                {
                    if (CollisionRules.CollisionRuleCalculator(this, testCollidable) == CollisionRule.Normal &&
                        testCollidable.RayCast(new Ray(wheel.suspension.worldAttachmentPoint, wheel.suspension.worldDirection), wheel.suspension.restLength, out rayHit) &&
                        rayHit.T < suspensionLength)
                    {
                        suspensionLength = rayHit.T;
                        EntityCollidable entityCollidable;
                        if ((entityCollidable = testCollidable as EntityCollidable) != null)
                        {
                            entity = entityCollidable.Entity;
                            material = entityCollidable.Entity.Material;
                        }
                        else
                        {
                            entity = null;
                            supportingCollidable = testCollidable;
                            var materialOwner = testCollidable as IMaterialOwner;
                            if (materialOwner != null)
                                material = materialOwner.Material;
                        }
                        location = rayHit.Location;
                        normal = rayHit.Normal;
                        hit = true;
                    }
                }
            }
            if (hit)
            {
                if (suspensionLength > 0)
                    normal.Normalize();
                else
                    Vector3.Negate(ref wheel.suspension.worldDirection, out normal);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Initializes the detector entity and any other necessary logic.
        /// </summary>
        protected internal override void Initialize()
        {
            //Setup the dimensions of the detector.
            Vector3 startpoint = wheel.suspension.localAttachmentPoint;
            Vector3 endpoint = startpoint + wheel.suspension.localDirection * wheel.suspension.restLength;
            Vector3 min, max;
            Vector3.Min(ref startpoint, ref endpoint, out min);
            Vector3.Max(ref startpoint, ref endpoint, out max);

            detector.Width = max.X - min.X;
            detector.Height = max.Y - min.Y;
            detector.Length = max.Z - min.Z;
        }

        /// <summary>
        /// Updates the position of the detector before each step.
        /// </summary>
        protected internal override void UpdateDetectorPosition()
        {
#if !WINDOWS
            Vector3 newPosition = new Vector3();
#else
            Vector3 newPosition;
#endif

            newPosition.X = wheel.suspension.worldAttachmentPoint.X + wheel.suspension.worldDirection.X * wheel.suspension.restLength * .5f;
            newPosition.Y = wheel.suspension.worldAttachmentPoint.Y + wheel.suspension.worldDirection.Y * wheel.suspension.restLength * .5f;
            newPosition.Z = wheel.suspension.worldAttachmentPoint.Z + wheel.suspension.worldDirection.Z * wheel.suspension.restLength * .5f;

            detector.Position = newPosition;
            detector.OrientationMatrix = wheel.Vehicle.Body.orientationMatrix;
            Vector3 linearVelocity;
            Vector3.Subtract(ref newPosition, ref wheel.vehicle.Body.position, out linearVelocity);
            Vector3.Cross(ref linearVelocity, ref wheel.vehicle.Body.angularVelocity, out linearVelocity);
            Vector3.Add(ref linearVelocity, ref wheel.vehicle.Body.linearVelocity, out linearVelocity);
            detector.LinearVelocity = linearVelocity;
            detector.AngularVelocity = wheel.vehicle.Body.angularVelocity;
        }
    }
}