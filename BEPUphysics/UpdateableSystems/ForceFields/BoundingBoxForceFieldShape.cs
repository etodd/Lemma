using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;

namespace BEPUphysics.UpdateableSystems.ForceFields
{
    /// <summary>
    /// Defines the area in which a force field works using an entity's shape.
    /// </summary>
    public class BoundingBoxForceFieldShape : ForceFieldShape
    {
        private readonly List<Entity> affectedEntities = new List<Entity>();
        private readonly RawList<BroadPhaseEntry> affectedEntries = new RawList<BroadPhaseEntry>();

        /// <summary>
        /// Constructs a new force field shape using a bounding box.
        /// </summary>
        /// <param name="box">Bounding box to use.</param>
        public BoundingBoxForceFieldShape(BoundingBox box)
        {
            BoundingBox = box;
        }

        /// <summary>
        /// Gets or sets the bounding box used by the shape.
        /// </summary>
        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// Determines the possibly involved entities.
        /// </summary>
        /// <returns>Possibly involved entities.</returns>
        public override IList<Entity> GetPossiblyAffectedEntities()
        {
            affectedEntities.Clear();
            ForceField.QueryAccelerator.GetEntries(BoundingBox, affectedEntries);
            for (int i = 0; i < affectedEntries.count; i++)
            {
                var EntityCollidable = affectedEntries[i] as EntityCollidable;
                if (EntityCollidable != null)
                    affectedEntities.Add(EntityCollidable.Entity);
            }
            affectedEntries.Clear();
            return affectedEntities;
        }

        /// <summary>
        /// Determines if the entity is affected by the force field.
        /// </summary>
        /// <param name="testEntity">Entity to test.</param>
        /// <returns>Whether the entity is affected.</returns>
        public override bool IsEntityAffected(Entity testEntity)
        {
            return true;
        }
    }
}