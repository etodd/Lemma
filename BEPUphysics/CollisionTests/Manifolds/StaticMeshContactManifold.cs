using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUutilities.DataStructures;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contacts between a static mesh and a convex.
    ///</summary>
    public abstract class StaticMeshContactManifold : TriangleMeshConvexContactManifold
    {


        protected StaticMesh mesh;

        internal RawList<int> overlappedTriangles = new RawList<int>(4);

        ///<summary>
        /// Gets the static mesh associated with this pair.
        ///</summary>
        public StaticMesh Mesh
        {
            get
            {
                return mesh;
            }
        }

        protected internal override int FindOverlappingTriangles(float dt)
        {
            mesh.Mesh.Tree.GetOverlaps(convex.boundingBox, overlappedTriangles);
            return overlappedTriangles.Count;
        }

        protected override bool ConfigureTriangle(int i, out TriangleIndices indices)
        {
            int triangleIndex = overlappedTriangles.Elements[i];
            mesh.Mesh.Data.GetTriangle(triangleIndex, out localTriangleShape.vA, out localTriangleShape.vB, out localTriangleShape.vC);
            localTriangleShape.sidedness = mesh.sidedness;
            localTriangleShape.collisionMargin = 0;
            indices = new TriangleIndices
                          {
                              A = mesh.Mesh.Data.indices[triangleIndex],
                              B = mesh.Mesh.Data.indices[triangleIndex + 1],
                              C = mesh.Mesh.Data.indices[triangleIndex + 2]
                          };
            return true;
        }

        protected internal override void CleanUpOverlappingTriangles()
        {
            overlappedTriangles.Clear();
        }

        protected override bool UseImprovedBoundaryHandling
        {
            get { return mesh.improveBoundaryBehavior; }
        }


        ///<summary>
        /// Cleans up the manifold.
        ///</summary>
        public override void CleanUp()
        {
            mesh = null;
            convex = null;
            base.CleanUp();
        }

        ///<summary>
        /// Initializes the manifold.
        ///</summary>
        ///<param name="newCollidableA">First collidable.</param>
        ///<param name="newCollidableB">Second collidable.</param>
        public override void Initialize(Collidable newCollidableA, Collidable newCollidableB)
        {
            convex = newCollidableA as ConvexCollidable;
            mesh = newCollidableB as StaticMesh;


            if (convex == null || mesh == null)
            {
                convex = newCollidableB as ConvexCollidable;
                mesh = newCollidableA as StaticMesh;
                if (convex == null || mesh == null)
                    throw new ArgumentException("Inappropriate types used to initialize contact manifold.");
            }

        }


    }
}
