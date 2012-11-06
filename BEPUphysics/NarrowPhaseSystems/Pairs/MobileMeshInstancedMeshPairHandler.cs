using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.Collision;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionTests;
using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a mobile mesh-mobile mesh collision pair.
    ///</summary>
    public class MobileMeshInstancedMeshPairHandler : MobileMeshMeshPairHandler
    {


        InstancedMesh mesh;

        public override Collidable CollidableB
        {
            get { return mesh; }
        }
        public override Entities.Entity EntityB
        {
            get { return null; }
        }
        protected override Materials.Material MaterialB
        {
            get { return mesh.material; }
        }

        protected override TriangleCollidable GetOpposingCollidable(int index)
        {
            //Construct a TriangleCollidable from the static mesh.
            var toReturn = Resources.GetTriangleCollidable();
            var shape = toReturn.Shape;
            mesh.Shape.TriangleMesh.Data.GetTriangle(index, out shape.vA, out shape.vB, out shape.vC);
            Matrix3X3.Transform(ref shape.vA, ref mesh.worldTransform.LinearTransform, out shape.vA);
            Matrix3X3.Transform(ref shape.vB, ref mesh.worldTransform.LinearTransform, out shape.vB);
            Matrix3X3.Transform(ref shape.vC, ref mesh.worldTransform.LinearTransform, out shape.vC);
            Vector3 center;
            Vector3.Add(ref shape.vA, ref shape.vB, out center);
            Vector3.Add(ref center, ref shape.vC, out center);
            Vector3.Multiply(ref center, 1 / 3f, out center);
            Vector3.Subtract(ref shape.vA, ref center, out shape.vA);
            Vector3.Subtract(ref shape.vB, ref center, out shape.vB);
            Vector3.Subtract(ref shape.vC, ref center, out shape.vC);

            Vector3.Add(ref center, ref mesh.worldTransform.Translation, out center);
            //The bounding box doesn't update by itself.
            toReturn.worldTransform.Position = center;
            toReturn.worldTransform.Orientation = Quaternion.Identity;
            toReturn.UpdateBoundingBoxInternal(0);
            shape.sidedness = mesh.Sidedness;
            shape.collisionMargin = mobileMesh.Shape.MeshCollisionMargin;
            return toReturn;
        }

      

        protected override void ConfigureCollidable(TriangleEntry entry, float dt)
        {

        }

        ///<summary>
        /// Initializes the pair handler.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        ///<param name="entryB">Second entry in the pair.</param>
        public override void Initialize(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            mesh = entryA as InstancedMesh;
            if (mesh == null)
            {
                mesh = entryB as InstancedMesh;
                if (mesh == null)
                {
                    throw new Exception("Inappropriate types used to initialize pair.");
                }
            }

            base.Initialize(entryA, entryB);
        }






        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {

            base.CleanUp();
            mesh = null;


        }




        protected override void UpdateContainedPairs(float dt)
        {
            var overlappedElements = Resources.GetIntList();
            BoundingBox localBoundingBox;

            Vector3 sweep;
            Vector3.Multiply(ref mobileMesh.entity.linearVelocity, dt, out sweep);
            mobileMesh.Shape.GetSweptLocalBoundingBox(ref mobileMesh.worldTransform, ref mesh.worldTransform, ref sweep, out localBoundingBox);
            mesh.Shape.TriangleMesh.Tree.GetOverlaps(localBoundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.count; i++)
            {
                TryToAdd(overlappedElements.Elements[i]);
            }

            Resources.GiveBack(overlappedElements);

        }


    }
}
