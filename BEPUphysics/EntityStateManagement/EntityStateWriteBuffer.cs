using System.Runtime.InteropServices;
using BEPUphysics.Entities;
using BEPUphysics.Threading;
using Microsoft.Xna.Framework;

namespace BEPUphysics.EntityStateManagement
{

    ///<summary>
    /// Buffer containing pending writes to entity states.
    ///</summary>
    public class EntityStateWriteBuffer : ProcessingStage
    {
        internal enum TargetField : byte
        {
            Position,
            Orientation,
            LinearVelocity,
            AngularVelocity,
        }

        //TODO: Not a particularly elegant buffering mechanism.  Make a better in-order buffering scheme.
        //There are platform requirements on layout that cause issues with the WINDOWS explicit version.
        //TODO: There's probably a better way to handle it on the XBOX/WP7 than the "give up" approach taken below.
#if WINDOWS
        [StructLayout(LayoutKind.Explicit)]
        internal struct EntityStateChange
        {
            [FieldOffset(0)]
            internal Quaternion orientationQuaternion;
            [FieldOffset(0)]
            internal Vector3 vector;
            [FieldOffset(16)]
            internal TargetField targetField;
            [FieldOffset(20)]
            internal Entity target;
        }
#else
        internal struct EntityStateChange
        {
            internal Quaternion orientationQuaternion;
            internal Vector3 vector;
            internal TargetField targetField;
            internal Entity target;
        }
#endif

        private ConcurrentDeque<EntityStateChange> stateChanges = new ConcurrentDeque<EntityStateChange>();

        ///<summary>
        /// Constructs the write buffer.
        ///</summary>
        public EntityStateWriteBuffer()
        {
            Enabled = true;
        }

        ///<summary>
        /// Enqueues a change to an entity's position.
        ///</summary>
        ///<param name="entity">Entity to target.</param>
        ///<param name="newPosition">New position of the entity.</param>
        public void EnqueuePosition(Entity entity, ref Vector3 newPosition)
        {
            stateChanges.Enqueue(new EntityStateChange { target = entity, vector = newPosition, targetField = TargetField.Position });
        }
        ///<summary>
        /// Enqueues a change to an entity's orientation.
        ///</summary>
        ///<param name="entity">Entity to target.</param>
        ///<param name="newOrientationQuaternion">New orientation of the entity.</param>
        public void EnqueueOrientation(Entity entity, ref Quaternion newOrientationQuaternion)
        {
            stateChanges.Enqueue(new EntityStateChange { target = entity, orientationQuaternion = newOrientationQuaternion, targetField = TargetField.Orientation });
        }
        ///<summary>
        /// Enqueues a change to an entity's linear velocity.
        ///</summary>
        ///<param name="entity">Entity to target.</param>
        ///<param name="newLinearVelocity">New linear velocity of the entity.</param>
        public void EnqueueLinearVelocity(Entity entity, ref Vector3 newLinearVelocity)
        {
            stateChanges.Enqueue(new EntityStateChange { target = entity, vector = newLinearVelocity, targetField = TargetField.LinearVelocity });
        }
        ///<summary>
        /// Enqueues a change to an entity's angular velocity.
        ///</summary>
        ///<param name="entity">Entity to target.</param>
        ///<param name="newAngularVelocity">New angular velocity of the entity.</param>
        public void EnqueueAngularVelocity(Entity entity, ref Vector3 newAngularVelocity)
        {
            stateChanges.Enqueue(new EntityStateChange { target = entity, vector = newAngularVelocity, targetField = TargetField.AngularVelocity });
        }


        protected override void UpdateStage()
        {
            EntityStateChange item;
            while (stateChanges.TryDequeueFirst(out item))
            {
                Entity target = item.target;
                switch (item.targetField)
                {
                    case TargetField.Position:
                        target.Position = item.vector;
                        break;
                    case TargetField.Orientation:
                        target.Orientation = item.orientationQuaternion;
                        break;
                    case TargetField.LinearVelocity:
                        target.LinearVelocity = item.vector;
                        break;
                    case TargetField.AngularVelocity:
                        target.AngularVelocity = item.vector;
                        break;
                }
            }
        }



    }
}
