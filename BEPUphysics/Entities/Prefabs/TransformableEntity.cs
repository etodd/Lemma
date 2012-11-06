using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.EntityStateManagement;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.Entities.Prefabs
{
    /// <summary>
    /// Ball-shaped object that can collide and move.  After making an entity, add it to a Space so that the engine can manage it.
    /// </summary>
    public class TransformableEntity : Entity<ConvexCollidable<TransformableShape>>
    {
        ///<summary>
        /// Gets the shape on which the transformable shape is based.
        ///</summary>
        public ConvexShape Shape
        {
            get
            {
                return CollisionInformation.Shape.Shape;
            }
            set
            {
                CollisionInformation.Shape.Shape = value;
            }
        }

        ///<summary>
        /// Gets the linear transform that the shape uses to transform its base shape.
        ///</summary>
        public Matrix3X3 Transform
        {
            get
            {
                return CollisionInformation.Shape.Transform;
            }
            set
            {
                CollisionInformation.Shape.Transform = value;
            }
        }

        private TransformableEntity(ConvexShape shape, Matrix3X3 transform)
            : base(new ConvexCollidable<TransformableShape>(new TransformableShape(shape, transform)))
        {
        }

        private TransformableEntity(ConvexShape shape, Matrix3X3 transform, float mass)
            : base(new ConvexCollidable<TransformableShape>(new TransformableShape(shape, transform)), mass)
        {
        }



        /// <summary>
        /// Constructs a dynamic transformable entity.
        /// </summary>
        /// <param name="position">Position of the entity.</param>
        /// <param name="shape">Shape to transform.</param>
        /// <param name="transform">Transform to apply to the shape.</param>
        /// <param name="mass">Mass of the object.</param>
        public TransformableEntity(Vector3 position, ConvexShape shape, Matrix3X3 transform, float mass)
            : this(shape, transform, mass)
        {
            Position = position;
        }


        /// <summary>
        /// Constructs a kinematic transformable entity.
        /// </summary>
        /// <param name="position">Position of the entity.</param>
        /// <param name="shape">Shape to transform.</param>
        /// <param name="transform">Transform to apply to the shape.</param>
        public TransformableEntity(Vector3 position, ConvexShape shape, Matrix3X3 transform)
            : this(shape, transform)
        {
            Position = position;
        }


        /// <summary>
        /// Constructs a dynamic transformable entity.
        /// </summary>
        /// <param name="motionState">Initial motion state of the entity.</param>
        /// <param name="shape">Shape to transform.</param>
        /// <param name="transform">Transform to apply to the shape.</param>
        /// <param name="mass">Mass of the object.</param>
        public TransformableEntity(MotionState motionState, ConvexShape shape, Matrix3X3 transform, float mass)
            : this(shape, transform, mass)
        {
            MotionState = motionState;
        }

        /// <summary>
        /// Constructs a kinematic transformable entity.
        /// </summary>
        /// <param name="motionState">Initial motion state of the entity.</param>
        /// <param name="shape">Shape to transform.</param>
        /// <param name="transform">Transform to apply to the shape.</param>
        public TransformableEntity(MotionState motionState, ConvexShape shape, Matrix3X3 transform)
            : this(shape, transform)
        {
            MotionState = motionState;
        }



    }
}