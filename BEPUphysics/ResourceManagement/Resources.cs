using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.DataStructures;
using BEPUphysics.DeactivationManagement;
using System.Diagnostics;
using BEPUphysics.Collidables;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.ResourceManagement
{
    /// <summary>
    /// Handles allocation and management of commonly used resources.
    /// </summary>
    public static class Resources
    {
        static Resources()
        {
            ResetPools();
        }

        public static void ResetPools()
        {
            SubPoolRayHitList = new LockingResourcePool<RawList<RayHit>>();
            SubPoolRayCastResultList = new LockingResourcePool<RawList<RayCastResult>>();
            SubPoolBroadPhaseEntryList = new LockingResourcePool<RawList<BroadPhaseEntry>>();
            SubPoolCollidableList = new LockingResourcePool<RawList<Collidable>>();
            SubPoolCompoundChildList = new LockingResourcePool<RawList<CompoundChild>>();
            SubPoolIntList = new LockingResourcePool<RawList<int>>();
            SubPoolIntSet = new LockingResourcePool<HashSet<int>>();
            SubPoolFloatList = new LockingResourcePool<RawList<float>>();
            SubPoolVectorList = new LockingResourcePool<RawList<Vector3>>();
            SubPoolEntityRawList = new LockingResourcePool<RawList<Entity>>(16);
            SubPoolTriangleShape = new LockingResourcePool<TriangleShape>();
            SubPoolTriangleCollidables = new LockingResourcePool<TriangleCollidable>();
            SubPoolTriangleIndicesList = new LockingResourcePool<RawList<BEPUphysics.CollisionTests.Manifolds.TriangleMeshConvexContactManifold.TriangleIndices>>();
            SimulationIslandConnections = new LockingResourcePool<SimulationIslandConnection>();
        }

        //#if WINDOWS
        //        //[ThreadStatic]
        //        private static ResourcePool<List<bool>> subPoolBoolList;

        //        private static ResourcePool<List<bool>> SubPoolBoolList
        //        {
        //            get { return subPoolBoolList ?? (subPoolBoolList = new UnsafeResourcePool<List<bool>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<RawList<RayHit>> subPoolRayHitList;

        //        private static ResourcePool<RawList<RayHit>> SubPoolRayHitList
        //        {
        //            get { return subPoolRayHitList ?? (subPoolRayHitList = new UnsafeResourcePool<RawList<RayHit>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<RawList<RayCastResult>> subPoolRayCastResultList;

        //        private static ResourcePool<RawList<RayCastResult>> SubPoolRayCastResultList
        //        {
        //            get { return subPoolRayCastResultList ?? (subPoolRayCastResultList = new UnsafeResourcePool<RawList<RayCastResult>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<RawList<BroadPhaseEntry>> subPoolCollisionEntryList;

        //        private static ResourcePool<RawList<BroadPhaseEntry>> SubPoolCollisionEntryList
        //        {
        //            get { return subPoolCollisionEntryList ?? (subPoolCollisionEntryList = new UnsafeResourcePool<RawList<BroadPhaseEntry>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<RawList<CompoundChild>> subPoolCompoundChildList;

        //        private static ResourcePool<RawList<CompoundChild>> SubPoolCompoundChildList
        //        {
        //            get { return subPoolCompoundChildList ?? (subPoolCompoundChildList = new UnsafeResourcePool<RawList<CompoundChild>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<List<int>> subPoolIntList;

        //        private static ResourcePool<List<int>> SubPoolIntList
        //        {
        //            get { return subPoolIntList ?? (subPoolIntList = new UnsafeResourcePool<List<int>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<Queue<int>> subPoolIntQueue;

        //        private static ResourcePool<Queue<int>> SubPoolIntQueue
        //        {
        //            get { return subPoolIntQueue ?? (subPoolIntQueue = new UnsafeResourcePool<Queue<int>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<List<float>> subPoolFloatList;

        //        private static ResourcePool<List<float>> SubPoolFloatList
        //        {
        //            get { return subPoolFloatList ?? (subPoolFloatList = new UnsafeResourcePool<List<float>>()); }
        //        }

        //        //[ThreadStatic]
        //        private static ResourcePool<List<Vector3>> subPoolVectorList;

        //        private static ResourcePool<List<Vector3>> SubPoolVectorList
        //        {
        //            get { return subPoolVectorList ?? (subPoolVectorList = new UnsafeResourcePool<List<Vector3>>()); }
        //        }

        //        private static readonly ResourcePool<List<Entity>> SubPoolEntityList = new LockingResourcePool<List<Entity>>(16, InitializeEntityListDelegate);

        //        //[ThreadStatic]
        //        private static ResourcePool<RawList<Entity>> subPoolEntityRawList;
        //        private static ResourcePool<RawList<Entity>> SubPoolEntityRawList
        //        {
        //            get { return subPoolEntityRawList ?? (subPoolEntityRawList = new UnsafeResourcePool<RawList<Entity>>(16)); }
        //        }


        //        //[ThreadStatic]
        //        private static ResourcePool<Queue<Entity>> subPoolEntityQueue;

        //        private static ResourcePool<Queue<Entity>> SubPoolEntityQueue
        //        {
        //            get { return subPoolEntityQueue ?? (subPoolEntityQueue = new UnsafeResourcePool<Queue<Entity>>()); }
        //        }



        //        //[ThreadStatic]
        //        private static ResourcePool<TriangleShape> subPoolTriangleShape;

        //        private static ResourcePool<TriangleShape> SubPoolTriangleShape
        //        {
        //            get { return subPoolTriangleShape ?? (subPoolTriangleShape = new UnsafeResourcePool<TriangleShape>()); }
        //        }

        //#else
        static ResourcePool<RawList<RayHit>> SubPoolRayHitList;
        static ResourcePool<RawList<RayCastResult>> SubPoolRayCastResultList;
        static ResourcePool<RawList<BroadPhaseEntry>> SubPoolBroadPhaseEntryList;
        static ResourcePool<RawList<Collidable>> SubPoolCollidableList;
        static ResourcePool<RawList<int>> SubPoolIntList;
        static ResourcePool<HashSet<int>> SubPoolIntSet;
        static ResourcePool<RawList<float>> SubPoolFloatList;
        static ResourcePool<RawList<Vector3>> SubPoolVectorList;
        static ResourcePool<RawList<Entity>> SubPoolEntityRawList;
        static ResourcePool<TriangleShape> SubPoolTriangleShape;
        static ResourcePool<RawList<CompoundChild>> SubPoolCompoundChildList;
        static ResourcePool<TriangleCollidable> SubPoolTriangleCollidables;
        static ResourcePool<RawList<BEPUphysics.CollisionTests.Manifolds.TriangleMeshConvexContactManifold.TriangleIndices>> SubPoolTriangleIndicesList;
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
        /// Retrieves a ray hit list from the resource pool.
        /// </summary>
        /// <returns>Empty ray hit list.</returns>
        public static RawList<RayHit> GetRayHitList()
        {
            return SubPoolRayHitList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<RayHit> list)
        {
            list.Clear();
            SubPoolRayHitList.GiveBack(list);
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
        /// Retrieves a int list from the resource pool.
        /// </summary>
        /// <returns>Empty int list.</returns>
        public static RawList<int> GetIntList()
        {
            return SubPoolIntList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<int> list)
        {
            list.Clear();
            SubPoolIntList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves a int hash set from the resource pool.
        /// </summary>
        /// <returns>Empty int set.</returns>
        public static HashSet<int> GetIntSet()
        {
            return SubPoolIntSet.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="set">Set to return.</param>
        public static void GiveBack(HashSet<int> set)
        {
            set.Clear();
            SubPoolIntSet.GiveBack(set);
        }

        /// <summary>
        /// Retrieves a float list from the resource pool.
        /// </summary>
        /// <returns>Empty float list.</returns>
        public static RawList<float> GetFloatList()
        {
            return SubPoolFloatList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<float> list)
        {
            list.Clear();
            SubPoolFloatList.GiveBack(list);
        }

        /// <summary>
        /// Retrieves a Vector3 list from the resource pool.
        /// </summary>
        /// <returns>Empty Vector3 list.</returns>
        public static RawList<Vector3> GetVectorList()
        {
            return SubPoolVectorList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="list">List to return.</param>
        public static void GiveBack(RawList<Vector3> list)
        {
            list.Clear();
            SubPoolVectorList.GiveBack(list);
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
        public static RawList<BEPUphysics.CollisionTests.Manifolds.TriangleMeshConvexContactManifold.TriangleIndices> GetTriangleIndicesList()
        {
            return SubPoolTriangleIndicesList.Take();
        }

        /// <summary>
        /// Returns a resource to the pool.
        /// </summary>
        /// <param name="triangleIndices">TriangleIndices list to return.</param>
        public static void GiveBack(RawList<BEPUphysics.CollisionTests.Manifolds.TriangleMeshConvexContactManifold.TriangleIndices> triangleIndices)
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