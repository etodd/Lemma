using System;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// Superclass of all collision shapes.
    /// Collision shapes are composed entirely of local space information.
    /// Collidables provide the world space information needed to use the shapes to do collision detection.
    ///</summary>
    public abstract class CollisionShape
    {
        ///<summary>
        /// Fires when some of the local space information in the shape changes.
        ///</summary>
        public event Action<CollisionShape> ShapeChanged;

        protected virtual void OnShapeChanged()
        {
            if (ShapeChanged != null)
                ShapeChanged(this);
        }



    }
}
