using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Entities;
using BEPUutilities.DataStructures;

namespace BEPUphysics.UpdateableSystems.ForceFields
{
    /// <summary>
    /// Defines the area in which a force field works using an entity's shape.
    /// </summary>
    public class VolumeForceFieldShape : ForceFieldShape
    {
        private readonly RawList<Entity> affectedEntities = new RawList<Entity>();

        /// <summary>
        /// Constructs a new force field shape using a detector volume.
        /// </summary>
        /// <param name="volume">Volume to use.</param>
        public VolumeForceFieldShape(DetectorVolume volume)
        {
            Volume = volume;
        }

        /// <summary>
        /// Gets or sets the volume used by the shape.
        /// </summary>
        public DetectorVolume Volume { get; set; }

        /// <summary>
        /// Determines the possibly involved entities.
        /// </summary>
        /// <returns>Possibly involved entities.</returns>
        public override IList<Entity> GetPossiblyAffectedEntities()
        {
            affectedEntities.Clear();
            foreach (var entity in Volume.pairs.Keys)
            {
                affectedEntities.Add(entity);
            }
            return affectedEntities;
        }

        /// <summary>
        /// Determines if the entity is affected by the force field.
        /// </summary>
        /// <param name="testEntity">Entity to test.</param>
        /// <returns>Whether the entity is affected.</returns>
        public override bool IsEntityAffected(Entity testEntity)
        {
            return Volume.pairs[testEntity].Touching;
        }
    }
}