using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables.Events;
using BEPUphysics.CollisionShapes;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using BEPUphysics.DataStructures;
using BEPUphysics.Materials;
using System.Collections.ObjectModel;
using BEPUphysics.CollisionRuleManagement;
using System;

namespace BEPUphysics.Collidables.MobileCollidables
{
    ///<summary>
    /// Collidable used by compound shapes.
    ///</summary>
    public class CompoundCollidable : EntityCollidable
    {
        /// <summary>
        /// Gets or sets the event manager for the collidable.
        /// Compound collidables must use a special CompoundEventManager in order for the deferred events created
        /// by child collidables to be dispatched.
        /// If this method is bypassed and a different event manager is used, this method will return null and 
        /// deferred events from children will fail.
        /// </summary>
        public new CompoundEventManager Events
        {
            get
            {
                return events as CompoundEventManager;
            }
            set
            {
                var compoundEvents = events as CompoundEventManager;
                //Tell every child to update their parent references to the new object.
                foreach (var child in children)
                {
                    child.CollisionInformation.events.Parent = value;
                }
                base.Events = value;
            }
        }

        ///<summary>
        /// Gets the shape of the collidable.
        ///</summary>
        public new CompoundShape Shape
        {
            get
            {
                return (CompoundShape)shape;
            }
            protected internal set
            {
                base.Shape = value;
            }
        }

        internal RawList<CompoundChild> children = new RawList<CompoundChild>();
        ///<summary>
        /// Gets a list of the children in the collidable.
        ///</summary>
        public ReadOnlyList<CompoundChild> Children
        {
            get
            {
                return new ReadOnlyList<CompoundChild>(children);
            }
        }



        protected override void OnEntityChanged()
        {
            for (int i = 0; i < children.count; i++)
            {
                children.Elements[i].CollisionInformation.Entity = entity;
                if (children.Elements[i].Material == null)
                    children.Elements[i].Material = entity.material;
            }
            base.OnEntityChanged();
        }




        private CompoundChild GetChild(CompoundChildData data, int index)
        {
            var instance = data.Entry.Shape.GetCollidableInstance();

            if (data.Events != null)
                instance.Events = data.Events;
            //Establish the link between the child event manager and our event manager.
            instance.events.Parent = Events;

            if (data.CollisionRules != null)
                instance.CollisionRules = data.CollisionRules;

            instance.Tag = data.Tag;

            if (data.Material == null)
                data.Material = new Material();

            return new CompoundChild(Shape, instance, data.Material, index);
        }

        private CompoundChild GetChild(CompoundShapeEntry entry, int index)
        {
            var instance = entry.Shape.GetCollidableInstance();
            //Establish the link between the child event manager and our event manager.
            instance.events.Parent = Events;
            return new CompoundChild(Shape, instance, index);
        }

        //Used to efficiently split compounds.
        internal CompoundCollidable()
        {
            Events = new CompoundEventManager();

            hierarchy = new CompoundHierarchy(this);

        }

        ///<summary>
        /// Constructs a compound collidable using additional information about the shapes in the compound.
        ///</summary>
        ///<param name="children">Data representing the children of the compound collidable.</param>
        public CompoundCollidable(IList<CompoundChildData> children)
        {
            Events = new CompoundEventManager();

            var shapeList = new RawList<CompoundShapeEntry>();
            //Create the shape first.
            for (int i = 0; i < children.Count; i++)
            {
                shapeList.Add(children[i].Entry);
            }
            base.Shape = new CompoundShape(shapeList);
            //Now create the actual child objects.
            for (int i = 0; i < children.Count; i++)
            {
                this.children.Add(GetChild(children[i], i));
            }
            hierarchy = new CompoundHierarchy(this);

        }

        ///<summary>
        /// Constructs a compound collidable using additional information about the shapes in the compound.
        ///</summary>
        ///<param name="children">Data representing the children of the compound collidable.</param>
        ///<param name="center">Location computed to be the center of the compound object.</param>
        public CompoundCollidable(IList<CompoundChildData> children, out Vector3 center)
        {
            Events = new CompoundEventManager();

            var shapeList = new RawList<CompoundShapeEntry>();
            //Create the shape first.
            for (int i = 0; i < children.Count; i++)
            {
                shapeList.Add(children[i].Entry);
            }
            base.Shape = new CompoundShape(shapeList, out center);
            //Now create the actual child objects.
            for (int i = 0; i < children.Count; i++)
            {
                this.children.Add(GetChild(children[i], i));
            }
            hierarchy = new CompoundHierarchy(this);

        }


        ///<summary>
        /// Constructs a new CompoundCollidable.
        ///</summary>
        ///<param name="compoundShape">Compound shape to use for the collidable.</param>
        public CompoundCollidable(CompoundShape compoundShape)
            : base(compoundShape)
        {
            Events = new CompoundEventManager();

            for (int i = 0; i < compoundShape.shapes.count; i++)
            {
                CompoundChild child = GetChild(compoundShape.shapes.Elements[i], i);
                this.children.Add(child);
            }
            hierarchy = new CompoundHierarchy(this);


        }






        internal CompoundHierarchy hierarchy;
        ///<summary>
        /// Gets the hierarchy of children used by the collidable.
        ///</summary>
        public CompoundHierarchy Hierarchy
        {
            get
            {
                return hierarchy;
            }
        }


        ///<summary>
        /// Updates the world transform of the collidable.
        ///</summary>
        ///<param name="position">Position to use for the calculation.</param>
        ///<param name="orientation">Orientation to use for the calculation.</param>
        public override void UpdateWorldTransform(ref Vector3 position, ref Quaternion orientation)
        {
            base.UpdateWorldTransform(ref position, ref orientation);
            var shapeList = Shape.shapes;
            for (int i = 0; i < children.count; i++)
            {
                RigidTransform transform;
                RigidTransform.Transform(ref shapeList.Elements[children.Elements[i].shapeIndex].LocalTransform, ref worldTransform, out transform);
                children.Elements[i].CollisionInformation.UpdateWorldTransform(ref transform.Position, ref transform.Orientation);
            }
        }

        protected internal override void UpdateBoundingBoxInternal(float dt)
        {
            for (int i = 0; i < children.count; i++)
            {
                children.Elements[i].CollisionInformation.UpdateBoundingBoxInternal(dt);
            }
            hierarchy.Tree.Refit();
            boundingBox = hierarchy.Tree.BoundingBox;

        }


        /// <summary>
        /// Tests a ray against the collidable.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="rayHit">Hit location of the ray on the collidable, if any.</param>
        /// <returns>Whether or not the ray hit the collidable.</returns>
        public override bool RayCast(Ray ray, float maximumLength, out RayHit rayHit)
        {
            rayHit = new RayHit();
            var hitElements = Resources.GetCompoundChildList();
            if (hierarchy.Tree.GetOverlaps(ray, maximumLength, hitElements))
            {
                rayHit.T = float.MaxValue;
                for (int i = 0; i < hitElements.count; i++)
                {
                    RayHit tempHit;
                    if (hitElements.Elements[i].CollisionInformation.RayCast(ray, maximumLength, out tempHit) && tempHit.T < rayHit.T)
                    {
                        rayHit = tempHit;
                    }
                }
                Resources.GiveBack(hitElements);
                return rayHit.T != float.MaxValue;
            }
            Resources.GiveBack(hitElements);
            return false;
        }

        //TODO: Substantial redundancy here.  If a change is ever needed, compress them down.

        /// <summary>
        /// Tests a ray against the collidable.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="filter">Can be used to filter sets of objects out of the raycasting.</param>
        /// <param name="rayHit">Hit location of the ray on the collidable, if any.</param>
        /// <returns>Whether or not the ray hit the collidable.</returns>
        public override bool RayCast(Ray ray, float maximumLength, Func<BroadPhaseEntry, bool> filter, out RayHit rayHit)
        {
            rayHit = new RayHit();
            if (filter(this))
            {
                var hitElements = Resources.GetCompoundChildList();
                if (hierarchy.Tree.GetOverlaps(ray, maximumLength, hitElements))
                {
                    rayHit.T = float.MaxValue;
                    for (int i = 0; i < hitElements.count; i++)
                    {
                        RayHit tempHit;
                        if (hitElements.Elements[i].CollisionInformation.RayCast(ray, maximumLength, filter, out tempHit) && tempHit.T < rayHit.T)
                        {
                            rayHit = tempHit;
                        }
                    }
                    Resources.GiveBack(hitElements);
                    return rayHit.T != float.MaxValue;
                }
                Resources.GiveBack(hitElements);
            }
            return false;
        }


        /// <summary>
        /// Tests a ray against the compound.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="rayHit">Hit data and the hit child collidable, if any.</param>
        /// <returns>Whether or not the ray hit the entry.</returns>
        public bool RayCast(Ray ray, float maximumLength, out RayCastResult rayHit)
        {
            rayHit = new RayCastResult();
            var hitElements = Resources.GetCompoundChildList();
            if (hierarchy.Tree.GetOverlaps(ray, maximumLength, hitElements))
            {
                rayHit.HitData.T = float.MaxValue;
                for (int i = 0; i < hitElements.count; i++)
                {
                    EntityCollidable candidate = hitElements.Elements[i].CollisionInformation;
                    RayHit tempHit;
                    if (candidate.RayCast(ray, maximumLength, out tempHit) && tempHit.T < rayHit.HitData.T)
                    {
                        rayHit.HitData = tempHit;
                        rayHit.HitObject = candidate;
                    }
                }
                Resources.GiveBack(hitElements);
                return rayHit.HitData.T != float.MaxValue;
            }
            Resources.GiveBack(hitElements);
            return false;
        }

        /// <summary>
        /// Tests a ray against the collidable.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="rayHit">Hit data, if any.</param>
        /// <param name="hitChild">Child collidable hit by the ray, if any.</param>
        /// <returns>Whether or not the ray hit the entry.</returns>
        public bool RayCast(Ray ray, float maximumLength, out RayHit rayHit, out CompoundChild hitChild)
        {
            rayHit = new RayHit();
            hitChild = null;
            var hitElements = Resources.GetCompoundChildList();
            if (hierarchy.Tree.GetOverlaps(ray, maximumLength, hitElements))
            {
                rayHit.T = float.MaxValue;
                for (int i = 0; i < hitElements.count; i++)
                {
                    EntityCollidable candidate = hitElements.Elements[i].CollisionInformation;
                    RayHit tempHit;
                    if (candidate.RayCast(ray, maximumLength, out tempHit) && tempHit.T < rayHit.T)
                    {
                        rayHit = tempHit;
                        hitChild = hitElements.Elements[i];
                    }
                }
                Resources.GiveBack(hitElements);
                return rayHit.T != float.MaxValue;
            }
            Resources.GiveBack(hitElements);
            return false;
        }


        /// <summary>
        /// Casts a convex shape against the collidable.
        /// </summary>
        /// <param name="castShape">Shape to cast.</param>
        /// <param name="startingTransform">Initial transform of the shape.</param>
        /// <param name="sweep">Sweep to apply to the shape.</param>
        /// <param name="hit">Hit data, if any.</param>
        /// <returns>Whether or not the cast hit anything.</returns>
        public override bool ConvexCast(CollisionShapes.ConvexShapes.ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweep, out RayHit hit)
        {
            hit = new RayHit();
            BoundingBox boundingBox;
            Toolbox.GetExpandedBoundingBox(ref castShape, ref startingTransform, ref sweep, out boundingBox);
            var hitElements = Resources.GetCompoundChildList();
            if (hierarchy.Tree.GetOverlaps(boundingBox, hitElements))
            {
                hit.T = float.MaxValue;
                for (int i = 0; i < hitElements.count; i++)
                {
                    var candidate = hitElements.Elements[i].CollisionInformation;
                    RayHit tempHit;
                    if (candidate.ConvexCast(castShape, ref startingTransform, ref sweep, out tempHit) && tempHit.T < hit.T)
                    {
                        hit = tempHit;
                    }
                }
                Resources.GiveBack(hitElements);
                return hit.T != float.MaxValue;
            }
            Resources.GiveBack(hitElements);
            return false;
        }

    }

    ///<summary>
    /// Data which can be used to create a CompoundChild.
    /// This data is not itself a child yet; another system
    /// will use it as input to construct the children.
    ///</summary>
    public struct CompoundChildData
    {
        ///<summary>
        /// Shape entry of the compound child.
        ///</summary>
        public CompoundShapeEntry Entry;
        ///<summary>
        /// Event manager for the new child.
        ///</summary>
        public ContactEventManager<EntityCollidable> Events;
        ///<summary>
        /// Collision rules for the new child.
        ///</summary>
        public CollisionRules CollisionRules;
        ///<summary>
        /// Material for the new child.
        ///</summary>
        public Material Material;
        /// <summary>
        /// Tag to assign to the collidable created for this child.
        /// </summary>
        public object Tag;

    }


    ///<summary>
    /// A collidable child of a compound.
    ///</summary>
    public class CompoundChild : IBoundingBoxOwner
    {
        CompoundShape shape;
        internal int shapeIndex;

        /// <summary>
        /// Gets the index of the shape used by this child in the CompoundShape's shapes list.
        /// </summary>
        public int ShapeIndex
        {
            get
            {
                return shapeIndex;
            }
        }

        private EntityCollidable collisionInformation;
        ///<summary>
        /// Gets the Collidable associated with the child.
        ///</summary>
        public EntityCollidable CollisionInformation
        {
            get
            {
                return collisionInformation;
            }
        }

        ///<summary>
        /// Gets or sets the material associated with the child.
        ///</summary>
        public Material Material { get; set; }

        /// <summary>
        /// Gets the index of the shape associated with this child in the CompoundShape's shapes list.
        /// </summary>
        public CompoundShapeEntry Entry
        {
            get
            {
                return shape.shapes.Elements[shapeIndex];
            }

        }

        internal CompoundChild(CompoundShape shape, EntityCollidable collisionInformation, Material material, int index)
        {
            this.shape = shape;
            this.collisionInformation = collisionInformation;
            Material = material;
            this.shapeIndex = index;
        }

        internal CompoundChild(CompoundShape shape, EntityCollidable collisionInformation, int index)
        {
            this.shape = shape;
            this.collisionInformation = collisionInformation;
            this.shapeIndex = index;
        }

        /// <summary>
        /// Gets the bounding box of the child.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get { return collisionInformation.boundingBox; }
        }


    }
}
