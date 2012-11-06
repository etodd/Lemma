using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// Acts as an entity's view into the buffered states system.
    /// Buffered states are updated each frame and contain the latest known states.
    /// These states can also be written to.  Writes will not be immediately visible;
    /// the next frame's write buffer flush will write the changes to the entities.
    ///</summary>
    public class BufferedStatesAccessor
    {
        internal EntityBufferedStates bufferedStates;
        ///<summary>
        /// Gets and sets the states write buffer used when buffered properties are written.
        ///</summary>
        public EntityStateWriteBuffer WriteBuffer { get; set; }
        ///<summary>
        /// Constructs a new accessor.
        ///</summary>
        ///<param name="bufferedStates">The owning states system.</param>
        public BufferedStatesAccessor(EntityBufferedStates bufferedStates)
        {
            this.bufferedStates = bufferedStates;
        }

        bool IsReadBufferAccessible()
        {
            return bufferedStates.BufferedStatesManager != null && bufferedStates.BufferedStatesManager.Enabled && bufferedStates.BufferedStatesManager.ReadBuffers.Enabled;
        }

        bool IsWriteBufferAccessible()
        {
            return WriteBuffer != null && WriteBuffer.Enabled;
        }


        ///<summary>
        /// Gets or sets the buffered position of the entity.
        ///</summary>
        public Vector3 Position
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).Position;
                return bufferedStates.Entity.Position;
            }
            set
            {
                if (IsWriteBufferAccessible())
                    WriteBuffer.EnqueuePosition(bufferedStates.Entity, ref value);
                else
                    bufferedStates.Entity.Position = value;
            }
        }

        ///<summary>
        /// Gets or sets the buffered orientation quaternion of the entity.
        ///</summary>
        public Quaternion Orientation
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).Orientation;
                return bufferedStates.Entity.Orientation;
            }
            set
            {
                if (IsWriteBufferAccessible())
                    WriteBuffer.EnqueueOrientation(bufferedStates.Entity, ref value);
                else
                    bufferedStates.Entity.Orientation = value;
            }
        }

        ///<summary>
        /// Gets or sets the buffered orientation matrix of the entity.
        ///</summary>
        public Matrix3X3 OrientationMatrix
        {
            get
            {
                Matrix3X3 toReturn;
                if (IsReadBufferAccessible())
                {
                    Quaternion o = bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).Orientation;
                    Matrix3X3.CreateFromQuaternion(ref o, out toReturn);
                }
                else
                    Matrix3X3.CreateFromQuaternion(ref bufferedStates.Entity.orientation, out toReturn);
                return toReturn;
            }
            set
            {
                if (IsWriteBufferAccessible())
                {
                    Quaternion toSet = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(Matrix3X3.ToMatrix4X4(value)));
                    WriteBuffer.EnqueueOrientation(bufferedStates.Entity, ref toSet);
                }
                else
                {
                    bufferedStates.Entity.OrientationMatrix = value;
                }
            }
        }

        ///<summary>
        /// Gets or sets the buffered linear velocity of the entity.
        ///</summary>
        public Vector3 LinearVelocity
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).LinearVelocity;
                return bufferedStates.Entity.LinearVelocity;
            }
            set
            {
                if (IsWriteBufferAccessible())
                    WriteBuffer.EnqueueLinearVelocity(bufferedStates.Entity, ref value);
                else
                    bufferedStates.Entity.LinearVelocity = value;
            }
        }


        ///<summary>
        /// Gets or sets the buffered angular velocity of the entity.
        ///</summary>
        public Vector3 AngularVelocity
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).AngularVelocity;
                return bufferedStates.Entity.AngularVelocity;
            }
            set
            {
                if (IsWriteBufferAccessible())
                    WriteBuffer.EnqueueAngularVelocity(bufferedStates.Entity, ref value);
                else
                    bufferedStates.Entity.AngularVelocity = value;
            }
        }

        ///<summary>
        /// Gets or sets the buffered world transform of the entity.
        ///</summary>
        public Matrix WorldTransform
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex).WorldTransform;
                return bufferedStates.Entity.WorldTransform;
            }
            set
            {
                if (IsWriteBufferAccessible())
                {
                    Vector3 translation = value.Translation;
                    Quaternion orientation;
                    Quaternion.CreateFromRotationMatrix(ref value, out orientation);
                    orientation.Normalize();
                    WriteBuffer.EnqueueOrientation(bufferedStates.Entity, ref orientation);
                    WriteBuffer.EnqueuePosition(bufferedStates.Entity, ref translation);
                }
                else
                {
                    bufferedStates.Entity.WorldTransform = value;
                }
            }
        }

        ///<summary>
        /// Gets or sets the buffered motion state of the entity.
        ///</summary>
        public MotionState MotionState
        {
            get
            {
                if (IsReadBufferAccessible())
                    return bufferedStates.BufferedStatesManager.ReadBuffers.GetState(bufferedStates.motionStateIndex);
                return bufferedStates.Entity.MotionState;
            }
            set
            {
                if (IsWriteBufferAccessible())
                {
                    WriteBuffer.EnqueueLinearVelocity(bufferedStates.Entity, ref value.LinearVelocity);
                    WriteBuffer.EnqueueAngularVelocity(bufferedStates.Entity, ref value.AngularVelocity);
                    WriteBuffer.EnqueueOrientation(bufferedStates.Entity, ref value.Orientation);
                    WriteBuffer.EnqueuePosition(bufferedStates.Entity, ref value.Position);
                }
                else
                {
                    bufferedStates.Entity.MotionState = value;
                }
            }
        }


    }
}
