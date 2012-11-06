using BEPUphysics.Entities;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// Contains a single entity's buffered states.
    ///</summary>
    public class EntityBufferedStates
    {
        ///<summary>
        /// Gets the buffered states manager that owns this entry.
        ///</summary>
        public BufferedStatesManager BufferedStatesManager { get; internal set; }

        ///<summary>
        /// Gets the buffered states accessor for this entity.
        /// Contains the current snapshot of the entity's states.
        ///</summary>
        public BufferedStatesAccessor States { get; private set; }
        ///<summary>
        /// Gets the interpolated states accessor for this entity.
        /// Contains a blended snapshot between the previous and current states based on the
        /// internal timestepping remainder.
        ///</summary>
        public InterpolatedStatesAccessor InterpolatedStates { get; private set; }

        internal int motionStateIndex;
        ///<summary>
        /// Gets the motion state index of this entity.
        ///</summary>
        public int MotionStateIndex { get { return motionStateIndex; } internal set { motionStateIndex = value; } }

        ///<summary>
        /// Constructs a new buffered states entry.
        ///</summary>
        ///<param name="entity">Owning entity.</param>
        public EntityBufferedStates(Entity entity)
        {
            Entity = entity;
            States = new BufferedStatesAccessor(this);
            InterpolatedStates = new InterpolatedStatesAccessor(this);
        }

        ///<summary>
        /// Gets the entity owning this entry.
        ///</summary>
        public Entity Entity { get; private set; }
    }
}
