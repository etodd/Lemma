using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.DataStructures;
using BEPUphysics.UpdateableSystems;

namespace BEPUphysics.NarrowPhaseSystems
{
    /// <summary>
    /// Contains the various factories that are used by default in the engine.
    /// </summary>
    public class Factories
    {
        /// <summary>
        /// Gets the factory for the box-box case.
        /// </summary>
        public NarrowPhasePairFactory<BoxPairHandler> BoxBox { get; private set; }
        /// <summary>
        /// Gets the factory for the box-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<BoxSpherePairHandler> BoxSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the sphere-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<SpherePairHandler> SphereSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the convex-convex case.  This works for any two convexes, though some other special cases (e.g. box-box) supersede it.
        /// </summary>
        public NarrowPhasePairFactory<GeneralConvexPairHandler> ConvexConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the triangle-convex case.
        /// </summary>
        public NarrowPhasePairFactory<TriangleConvexPairHandler> TriangleConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-convex case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundConvexPairHandler> CompoundConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-compound case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundPairHandler> CompoundCompound { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-static mesh case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundStaticMeshPairHandler> CompoundStaticMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-terrain case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundTerrainPairHandler> CompoundTerrain { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-instanced mesh case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundInstancedMeshPairHandler> CompoundInstancedMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the compound-mobile mesh case.
        /// </summary>
        public NarrowPhasePairFactory<CompoundMobileMeshPairHandler> CompoundMobileMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the static mesh-convex case.
        /// </summary>
        public NarrowPhasePairFactory<StaticMeshConvexPairHandler> StaticMeshConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the static mesh-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<StaticMeshSpherePairHandler> StaticMeshSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the terrain-convex case.
        /// </summary>
        public NarrowPhasePairFactory<TerrainConvexPairHandler> TerrainConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the terrain-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<TerrainSpherePairHandler> TerrainSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the instanced mesh-convex case.
        /// </summary>
        public NarrowPhasePairFactory<InstancedMeshConvexPairHandler> InstancedMeshConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the instanced mesh-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<InstancedMeshSpherePairHandler> InstancedMeshSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-convex case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshConvexPairHandler> MobileMeshConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-sphere case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshSpherePairHandler> MobileMeshSphere { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-triangle case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshTrianglePairHandler> MobileMeshTriangle { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-static mesh case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshStaticMeshPairHandler> MobileMeshStaticMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-instanced mesh case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshInstancedMeshPairHandler> MobileMeshInstancedMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-terrain case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshTerrainPairHandler> MobileMeshTerrain { get; private set; }
        /// <summary>
        /// Gets the factory for the mobile mesh-mobile mesh case.
        /// </summary>
        public NarrowPhasePairFactory<MobileMeshMobileMeshPairHandler> MobileMeshMobileMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the static group-convex case.
        /// </summary>
        public NarrowPhasePairFactory<StaticGroupConvexPairHandler> StaticGroupConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the static group-compound case.
        /// </summary>
        public NarrowPhasePairFactory<StaticGroupCompoundPairHandler> StaticGroupCompound { get; private set; }
        /// <summary>
        /// Gets the factory for the static group-mobile mesh case.
        /// </summary>
        public NarrowPhasePairFactory<StaticGroupMobileMeshPairHandler> StaticGroupMobileMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the detector volume-convex case.
        /// </summary>
        public NarrowPhasePairFactory<DetectorVolumeConvexPairHandler> DetectorVolumeConvex { get; private set; }
        /// <summary>
        /// Gets the factory for the detector volume-mobile mesh case.
        /// </summary>
        public NarrowPhasePairFactory<DetectorVolumeMobileMeshPairHandler> DetectorVolumeMobileMesh { get; private set; }
        /// <summary>
        /// Gets the factory for the detector volume-compound case.
        /// </summary>
        public NarrowPhasePairFactory<DetectorVolumeCompoundPairHandler> DetectorVolumeCompound { get; private set; }

        RawList<NarrowPhasePairFactory> factories = new RawList<NarrowPhasePairFactory>();
        /// <summary>
        /// Gets a collection of all the default factories.
        /// </summary>
        public ReadOnlyList<NarrowPhasePairFactory> All
        {
            get
            {
                return new ReadOnlyList<NarrowPhasePairFactory>(factories);
            }
        }

        /// <summary>
        /// Constructs all factories.
        /// </summary>
        public Factories()
        {
            factories.Add(BoxBox = new NarrowPhasePairFactory<BoxPairHandler>());
            factories.Add(BoxSphere = new NarrowPhasePairFactory<BoxSpherePairHandler>());
            factories.Add(SphereSphere = new NarrowPhasePairFactory<SpherePairHandler>());
            factories.Add(ConvexConvex = new NarrowPhasePairFactory<GeneralConvexPairHandler>());
            factories.Add(TriangleConvex = new NarrowPhasePairFactory<TriangleConvexPairHandler>());
            factories.Add(CompoundConvex = new NarrowPhasePairFactory<CompoundConvexPairHandler>());
            factories.Add(CompoundCompound = new NarrowPhasePairFactory<CompoundPairHandler>());
            factories.Add(CompoundStaticMesh = new NarrowPhasePairFactory<CompoundStaticMeshPairHandler>());
            factories.Add(CompoundTerrain = new NarrowPhasePairFactory<CompoundTerrainPairHandler>());
            factories.Add(CompoundInstancedMesh = new NarrowPhasePairFactory<CompoundInstancedMeshPairHandler>());
            factories.Add(CompoundMobileMesh = new NarrowPhasePairFactory<CompoundMobileMeshPairHandler>());
            factories.Add(StaticMeshConvex = new NarrowPhasePairFactory<StaticMeshConvexPairHandler>());
            factories.Add(StaticMeshSphere = new NarrowPhasePairFactory<StaticMeshSpherePairHandler>());
            factories.Add(TerrainConvex = new NarrowPhasePairFactory<TerrainConvexPairHandler>());
            factories.Add(TerrainSphere = new NarrowPhasePairFactory<TerrainSpherePairHandler>());
            factories.Add(InstancedMeshConvex = new NarrowPhasePairFactory<InstancedMeshConvexPairHandler>());
            factories.Add(InstancedMeshSphere = new NarrowPhasePairFactory<InstancedMeshSpherePairHandler>());
            factories.Add(MobileMeshConvex = new NarrowPhasePairFactory<MobileMeshConvexPairHandler>());
            factories.Add(MobileMeshSphere = new NarrowPhasePairFactory<MobileMeshSpherePairHandler>());
            factories.Add(MobileMeshTriangle = new NarrowPhasePairFactory<MobileMeshTrianglePairHandler>());
            factories.Add(MobileMeshStaticMesh = new NarrowPhasePairFactory<MobileMeshStaticMeshPairHandler>());
            factories.Add(MobileMeshInstancedMesh = new NarrowPhasePairFactory<MobileMeshInstancedMeshPairHandler>());
            factories.Add(MobileMeshTerrain = new NarrowPhasePairFactory<MobileMeshTerrainPairHandler>());
            factories.Add(MobileMeshMobileMesh = new NarrowPhasePairFactory<MobileMeshMobileMeshPairHandler>());
            factories.Add(StaticGroupConvex = new NarrowPhasePairFactory<StaticGroupConvexPairHandler>());
            factories.Add(StaticGroupCompound = new NarrowPhasePairFactory<StaticGroupCompoundPairHandler>());
            factories.Add(StaticGroupMobileMesh = new NarrowPhasePairFactory<StaticGroupMobileMeshPairHandler>());
            factories.Add(DetectorVolumeConvex = new NarrowPhasePairFactory<DetectorVolumeConvexPairHandler>());
            factories.Add(DetectorVolumeMobileMesh = new NarrowPhasePairFactory<DetectorVolumeMobileMeshPairHandler>());
            factories.Add(DetectorVolumeCompound = new NarrowPhasePairFactory<DetectorVolumeCompoundPairHandler>());
        }


    }

    ///<summary>
    /// Contains the collision managers dictionary and other helper methods for creating pairs.
    ///</summary>
    public static class NarrowPhaseHelper
    {
        /// <summary>
        /// Gets the factories used by default to construct various pair types in the narrow phase.
        /// These do not necessarily reflect the state of the narrow phase helper's CollisionManagers dictionary
        /// if changes are made to its entries.
        /// </summary>
        public static Factories Factories
        {
            get;
            private set;
        }

        static NarrowPhaseHelper()
        {
            Factories = new NarrowPhaseSystems.Factories();
            collisionManagers = new Dictionary<TypePair, NarrowPhasePairFactory>();
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(ConvexCollidable<BoxShape>)), Factories.BoxBox);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(ConvexCollidable<SphereShape>)), Factories.BoxSphere);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(ConvexCollidable<SphereShape>)), Factories.SphereSphere);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(ConvexCollidable<TriangleShape>)), Factories.TriangleConvex);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(StaticMesh)), Factories.StaticMeshSphere);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(StaticMesh)), Factories.StaticMeshConvex);
            collisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(StaticMesh)), Factories.StaticMeshConvex);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(Terrain)), Factories.TerrainSphere);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(Terrain)), Factories.TerrainConvex);
            collisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(Terrain)), Factories.TerrainConvex);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(InstancedMesh)), Factories.InstancedMeshSphere);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(InstancedMesh)), Factories.InstancedMeshConvex);
            collisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(InstancedMesh)), Factories.InstancedMeshConvex);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(CompoundCollidable)), Factories.CompoundConvex);
            collisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(CompoundCollidable)), Factories.CompoundConvex);

            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(CompoundCollidable)), Factories.CompoundCompound);
            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(StaticMesh)), Factories.CompoundStaticMesh);
            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(Terrain)), Factories.CompoundTerrain);
            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(InstancedMesh)), Factories.CompoundInstancedMesh);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshSphere);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshTriangle);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(MobileMeshCollidable)), Factories.MobileMeshConvex);

            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(MobileMeshCollidable)), Factories.CompoundMobileMesh);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(StaticMesh)), Factories.MobileMeshStaticMesh);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(InstancedMesh)), Factories.MobileMeshInstancedMesh);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(Terrain)), Factories.MobileMeshTerrain);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(MobileMeshCollidable)), Factories.MobileMeshMobileMesh);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(TriangleCollidable)), Factories.MobileMeshTriangle);

            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<BoxShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<SphereShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CapsuleShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TriangleShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<CylinderShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConeShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<TransformableShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<MinkowskiSumShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<WrappedShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(ConvexCollidable<ConvexHullShape>), typeof(StaticGroup)), Factories.StaticGroupConvex);
            collisionManagers.Add(new TypePair(typeof(TriangleCollidable), typeof(StaticGroup)), Factories.StaticGroupConvex);

            collisionManagers.Add(new TypePair(typeof(CompoundCollidable), typeof(StaticGroup)), Factories.StaticGroupCompound);
            collisionManagers.Add(new TypePair(typeof(MobileMeshCollidable), typeof(StaticGroup)), Factories.StaticGroupMobileMesh);

            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<BoxShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<SphereShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<CapsuleShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<TriangleShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<CylinderShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<ConeShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<TransformableShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<MinkowskiSumShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<WrappedShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(ConvexCollidable<ConvexHullShape>)), Factories.DetectorVolumeConvex);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(TriangleCollidable)), Factories.DetectorVolumeConvex);

            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(MobileMeshCollidable)), Factories.DetectorVolumeMobileMesh);
            collisionManagers.Add(new TypePair(typeof(DetectorVolume), typeof(CompoundCollidable)), Factories.DetectorVolumeCompound);

        }

        internal static Dictionary<TypePair, NarrowPhasePairFactory> collisionManagers;
        ///<summary>
        /// Gets or sets the dictionary that defines the factory to use for various type pairs.
        ///</summary>
        public static Dictionary<TypePair, NarrowPhasePairFactory> CollisionManagers
        {
            get
            {
                return collisionManagers;
            }
            set
            {
                collisionManagers = value;
            }
        }

        ///<summary>
        /// Gets a narrow phase pair for a given broad phase overlap.
        ///</summary>
        ///<param name="pair">Overlap to use to create the pair.</param>
        ///<returns>A INarrowPhasePair for the overlap.</returns>
        public static NarrowPhasePair GetPairHandler(ref BroadPhaseOverlap pair)
        {
            NarrowPhasePairFactory factory;
            if (collisionManagers.TryGetValue(new TypePair(pair.entryA.GetType(), pair.entryB.GetType()), out factory))
            {
                var toReturn = factory.GetNarrowPhasePair();
                toReturn.BroadPhaseOverlap = pair;
                toReturn.Factory = factory;
                return toReturn;
            }
            //Convex-convex collisions are a pretty significant chunk of all tests, so rather than defining them all, just have a fallback.
            var a = pair.entryA as ConvexCollidable;
            var b = pair.entryB as ConvexCollidable;
            if (a != null && b != null)
            {
                NarrowPhasePair toReturn = Factories.ConvexConvex.GetNarrowPhasePair();
                toReturn.BroadPhaseOverlap = pair;
                toReturn.Factory = Factories.ConvexConvex;
                return toReturn;
            }
            return null;
        }

        ///<summary>
        /// Gets a narrow phase pair for a given pair of entries.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        /// <param name="entryB">Second entry in the pair.</param>
        /// <param name="rule">Collision rule governing the pair.</param>
        ///<returns>A NarrowPhasePair for the overlap.</returns>
        public static NarrowPhasePair GetPairHandler(BroadPhaseEntry entryA, BroadPhaseEntry entryB, CollisionRule rule)
        {
            var overlap = new BroadPhaseOverlap(entryA, entryB, rule);
            return GetPairHandler(ref overlap);
        }

        ///<summary>
        /// Gets a narrow phase pair for a given pair of entries.
        ///</summary>
        ///<param name="entryA">First entry in the pair.</param>
        /// <param name="entryB">Second entry in the pair.</param>
        ///<returns>AINarrowPhasePair for the overlap.</returns>
        public static NarrowPhasePair GetPairHandler(BroadPhaseEntry entryA, BroadPhaseEntry entryB)
        {
            var overlap = new BroadPhaseOverlap(entryA, entryB);
            return GetPairHandler(ref overlap);
        }

        /// <summary>
        /// Gets a collidable pair handler for a pair of collidables.
        /// </summary>
        /// <param name="pair">Pair of collidables to use to create the pair handler.</param>
        /// <param name="rule">Collision rule governing the pair.</param>
        /// <returns>CollidablePairHandler for the pair.</returns>
        public static CollidablePairHandler GetPairHandler(ref CollidablePair pair, CollisionRule rule)
        {
            var overlap = new BroadPhaseOverlap(pair.collidableA, pair.collidableB, rule);
            return GetPairHandler(ref overlap) as CollidablePairHandler;
        }
        /// <summary>
        /// Gets a collidable pair handler for a pair of collidables.
        /// </summary>
        /// <param name="pair">Pair of collidables to use to create the pair handler.</param>
        /// <returns>CollidablePairHandler for the pair.</returns>
        public static CollidablePairHandler GetPairHandler(ref CollidablePair pair)
        {
            var overlap = new BroadPhaseOverlap(pair.collidableA, pair.collidableB);
            return GetPairHandler(ref overlap) as CollidablePairHandler;
        }

        /// <summary>
        /// Tests the pair of collidables for intersection without regard for collision rules.
        /// </summary>
        /// <param name="pair">Pair to test.</param>
        /// <returns>Whether or not the pair is intersecting.</returns>
        public static bool Intersecting(ref CollidablePair pair)
        {
            var pairHandler = GetPairHandler(ref pair);
            pairHandler.SuppressEvents = true;
            pairHandler.UpdateCollision(0);
            //Technically, contacts with negative depth do not count.
            //The current implementation of collision detection does not generate
            //negative depths on the first execution of UpdateCollision, though,
            //so we don't need to worry about that- yet.
            bool toReturn = pairHandler.ContactCount > 0;
            pairHandler.SuppressEvents = false;
            pairHandler.CleanUp();
            pairHandler.Factory.GiveBack(pairHandler);
            return toReturn;
        }
    }
}
