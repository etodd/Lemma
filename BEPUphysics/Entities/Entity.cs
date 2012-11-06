using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.Entities
{
    ///<summary>
    /// Superclass of all entities which have a defined collidable type.
    /// After construction, the collidable on this sort of Entity cannot be changed.
    /// It can be constructed directly, or one of its prefab children (Box, Sphere, etc.) can be used.
    ///</summary>
    /// <remarks>If the collidable needs to be changed after construction, consider using the MorphableEntity.</remarks>
    ///<typeparam name="T">Type of EntityCollidable to use for the entity.</typeparam>
    public class Entity<T> : Entity where T : EntityCollidable
    {
        ///<summary>
        /// Gets the collidable used by the entity.
        ///</summary>
        public new T CollisionInformation
        {
            get { return (T)collisionInformation; }
        }

        protected internal Entity()
        {

        }

        ///<summary>
        /// Constructs a kinematic Entity.
        ///</summary>
        ///<param name="collisionInformation">Collidable for the entity.</param>
        public Entity(T collisionInformation)
        {
            Initialize(collisionInformation);
        }

        ///<summary>
        /// Constructs a kinematic Entity.
        ///</summary>
        ///<param name="collisionInformation">Collidable for the entity.</param>
        public Entity(T collisionInformation, bool computeVolume)
        {
            Initialize(collisionInformation);
        }


        ///<summary>
        /// Constructs a dynamic Entity.
        ///</summary>
        ///<param name="collisionInformation">Collidable for the entity.</param>
        /// <param name="mass">Mass of the entity.</param>
        public Entity(T collisionInformation, float mass)
        {
            Initialize(collisionInformation, mass);
        }
        ///<summary>
        /// Constructs a dynamic Entity.
        ///</summary>
        ///<param name="collisionInformation">Collidable for the entity.</param>
        /// <param name="mass">Mass of the entity.</param>
        /// <param name="inertiaTensor">Inertia of the entity.</param>
        public Entity(T collisionInformation, float mass, Matrix3X3 inertiaTensor)
        {
            Initialize(collisionInformation, mass, inertiaTensor);
        }

        ///<summary>
        /// Constructs a dynamic Entity.
        ///</summary>
        ///<param name="collisionInformation">Collidable for the entity.</param>
        /// <param name="mass">Mass of the entity.</param>
        /// <param name="inertiaTensor">Inertia of the entity.</param>
        /// <param name="volume">Volume of the entity.</param>
        public Entity(T collisionInformation, float mass, Matrix3X3 inertiaTensor, float volume)
        {
            Initialize(collisionInformation, mass, inertiaTensor, volume);
        }




    }
}
