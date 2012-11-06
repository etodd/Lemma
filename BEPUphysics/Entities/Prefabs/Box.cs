using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.EntityStateManagement;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace BEPUphysics.Entities.Prefabs
{
    /// <summary>
    /// Box-shaped object that can collide and move.  After making an entity, add it to a Space so that the engine can manage it.
    /// </summary>
    public class Box : Entity<ConvexCollidable<BoxShape>>
    {

        private Box(float width, float height, float length)
            :base(new ConvexCollidable<BoxShape>(new BoxShape(width, height, length)))
        {
        }

        private Box(float width, float height, float length, float mass)
            :base(new ConvexCollidable<BoxShape>(new BoxShape(width, height, length)), mass)
        {
        }

        /// <summary>
        /// Constructs a physically simulated box.
        /// </summary>
        /// <param name="pos">Position of the box.</param>
        /// <param name="width">Width of the box.</param>
        /// <param name="length">Length of the box.</param>
        /// <param name="height">Height of the box.</param>
        /// <param name="mass">Mass of the object.</param>
        public Box(Vector3 pos, float width, float height, float length, float mass)
            : this(width, height, length, mass)
        {
            Position = pos;
        }

        /// <summary>
        /// Constructs a nondynamic box.
        /// </summary>
        /// <param name="pos">Position of the box.</param>
        /// <param name="width">Width of the box.</param>
        /// <param name="length">Length of the box.</param>
        /// <param name="height">Height of the box.</param>
        public Box(Vector3 pos, float width, float height, float length)
            : this(width, height, length)
        {
            Position = pos;
        }

        /// <summary>
        /// Constructs a physically simulated box.
        /// </summary>
        /// <param name="motionState">Motion state specifying the entity's initial state.</param>
        /// <param name="width">Width of the box.</param>
        /// <param name="length">Length of the box.</param>
        /// <param name="height">Height of the box.</param>
        /// <param name="mass">Mass of the object.</param>
        public Box(MotionState motionState, float width, float height, float length, float mass)
            : this(width, height, length, mass)
        {
            MotionState = motionState;
        }



        /// <summary>
        /// Constructs a nondynamic box.
        /// </summary>
        /// <param name="motionState">Motion state specifying the entity's initial state.</param>
        /// <param name="width">Width of the box.</param>
        /// <param name="length">Length of the box.</param>
        /// <param name="height">Height of the box.</param>
        public Box(MotionState motionState, float width, float height, float length)
            : this(width, height, length)
        {
            MotionState = motionState;
        }

        /// <summary>
        /// Width of the box divided by two.
        /// </summary>
        public float HalfWidth
        {
            get { return CollisionInformation.Shape.HalfWidth; }
            set { CollisionInformation.Shape.HalfWidth = value; }
        }


        /// <summary>
        /// Height of the box divided by two.
        /// </summary>
        public float HalfHeight
        {
            get { return CollisionInformation.Shape.HalfHeight; }
            set { CollisionInformation.Shape.HalfHeight = value; }
        }

        /// <summary>
        /// Length of the box divided by two.
        /// </summary>
        public float HalfLength
        {
            get { return CollisionInformation.Shape.HalfLength; }
            set { CollisionInformation.Shape.HalfLength = value; }
        }



        /// <summary>
        /// Width of the box.
        /// </summary>
        public float Width
        {
            get { return CollisionInformation.Shape.Width; }
            set { CollisionInformation.Shape.Width = value; }
        }

        /// <summary>
        /// Height of the box.
        /// </summary>
        public float Height
        {
            get { return CollisionInformation.Shape.Height; }
            set { CollisionInformation.Shape.Height = value; }
        }

        /// <summary>
        /// Length of the box.
        /// </summary>
        public float Length
        {
            get { return CollisionInformation.Shape.Length; }
            set { CollisionInformation.Shape.Length = value; }
        }





    }
}