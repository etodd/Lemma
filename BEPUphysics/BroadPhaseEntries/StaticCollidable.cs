using System;
using BEPUphysics.Collidables.Events;
using BEPUphysics.CollisionShapes;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.Materials;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.ResourceManagement;
using BEPUphysics.OtherSpaceStages;

namespace BEPUphysics.Collidables
{
    ///<summary>
    /// Superclass of static collidable objects which can be added directly to a space.  Static objects cannot move.
    ///</summary>
    public abstract class StaticCollidable : Collidable, ISpaceObject, IMaterialOwner, IDeferredEventCreatorOwner
    {


        ///<summary>
        /// Performs common initialization.
        ///</summary>
        protected StaticCollidable()
        {
            collisionRules.group = CollisionRules.DefaultKinematicCollisionGroup;
            //Note that the Events manager is not created here.  That is left for subclasses to implement so that the type is more specific.
            //Entities can get away with having EntityCollidable specificity since you generally care more about the entity than the collidable,
            //but with static objects, the collidable is the only important object.  It would be annoying to cast to the type you know it is every time
            //just to get access to some type-specific properties.

            material = new Material();
            materialChangedDelegate = OnMaterialChanged;
            material.MaterialChanged += materialChangedDelegate;
        }

        protected override void OnShapeChanged(CollisionShape collisionShape)
        {
            if (!IgnoreShapeChanges)
                UpdateBoundingBox();
        }

        internal Material material;
        //NOT thread safe due to material change pair update.
        ///<summary>
        /// Gets or sets the material used by the collidable.
        ///</summary>
        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                if (material != null)
                    material.MaterialChanged -= materialChangedDelegate;
                material = value;
                if (material != null)
                    material.MaterialChanged += materialChangedDelegate;
                OnMaterialChanged(material);
            }
        }

        Action<Material> materialChangedDelegate;
        protected virtual void OnMaterialChanged(Material newMaterial)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                pairs[i].UpdateMaterialProperties();
            }
        }

        protected internal override bool IsActive
        {
            get { return false; }
        }

        

        ISpace space;
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
        /// Gets the space that owns the mesh.
        ///</summary>
        public ISpace Space
        {
            get
            {
                return space;
            }
        }

        void ISpaceObject.OnAdditionToSpace(ISpace newSpace)
        {
        }

        void ISpaceObject.OnRemovalFromSpace(ISpace oldSpace)
        {
        }



        IDeferredEventCreator IDeferredEventCreatorOwner.EventCreator
        {
            get
            {
                return EventCreator;
            }
        }

        /// <summary>
        /// Gets the event creator associated with this collidable.
        /// </summary>
        protected abstract IDeferredEventCreator EventCreator
        {
            get;
        }
    }
}
