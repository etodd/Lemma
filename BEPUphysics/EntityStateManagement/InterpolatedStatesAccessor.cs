using Microsoft.Xna.Framework;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.EntityStateManagement
{
    ///<summary>
    /// Accesses an entity's interpolated states.
    /// Interpolated states are blended states between the previous and current entity states based
    /// on the time remainder from interal timestepping.
    ///</summary>
    public class InterpolatedStatesAccessor
    {
        internal EntityBufferedStates bufferedStates;
        ///<summary>
        /// Constructs a new accessor.
        ///</summary>
        ///<param name="bufferedStates">Owning entry.</param>
        public InterpolatedStatesAccessor(EntityBufferedStates bufferedStates)
        {
            this.bufferedStates = bufferedStates;
        }

        bool IsBufferAccessible()
        {
            return bufferedStates.BufferedStatesManager != null && bufferedStates.BufferedStatesManager.Enabled && bufferedStates.BufferedStatesManager.InterpolatedStates.Enabled;
        }



        ///<summary>
        /// Gets the interpolated position of the entity.
        ///</summary>
        public Vector3 Position
        {
            get
            {
                if (IsBufferAccessible())
                    return bufferedStates.BufferedStatesManager.InterpolatedStates.GetState(bufferedStates.motionStateIndex).Position;
                return bufferedStates.Entity.Position;
            }
        }

        ///<summary>
        /// Gets the interpolated orientation of the entity.
        ///</summary>
        public Quaternion Orientation
        {
            get
            {
                if (IsBufferAccessible())
                    return bufferedStates.BufferedStatesManager.InterpolatedStates.GetState(bufferedStates.motionStateIndex).Orientation;
                return bufferedStates.Entity.Orientation;
            }
        }

        ///<summary>
        /// Gets the interpolated orientation matrix of the entity.
        ///</summary>
        public Matrix3X3 OrientationMatrix
        {
            get
            {
                Matrix3X3 toReturn;
                if (IsBufferAccessible())
                {
                    Quaternion o = bufferedStates.BufferedStatesManager.InterpolatedStates.GetState(bufferedStates.motionStateIndex).Orientation;
                    Matrix3X3.CreateFromQuaternion(ref o, out toReturn);
                }
                else
                    Matrix3X3.CreateFromQuaternion(ref bufferedStates.Entity.orientation, out toReturn);
                return toReturn;
            }
        }

        ///<summary>
        /// Gets the interpolated world transform of the entity.
        ///</summary>
        public Matrix WorldTransform
        {
            get
            {
                if (IsBufferAccessible())
                    return bufferedStates.BufferedStatesManager.InterpolatedStates.GetState(bufferedStates.motionStateIndex).Matrix;
                return bufferedStates.Entity.WorldTransform;
            }
        }

        ///<summary>
        /// Gets the interpolated rigid transform of the entity.
        ///</summary>
        public RigidTransform RigidTransform
        {
            get
            {
                if (IsBufferAccessible())
                    return bufferedStates.BufferedStatesManager.InterpolatedStates.GetState(bufferedStates.motionStateIndex);
                var toReturn = new RigidTransform
                                   {
                                       Position = bufferedStates.Entity.position,
                                       Orientation = bufferedStates.Entity.orientation
                                   };
                return toReturn;
            }

        }



    }
}
