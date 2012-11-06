using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUphysics.DataStructures;
using BEPUphysics.Entities;
using BEPUphysics.MathExtensions;
using BEPUphysics.OtherSpaceStages;
using BEPUphysics.ResourceManagement;
using BEPUphysics.Threading;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace BEPUphysics.Collidables
{
    /// <summary>
    /// Stores flags regarding an object's degree of inclusion in a volume.
    /// </summary>
    public struct ContainmentState
    {
        /// <summary>
        /// Whether or not the object is fully contained.
        /// </summary>
        public bool IsContained;

        /// <summary>
        /// Whether or not the object is partially or fully contained.
        /// </summary>
        public bool IsTouching;

        /// <summary>
        /// Whether or not the entity associated with this state has been refreshed during the last update.
        /// </summary>
        internal bool StaleState;

        /// <summary>
        /// Constructs a new ContainmentState.
        /// </summary>
        /// <param name="touching">Whether or not the object is partially or fully contained.</param>
        /// <param name="contained">Whether or not the object is fully contained.</param>
        public ContainmentState(bool touching, bool contained)
        {
            IsTouching = touching;
            IsContained = contained;
            StaleState = false;
        }
        /// <summary>
        /// Constructs a new ContainmentState.
        /// </summary>
        /// <param name="touching">Whether or not the object is partially or fully contained.</param>
        /// <param name="contained">Whether or not the object is fully contained.</param>
        /// <param name="stale">Whether or not the entity associated with this state has been refreshed in the previous update.</param>
        internal ContainmentState(bool touching, bool contained, bool stale)
        {
            IsTouching = touching;
            IsContained = contained;
            StaleState = stale;
        }


    }

    /// <summary>
    /// Manages the detection of entities within an arbitrary closed triangle mesh.
    /// </summary>
    public class DetectorVolume : BroadPhaseEntry, ISpaceObject, IDeferredEventCreator
    {

        internal Dictionary<Entity, DetectorVolumePairHandler> pairs = new Dictionary<Entity, DetectorVolumePairHandler>();
        /// <summary>
        /// Gets the list of pairs associated with the detector volume.
        /// </summary>
        public ReadOnlyDictionary<Entity, DetectorVolumePairHandler> Pairs
        {
            get
            {
                return new ReadOnlyDictionary<Entity, DetectorVolumePairHandler>(pairs);
            }
        }


        TriangleMesh triangleMesh;
        /// <summary>
        /// Gets or sets the triangle mesh data and acceleration structure.  Must be a closed mesh with consistent winding.
        /// </summary>
        public TriangleMesh TriangleMesh
        {
            get
            {
                return triangleMesh;
            }
            set
            {
                triangleMesh = value;
                UpdateBoundingBox();
                Reinitialize();
            }
        }






        /// <summary>
        /// Creates a detector volume.
        /// </summary>
        /// <param name="triangleMesh">Closed and consistently wound mesh defining the volume.</param>
        public DetectorVolume(TriangleMesh triangleMesh)
        {
            TriangleMesh = triangleMesh;
            UpdateBoundingBox();
        }


        
        /// <summary>
        /// Fires when an entity comes into contact with the volume.
        /// </summary>
        public event EntityBeginsTouchingVolumeEventHandler EntityBeganTouching;

        /// <summary>
        /// Fires when an entity ceases to intersect the volume.
        /// </summary>
        public event EntityStopsTouchingVolumeEventHandler EntityStoppedTouching;

        /// <summary>
        /// Fires when an entity becomes fully engulfed by a volume.
        /// </summary>
        public event VolumeBeginsContainingEntityEventHandler VolumeBeganContainingEntity;

        /// <summary>
        /// Fires when an entity ceases to be fully engulfed by a volume.
        /// </summary>
        public event VolumeStopsContainingEntityEventHandler VolumeStoppedContainingEntity;


        

        private ISpace space;
        ISpace ISpaceObject.Space
        {
            get
            {
                return space;
            }
            set
            {
                space = value;
            }
        }

        ///<summary>
        /// Space that owns the detector volume.
        ///</summary>
        public ISpace Space
        {
            get
            {
                return space;
            }
        }

        private bool innerFacingIsClockwise;

        /// <summary>
        /// Determines if a point is contained by the detector volume.
        /// </summary>
        /// <param name="point">Point to check for containment.</param>
        /// <returns>Whether or not the point is contained by the detector volume.</returns>
        public bool IsPointContained(Vector3 point)
        {
            var triangles = Resources.GetIntList();
            bool contained = IsPointContained(ref point, triangles);
            Resources.GiveBack(triangles);
            return contained;
        }

        internal bool IsPointContained(ref Vector3 point, RawList<int> triangles)
        {
            Vector3 rayDirection;
            //Point from the approximate center of the mesh outwards.
            //This is a cheap way to reduce the number of unnecessary checks when objects are external to the mesh.
            Vector3.Add(ref boundingBox.Max, ref boundingBox.Min, out rayDirection);
            Vector3.Multiply(ref rayDirection, .5f, out rayDirection);
            Vector3.Subtract(ref point, ref rayDirection, out rayDirection);
            //If the point is right in the middle, we'll need a backup.
            if (rayDirection.LengthSquared() < .01f)
                rayDirection = Vector3.Up;

            var ray = new Ray(point, rayDirection);
            triangleMesh.Tree.GetOverlaps(ray, triangles);

            float minimumT = float.MaxValue;
            bool minimumIsClockwise = false;

            for (int i = 0; i < triangles.count; i++)
            {
                Vector3 a, b, c;
                triangleMesh.Data.GetTriangle(triangles.Elements[i], out a, out b, out c);

                RayHit hit;
                bool hitClockwise;
                if (Toolbox.FindRayTriangleIntersection(ref ray, float.MaxValue, ref a, ref b, ref c, out hitClockwise, out hit))
                {
                    if (hit.T < minimumT)
                    {
                        minimumT = hit.T;
                        minimumIsClockwise = hitClockwise;
                    }
                }
            }

            triangles.Clear();

            //If the first hit is on the inner surface, then the ray started inside the mesh.
            return minimumT < float.MaxValue && minimumIsClockwise == innerFacingIsClockwise;
        }

        protected override void CollisionRulesUpdated()
        {
            foreach (var pair in pairs.Values)
                pair.CollisionRule = CollisionRules.CollisionRuleCalculator(pair.BroadPhaseOverlap.entryA, pair.BroadPhaseOverlap.entryB);

        }

        protected internal override bool IsActive
        {
            get { return false; }
        }

        public override bool RayCast(Ray ray, float maximumLength, out RayHit rayHit)
        {
            return triangleMesh.RayCast(ray, maximumLength, TriangleSidedness.DoubleSided, out rayHit);
        }

        public override bool ConvexCast(ConvexShape castShape, ref MathExtensions.RigidTransform startingTransform, ref Vector3 sweep, out RayHit hit)
        {
            hit = new RayHit();
            BoundingBox boundingBox;
            Toolbox.GetExpandedBoundingBox(ref castShape, ref startingTransform, ref sweep, out boundingBox);
            var tri = Resources.GetTriangle();
            var hitElements = Resources.GetIntList();
            if (triangleMesh.Tree.GetOverlaps(boundingBox, hitElements))
            {
                hit.T = float.MaxValue;
                for (int i = 0; i < hitElements.Count; i++)
                {
                    triangleMesh.Data.GetTriangle(hitElements[i], out tri.vA, out tri.vB, out tri.vC);
                    Vector3 center;
                    Vector3.Add(ref tri.vA, ref tri.vB, out center);
                    Vector3.Add(ref center, ref tri.vC, out center);
                    Vector3.Multiply(ref center, 1f / 3f, out center);
                    Vector3.Subtract(ref tri.vA, ref center, out tri.vA);
                    Vector3.Subtract(ref tri.vB, ref center, out tri.vB);
                    Vector3.Subtract(ref tri.vC, ref center, out tri.vC);
                    tri.maximumRadius = tri.vA.LengthSquared();
                    float radius = tri.vB.LengthSquared();
                    if (tri.maximumRadius < radius)
                        tri.maximumRadius = radius;
                    radius = tri.vC.LengthSquared();
                    if (tri.maximumRadius < radius)
                        tri.maximumRadius = radius;
                    tri.maximumRadius = (float)Math.Sqrt(tri.maximumRadius);
                    tri.collisionMargin = 0;
                    var triangleTransform = new RigidTransform { Orientation = Quaternion.Identity, Position = center };
                    RayHit tempHit;
                    if (MPRToolbox.Sweep(castShape, tri, ref sweep, ref Toolbox.ZeroVector, ref startingTransform, ref triangleTransform, out tempHit) && tempHit.T < hit.T)
                    {
                        hit = tempHit;
                    }
                }
                tri.maximumRadius = 0;
                Resources.GiveBack(tri);
                Resources.GiveBack(hitElements);
                return hit.T != float.MaxValue;
            }
            Resources.GiveBack(tri);
            Resources.GiveBack(hitElements);
            return false;
        }

        /// <summary>
        /// Sets the bounding box of the detector volume to the current hierarchy root bounding box.  This is called automatically if the TriangleMesh property is set.
        /// </summary>
        public override void UpdateBoundingBox()
        {
            boundingBox = triangleMesh.Tree.BoundingBox;
        }

        /// <summary>
        /// Updates the detector volume's interpretation of the mesh.  This should be called when the the TriangleMesh is changed significantly.  This is called automatically if the TriangleMesh property is set.
        /// </summary>
        public void Reinitialize()
        {
            //Pick a point that is known to be outside the mesh as the origin.
            Vector3 origin = (triangleMesh.Tree.BoundingBox.Max - triangleMesh.Tree.BoundingBox.Min) * 1.5f + triangleMesh.Tree.BoundingBox.Min;

            //Pick a direction which will definitely hit the mesh.
            Vector3 a, b, c;
            triangleMesh.Data.GetTriangle(0, out a, out b, out c);
            var direction = (a + b + c) / 3 - origin;

            var ray = new Ray(origin, direction);
            var triangles = Resources.GetIntList();
            triangleMesh.Tree.GetOverlaps(ray, triangles);

            float minimumT = float.MaxValue;

            for (int i = 0; i < triangles.count; i++)
            {
                triangleMesh.Data.GetTriangle(triangles.Elements[i], out a, out b, out c);

                RayHit hit;
                bool hitClockwise;
                if (Toolbox.FindRayTriangleIntersection(ref ray, float.MaxValue, ref a, ref b, ref c, out hitClockwise, out hit))
                {
                    if (hit.T < minimumT)
                    {
                        minimumT = hit.T;
                        innerFacingIsClockwise = !hitClockwise;
                    }
                }
            }
            Resources.GiveBack(triangles);
        }


        void ISpaceObject.OnAdditionToSpace(ISpace newSpace)
        {

        }

        void ISpaceObject.OnRemovalFromSpace(ISpace oldSpace)
        {

        }


        /// <summary>
        /// Used to protect against containment changes coming in from multithreaded narrowphase contexts.
        /// </summary>
        SpinLock locker = new SpinLock();
        struct ContainmentChange
        {
            public Entity Entity;
            public ContainmentChangeType Change;
        }
        enum ContainmentChangeType : byte
        {
            BeganTouching,
            StoppedTouching,
            BeganContaining,
            StoppedContaining
        }
        private Queue<ContainmentChange> containmentChanges = new Queue<ContainmentChange>();
        internal void BeganTouching(DetectorVolumePairHandler pair)
        {
            locker.Enter();
            containmentChanges.Enqueue(new ContainmentChange
            {
                Change = ContainmentChangeType.BeganTouching,
                Entity = pair.Collidable.entity
            });
            locker.Exit();
        }

        internal void StoppedTouching(DetectorVolumePairHandler pair)
        {
            locker.Enter();
            containmentChanges.Enqueue(new ContainmentChange
            {
                Change = ContainmentChangeType.StoppedTouching,
                Entity = pair.Collidable.entity
            });
            locker.Exit();
        }

        internal void BeganContaining(DetectorVolumePairHandler pair)
        {
            locker.Enter();
            containmentChanges.Enqueue(new ContainmentChange
            {
                Change = ContainmentChangeType.BeganContaining,
                Entity = pair.Collidable.entity
            });
            locker.Exit();
        }

        internal void StoppedContaining(DetectorVolumePairHandler pair)
        {
            locker.Enter();
            containmentChanges.Enqueue(new ContainmentChange
            {
                Change = ContainmentChangeType.StoppedContaining,
                Entity = pair.Collidable.entity
            });
            locker.Exit();
        }


        DeferredEventDispatcher IDeferredEventCreator.DeferredEventDispatcher { get; set; }

        bool IDeferredEventCreator.IsActive
        {
            get { return true; }
            set { throw new NotSupportedException("Detector volumes are always active deferred event generators."); }
        }

        void IDeferredEventCreator.DispatchEvents()
        {
            while (containmentChanges.Count > 0)
            {
                var change = containmentChanges.Dequeue();
                switch (change.Change)
                {
                    case ContainmentChangeType.BeganTouching:
                        if (EntityBeganTouching != null)
                            EntityBeganTouching(this, change.Entity);
                        break;
                    case ContainmentChangeType.StoppedTouching:
                        if (EntityStoppedTouching != null)
                            EntityStoppedTouching(this, change.Entity);
                        break;
                    case ContainmentChangeType.BeganContaining:
                        if (VolumeBeganContainingEntity != null)
                            VolumeBeganContainingEntity(this, change.Entity);
                        break;
                    case ContainmentChangeType.StoppedContaining:
                        if (VolumeStoppedContainingEntity != null)
                            VolumeStoppedContainingEntity(this, change.Entity);
                        break;
                }
            }
        }

        int IDeferredEventCreator.ChildDeferredEventCreators
        {
            get { return 0; }
            set
            {
                throw new NotSupportedException("The detector volume does not allow child deferred event creators.");
            }
        }
    }


    /// <summary>
    /// Handles any special logic to perform when an entry begins touching a detector volume.
    /// Runs within an update loop for updateables; modifying the updateable listing during the event is disallowed.
    /// </summary>
    /// <param name="volume">DetectorVolume being touched.</param>
    /// <param name="toucher">Entry touching the volume.</param>
    public delegate void EntityBeginsTouchingVolumeEventHandler(DetectorVolume volume, Entity toucher);

    /// <summary>
    /// Handles any special logic to perform when an entry stops touching a detector volume.
    /// Runs within an update loop for updateables; modifying the updateable listing during the event is disallowed.
    /// </summary>
    /// <param name="volume">DetectorVolume no longer being touched.</param>
    /// <param name="toucher">Entry no longer touching the volume.</param>
    public delegate void EntityStopsTouchingVolumeEventHandler(DetectorVolume volume, Entity toucher);

    /// <summary>
    /// Handles any special logic to perform when an entity begins being contained by a detector volume.
    /// Runs within an update loop for updateables; modifying the updateable listing during the event is disallowed.
    /// </summary>
    /// <param name="volume">DetectorVolume containing the entry.</param>
    /// <param name="entity">Entity contained by the volume.</param>
    public delegate void VolumeBeginsContainingEntityEventHandler(DetectorVolume volume, Entity entity);

    /// <summary>
    /// Handles any special logic to perform when an entry stops being contained by a detector volume.
    /// Runs within an update loop for updateables; modifying the updateable listing during the event is disallowed.
    /// </summary>
    /// <param name="volume">DetectorVolume no longer containing the entry.</param>
    /// <param name="entity">Entity no longer contained by the volume.</param>
    public delegate void VolumeStopsContainingEntityEventHandler(DetectorVolume volume, Entity entity);
}