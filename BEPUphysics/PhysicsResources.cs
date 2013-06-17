using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionTests.Manifolds;
using BEPUphysics.Entities;
 
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.DeactivationManagement;
using BEPUutilities;
using BEPUutilities.DataStructures;
using BEPUutilities.ResourceManagement;
using Microsoft.Xna.Framework;

namespace BEPUphysics
{
    /// <summary>
    /// Handles allocation and management of commonly used resources.
    /// </summary>
    public static class PhysicsResources
    {
        static PhysicsResources()
        {
            ResetPools();
        }

        public static void ResetPools()
        {

            SubPoolRayCastResultList = new LockingResourcePool<RawList<RayCastResult>>();
            SubPoolBroadPhaseEntryList = new LockingResourcePool<RawList<BroadPhaseEntry>>();
            SubPoolCollidableList = new LockingResourcePool<RawList<Collidable>>();
            SubPoolCompoundChildList = new LockingResourcePool<RawList<CompoundChild>>();

            SubPoolEntityRawList = new LockingResourcePool<RawList<Entity>>(16);
            SubPoolTriangleShape = new LockingResourcePool<TriangleShape>();
            SubPoolTriangleCollidables = new LockingResourcePool<TriangleCollidable>();
            SubPoolTriangleIndicesList = new LockingResourcePool<RawList<TriangleMeshConvexContactManifold.TriangleIndices>>();
            SimulationIslandConnections = new LockingResourcePool<SimulationIslandConnection>();
        }

        static ResourcePool<RawList<RayCastResult>> SubPoolRayCastResultList;
        static ResourcePool<RawList<BroadPhaseEntry>> SubPoolBroadPhaseEntryList;
        static ResourcePool<RawList<Collidable>> SubPoolCollidableList;
        static ResourcePool<RawList<Entity>> SubPoolEntityRawList;
        static ResourcePool<TriangleShape> SubPoolTriangleShape;
        static ResourcePool<RawList<CompoundChild>> SubPoolCompoundChildList;
        static ResourcePool<TriangleCollidable> SubPoolTriangleCollidables;
        static ResourcePool<RawList<TriangleMeshConvexContactManifold.TriangleIndices>> SubPoolTriangleIndicesList;
        static ResourcePool<SimulationIslandConnection> SimulationIslandConnections;
        //#endif
        /// <summary>
        /// Retrieves a ray cast result list from the resource pool.
        /// </summary>
        /// <returns>Empty ray cast result list.</returns>
        public static RawList<RayCastResult> GetRayCastResultList()
        {
            return SubPoolRayCastResultList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<RayCastResult> list)
        {
            list.Clear();
            SubPoolRayCastResultList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves an BroadPhaseEntry list from the resource pool.
        /// </summary>
        /// <returns>Empty BroadPhaseEntry list.</returns>
        public static RawList<BroadPhaseEntry> GetBroadPhaseEntryList()
        {
            return SubPoolBroadPhaseEntryList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<BroadPhaseEntry> list)
        {
            list.Clear();
            SubPoolBroadPhaseEntryList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves a Collidable list from the resource pool.
        /// </summary>
        /// <returns>Empty Collidable list.</returns>
        public static RawList<Collidable> GetCollidableList()
        {
            return SubPoolCollidableList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<Collidable> list)
        {
            list.Clear();
            SubPoolCollidableList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves an CompoundChild list from the resource pool.
        /// </summary>
        /// <returns>Empty information list.</returns>
        public static RawList<CompoundChild> GetCompoundChildList()
        {
            return SubPoolCompoundChildList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<CompoundChild> list)
        {
            list.Clear();
            SubPoolCompoundChildList.GiveBack(list);
        }

      

        /// <summary>
        /// Retrieves an Entity RawList from the resource pool.
        /// </summary>
        /// <returns>Empty Entity raw list.</returns>
        public static RawList<Entity> GetEntityRawList()
        {
            return SubPoolEntityRawList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<Entity> list)
        {
            list.Clear();
            SubPoolEntityRawList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves a Triangle shape from the resource pool.
        /// </summary>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        /// <returns>Initialized TriangleShape.</returns>
        public static TriangleShape GetTriangle(ref Vector3 v1, ref Vector3 v2, ref Vector3 v3)
        {
            TriangleShape toReturn = SubPoolTriangleShape.Take();
            toReturn.vA = v1;
            toReturn.vB = v2;
            toReturn.vC = v3;
            return toReturn;
        }

        /// <summary>
        /// Retrieves a Triangle shape from the resource pool.
        /// </summary>
        /// <returns>Initialized TriangleShape.</returns>
        public static TriangleShape GetTriangle()
        {
            return SubPoolTriangleShape.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="triangle">Triangle to return.</param>
        public static void GiveBack(TriangleShape triangle)
        {
            triangle.collisionMargin = 0;
            triangle.sidedness = TriangleSidedness.DoubleSided;
            SubPoolTriangleShape.GiveBack(triangle);
        }


        /// <summary>
        /// Retrieves a TriangleCollidable from the resource pool.
        /// </summary>
        /// <param name="a">First vertex in the triangle.</param>
        /// <param name="b">Second vertex in the triangle.</param>
        /// <param name="c">Third vertex in the triangle.</param>
        /// <returns>Initialized TriangleCollidable.</returns>
        public static TriangleCollidable GetTriangleCollidable(ref Vector3 a, ref Vector3 b, ref Vector3 c)
        {
            var tri = SubPoolTriangleCollidables.Take();
            var shape = tri.Shape;
            shape.vA = a;
            shape.vB = b;
            shape.vC = c;
            var identity = RigidTransform.Identity;
            tri.UpdateBoundingBoxForTransform(ref identity);
            return tri;

        }

        /// <summary>
        /// Retrieves a TriangleCollidable from the resource pool.
        /// </summary>
        /// <returns>Initialized TriangleCollidable.</returns>
        public static TriangleCollidable GetTriangleCollidable()
        {
            return SubPoolTriangleCollidables.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="triangle">Triangle collidable to return.</param>
        public static void GiveBack(TriangleCollidable triangle)
        {
            triangle.CleanUp();
            SubPoolTriangleCollidables.GiveBack(triangle);
        }

        /// <summary>
        /// Retrieves a TriangleIndices list from the resource pool.
        /// </summary>
        /// <returns>TriangleIndices list.</returns>
        public static RawList<TriangleMeshConvexContactManifold.TriangleIndices> GetTriangleIndicesList()
        {
            return SubPoolTriangleIndicesList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="triangleIndices">TriangleIndices list to return.</param>
        public static void GiveBack(RawList<TriangleMeshConvexContactManifold.TriangleIndices> triangleIndices)
        {
            triangleIndices.Clear();
            SubPoolTriangleIndicesList.GiveBack(triangleIndices);
        }

        /// <summary>
        /// Retrieves a simulation island connection from the resource pool.
        /// </summary>
        /// <returns>Uninitialized simulation island connection.</returns>
        public static SimulationIslandConnection GetSimulationIslandConnection()
        {
            return SimulationIslandConnections.Take();

        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="connection">Connection to return.</param>
        public static void GiveBack(SimulationIslandConnection connection)
        {
            connection.CleanUp();
            SimulationIslandConnections.GiveBack(connection);

        }
    }
}