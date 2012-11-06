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
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionTests.Manifolds;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Handles a mobile mesh-mobile mesh collision pair.
    ///</summary>
    public class MobileMeshTerrainPairHandler : MobileMeshMeshPairHandler
    {


        Terrain mesh;

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
            Vector3 terrainUp = new Vector3(mesh.worldTransform.LinearTransform.M21, mesh.worldTransform.LinearTransform.M22, mesh.worldTransform.LinearTransform.M23);
            float dot;
            Vector3 AB, AC, normal;
            var shape = toReturn.Shape;
            mesh.Shape.GetTriangle(index, ref mesh.worldTransform, out shape.vA, out shape.vB, out shape.vC);
            Vector3 center;
            Vector3.Add(ref shape.vA, ref shape.vB, out center);
            Vector3.Add(ref center, ref shape.vC, out center);
            Vector3.Multiply(ref center, 1 / 3f, out center);
            Vector3.Subtract(ref shape.vA, ref center, out shape.vA);
            Vector3.Subtract(ref shape.vB, ref center, out shape.vB);
            Vector3.Subtract(ref shape.vC, ref center, out shape.vC);

            //The bounding box doesn't update by itself.
            toReturn.worldTransform.Position = center;
            toReturn.worldTransform.Orientation = Quaternion.Identity;
            toReturn.UpdateBoundingBoxInternal(0);

            Vector3.Subtract(ref shape.vB, ref shape.vA, out AB);
            Vector3.Subtract(ref shape.vC, ref shape.vA, out AC);
            Vector3.Cross(ref AB, ref AC, out normal);
            Vector3.Dot(ref terrainUp, ref normal, out dot);
            if (dot > 0)
            {
                shape.sidedness = TriangleSidedness.Clockwise;
            }
            else
            {
                shape.sidedness = TriangleSidedness.Counterclockwise;
            }
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
            mesh = entryA as Terrain;
            if (mesh == null)
            {
                mesh = entryB as Terrain;
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
            mesh.Shape.GetOverlaps(localBoundingBox, overlappedElements);
            for (int i = 0; i < overlappedElements.count; i++)
            {
                TryToAdd(overlappedElements.Elements[i]);
            }

            Resources.GiveBack(overlappedElements);

        }


    }
}
