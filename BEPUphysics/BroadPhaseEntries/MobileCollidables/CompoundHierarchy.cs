using System;
using BEPUphysics.DataStructures;

namespace BEPUphysics.Collidables.MobileCollidables
{
    ///<summary>
    /// Hierarchy of children used to accelerate queries and tests for compound collidables.
    ///</summary>
    public class CompoundHierarchy
    {
        private BoundingBoxTree<CompoundChild> tree;
        ///<summary>
        /// Gets the bounding box tree of the hierarchy.
        ///</summary>
        public BoundingBoxTree<CompoundChild> Tree
        {
            get
            {
                return tree;
            }
        }

        private CompoundCollidable owner;
        ///<summary>
        /// Gets the CompoundCollidable that owns this hierarchy.
        ///</summary>
        public CompoundCollidable Owner
        {
            get
            {
                return owner;
            }
        }

        ///<summary>
        /// Constructs a new compound hierarchy.
        ///</summary>
        ///<param name="owner">Owner of the hierarchy.</param>
        public CompoundHierarchy(CompoundCollidable owner)
        {
            this.owner = owner;
            var children = new CompoundChild[owner.children.count];
            Array.Copy(owner.children.Elements, children, owner.children.count);
            //In order to initialize a good tree, the local space bounding boxes should first be computed.
            //Otherwise, the tree would try to create a hierarchy based on a bunch of zeroed out bounding boxes!
            for (int i = 0; i < children.Length; i++)
            {
                children[i].CollisionInformation.worldTransform = owner.Shape.shapes.Elements[i].LocalTransform;
                children[i].CollisionInformation.UpdateBoundingBoxInternal(0);
            }
            tree = new BoundingBoxTree<CompoundChild>(children);
        }



    }
}
