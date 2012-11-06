using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.EntityStateManagement;
using Microsoft.Xna.Framework;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.MathExtensions;

namespace BEPUphysics.Entities.Prefabs
{
    /// <summary>
    /// Triangle-shaped object that can collide and move.  After making an entity, add it to a Space so that the engine can manage it.
    /// </summary>
    public class Triangle : Entity<ConvexCollidable<TriangleShape>>
    {

        ///<summary>
        /// Gets or sets the first vertex of the triangle in local space.
        ///</summary>
        public Vector3 LocalVertexA
        {
            get
            {
                return CollisionInformation.Shape.VertexA;
            }
            set
            {
                CollisionInformation.Shape.VertexA = value;
            }
        }
        ///<summary>
        /// Gets or sets the second vertex of the triangle in local space.
        ///</summary>
        public Vector3 LocalVertexB
        {
            get
            {
                return CollisionInformation.Shape.VertexB;
            }
            set
            {
                CollisionInformation.Shape.VertexB = value;
            }
        }
        ///<summary>
        /// Gets or sets the third vertex of the triangle in local space.
        ///</summary>
        public Vector3 LocalVertexC
        {
            get
            {
                return CollisionInformation.Shape.VertexC;
            }
            set
            {
                CollisionInformation.Shape.VertexC = value;
            }
        }


        ///<summary>
        /// Gets or sets the first vertex of the triangle in world space.
        ///</summary>
        public Vector3 VertexA
        {
            get
            {
                return Matrix3X3.Transform(CollisionInformation.Shape.VertexA, orientationMatrix) + position;
            }
            set
            {
                CollisionInformation.Shape.VertexA = Matrix3X3.TransformTranspose(value - position, orientationMatrix);
            }
        }
        ///<summary>
        /// Gets or sets the second vertex of the triangle in world space.
        ///</summary>
        public Vector3 VertexB
        {
            get
            {
                return Matrix3X3.Transform(CollisionInformation.Shape.VertexB, orientationMatrix) + position;
            }
            set
            {
                CollisionInformation.Shape.VertexB = Matrix3X3.TransformTranspose(value - position, orientationMatrix);
            }
        }
        ///<summary>
        /// Gets or sets the third vertex of the triangle in world space.
        ///</summary>
        public Vector3 VertexC
        {
            get
            {
                return Matrix3X3.Transform(CollisionInformation.Shape.VertexB, orientationMatrix) + position;
            }
            set
            {
                CollisionInformation.Shape.VertexC = Matrix3X3.TransformTranspose(value - position, orientationMatrix);
            }
        }

        ///<summary>
        /// Gets or sets the sidedness of the triangle.
        ///</summary>
        public TriangleSidedness Sidedness
        {
            get { return CollisionInformation.Shape.Sidedness; }
            set
            {
                CollisionInformation.Shape.Sidedness = value;
            }
        }



        /// <summary>
        /// Constructs a dynamic triangle.
        /// </summary>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        /// <param name="mass">Mass of the object.</param>
        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, float mass)
        {
            Vector3 center;
            var shape = new TriangleShape(v1, v2, v3, out center);
            Initialize(new ConvexCollidable<TriangleShape>(shape), mass);
            Position = center;
        }

        /// <summary>
        /// Constructs a nondynamic triangle.
        /// </summary>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 center;
            var shape = new TriangleShape(v1, v2, v3, out center);
            Initialize(new ConvexCollidable<TriangleShape>(shape));
            Position = center;
        }

        /// <summary>
        /// Constructs a dynamic triangle.
        /// </summary>
        /// <param name="pos">Position where the triangle is initialy centered.</param>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        /// <param name="mass">Mass of the object.</param>
        public Triangle(Vector3 pos, Vector3 v1, Vector3 v2, Vector3 v3, float mass)
            : this(v1, v2, v3, mass)
        {
            Position = pos;
        }

        /// <summary>
        /// Constructs a nondynamic triangle.
        /// </summary>
        /// <param name="pos">Position where the triangle is initially centered.</param>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        public Triangle(Vector3 pos, Vector3 v1, Vector3 v2, Vector3 v3)
            : this(v1, v2, v3)
        {
            Position = pos;
        }

        /// <summary>
        /// Constructs a dynamic triangle.
        /// </summary>
        /// <param name="motionState">Motion state specifying the entity's initial state.</param>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        /// <param name="mass">Mass of the object.</param>
        public Triangle(MotionState motionState, Vector3 v1, Vector3 v2, Vector3 v3, float mass)
            : this(v1, v2, v3, mass)
        {
            MotionState = motionState;
        }

        /// <summary>
        /// Constructs a nondynamic triangle.
        /// </summary>
        /// <param name="motionState">Motion state specifying the entity's initial state.</param>
        /// <param name="v1">Position of the first vertex.</param>
        /// <param name="v2">Position of the second vertex.</param>
        /// <param name="v3">Position of the third vertex.</param>
        public Triangle(MotionState motionState, Vector3 v1, Vector3 v2, Vector3 v3)
            : this(v1, v2, v3)
        {
            MotionState = motionState;
        }




    }
}