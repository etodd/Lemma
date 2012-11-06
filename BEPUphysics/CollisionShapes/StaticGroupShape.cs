using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using System.Collections.Generic;
using BEPUphysics.Collidables;
using BEPUphysics.ResourceManagement;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// The shape information used by a StaticGroup.
    /// Unlike most shapes, a StaticGroupShape cannot be shared between multiple StaticGroups;
    /// a StaticGroupShape is linked to a single StaticGroup.
    ///</summary>
    public class StaticGroupShape : CollisionShape
    {
        //Technically, while this is superior to the 'put a bunch of stuff in the Space' approach, it is still 
        //inferior in maximum performance to an approach which allows for the direct comparison of a static tree against a nonstatic tree.
        //This would require tighter integration with the broad phase, though.  It's not clear that the performance boost is worth it.

        /// <summary>
        /// Gets the StaticGroup associated with this StaticGroupShape.  Unlike most shapes, there is a one-to-one relationship
        /// between StaticGroupShapes and StaticGroups.
        /// </summary>
        public StaticGroup StaticGroup
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the bounding box tree associated with this shape.
        /// Contains Collidable instances as opposed to shapes.
        /// </summary>
        public BoundingBoxTree<Collidable> CollidableTree
        {
            get;
            private set;
        }


        ///<summary>
        /// Constructs a new StaticGroupShape.
        ///</summary>
        ///<param name="collidables">List of collidables in the StaticGroup.</param>
        ///<param name="owner">StaticGroup directly associated with this shape.</param>
        public StaticGroupShape(IList<Collidable> collidables, StaticGroup owner)
        {
            this.StaticGroup = owner;
            CollidableTree = new BoundingBoxTree<Collidable>(collidables);
            //Rather than hooking up a bunch of ShapeChanged events here that don't capture the full capacity of change
            //in our child collidables, we will rely on the user telling the collidable tree to reformat itself directly.
        }


        /// <summary>
        /// Tests a ray against the collidable.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="result">Hit data, if any.</param>
        /// <returns>Whether or not the ray hit the entry.</returns>
        public bool RayCast(Ray ray, float maximumLength, out RayCastResult result)
        {
            var outputOverlappedElements = Resources.GetCollidableList();
            var rayHits = Resources.GetRayHitList();
            CollidableTree.GetOverlaps(ray, maximumLength, outputOverlappedElements);
            result = new RayCastResult();
            result.HitData.T = float.MaxValue;
            for (int i = 0; i < outputOverlappedElements.count; ++i)
            {
                RayHit hit;
                if (outputOverlappedElements.Elements[i].RayCast(ray, maximumLength, out hit))
                {
                    if (hit.T < result.HitData.T)
                    {
                        result.HitData = hit;
                        result.HitObject = outputOverlappedElements.Elements[i];
                    }
                }
            }
            Resources.GiveBack(rayHits);
            Resources.GiveBack(outputOverlappedElements);
            return result.HitData.T < float.MaxValue;
        }


        /// <summary>
        /// Casts a convex shape against the collidable.
        /// </summary>
        /// <param name="castShape">Shape to cast.</param>
        /// <param name="startingTransform">Initial transform of the shape.</param>
        /// <param name="sweep">Sweep to apply to the shape.</param>
        /// <param name="result">Hit data, if any.</param>
        /// <returns>Whether or not the cast hit anything.</returns>
        public bool ConvexCast(ConvexShapes.ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweep, out RayCastResult result)
        {
            var outputOverlappedElements = Resources.GetCollidableList();
            var rayHits = Resources.GetRayHitList();
            BoundingBox boundingBox;
            Toolbox.GetExpandedBoundingBox(ref castShape, ref startingTransform, ref sweep, out boundingBox);

            CollidableTree.GetOverlaps(boundingBox, outputOverlappedElements);
            result = new RayCastResult();
            result.HitData.T = float.MaxValue;
            for (int i = 0; i < outputOverlappedElements.count; ++i)
            {
                RayHit hit;
                if (outputOverlappedElements.Elements[i].ConvexCast(castShape, ref startingTransform, ref sweep, out hit))
                {
                    if (hit.T < result.HitData.T)
                    {
                        result.HitData = hit;
                        result.HitObject = outputOverlappedElements.Elements[i];
                    }
                }
            }
            Resources.GiveBack(rayHits);
            Resources.GiveBack(outputOverlappedElements);
            return result.HitData.T < float.MaxValue;
        }


    }
}
